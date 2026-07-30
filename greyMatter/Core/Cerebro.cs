using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GreyMatter.Core;
using GreyMatter.Storage;

namespace GreyMatter.Core
{
    /// <summary>
    /// Cerebro: Main orchestrator for the SBIJ system
    /// Manages neuron clusters, learning, and dynamic scaling with hierarchical learning support
    /// </summary>
    public class Cerebro : IBrainInterface
    {
        private readonly EnhancedBrainStorage _storage; // Use only enhanced storage
        private readonly LRUCache<Guid, NeuronCluster> _loadedClusters = new(maxSize: 800); // Phase 4: LRU eviction
        private readonly Dictionary<Guid, DateTime> _clusterAccessTimes = new(); // Track last access for eviction
        private CancellationTokenSource? _evictionCancellation;
        private Task? _evictionTask;
        private readonly Dictionary<Guid, Synapse> _synapses = new();
        private readonly FeatureMapper _featureMapper = new();
        private readonly Random _random = new();
        private readonly ConceptDependencyGraph _dependencyGraph = new();
        // private ContinuousProcessor? _continuousProcessor; // Temporarily disabled - ContinuousProcessor excluded from build
        
        // ADPC-Net: Pattern-based learning components (Phase 1)
        private readonly FeatureEncoder _featureEncoder;
        private readonly LSHPartitioner _lshPartitioner; // Legacy - replaced by VQ-VAE in Phase 5
        private ActivationStats _activationStats; // Not readonly - can be reloaded from storage
        private readonly Dictionary<string, List<Guid>> _regionToClusterMapping = new(); // region_id → cluster IDs
        
        // ADPC-Net Phase 2: Hypernetwork for dynamic neuron generation
        private readonly NeuronHypernetwork _neuronHypernetwork;
        
        // ADPC-Net Phase 3: Sparse synaptic graph for Hebbian learning
        private readonly SparseSynapticGraph _synapticGraph;
        
        // ADPC-Net Phase 4 & 5: Learned vector quantization
        private VectorQuantizer _vectorQuantizer;  // Not readonly - can be reloaded from storage
        private bool _useVQVAE = true;  // Toggle between LSH (legacy) and VQ-VAE (new)
        
        // Brain configuration
        public int MaxLoadedClusters { get; set; } = 10;
        public int MaxNeuronsPerCluster { get; set; } = 100;
        public double ClusterCreationThreshold { get; set; } = 0.3;
        public TimeSpan ClusterUnloadTime { get; set; } = TimeSpan.FromMinutes(30);
        
        // Learning parameters
        public double GlobalLearningRate { get; set; } = 0.01;
        public double ConceptSimilarityThreshold { get; set; } = 0.7;
        
        // Statistics
        public int TotalClustersCreated { get; private set; } = 0;
        public int TotalNeuronsCreated { get; private set; } = 0;
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        private CerebroConfiguration? _configForLogging; // to access verbosity during save

        // Reporting config and state
        private int _reportingInterval = 1000; // items per report block
        private double _reportingSampleRate = 0.005; // 0.5% detailed logs by default
        private readonly Random _reportRand = new Random();
        private long _learnEvents = 0;
        private int _blockConcepts = 0;
        private int _blockNeurons = 0;
        private readonly HashSet<Guid> _blockClusters = new();
        private readonly Stopwatch _learnSw = Stopwatch.StartNew();

        // Instrumentation aggregates (reset each reporting block)
        private long _instrCount = 0;
        private double _tFindMsSum = 0, _tLookupMsSum = 0, _tCapacityMsSum = 0, _tTrainMsSum = 0, _tSynMsSum = 0, _tTotalMsSum = 0;
        private long _neuronsAddedSum = 0, _neuronsUsedSum = 0;

        // Phase 6A: Sparse activation tracking (biological alignment metrics)
        private long _queryCount = 0;
        private long _totalActivatedNeurons = 0;
        private long _totalLoadedNeurons = 0;
        private readonly HashSet<Guid> _accessedClusters = new(); // Track working set

        // Save lock to prevent concurrent save operations
        private readonly SemaphoreSlim _saveLock = new SemaphoreSlim(1, 1);

        private bool ShouldSampleLog() => (_configForLogging?.Verbosity ?? 0) >= 2 && _reportRand.NextDouble() <= _reportingSampleRate;
        private void ReportSampler(string concept, int neuronsUsed, Guid clusterId)
        {
            if (ShouldSampleLog())
            {
                Console.WriteLine($"🎓 Learning concept: {concept}");
                Console.WriteLine($" Learned '{concept}' using {neuronsUsed} neurons in cluster {clusterId:N}");
            }
        }
        private void AccumulateReportBlock(string concept, int neuronsUsed, Guid clusterId)
        {
            _learnEvents++;
            _blockConcepts++;
            _blockNeurons += neuronsUsed;
            _blockClusters.Add(clusterId);
            if (_learnEvents % Math.Max(1, _reportingInterval) == 0)
            {
                var elapsed = _learnSw.Elapsed;
                var cps = _learnEvents > 0 ? _learnEvents / Math.Max(0.001, elapsed.TotalSeconds) : 0.0;
                Console.WriteLine($"📊 Block: {_blockConcepts} concepts, {_blockNeurons} neurons, {_blockClusters.Count} clusters | total concepts: {_learnEvents} | elapsed {FormatTimeSpan(elapsed)} | rate {cps:F1} cps");
                _blockConcepts = 0;
                _blockNeurons = 0;
                _blockClusters.Clear();
            }
        }

        // Adaptive concept capacity (Option B): load/save per-concept target counts; compute initial target from emergent model; apply slow EMA updates with hysteresis; use target for neuron growth to stabilize membership.
        private Dictionary<string, int> _conceptCapacities = new(StringComparer.OrdinalIgnoreCase);
        private const int MinConceptNeurons = 50;
        private const int MaxConceptNeurons = 600; // lowered from 5000 to rein in oversizing until staged growth lands
        private const double CapacityEmaAlpha = 0.05; // slow adjustment
        private const double CapacityHysteresis = 0.15; // 15% band before changes

        // Concept→cluster cache to bypass repeated similarity lookups
        private readonly Dictionary<string, Guid> _conceptClusterCache = new(StringComparer.OrdinalIgnoreCase);

        // Growth controls (tunable)
        private int MaxAddPerConceptPerRun = 64;        // cap growth per concept per run

        // Stable hash for deterministic seeding across runs
        private static int StableHash(string s)
        {
            unchecked
            {
                uint hash = 2166136261;
                for (int i = 0; i < s.Length; i++)
                {
                    hash ^= s[i];
                    hash *= 16777619;
                }
                return (int)(hash & 0x7FFFFFFF);
            }
        }

        private int CalculateRequiredNeuronsDeterministic(string concept, Dictionary<string, double> features)
        {
            // ADPC-Net Phase 2: Use hypernetwork for dynamic neuron generation
            
            // Encode concept to get feature vector
            var featureVector = _featureEncoder.Encode(concept);
            
            // Get region ID for novelty calculation (Phase 5: VQ-VAE or legacy LSH)
            var regionId = GetRegionId(featureVector);
            
            // Calculate novelty (0.0 = repeated, 1.0 = first time)
            var novelty = _activationStats.CalculateNovelty(regionId, featureVector);
            
            // Get activation frequency for this region
            var frequency = _activationStats.GetRegionFrequency(regionId);
            
            // Calculate pattern complexity from feature vector
            var complexity = _neuronHypernetwork.CalculateComplexity(featureVector);
            
            // Use hypernetwork formula to determine neuron count
            var neuronCount = _neuronHypernetwork.CalculateNeuronCount(novelty, frequency, complexity);
            
            if ((_configForLogging?.Verbosity ?? 0) > 1)
            {
                Console.WriteLine($"   🧬 Hypernetwork: '{concept}' → novelty={novelty:F3}, freq={frequency:F3}, complexity={complexity:F3} → {neuronCount} neurons");
            }
            
            return neuronCount;
        }

        private int GetTargetNeuronsForConcept(string concept, Dictionary<string, double> features)
        {
            if (_conceptCapacities.TryGetValue(concept, out var target))
            {
                return Math.Clamp(target, MinConceptNeurons, MaxConceptNeurons);
            }

            // Initialize with deterministic base so it’s stable across runs
            var baseTarget = CalculateRequiredNeuronsDeterministic(concept, features);
            baseTarget = Math.Clamp(baseTarget, MinConceptNeurons, MaxConceptNeurons);
            _conceptCapacities[concept] = baseTarget;
            return baseTarget;
        }

        private void AdjustConceptCapacity(string concept, int observedNeurons, double demandSignal)
        {
            // Use current target as anchor; adjust only if observed deviates beyond hysteresis band
            var current = _conceptCapacities.GetValueOrDefault(concept, Math.Clamp(observedNeurons, MinConceptNeurons, MaxConceptNeurons));
            current = Math.Clamp(current, MinConceptNeurons, MaxConceptNeurons);

            // Deviation relative to target
            var ratio = (double)Math.Max(1, observedNeurons) / Math.Max(1, current);
            var lowerRatio = 1.0 - CapacityHysteresis;
            var upperRatio = 1.0 + CapacityHysteresis;

            if (ratio < lowerRatio || ratio > upperRatio)
            {
                // Nudge toward observed when outside the band
                var desired = Math.Clamp(observedNeurons, MinConceptNeurons, MaxConceptNeurons);
                var updated = (int)Math.Round(current * (1 - CapacityEmaAlpha) + desired * CapacityEmaAlpha);
                _conceptCapacities[concept] = Math.Clamp(updated, MinConceptNeurons, MaxConceptNeurons);
            }
            // Else: keep capacity unchanged to avoid churn when on target
        }
        
        // ─── P1 instrumentation (REFOCUS.md): Hebbian activation histogram ───
        // Accumulated across calls; read/reset via GetHebbianActivationSummary().
        private long _hebbCalls;
        private long _hebbNeuronsSeen;
        private long _hebbNeuronsPassed;
        private long _hebbSkippedFewNeurons;
        private long _hebbSkippedNonePassed;
        private long _hebbSynapsesCreated;
        private double _hebbDeltaMin = double.PositiveInfinity;
        private double _hebbDeltaMax = double.NegativeInfinity;
        private double _hebbDeltaSum;

        /// <summary>
        /// P1 instrumentation: summary of Hebbian gate behavior since last reset.
        /// delta = CurrentPotential - RestingPotential (what the 0.1 gate tests).
        /// </summary>
        public string GetHebbianActivationSummary(bool reset = true)
        {
            string summary;
            if (_hebbCalls == 0)
            {
                summary = "   📊 Hebbian histogram: no calls recorded";
            }
            else
            {
                var deltaPart = _hebbNeuronsSeen > 0
                    ? $"delta[min={_hebbDeltaMin:F2} avg={_hebbDeltaSum / _hebbNeuronsSeen:F2} max={_hebbDeltaMax:F2}] "
                    : "delta[n/a] ";
                var passedPct = _hebbNeuronsSeen > 0 ? 100.0 * _hebbNeuronsPassed / _hebbNeuronsSeen : 0.0;
                summary = $"   📊 Hebbian histogram: calls={_hebbCalls:N0} neurons={_hebbNeuronsSeen:N0} " +
                          $"passed={_hebbNeuronsPassed:N0} ({passedPct:F1}%) " + deltaPart +
                          $"skipped[few={_hebbSkippedFewNeurons:N0} none_passed={_hebbSkippedNonePassed:N0}] " +
                          $"synapses_created={_hebbSynapsesCreated:N0}";
            }
            if (reset)
            {
                _hebbCalls = _hebbNeuronsSeen = _hebbNeuronsPassed = 0;
                _hebbSkippedFewNeurons = _hebbSkippedNonePassed = _hebbSynapsesCreated = 0;
                _hebbDeltaMin = double.PositiveInfinity;
                _hebbDeltaMax = double.NegativeInfinity;
                _hebbDeltaSum = 0;
            }
            return summary;
        }

        /// <summary>
        /// Synapse count in the Hebbian sparse graph (the real learned connectivity).
        /// Note: BrainStats.TotalSynapses reports the legacy _synapses list, not this.
        /// </summary>
        public int GetSynapticGraphSynapseCount() => _synapticGraph.GetSynapseCount();

        /// <summary>
        /// Continuous forgetting (called from the maintenance loop, not checkpoints).
        /// At 0.97/pass with ~2-min passes: newborn synapses (~0.106) die within
        /// ~3 un-reinforced passes (≈6 min); established pathways (≈1.0) halve in
        /// ~45 min without any reinforcement.
        /// </summary>
        public (int before, int after, long blockedByBudget) DecayAndPruneSynapses(float decayFactor = 0.97f)
        {
            var before = _synapticGraph.GetSynapseCount();
            _synapticGraph.ApplyDecay(decayFactor);
            return (before, _synapticGraph.GetSynapseCount(), _synapticGraph.CreationsBlockedByBudget);
        }

        // ─── P1.6 instrumentation: allocation / assembly-reuse counters ───
        private long _allocEvents, _allocReuseHits, _allocAssemblyPrefHits, _allocGrowEvents, _allocNeuronsGrown;

        // Inline decay cadence (training-path, single-threaded with graph writes)
        private const int DecayEveryNLearnEvents = 5000;
        private int _learnEventsSinceDecay;

        // VQ codebook warmup: adapt to the data, then freeze so pattern → code
        // (and therefore concept → cluster) assignment stops drifting. See
        // VectorQuantizer.IsLearning for why drift is harmful in two places.
        private const long VqWarmupUpdates = 20000;

        // Staged growth: recruit an assembly incrementally instead of allocating
        // the full target on first sight. Most words in a corpus are seen once
        // (Zipf), so full-target-on-first-sight is where the neurons went:
        // 39% of learn events grew ~69 neurons each.
        private const int FirstAllocationNeurons = 16;

        /// <summary>
        /// P1.6 instrumentation: where do neurons come from? reuse% should rise
        /// toward ~100 as vocabulary saturates; grew_events/avg_grow expose
        /// capacity-target ratcheting (growth on concepts that were found).
        /// </summary>
        public string GetAllocationSummary(bool reset = true)
        {
            var ev = Math.Max(1, _allocEvents);
            var summary = _allocEvents == 0
                ? "   📊 Allocation: no events recorded"
                : $"   📊 Allocation: events={_allocEvents:N0} reuse={100.0 * _allocReuseHits / ev:F1}% " +
                  $"(assembly_pref={_allocAssemblyPrefHits:N0}) grew_events={_allocGrowEvents:N0} " +
                  $"({100.0 * _allocGrowEvents / ev:F1}%) avg_grow={(_allocGrowEvents > 0 ? (double)_allocNeuronsGrown / _allocGrowEvents : 0):F1}";
            if (reset)
            {
                _allocEvents = _allocReuseHits = _allocAssemblyPrefHits = 0;
                _allocGrowEvents = _allocNeuronsGrown = 0;
            }
            return summary;
        }

        /// <summary>
        /// ADPC-Net Phase 3: Record Hebbian co-activation between neurons
        /// Neurons that fire together, wire together
        /// </summary>
        private void RecordHebbianCoactivation(List<(HybridNeuron neuron, double match)> contest)
        {
            var synapseCountBefore = _synapticGraph.GetSynapseCount();

            _hebbCalls++;
            foreach (var (_, match) in contest)
            {
                _hebbNeuronsSeen++;
                _hebbDeltaSum += match;
                if (match < _hebbDeltaMin) _hebbDeltaMin = match;
                if (match > _hebbDeltaMax) _hebbDeltaMax = match;
            }

            if (contest.Count < 2)
            {
                _hebbSkippedFewNeurons++;
                DebugLog.Debug($"   🧬 Hebbian: <2 neurons ({contest.Count}), skipping");
                return; // Need at least 2 neurons for connections
            }

            // P2.1: co-activation now uses MatchQuality — the same [0,1] measure
            // used for training and recall — instead of reading CurrentPotential,
            // which ProcessInputs used to set as a side effect of the old training
            // path. Removing that call left every neuron at resting potential
            // (max=-70.000, avg=-70.000, above_threshold=0), so no synapse was ever
            // created and the graph saved empty.
            var activations = contest
                .Where(p => p.match > HebbianCoactivationThreshold)
                .Select(p => (p.neuron.Id, activation: (float)p.match))
                .ToList();
            _hebbNeuronsPassed += activations.Count;
            
            // Per-call detail is DEBUG only. These lines used to fire on every call
            // whenever the graph was empty (isFirstCall = count < 100), which during
            // the zero-synapse regression flooded the console hard enough to
            // overflow the buffer and hide everything else. The 10s histogram is the
            // level-0 signal; this is for when you already know what you're hunting.
            DebugLog.Debug($"   🧬 Hebbian: {contest.Count} neurons, " +
                           $"match[max={(contest.Count > 0 ? contest.Max(p => p.match) : 0):F3} " +
                           $"avg={(contest.Count > 0 ? contest.Average(p => p.match) : 0):F3}] " +
                           $"above_threshold={activations.Count}");

            if (activations.Count < 2)
            {
                _hebbSkippedNonePassed++;
                DebugLog.Debug($"   🧬 Hebbian: <2 above threshold ({activations.Count}), skipping synapse creation");
                return;
            }

            // Record co-activation pattern in sparse graph
            _synapticGraph.RecordCoactivationPattern(activations);

            var synapseCountAfter = _synapticGraph.GetSynapseCount();
            _hebbSynapsesCreated += Math.Max(0, synapseCountAfter - synapseCountBefore);
            DebugLog.Debug($"   🧬 Hebbian: Recorded {activations.Count} co-active neurons, " +
                           $"total synapses: {synapseCountBefore:N0} → {synapseCountAfter:N0} (+{synapseCountAfter - synapseCountBefore})");
        }

        public Cerebro(string storagePath)
        {
            _storage = new EnhancedBrainStorage(storagePath);
            // _continuousProcessor = new ContinuousProcessor(this); // Temporarily disabled - ContinuousProcessor excluded from build
            
            // Initialize ADPC-Net components (Phase 1)
            _featureEncoder = new FeatureEncoder(dimensions: 128);
            _lshPartitioner = new LSHPartitioner(dimensions: 128, numBands: 16, rowsPerBand: 4);
            _activationStats = new ActivationStats();
            
            // Initialize ADPC-Net Phase 2: Hypernetwork
            _neuronHypernetwork = new NeuronHypernetwork(
                alphaFrequency: 20.0,   // Log-scaled frequency component
                betaNovelty: 100.0,     // Linear novelty boost
                gammaComplexity: 50.0,  // Pattern complexity factor
                minNeurons: 5,          // Minimum cluster size
                maxNeurons: 500,        // Maximum cluster size
                seed: 42                // Deterministic seed
            );
            
            // Initialize ADPC-Net Phase 3: Sparse synaptic graph
            _synapticGraph = new SparseSynapticGraph(
                learningRate: 0.01f,     // Hebbian learning rate
                minWeight: 0.0f,         // Minimum synapse weight
                maxWeight: 1.0f,         // Maximum synapse weight
                pruneThreshold: 0.1f     // Prune synapses below this weight
            );
            
            // Initialize ADPC-Net Phase 4: VQ-VAE codebook (replaces LSH)
            _vectorQuantizer = new VectorQuantizer(
                codebookSize: 512,       // 512 learned codes
                embeddingDim: 128,       // Match feature encoder
                commitment: 0.25f,       // Commitment loss coefficient
                emaDecay: 0.99f          // Codebook EMA decay
            );
            
            // Phase 6B: Attach procedural regeneration components to storage layer
            _storage.AttachProceduralComponents(_vectorQuantizer, _featureEncoder);
            
            if ((_configForLogging?.Verbosity ?? 0) > 0)
            {
                Console.WriteLine("🧬 ADPC-Net initialized:");
                Console.WriteLine($"   Feature encoder: 128-dim vectors");
                Console.WriteLine($"   {_lshPartitioner.GetStats()}");
                Console.WriteLine($"   VQ-VAE: 512-code learned codebook");
                Console.WriteLine($"   Hypernetwork: 5-500 dynamic neurons/cluster");
                Console.WriteLine($"   Synaptic graph: Sparse Hebbian connections");
            }
        }

        /// <summary>
        /// Get region ID for a feature vector using VQ-VAE (Phase 5) or LSH (legacy)
        /// </summary>
        private string GetRegionId(double[] featureVector)
        {
            if (_useVQVAE)
            {
                // Phase 5: Use learned VQ-VAE codebook with EMA updates
                var floatVector = featureVector.Select(x => (float)x).ToArray();
                var (code, distance) = _vectorQuantizer.QuantizeAndUpdate(floatVector);
                return $"vq_{code}";
            }
            else
            {
                // Phase 1: Legacy LSH partitioning
                return _lshPartitioner.GetRegionId(featureVector);
            }
        }

        /// <summary>
        /// Get nearby region IDs for k-nearest codes (VQ-VAE) or LSH neighbors
        /// </summary>
        private List<string> GetNearbyRegions(double[] featureVector, int neighbors = 5)
        {
            if (_useVQVAE)
            {
                // Phase 5: Get k-nearest codes from VQ-VAE codebook
                var floatVector = featureVector.Select(x => (float)x).ToArray();
                var nearestCodes = _vectorQuantizer.GetNearestCodes(floatVector, neighbors);
                return nearestCodes.Select(code => $"vq_{code}").ToList();
            }
            else
            {
                // Phase 1: Legacy LSH nearby regions
                return _lshPartitioner.GetNearbyRegions(featureVector, neighbors);
            }
        }
        /// <summary>
        /// Get the storage path for this brain instance
        /// </summary>
        public string GetStoragePath()
        {
            return _storage.GetBasePath();
        }

        /// <summary>
        /// Initialize the brain - load existing clusters and synapses
        /// </summary>
        public async Task InitializeAsync()
        {
            Console.WriteLine("Initializing Cerebro...");
            
            // Load feature mappings first
            var featureMappings = await _storage.LoadFeatureMappingsAsync();
            _featureMapper.RestoreFromSnapshot(featureMappings);
            Console.WriteLine($"Loaded {featureMappings.FeatureMappings.Count} feature mappings");
            
            // Load synapses (Phase 3b TODO: implement lazy partition loading)
            var synapseSnapshots = await _storage.LoadSynapsesAsync();
            foreach (var snapshot in synapseSnapshots)
            {
                _synapses[snapshot.Id] = Synapse.FromSnapshot(snapshot);
            }
            
            Console.WriteLine($"Loaded {_synapses.Count} synapses");
            
            // CRITICAL FIX: Import synapses into SparseSynapticGraph for cascade propagation
            // The _synapticGraph is used during queries, but _synapses is what's saved/loaded
            _synapticGraph.ImportSynapses(synapseSnapshots);
            Console.WriteLine($"🔗 Imported {_synapses.Count} synapses into synaptic graph");
            
            // ADPC-Net: Load region→cluster mappings
            _regionToClusterMapping.Clear();
            var loadedMappings = await _storage.LoadRegionMappingsAsync();
            foreach (var kvp in loadedMappings)
            {
                _regionToClusterMapping[kvp.Key] = kvp.Value;
            }
            if ((_configForLogging?.Verbosity ?? 0) > 0)
                Console.WriteLine($"🧬 Loaded {_regionToClusterMapping.Count} region→cluster mappings");
            
            // ADPC-Net: Load activation statistics
            _activationStats = await _storage.LoadActivationStatsAsync();
            var statsSummary = _activationStats.GetSummary();
            if ((_configForLogging?.Verbosity ?? 0) > 0)
                Console.WriteLine($"📊 Loaded activation stats ({statsSummary.TotalActivations} activations, {statsSummary.UniqueRegions} regions)");
            
            // ADPC-Net Phase 5: Load VQ-VAE codebook (if available)
            if (_useVQVAE)
            {
                try
                {
                    var codebookSnapshot = await _storage.LoadVQCodebookAsync();
                    if (codebookSnapshot != null)
                    {
                        _vectorQuantizer.ImportCodebook(codebookSnapshot);
                        var vqStats = _vectorQuantizer.GetStats();
                        if ((_configForLogging?.Verbosity ?? 0) > 0)
                            Console.WriteLine($"🧬 Loaded VQ-VAE codebook (perplexity: {vqStats.Perplexity:F2}, utilization: {vqStats.CodebookUtilization:P1})");
                    }
                }
                catch (FileNotFoundException)
                {
                    // First run - codebook not yet saved
                    if ((_configForLogging?.Verbosity ?? 0) > 0)
                        Console.WriteLine($"🧬 VQ-VAE codebook not found (first run - will be created during training)");
                }
            }
            
            // Load cluster index (legacy) for counts
            var clusterIndex = await _storage.LoadClusterIndexAsync();
            Console.WriteLine($"Found {clusterIndex.Count} clusters in storage");
            
            // Optional: fast cached stats to avoid slow scans
            if ((_configForLogging?.Verbosity ?? 0) <= 1)
            {
                var stats = await _storage.GetStorageStatsAsync();
                Console.WriteLine($"Storage: {stats.ClusterCount} clusters, {stats.TotalSizeFormatted}");
            }
            else
            {
                var stats = await _storage.GetStorageStatsAsync();
                Console.WriteLine($"Storage: {stats.ClusterCount} clusters, {stats.TotalSizeFormatted}");
            }
            
            // Load concept capacities
            _conceptCapacities = await _storage.LoadConceptCapacitiesAsync();

            // Reset per-run cache
            _conceptClusterCache.Clear();
            
            // Phase 4: Start background cluster eviction loop
            _evictionCancellation = new CancellationTokenSource();
            _evictionTask = Task.Run(() => ClusterEvictionLoopAsync(_evictionCancellation.Token));
            Console.WriteLine("✓ Background cluster eviction started (check every 5 min, evict after 30 min idle)");
        }

        /// <summary>
        /// ADPC-Net Phase 1: Learn from input using pattern-based clustering
        /// NO concept name lookup - uses feature vectors only
        /// </summary>
        public async Task<LearningResult> LearnConceptAsync(string concept, Dictionary<string, double> features)
        {
            var result = new LearningResult { Concept = concept };

            // Timing buckets
            var tAll = Stopwatch.StartNew();
            var tFind = Stopwatch.StartNew();

            // ADPC-Net: Encode input to feature vector
            var featureVector = _featureEncoder.Encode(concept);
            
            // ADPC-Net: Find or create cluster based on pattern (NO NAME LOOKUP)
            var cluster = await FindOrCreateClusterForPattern(featureVector, debugLabel: concept);
            tFind.Stop();
            result.ClusterId = cluster.ClusterId;

            // Calculate novelty score for this pattern (Phase 5: VQ-VAE or legacy LSH)
            var regionId = GetRegionId(featureVector);
            var novelty = _activationStats.CalculateNovelty(regionId, featureVector);

            // Get neurons for this pattern (using concept as label for now, but not for lookup)
            var tLookup = Stopwatch.StartNew();
            var conceptNeurons = await cluster.FindNeuronsByConcept(concept);
            tLookup.Stop();

            // Capacity and growth based on NOVELTY (not predetermined formula)
            var tCapacity = Stopwatch.StartNew();
            var target = GetTargetNeuronsForConcept(concept, features);
            int grew = 0;

            // P1.6 instrumentation: reuse vs growth accounting
            _allocEvents++;
            if (conceptNeurons.Count > 0) _allocReuseHits++;

            // Continuous synaptic forgetting, inline on the training path (thread-safe
            // by construction: all graph writes happen here). Every 5,000 learn events
            // ≈ 1 min at observed rates. Newborns (~0.106) die within ~3 un-reinforced
            // passes; reinforced pathways persist. See REFOCUS.md P1.5/P1.6.
            if (++_learnEventsSinceDecay >= DecayEveryNLearnEvents)
            {
                _learnEventsSinceDecay = 0;
                var (dBefore, dAfter, dBlocked) = DecayAndPruneSynapses(0.97f);
                Console.WriteLine($"   ✂️  Synaptic decay: {dBefore:N0} → {dAfter:N0} " +
                                  $"(pruned {dBefore - dAfter:N0}, blocked_by_budget {dBlocked:N0})");
            }

            // Freeze the VQ codebook once warmed up: stable pattern → code means a
            // known concept keeps finding its existing assembly, and persisted
            // neuron VQ codes keep regenerating the same properties.
            if (_vectorQuantizer.IsLearning && _vectorQuantizer.UpdateCount >= VqWarmupUpdates)
            {
                _vectorQuantizer.FreezeCodebook();
                var vqStatsFreeze = _vectorQuantizer.GetStats();
                Console.WriteLine($"   🧊 VQ codebook frozen after {VqWarmupUpdates:N0} updates " +
                                  $"(codes claimed {_vectorQuantizer.SeededCount}, perplexity {vqStatsFreeze.Perplexity:F2}, " +
                                  $"utilization {vqStatsFreeze.CodebookUtilization:P1}) — pattern→code assignment is now stable");
            }

            // Dynamic creation balanced by reuse (removed arbitrary hit gating)
            if (conceptNeurons.Count < target)
            {
                var needed = target - conceptNeurons.Count;
                // Staged growth (REFOCUS P1.6d): cap the first allocation too.
                // A concept earns capacity through repetition rather than being
                // handed its full target the first time it is ever seen.
                needed = conceptNeurons.Count > 0
                    ? Math.Min(needed, Math.Max(0, MaxAddPerConceptPerRun))
                    : Math.Min(needed, FirstAllocationNeurons);

                if (needed > 0)
                {
                    var tGrow = Stopwatch.StartNew();
                    // Phase 6B: Pass VectorQuantizer and featureVector for VQ code extraction during neuron creation
                    var newNeurons = await cluster.GrowForConcept(concept, conceptNeurons.Count + needed, _vectorQuantizer, featureVector);
                    tGrow.Stop();
                    // GrowForConcept(targetSize) returns created neurons; ensure we track only added
                    grew = newNeurons.Count;
                    conceptNeurons.AddRange(newNeurons);
                    TotalNeuronsCreated += grew;
                    if (grew > 0) { _allocGrowEvents++; _allocNeuronsGrown += grew; }
                    
                    // Update cluster centroid with new pattern
                    cluster.UpdateCentroid(featureVector);
                    
                    if (ShouldSampleLog()) Console.WriteLine($"   ◽ grow: +{grew} (target {target}) in {tGrow.Elapsed.TotalMilliseconds:F1} ms");
                }
            }
            tCapacity.Stop();

            // Training pass — competitive (P2.1)
            //
            // Was: every neuron trained toward a constant target of 0.8, whose fixed
            // point is "respond to everything" and which cannot discriminate.
            // Now: lateral inhibition. All neurons in the assembly compute how well
            // they match the pattern; only the best responders learn it. Losers are
            // untouched, so they stay tuned to whatever they already prefer.
            var tTrain = Stopwatch.StartNew();
            var trainingFeatures = BuildTrainingFeatures(featureVector, features);
            var trainingInputs = _featureMapper.ConvertFeaturesToNeuronInputs(trainingFeatures);

            // Ensure every neuron is wired to this pattern's input lines before the
            // contest, otherwise a neuron can lose merely for never having been wired.
            foreach (var neuron in conceptNeurons)
                EnsureFeatureWiring(neuron, trainingFeatures);

            var contest = conceptNeurons
                .Select(n => (neuron: n, match: n.MatchQuality(trainingInputs)))
                .OrderByDescending(p => p.match)
                .ToList();

            var winnerCount = Math.Max(MinCompetitiveWinners,
                                       (int)Math.Ceiling(contest.Count * CompetitiveWinnerFraction));
            foreach (var (neuron, _) in contest.Take(winnerCount))
            {
                neuron.ReinforceTowardInput(trainingInputs, CompetitiveLearningRate);
            }
            tTrain.Stop();

            // P1.6l diagnostic: why do only ~16 of a concept's neurons ever fire?
            // Distinguishes "neuron has no weights for these inputs" (wiring gap)
            // from "has the weights but sums too low" (excitability problem).
            if (++_receptiveFieldSampleCounter >= ReceptiveFieldSampleEvery && conceptNeurons.Count > 20)
            {
                _receptiveFieldSampleCounter = 0;
                LogReceptiveFieldOverlap(concept, conceptNeurons, trainingFeatures);
            }

            // ADPC-Net Phase 3: Record Hebbian co-activation
            var tHebbian = Stopwatch.StartNew();
            RecordHebbianCoactivation(contest);
            tHebbian.Stop();

            // Capacity adjust
            var tAdjust = Stopwatch.StartNew();
            var demand = Math.Min(1.5, (double)conceptNeurons.Count / Math.Max(1, target));
            AdjustConceptCapacity(concept, conceptNeurons.Count, demand);
            tAdjust.Stop();

            // Synapses
            var tSyn = Stopwatch.StartNew();
            await CreateConceptualConnections(concept, features);
            tSyn.Stop();

            result.Success = true;
            result.NeuronsInvolved = conceptNeurons.Count;
            result.NeuronsCreated = grew;  // Track how many neurons were actually created this session

            // Update instrumentation aggregates
            tAll.Stop();
            _instrCount++;
            _tFindMsSum += tFind.Elapsed.TotalMilliseconds;
            _tLookupMsSum += tLookup.Elapsed.TotalMilliseconds;
            _tCapacityMsSum += tCapacity.Elapsed.TotalMilliseconds;
            _tTrainMsSum += tTrain.Elapsed.TotalMilliseconds;
            _tSynMsSum += tSyn.Elapsed.TotalMilliseconds;
            _tTotalMsSum += tAll.Elapsed.TotalMilliseconds;
            _neuronsAddedSum += grew;
            _neuronsUsedSum += conceptNeurons.Count;

            // Report (sampled)
            ReportSampler(concept, result.NeuronsInvolved, result.ClusterId);
            AccumulateReportBlock(concept, result.NeuronsInvolved, result.ClusterId);

            // Emit block-level perf summary
            if (_learnEvents % Math.Max(1, _reportingInterval) == 0 && _instrCount > 0)
            {
                var avg = new Func<double, double>(x => x / _instrCount);
                var addedPct = _neuronsUsedSum > 0 ? (100.0 * _neuronsAddedSum / (double)_neuronsUsedSum) : 0.0;
                Console.WriteLine($"   🧪 Perf(avg over {_instrCount} concepts): find {avg(_tFindMsSum):F1} ms | lookup {avg(_tLookupMsSum):F1} ms | capacity {avg(_tCapacityMsSum):F1} ms | train {avg(_tTrainMsSum):F1} ms | syn {avg(_tSynMsSum):F1} ms | total {avg(_tTotalMsSum):F1} ms | neurons added/used { _neuronsAddedSum }/{ _neuronsUsedSum } ({addedPct:F1}% new)");
                // reset
                _instrCount = 0;
                _tFindMsSum = _tLookupMsSum = _tCapacityMsSum = _tTrainMsSum = _tSynMsSum = _tTotalMsSum = 0;
                _neuronsAddedSum = _neuronsUsedSum = 0;
            }

            if (ShouldSampleLog())
            {
                Console.WriteLine($"⏱️ learn concept '{concept}': find {tFind.Elapsed.TotalMilliseconds:F1} ms | lookup {tLookup.Elapsed.TotalMilliseconds:F1} ms | capacity {tCapacity.Elapsed.TotalMilliseconds:F1} ms | train {tTrain.Elapsed.TotalMilliseconds:F1} ms | syn {tSyn.Elapsed.TotalMilliseconds:F1} ms | total {tAll.Elapsed.TotalMilliseconds:F1} ms");
            }
            return result;
        }

        /// <summary>
        /// Process input and generate response using relevant clusters
        /// </summary>
        public async Task<ProcessingResult> ProcessInputAsync(string input, Dictionary<string, double> features)
        {
            if (ShouldSampleLog()) Console.WriteLine($"🤔 Processing input: {input}");
            
            var result = new ProcessingResult { Input = input };
            var activatedClusters = new List<Guid>();
            var neuronOutputs = new Dictionary<Guid, double>();
            
            // SYNAPTIC PROPAGATION: Load trained neurons and follow synaptic paths
            // Extract concepts from input for feature encoding
            var inputConcepts = ExtractConcepts(input);
            
            // Phase 1: Load EXISTING trained neurons for each concept (don't create new)
            var seedNeurons = new Dictionary<Guid, double>();
            var relevantClusters = new List<NeuronCluster>();
            var clusterScores = new Dictionary<Guid, double>();
            
            foreach (var concept in inputConcepts)
            {
                // Load neurons that were TRAINED on this concept
                var (conceptNeurons, conceptClusters) = await LoadTrainedNeuronsForConcept(concept);
                
                // Add to seed neurons for propagation
                foreach (var (neuronId, activation) in conceptNeurons)
                {
                    if (!seedNeurons.ContainsKey(neuronId))
                        seedNeurons[neuronId] = activation;
                    else
                        seedNeurons[neuronId] = Math.Max(seedNeurons[neuronId], activation);
                }
                
                // Track clusters
                foreach (var (cluster, score) in conceptClusters)
                {
                    if (!clusterScores.ContainsKey(cluster.ClusterId))
                    {
                        relevantClusters.Add(cluster);
                        clusterScores[cluster.ClusterId] = score;
                    }
                    else
                    {
                        clusterScores[cluster.ClusterId] += score;
                    }
                }
            }
            
            // Sort by accumulated similarity score (top 5)
            var sortedClusters = relevantClusters
                .OrderByDescending(c => clusterScores[c.ClusterId])
                .ToList();
            if (sortedClusters.Count > 5)
                sortedClusters = sortedClusters.GetRange(0, 5);
            
            if (ShouldSampleLog())
            {
                Console.WriteLine($"   🎯 Loaded trained neurons: {inputConcepts.Length} concepts → {seedNeurons.Count} neurons in {sortedClusters.Count} clusters");
                for (int i = 0; i < Math.Min(3, sortedClusters.Count); i++)
                {
                    var cluster = sortedClusters[i];
                    var score = clusterScores[cluster.ClusterId];
                    Console.WriteLine($"      ◽ Cluster {cluster.ClusterId} (score: {score:F3}, neurons: {cluster.NeuronCount})");
                }
            }
            
            // Use seed neurons from trained patterns (no new neuron creation)
            neuronOutputs = new Dictionary<Guid, double>(seedNeurons);
            
            // Track activated clusters
            foreach (var cluster in sortedClusters)
            {
                activatedClusters.Add(cluster.ClusterId);
                _accessedClusters.Add(cluster.ClusterId);
            }
            
            // Phase 2: Propagate activation through synaptic graph (biological cascade)
            var propagationResult = await PropagateActivationThroughSynapticGraph(seedNeurons, maxDepth: 3);
            neuronOutputs = propagationResult.AllActivations;
            
            // Phase 3: Calculate natural novelty from cascade metrics
            var noveltyScore = CalculateNoveltyFromCascade(
                seedCount: seedNeurons.Count,
                totalActivated: neuronOutputs.Count,
                maxDepth: propagationResult.MaxDepthReached,
                layerGrowth: propagationResult.LayerSizes
            );
            
            // Phase 6A: Sparse activation metrics
            _queryCount++;
            var activatedNeurons = neuronOutputs.Count;
            var totalLoadedNeurons = sortedClusters.Sum(c => c.NeuronCount);
            _totalActivatedNeurons += activatedNeurons;
            _totalLoadedNeurons += totalLoadedNeurons;
            
            var activationPercent = totalLoadedNeurons > 0 
                ? (activatedNeurons * 100.0) / totalLoadedNeurons 
                : 0.0;
            
            // Log sparse activation for every query (critical metric)
            if (totalLoadedNeurons > 0)
            {
                Console.WriteLine($"⚡ Sparse Activation: {activatedNeurons:N0} / {totalLoadedNeurons:N0} neurons active ({activationPercent:F2}%) | clusters: {sortedClusters.Count}/{_loadedClusters.Count}");
            }
            
            // Phase 3: Log novelty score
            var noveltyLabel = noveltyScore < 0.3 ? "FAMILIAR" : noveltyScore > 0.7 ? "NOVEL" : "MODERATE";
            Console.WriteLine($"🧬 Novelty: {noveltyScore:F2} ({noveltyLabel}) | cascade: {seedNeurons.Count}→{neuronOutputs.Count} neurons");
            
            // Generate response based on activated neurons and clusters
            result.Response = GenerateResponse(neuronOutputs, inputConcepts, sortedClusters, noveltyScore);
            result.ActivatedClusters = activatedClusters;
            result.ActivatedNeurons = neuronOutputs.Count;
            result.Confidence = CalculateConfidence(neuronOutputs, sortedClusters);
            
            // Enhanced: Integrate emotional processing if consciousness is active
            // BUT avoid recursive loops for internal consciousness processing
            // TEMPORARILY DISABLED - ContinuousProcessor excluded from build
            /*
            if (_continuousProcessor != null && _continuousProcessor.IsProcessing && 
                !IsInternalCognitionInput(input))
            {
                // Let the emotional processor analyze this experience
                var emotionalProcessor = _continuousProcessor.GetInstinctualProcessor();
                if (emotionalProcessor != null)
                {
                    await emotionalProcessor.ProcessExperienceAsync(input, features, result.Confidence);
                }
                
                // Check for goal alignment if goals are active
                var goalSystem = _continuousProcessor.GetGoalSystem();
                if (goalSystem != null)
                {
                    await goalSystem.AssessGoalAlignmentAsync(input, features);
                }
            }
            */
            
            if (ShouldSampleLog()) Console.WriteLine($"💭 Generated response with confidence {result.Confidence:F2}");
            
            return result;
        }

        /// <summary>
        /// Check if input is from internal consciousness processing to prevent recursive loops
        /// </summary>
        private bool IsInternalCognitionInput(string input)
        {
            return input.StartsWith("emotional_context_") || input.StartsWith("emotional_memory_") ||
                   input.StartsWith("reflect on goal strategy:") || input.StartsWith("goal_reflection_") ||
                   input.StartsWith("reflecting on ") || input.StartsWith("concept_reflection_") ||
                   input.StartsWith("creative ") || input.StartsWith("creative_association_") ||
                   input.StartsWith("creative_blend_") || input.StartsWith("reinforce ") || 
                   input.StartsWith("learning_reinforcement_") || input.StartsWith("emotional memory processing") ||
                   input.StartsWith("emotional_memory_processing_") || input.StartsWith("consolidate learning patterns") ||
                   input.StartsWith("learning_pattern_consolidation_") || input.StartsWith("memory consolidation");
        }

        /// <summary>
        /// Save brain state to disk with enhanced partitioning
        /// </summary>
        public async Task SaveAsync()
        {
            // Ensure only one save operation at a time
            await _saveLock.WaitAsync();
            try
            {
                Console.WriteLine("💾 Saving brain state with enhanced partitioning...");
                var swTotal = Stopwatch.StartNew();

                // Take snapshot of loaded clusters to avoid concurrent modification
                var loadedClustersSnapshot = _loadedClusters.GetValues();
                Console.WriteLine($"   🧮 Checkpoint: _loadedClusters has {_loadedClusters.Count} entries, snapshot has {loadedClustersSnapshot.Count} clusters");

            // Use lightweight context for routine checkpoints (no full neuron loading)
            // BrainContext.AllNeurons is optional - partitioner works with empty dict for incremental saves
            var sw = Stopwatch.StartNew();
            var context = new BrainContext
            {
                AllNeurons = new Dictionary<Guid, HybridNeuron>(), // Empty - avoid loading 140K+ neurons
                AnalysisTime = DateTime.UtcNow
            };
            if ((_configForLogging?.Verbosity ?? 0) > 0)
                Console.WriteLine($"   ⏱️  Created lightweight checkpoint context in {sw.Elapsed.TotalMilliseconds:F1}ms");

            // STM->LTM consolidation with collection
            sw.Restart();
            int totalPromoted = 0;
            int clustersTouched = 0;
            // P2 FIX: this was `max(5, min(50, MaxParallelSaves*5))` = 10 neurons
            // per cluster per checkpoint. With ~336 clusters and one checkpoint in
            // a 5-minute run that promoted ~3,360 of ~70,000 neurons — under 5%.
            // The other 95% never had their learned STM deltas applied and kept
            // their random initial weights forever, so ProcessInputs was a random
            // projection and the probe could not tell "water" (0.788) from
            // "qwertyuiop" (0.839). A budget tuned for checkpoint speed was
            // silently discarding almost all learning.
            // Consolidation is cheap (dictionary adds); it is the SAVE that costs,
            // and a neuron whose weights actually changed has earned its write.
            int budgetPerCluster = int.MaxValue;
            var changedByCluster = new Dictionary<Guid, List<HybridNeuron>>();
            foreach (var cluster in loadedClustersSnapshot)
            {
                var changed = await cluster.ConsolidateStmCollectAsync(budgetPerCluster);
                if (changed.Count > 0)
                {
                    totalPromoted += changed.Count;
                    clustersTouched++;
                    changedByCluster[cluster.ClusterId] = changed;
                }
            }
            Console.WriteLine($"   🧠 Consolidation: promoted {totalPromoted:N0} neurons across {clustersTouched} clusters in {sw.Elapsed.TotalSeconds:F2}s (unbudgeted)");

            // Note: synaptic decay/pruning moved to the maintenance loop
            // (DecayAndPruneSynapses) — checkpoint-only decay never fired in
            // short runs, so 5-min benchmarks always reported "pruned 0".

            // Save feature mappings
            sw.Restart();
            var featureMappingSnapshot = _featureMapper.CreateSnapshot();
            await _storage.SaveFeatureMappingsAsync(featureMappingSnapshot);
            if ((_configForLogging?.Verbosity ?? 0) > 0)
                Console.WriteLine($"   ⏱️  Saved feature mappings in {sw.Elapsed.TotalSeconds:F2}s");
            
            // Persist changed neurons to neuron banks ONLY (batched by partition)
            sw.Restart();
            
            // Phase 6B: Procedural save mode - save ALL neurons in compact format
            if (_configForLogging?.UseProceduralSave == true)
            {
                // In procedural mode, save all neurons from all loaded clusters
                // (not just consolidated ones, since we want full checkpoints)
                var allClusterNeurons = new List<(NeuronCluster, IEnumerable<HybridNeuron>)>();
                foreach (var cluster in loadedClustersSnapshot)
                {
                    var neurons = await cluster.GetNeuronsAsync();
                    if (neurons.Count > 0)
                    {
                        allClusterNeurons.Add((cluster, neurons.Values));
                    }
                }
                
                int totalNeurons = allClusterNeurons.Sum(t => t.Item2.Count());
                Console.WriteLine($"   🔄 Procedural Save Mode: Saving {totalNeurons} neurons in compact format...");
                
                await _storage.SaveProceduralNeuronBanksAsync(allClusterNeurons, context);
            }
            else
            {
                // Regular save: persist only consolidated neurons (incremental)
                var changeTuples = changedByCluster
                    .Where(kvp => _loadedClusters.TryGetValue(kvp.Key, out _))
                    .Select(kvp => {
                        _loadedClusters.TryGetValue(kvp.Key, out var cluster);
                        return (cluster!, kvp.Value.AsEnumerable());
                    });
                await _storage.SaveNeuronBanksInBatchesAsync(changeTuples, context);
            }
            
            var neuronsPersisted = changedByCluster.Sum(kvp => kvp.Value.Count);
            if ((_configForLogging?.Verbosity ?? 0) > 0)
                Console.WriteLine($"   💾 Persisted neuron banks in batches; ~{neuronsPersisted} neurons updated in {sw.Elapsed.TotalSeconds:F2}s");

            // Determine clusters requiring membership/metadata save (use snapshot to avoid concurrent modification)
            // IMPORTANT: Even non-dirty clusters need membership pack updates when packs are missing/corrupt
            // So we pass ALL loaded clusters, not just dirty ones. SaveClustersEfficientlyAsync will detect
            // which ones actually need updates (missing packs, changed membership, etc.)
            var clustersToSave = loadedClustersSnapshot
                .Where(c => c.NeuronCount > 0)  // Skip empty clusters
                .Distinct()
                .ToList();
            
            var dirtyCount = clustersToSave.Count(c => c.HasUnsavedChanges);
            if ((_configForLogging?.Verbosity ?? 0) > 0)
                Console.WriteLine($"   🧮 Clusters: total={loadedClustersSnapshot.Count}, dirty={dirtyCount}, checking={clustersToSave.Count}");

            // Save clusters (membership + metadata) with throttling
            sw.Restart();
            await _storage.SaveClustersEfficientlyAsync(clustersToSave, context);
            if ((_configForLogging?.Verbosity ?? 0) > 0)
                Console.WriteLine($"   ⏱️  Saved {clustersToSave.Count} clusters in {sw.Elapsed.TotalSeconds:F2}s (parallel={_storage.MaxParallelSaves}, gzip={_storage.CompressClusters})");
            
            // Save cluster index (use snapshot) - ONLY save clusters with neurons to avoid ghost clusters
            sw.Restart();
            var clusterSnapshots = loadedClustersSnapshot
                .Where(c => c.NeuronCount > 0)  // Skip empty ghost clusters
                .Select(c => c.CreateSnapshot())
                .ToList();
            await _storage.SaveClusterIndexAsync(clusterSnapshots);
            if ((_configForLogging?.Verbosity ?? 0) > 0)
                Console.WriteLine($"   ⏱️  Saved cluster index ({clusterSnapshots.Count} non-empty clusters) in {sw.Elapsed.TotalSeconds:F2}s");
            
            // Save synapses directly to partitioned storage (Phase 3: prevents OOM)
            sw.Restart();
            var synapseCount = _synapticGraph.GetSynapseCount();
            Console.WriteLine($"   🔗 Saving {synapseCount:N0} synapses to partitioned storage...");
            
            try
            {
                await _storage.SaveSynapsesPartitionedAsync(_synapticGraph);
                Console.WriteLine($"   💾 Saved {synapseCount:N0} synapses in {sw.Elapsed.TotalSeconds:F2}s");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ ERROR during synapse save: {ex.GetType().Name}: {ex.Message}");
            }
            
            // ADPC-Net: Save region→cluster mappings
            sw.Restart();
            await _storage.SaveRegionMappingsAsync(_regionToClusterMapping);
            if ((_configForLogging?.Verbosity ?? 0) > 0)
                Console.WriteLine($"   🧬 Saved {_regionToClusterMapping.Count} region mappings in {sw.Elapsed.TotalSeconds:F2}s");
            
            // ADPC-Net: Save activation statistics
            sw.Restart();
            await _storage.SaveActivationStatsAsync(_activationStats);
            var statsSummary = _activationStats.GetSummary();
            if ((_configForLogging?.Verbosity ?? 0) > 0)
                Console.WriteLine($"   📊 Saved activation stats ({statsSummary.TotalActivations} activations, {statsSummary.UniqueRegions} regions) in {sw.Elapsed.TotalSeconds:F2}s");
            
            // ADPC-Net Phase 5: Save VQ-VAE codebook
            if (_useVQVAE && _vectorQuantizer != null)
            {
                sw.Restart();
                var codebookSnapshot = _vectorQuantizer.ExportCodebook();
                await _storage.SaveVQCodebookAsync(codebookSnapshot);
                var vqStats = _vectorQuantizer.GetStats();
                if ((_configForLogging?.Verbosity ?? 0) > 0)
                    Console.WriteLine($"   🧬 Saved VQ-VAE codebook (perplexity: {vqStats.Perplexity:F2}, utilization: {vqStats.CodebookUtilization:P1}) in {sw.Elapsed.TotalSeconds:F2}s");
            }
            
            // Persist concept capacities at end of save
            try { await _storage.SaveConceptCapacitiesAsync(_conceptCapacities); } catch { /* best effort */ }
            if ((_configForLogging?.Verbosity ?? 0) > 0)
            {
                var m = _storage.GetAndResetLastSaveMetrics();
                Console.WriteLine($"   📈 Save metrics: clustersExamined={m.ClustersExamined}, changedMembership={m.ClustersChangedMembership}, packsWritten={m.MembershipPacksWritten}, packsSkipped={m.MembershipPacksSkipped}, bankPartitions={m.NeuronBankPartitions}, neuronsUpserted={m.NeuronsUpserted}");
                Console.WriteLine($"   ⏱️  Total save time {swTotal.Elapsed.TotalSeconds:F2}s");
            }
            
            // Phase 6A: Report sparse activation and working set statistics
            if (_queryCount > 0)
            {
                var avgActivation = _totalLoadedNeurons > 0 
                    ? (_totalActivatedNeurons * 100.0) / _totalLoadedNeurons 
                    : 0.0;
                var workingSetPercent = TotalClustersCreated > 0 
                    ? (_accessedClusters.Count * 100.0) / TotalClustersCreated 
                    : 0.0;
                
                Console.WriteLine($"\n📊 BIOLOGICAL ALIGNMENT METRICS (Phase 6A)");
                Console.WriteLine($"   ⚡ Sparse Activation: {avgActivation:F2}% average (target: <2%)");
                Console.WriteLine($"   🎯 Queries Processed: {_queryCount:N0}");
                Console.WriteLine($"   🧠 Working Set: {_accessedClusters.Count}/{TotalClustersCreated} clusters ({workingSetPercent:F1}%)");
                Console.WriteLine($"   💾 Total Clusters Loaded: {loadedClustersSnapshot.Count:N0}");
                Console.WriteLine($"   🔗 Total Synapses: {_synapses.Count:N0}");
            }
            
            Console.WriteLine("\n✅ Brain state saved with hierarchical partitioning");

            // Optional quick integrity sampler when verbose
            if ((_configForLogging?.Verbosity ?? 0) > 0)
            {
                try { await RunIntegritySamplerAsync(5); } catch { /* best effort */ }
            }
            }
            finally
            {
                _saveLock.Release();
            }
        }

        /// <summary>
        /// Cleanup - unload old clusters and prune weak connections with memory consolidation
        /// </summary>
        public async Task MaintenanceAsync()
        {
            Console.WriteLine("🧹 Running brain maintenance with memory consolidation...");
            
            int unloadedClusters = 0;
            int prunedSynapses = 0;
            
            // Run memory consolidation to reorganize partitions
            await _storage.ConsolidateMemoryPartitions();
            
            // Unload old clusters
            var clustersToUnload = _loadedClusters.GetValues()
                .Where(c => !c.ShouldStayLoaded())
                .ToList();
            
            foreach (var cluster in clustersToUnload)
            {
                await cluster.PersistAndUnloadAsync(forceUnload: true);
                _loadedClusters.Remove(cluster.ClusterId);
                unloadedClusters++;
            }
            
            // Prune weak synapses
            var weakSynapses = _synapses.Values
                .Where(s => s.ShouldBePruned())
                .Select(s => s.Id)
                .ToList();
            
            foreach (var synapseId in weakSynapses)
            {
                _synapses.Remove(synapseId);
                prunedSynapses++;
            }
            
            // Age remaining synapses
            foreach (var synapse in _synapses.Values)
            {
                synapse.Age(TimeSpan.FromHours(1));
            }
            
            Console.WriteLine($"🧹 Maintenance complete: consolidated memory, unloaded {unloadedClusters} clusters, pruned {prunedSynapses} synapses");
        }

        /// <summary>
        /// Phase 4: Handle cluster eviction from LRU cache
        /// Persists cluster to disk before evicting from memory
        /// </summary>
        private async Task HandleClusterEvictionAsync(Guid clusterId, NeuronCluster cluster)
        {
            try
            {
                // Persist cluster before eviction
                await cluster.PersistAndUnloadAsync(forceUnload: true);
                
                // Clean up access time tracking
                _clusterAccessTimes.Remove(clusterId);
                
                DebugLog.Verbose($"   🗑️ LRU evicted cluster: {clusterId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ⚠️ Error evicting cluster {clusterId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Phase 4: Background loop for cluster eviction
        /// Runs every 5 minutes, evicts clusters idle for >30 minutes
        /// </summary>
        private async Task ClusterEvictionLoopAsync(CancellationToken cancellationToken)
        {
            const int CHECK_INTERVAL_MS = 5 * 60 * 1000; // 5 minutes
            const int MAX_IDLE_MINUTES = 30;
            
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(CHECK_INTERVAL_MS, cancellationToken);
                    
                    // Find stale clusters (not accessed in last 30 minutes)
                    var staleItems = _loadedClusters.GetStaleItems(
                        _clusterAccessTimes, 
                        TimeSpan.FromMinutes(MAX_IDLE_MINUTES));
                    
                    if (staleItems.Count > 0)
                    {
                        Console.WriteLine($"🧹 Evicting {staleItems.Count} idle clusters (>{MAX_IDLE_MINUTES} min inactive)...");
                        
                        foreach (var (clusterId, cluster) in staleItems)
                        {
                            await HandleClusterEvictionAsync(clusterId, cluster);
                            _loadedClusters.Remove(clusterId);
                        }
                        
                        Console.WriteLine($"   ✅ Evicted {staleItems.Count} idle clusters, {_loadedClusters.Count} remain in cache");
                    }
                }
                catch (OperationCanceledException)
                {
                    // Normal shutdown
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Error in eviction loop: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Get brain statistics
        /// </summary>
        public async Task<BrainStats> GetStatsAsync()
        {
            var storageStats = await _storage.GetStorageStatsAsync();
            
            return new BrainStats
            {
                LoadedClusters = _loadedClusters.Count,
                TotalClusters = storageStats.ClusterCount,
                TotalSynapses = _synapses.Count,
                TotalNeuronsCreated = TotalNeuronsCreated,
                StorageSizeFormatted = storageStats.TotalSizeFormatted,
                UptimeFormatted = FormatTimeSpan(DateTime.UtcNow - CreatedAt)
            };
        }

        /// <summary>
        /// Get enhanced brain statistics with partition analysis
        /// </summary>
        public async Task<EnhancedBrainStats> GetEnhancedStatsAsync()
        {
            var baseStats = await GetStatsAsync();
            var storageStats = await _storage.GetEnhancedStorageStatsAsync();
            
            return new EnhancedBrainStats
            {
                BaseStats = baseStats,
                StorageStats = storageStats,
                PartitionEfficiency = storageStats.HierarchicalEfficiency,
                TopPartitions = storageStats.PartitionStats
                    .OrderByDescending(p => p.Value.ClusterCount)
                    .Take(5)
                    .ToDictionary(p => p.Key, p => p.Value)
            };
        }

        /// <summary>
        /// Get concept mastery level for hierarchical learning
        /// </summary>
        public async Task<double> GetConceptMasteryLevelAsync(string concept)
        {
            var conceptNode = _dependencyGraph.GetConcept(concept);
            if (conceptNode != null)
            {
                return conceptNode.CurrentMastery;
            }

            // Calculate mastery based on neuron activation patterns
            var relevantClusters = await FindRelevantClusters(new[] { concept });
            if (!relevantClusters.Any())
            {
                return 0.0; // No knowledge of this concept
            }

            var totalActivation = 0.0;
            var neuronCount = 0;

            foreach (var cluster in relevantClusters.Take(3))
            {
                var neurons = await cluster.GetNeuronsAsync();
                foreach (var neuron in neurons.Values)
                {
                    if (neuron.AssociatedConcepts.Contains(concept, StringComparer.OrdinalIgnoreCase))
                    {
                        totalActivation += Math.Max(0, neuron.CurrentPotential - neuron.RestingPotential);
                        neuronCount++;
                    }
                }
            }

            return neuronCount > 0 ? totalActivation / neuronCount : 0.0;
        }

        /// <summary>
        /// Start continuous consciousness processing
        /// </summary>
        public async Task AwakeCognitionAsync()
        {
            // TEMPORARILY DISABLED - ContinuousProcessor excluded from build
            /*
            if (_continuousProcessor != null)
            {
                await _continuousProcessor.StartCognitionAsync();
            }
            */
            await Task.CompletedTask;
        }

        /// <summary>
        /// Stop continuous consciousness processing
        /// </summary>
        public async Task SleepCognitionAsync()
        {
            // TEMPORARILY DISABLED - ContinuousProcessor excluded from build
            /*
            if (_continuousProcessor != null)
            {
                await _continuousProcessor.StopCognitionAsync();
            }
            */
            await Task.CompletedTask;
        }

        /// <summary>
        /// Get consciousness status and statistics
        /// </summary>
        public CognitionStats GetCognitionStats()
        {
            // TEMPORARILY DISABLED - ContinuousProcessor excluded from build
            return new CognitionStats { IsConscious = false };
            
            /*
            if (_continuousProcessor == null)
            {
                return new CognitionStats { IsConscious = false };
            }

            var stats = new CognitionStats
            {
                IsConscious = _continuousProcessor.IsProcessing,
                CognitionIterations = _continuousProcessor.CognitionIterations,
                LastThought = _continuousProcessor.LastConsciousThought,
                CurrentFocus = _continuousProcessor.CurrentFocus,
                WisdomSeeking = _continuousProcessor.WisdomSeeking,
                UniversalCompassion = _continuousProcessor.UniversalCompassion,
                CreativeContribution = _continuousProcessor.CreativeContribution,
                CooperativeSpirit = _continuousProcessor.CooperativeSpirit,
                BenevolentCuriosity = _continuousProcessor.BenevolentCuriosity,
                CognitionFrequency = _continuousProcessor.CognitionInterval
            };

            // Add emotional state information
            var emotionalState = _continuousProcessor.CurrentInstinctualState;
            stats.DominantEmotion = emotionalState.DominantEmotion;
            stats.InstinctualBalance = emotionalState.InstinctualBalance;
            stats.InstinctualClarity = emotionalState.InstinctualClarity;

            // Add goal system information
            var goalStatus = _continuousProcessor.CurrentGoalStatus;
            stats.ActiveGoals = goalStatus.ActiveGoalCount;
            stats.CompletedGoals = goalStatus.CompletedGoalCount;
            stats.AverageGoalProgress = goalStatus.AverageProgress;

            // Formatted summaries are computed via read-only properties now; no direct assignment needed
            
            return stats;
            */
        }

        /// <summary>
        /// Get brain age for critical period analysis
        /// </summary>
        public async Task<TimeSpan> GetBrainAgeAsync()
        {
            return await Task.FromResult(DateTime.UtcNow - CreatedAt);
        }

        /// <summary>
        /// Enhanced learning with hierarchical concept checking
        /// </summary>
        public async Task<LearningResult> LearnConceptWithScaffoldingAsync(string concept, Dictionary<string, double> features)
        {
            Console.WriteLine($"🧠 Learning concept with scaffolding: {concept}");
            
            // Check if concept can be learned (prerequisites met)
            var canLearn = await _dependencyGraph.CanLearnConcept(concept, this);
            if (!canLearn)
            {
                var learningPath = await _dependencyGraph.GetLearningPath(concept, this);
                Console.WriteLine($"📚 Prerequisites needed: {string.Join(" → ", learningPath)}");
                
                // Learn prerequisites first
                foreach (var prerequisite in learningPath.Where(p => p != concept))
                {
                    var prereqMastery = await GetConceptMasteryLevelAsync(prerequisite);
                    if (prereqMastery < 0.7)
                    {
                        Console.WriteLine($"🎓 Learning prerequisite: {prerequisite}");
                        await LearnConceptAsync(prerequisite, features);
                    }
                }
            }
            
            // Now learn the target concept
            var result = await LearnConceptAsync(concept, features);
            
            // Update mastery tracking
            var masteryLevel = await GetConceptMasteryLevelAsync(concept);
            _dependencyGraph.UpdateConceptMastery(concept, masteryLevel);
            
            return result;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // ADPC-Net Phase 1: Pattern-Based Cluster Finding (NO WORD LOOKUPS)
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Calculate cosine similarity between two feature vectors
        /// Returns value between 0.0 (orthogonal) and 1.0 (identical)
        /// </summary>
        private double CosineSimilarity(double[] a, double[] b)
        {
            if (a == null || b == null || a.Length == 0 || b.Length == 0)
                return 0.0;
                
            int minLen = Math.Min(a.Length, b.Length);
            double dotProduct = 0.0;
            double magA = 0.0;
            double magB = 0.0;
            
            for (int i = 0; i < minLen; i++)
            {
                dotProduct += a[i] * b[i];
                magA += a[i] * a[i];
                magB += b[i] * b[i];
            }
            
            if (magA == 0.0 || magB == 0.0)
                return 0.0;
                
            return dotProduct / (Math.Sqrt(magA) * Math.Sqrt(magB));
        }

        /// <summary>
        /// Find clusters based on feature pattern similarity using VQ-VAE or LSH
        /// NO concept name lookup - uses only feature vectors
        /// Returns clusters with similarity scores for ranking
        /// </summary>
        private async Task<List<(NeuronCluster cluster, double similarity)>> FindClustersMatchingPattern(double[] featureVector, int maxClusters = 5)
        {
            // Get region ID from feature vector (Phase 5: VQ-VAE or legacy LSH)
            var regionId = GetRegionId(featureVector);
            
            // DEBUG: Log region lookup
            DebugLog.Debug($"   🔍 FindClustersMatchingPattern: regionId={regionId}, total mappings={_regionToClusterMapping.Count}");
            
            // Record activation for statistics
            _activationStats.RecordActivation(regionId, featureVector);
            
            // Get candidate regions (primary + nearby)
            var nearbyRegions = GetNearbyRegions(featureVector, neighbors: 5);
            
            DebugLog.Debug($"   🔍 Searching {nearbyRegions.Count} regions: {string.Join(", ", nearbyRegions.Take(3))}...");
            
            // CRITICAL: Ensure the primary region is ALWAYS included in search!
            if (!nearbyRegions.Contains(regionId))
            {
                nearbyRegions.Insert(0, regionId);  // Add primary region at front
            }
            
            // Collect clusters from these regions
            var candidateClusters = new List<(NeuronCluster cluster, double similarity)>();
            
            foreach (var region in nearbyRegions)
            {
                // Get clusters mapped to this region
                if (_regionToClusterMapping.TryGetValue(region, out var clusterIds))
                {
                    DebugLog.Debug($"   🔍 Region {region}: found {clusterIds.Count} clusters");
                    
                    foreach (var clusterId in clusterIds)
                    {
                        // Load cluster if not already loaded
                        if (!_loadedClusters.TryGetValue(clusterId, out var cluster))
                        {
                            // LRU cache returns null if not found
                            // Load from storage
                            try
                            {
                                Func<string, Task<List<NeuronSnapshot>>> hierLoad = id =>
                                    _storage.LoadClusterWithPartitioningAsync(id, new BrainContext
                                    {
                                        AllNeurons = new Dictionary<Guid, HybridNeuron>(),
                                        AnalysisTime = DateTime.UtcNow
                                    });
                                cluster = new NeuronCluster($"pattern_{clusterId}", clusterId, hierLoad, _storage.SaveClusterAsync);
                                
                                // CRITICAL: Restore centroid from persisted metadata for pattern matching!
                                var metadata = _storage.GetClusterMetadata(clusterId);
                                if (metadata?.Centroid != null && metadata.Centroid.Length > 0)
                                {
                                    // Restore centroid directly without recalculation
                                    cluster.RestoreCentroid(metadata.Centroid, metadata.CentroidNeuronCount);
                                }
                                
                                // Restore concept label for queryability
                                if (!string.IsNullOrEmpty(metadata?.ConceptLabel))
                                {
                                    cluster.ConceptLabel = metadata.ConceptLabel;
                                    _conceptClusterCache[metadata.ConceptLabel] = clusterId;
                                }
                                
                                // Phase 4: Add to LRU cache with automatic eviction
                                var evictionResult = _loadedClusters.Add(clusterId, cluster);
                                _clusterAccessTimes[clusterId] = DateTime.UtcNow;
                                
                                // Handle eviction if cache was full
                                if (evictionResult.evicted && evictionResult.key != default)
                                {
                                    await HandleClusterEvictionAsync(evictionResult.key, evictionResult.value!);
                                }
                            }
                            catch
                            {
                                // Cluster doesn't exist yet - skip
                                continue;
                            }
                        }
                        
                        if (cluster != null)
                        {
                            // Calculate ACTUAL cosine similarity between input and cluster centroid
                            double similarity = 0.0;
                            
                            if (cluster.Centroid != null)
                            {
                                // Use real pattern matching
                                similarity = CosineSimilarity(featureVector, cluster.Centroid);
                            }
                            else
                            {
                                // Cluster has no centroid yet (newly created or not initialized)
                                // Give it a moderate similarity based on region match
                                similarity = region == regionId ? 0.8 : 0.5;
                            }
                            
                            candidateClusters.Add((cluster, similarity));
                        }
                    }
                }
            }
            
            // Sort by similarity, return top matches with scores
            var matches = candidateClusters
                .OrderByDescending(x => x.similarity)
                .Take(maxClusters)
                .ToList();
                
            if (ShouldSampleLog() && matches.Count > 0)
            {
                Console.WriteLine($"   🎯 Pattern matched {matches.Count} clusters in region {regionId.Substring(0, Math.Min(20, regionId.Length))}...");
            }
            
            return matches;
        }

        /// <summary>
        /// Find or create cluster based on feature patterns (NO CONCEPT NAMES)
        /// This is the NEW pattern-based approach
        /// </summary>
        private async Task<NeuronCluster> FindOrCreateClusterForPattern(double[] featureVector, string debugLabel = "unknown")
        {
            // Get region from feature vector (Phase 5: VQ-VAE or legacy LSH)
            var regionId = GetRegionId(featureVector);
            
            // Try to find existing clusters in this region with sufficient similarity
            const double SIMILARITY_THRESHOLD = 0.65; // Lowered from 0.85 to 0.65 for better pattern matching
            
            var matches = await FindClustersMatchingPattern(featureVector, maxClusters: 5);

            // Assembly reuse (REFOCUS P1.6): among similarity-qualified candidates,
            // prefer the cluster where this concept already grew neurons. Without
            // this, centroid drift + VQ codebook learning make the same word's
            // best-match cluster change visit-to-visit, and it re-grows its full
            // allocation in each (observed: 783K neurons from 1,741 sentences).
            // Recall stays pattern-based; this only stabilizes training-time
            // allocation — a concept re-activates its existing assembly.
            if (!string.IsNullOrEmpty(debugLabel))
            {
                foreach (var m in matches)
                {
                    if (m.similarity < SIMILARITY_THRESHOLD) continue;
                    // Only probe clusters already resident: FindNeuronsByConcept
                    // calls EnsureLoadedAsync, so probing every candidate pulls up
                    // to 5 clusters off the NAS per learn event (measured: find
                    // 0.8ms → 28.8ms on a resumed brain).
                    if (!m.cluster.IsLoaded) continue;
                    var existing = await m.cluster.FindNeuronsByConcept(debugLabel);
                    if (existing.Count > 0)
                    {
                        _allocAssemblyPrefHits++;
                        if (ShouldSampleLog())
                            Console.WriteLine($"   ♻️ Assembly reuse: {debugLabel} → cluster {m.cluster.ClusterId.ToString().Substring(0, 8)} ({existing.Count} existing neurons, sim {m.similarity:F3})");
                        return m.cluster;
                    }
                }
            }

            var bestMatch = matches.FirstOrDefault(m => m.similarity >= SIMILARITY_THRESHOLD);
            
            // DEBUG: Sample logging (first 20 clusters, then every 1000th) to maintain visibility without spam
            if (TotalClustersCreated < 20 || TotalClustersCreated % 1000 == 0)
            {
                DebugLog.Debug($"   🔍 DEBUG cluster={TotalClustersCreated}: candidates={matches.Count()}, best={matches.FirstOrDefault().similarity:F3}, threshold={SIMILARITY_THRESHOLD:F2} [debug: {debugLabel}]");
            }
            
            if (bestMatch != default)
            {
                if (ShouldSampleLog())
                {
                    Console.WriteLine($"   ✓ Reusing cluster {bestMatch.cluster.ClusterId.ToString().Substring(0, 8)} (similarity: {bestMatch.similarity:F3}) [debug: {debugLabel}]");
                }
                return bestMatch.cluster;
            }
            
            // No existing cluster - create new one
            // Note: We DON'T use concept names anymore, but keep debug label for monitoring
            var regionLabel = regionId.Length > 8 ? regionId.Substring(0, 8) : regionId;
            var newCluster = new NeuronCluster($"pattern_{regionLabel}", _storage.LoadClusterAsync, _storage.SaveClusterAsync);
            
            // CRITICAL: Set ConceptLabel to the actual word for queryability
            newCluster.ConceptLabel = debugLabel;
            
            // Initialize centroid with the first pattern - CRITICAL for pattern matching!
            newCluster.UpdateCentroid(featureVector);
            
            var evictionResult = _loadedClusters.Add(newCluster.ClusterId, newCluster);
            _clusterAccessTimes[newCluster.ClusterId] = DateTime.UtcNow;
            if (evictionResult.evicted && evictionResult.key != default)
            {
                await HandleClusterEvictionAsync(evictionResult.key, evictionResult.value!);
            }
            
            // Map region → cluster
            if (!_regionToClusterMapping.ContainsKey(regionId))
                _regionToClusterMapping[regionId] = new List<Guid>();
            _regionToClusterMapping[regionId].Add(newCluster.ClusterId);
            
            TotalClustersCreated++;
            
            if (ShouldSampleLog())
            {
                var regionPreview = regionId.Length > 20 ? regionId.Substring(0, 20) + "..." : regionId;
                Console.WriteLine($"   ✨ Created new cluster for region {regionPreview} (label: {debugLabel})");
            }
            
            return newCluster;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // OLD METHODS (Kept for backward compatibility during migration)
        // ═══════════════════════════════════════════════════════════════════════

        // Private helper methods

        public async Task<NeuronCluster> FindOrCreateClusterForConcept(string concept)
        {
            // Fast path: cache
            if (_conceptClusterCache.TryGetValue(concept, out var cachedId))
            {
                if (_loadedClusters.TryGetValue(cachedId, out var cachedCluster))
                    return cachedCluster;
                // Attempt to load from storage by GUID
                Func<string, Task<List<NeuronSnapshot>>> hierLoad = id =>
                    _storage.LoadClusterWithPartitioningAsync(id, new BrainContext
                    {
                        AllNeurons = new Dictionary<Guid, HybridNeuron>(),
                        AnalysisTime = DateTime.UtcNow
                    });
                var clusterFromCache = new NeuronCluster(concept, cachedId, hierLoad, _storage.SaveClusterAsync);
                var evictionResult5 = _loadedClusters.Add(cachedId, clusterFromCache);
                _clusterAccessTimes[cachedId] = DateTime.UtcNow;
                if (evictionResult5.evicted && evictionResult5.key != default)
                {
                    await HandleClusterEvictionAsync(evictionResult5.key, evictionResult5.value!);
                }
                return clusterFromCache;
            }

            // Look for existing cluster with high relevance
            var relevantClusters = await FindRelevantClusters(new[] { concept });
            var bestCluster = relevantClusters.FirstOrDefault();
            if (bestCluster != null && bestCluster.CalculateRelevance(new[] { concept }) > ConceptSimilarityThreshold)
            {
                _conceptClusterCache[concept] = bestCluster.ClusterId;
                return bestCluster;
            }

            // Try storage metadata lookup for a stable cluster to avoid creating duplicates
            var similar = _storage.FindSimilarClusters(new[] { concept }, 0.5);
            var chosen = similar.FirstOrDefault();
            if (chosen != null)
            {
                // Use hierarchical loader that resolves by ClusterId via membership packs
                Func<string, Task<List<NeuronSnapshot>>> hierLoad = id =>
                    _storage.LoadClusterWithPartitioningAsync(id, new BrainContext
                    {
                        AllNeurons = new Dictionary<Guid, HybridNeuron>(),
                        AnalysisTime = DateTime.UtcNow
                    });
                var existing = new NeuronCluster(chosen.ConceptDomain, chosen.ClusterId, hierLoad, _storage.SaveClusterAsync);
                var evictionResult6 = _loadedClusters.Add(existing.ClusterId, existing);
                _clusterAccessTimes[existing.ClusterId] = DateTime.UtcNow;
                if (evictionResult6.evicted && evictionResult6.key != default)
                {
                    await HandleClusterEvictionAsync(evictionResult6.key, evictionResult6.value!);
                }
                _conceptClusterCache[concept] = existing.ClusterId;
                return existing;
            }

            // Create new cluster
            var newCluster = new NeuronCluster(concept, _storage.LoadClusterAsync, _storage.SaveClusterAsync);
            newCluster.ConceptLabel = concept; // Set the primary concept label for queryability
            var evictionResult = _loadedClusters.Add(newCluster.ClusterId, newCluster);
            _clusterAccessTimes[newCluster.ClusterId] = DateTime.UtcNow;
            if (evictionResult.evicted && evictionResult.key != default)
            {
                await HandleClusterEvictionAsync(evictionResult.key, evictionResult.value!);
            }
            _conceptClusterCache[concept] = newCluster.ClusterId;
            TotalClustersCreated++;
            return newCluster;
        }

        private async Task<List<NeuronCluster>> FindRelevantClusters(IEnumerable<string> concepts)
        {
            var allClusters = _loadedClusters.GetValues();

            // Seed with cached clusters for provided concepts
            foreach (var c in concepts)
            {
                if (_conceptClusterCache.TryGetValue(c, out var cid))
                {
                    if (!_loadedClusters.TryGetValue(cid, out var cached))
                    {
                        // Lazy load cached cluster by GUID
                        Func<string, Task<List<NeuronSnapshot>>> hierLoad = id =>
                            _storage.LoadClusterWithPartitioningAsync(id, new BrainContext
                            {
                                AllNeurons = new Dictionary<Guid, HybridNeuron>(),
                                AnalysisTime = DateTime.UtcNow
                            });
                        cached = new NeuronCluster(c, cid, hierLoad, _storage.SaveClusterAsync);
                        var evictionResult3 = _loadedClusters.Add(cid, cached);
                        _clusterAccessTimes[cid] = DateTime.UtcNow;
                        if (evictionResult3.evicted && evictionResult3.key != default)
                        {
                            await HandleClusterEvictionAsync(evictionResult3.key, evictionResult3.value!);
                        }
                    }
                    if (cached != null)
                    {
                        allClusters.Add(cached);
                    }
                }
            }

            // Use enhanced storage to find conceptually similar clusters
            var similarClusters = _storage.FindSimilarClusters(concepts, 0.5);
            // Load additional clusters if needed
            if (allClusters.Count < 3 && similarClusters.Any())
            {
                Func<string, Task<List<NeuronSnapshot>>> hierLoad = id =>
                    _storage.LoadClusterWithPartitioningAsync(id, new BrainContext
                    {
                        AllNeurons = new Dictionary<Guid, HybridNeuron>(),
                        AnalysisTime = DateTime.UtcNow
                    });
                foreach (var clusterRef in similarClusters.Take(5))
                {
                    if (!_loadedClusters.ContainsKey(clusterRef.ClusterId))
                    {
                        var cluster = new NeuronCluster(
                            clusterRef.ConceptDomain,
                            clusterRef.ClusterId,
                            hierLoad,
                            _storage.SaveClusterAsync);
                        var evictionResult = _loadedClusters.Add(cluster.ClusterId, cluster);
                        _clusterAccessTimes[cluster.ClusterId] = DateTime.UtcNow;
                        if (evictionResult.evicted && evictionResult.key != default)
                        {
                            await HandleClusterEvictionAsync(evictionResult.key, evictionResult.value!);
                        }
                        allClusters.Add(cluster);
                    }
                }
            }
            
            // TEMPORARILY DISABLED - Fallback to legacy cluster index search (incompatible format)
            // The LoadClusterIndexAsync returns Dictionary<string,object> but this code expects objects with AssociatedConcepts
            /*
            if (allClusters.Count < 3)
            {
                var clusterIndex = await _storage.LoadClusterIndexAsync();
                var conceptSet = concepts.Select(c => c.ToLowerInvariant()).ToHashSet();
                
                var relevantClusterSnapshots = clusterIndex
                    .Where(c => c.AssociatedConcepts.Any(ac => conceptSet.Contains(ac.ToLowerInvariant())))
                    .OrderByDescending(c => c.AverageImportance)
                    .Take(3)
                    .ToList();
                
                foreach (var snapshot in relevantClusterSnapshots)
                {
                    if (!_loadedClusters.ContainsKey(snapshot.ClusterId))
                    {
                        var cluster = new NeuronCluster(snapshot.ConceptDomain, _storage.LoadClusterAsync, _storage.SaveClusterAsync);
                        _loadedClusters[cluster.ClusterId] = cluster;
                        allClusters.Add(cluster);
                    }
                }
            }
            */
            
            return allClusters
                .Where(c => c != null)
                .OrderByDescending(c => c.CalculateRelevance(concepts))
                .ToList();
        }

        // Concept-identity features (REFOCUS P1.6j). The caller's `features` are a
        // SENTENCE fingerprint (length, word count, 3 booleans, first 5 chars) and
        // ProductionTrainingService passes the SAME dict for every word in the
        // sentence — so "cat" and "the" were trained on identical input and a
        // neuron's receptive field encoded the sentence, never the concept.
        // Symptom: a concept accumulated one 16-neuron cohort per sentence-context
        // and only the matching cohort fired, pinning the Hebbian pass rate at
        // ~22% with `passed` always a multiple of 16 (16, 32, 208 = 13x16).
        // Fix: the neuron's receptive field is built from the concept's own
        // encoding; sentence context is retained but down-weighted to modulation.
        private const int ConceptFeatureDims = 32;   // top-magnitude dims, keeps input sparse
        private const double ContextFeatureWeight = 0.25;

        // P1.6l: receptive-field overlap diagnostic
        private const int ReceptiveFieldSampleEvery = 4000;
        private int _receptiveFieldSampleCounter;

        /// <summary>
        /// P1.7: fraction of a concept's inputs any single neuron listens to.
        /// 0.2 of ~42 inputs ≈ 8 per neuron; with ~78 neurons per assembly each
        /// input is still covered ~15 times, so the assembly collectively sees
        /// everything while no two neurons see the same thing.
        /// </summary>
        private const double ReceptiveFieldDensity = 0.2;

        /// <summary>
        /// Deterministic membership test: does this neuron listen to this input?
        ///
        /// Derived purely from the neuron's identity, so a neuron's receptive-field
        /// SHAPE is procedurally regenerable and never needs persisting — only the
        /// learned weight values do. That is the thesis applied to the receptive
        /// field itself (see P3: ProceduralNeuronData can drop the key set and keep
        /// only learned deviations).
        ///
        /// FNV-1a over the neuron GUID and the feature key, then an avalanche mix
        /// so neighbouring keys ("cf_11_p"/"cf_12_p") don't correlate.
        /// </summary>
        private static bool NeuronSamplesFeature(Guid neuronId, string featureKey)
        {
            unchecked
            {
                const uint fnvOffset = 2166136261;
                const uint fnvPrime = 16777619;

                uint h = fnvOffset;
                Span<byte> guidBytes = stackalloc byte[16];
                neuronId.TryWriteBytes(guidBytes);
                foreach (var b in guidBytes) { h ^= b; h *= fnvPrime; }
                foreach (var c in featureKey) { h ^= (byte)c; h *= fnvPrime; h ^= (byte)(c >> 8); h *= fnvPrime; }

                // avalanche (murmur3 finalizer)
                h ^= h >> 16; h *= 0x85ebca6b;
                h ^= h >> 13; h *= 0xc2b2ae35;
                h ^= h >> 16;

                return h / (double)uint.MaxValue < ReceptiveFieldDensity;
            }
        }

        private void LogReceptiveFieldOverlap(string concept, List<HybridNeuron> neurons, Dictionary<string, double> inputs)
        {
            var inputIds = new HashSet<Guid>();
            foreach (var key in inputs.Keys)
                inputIds.Add(_featureMapper.GetNeuronIdForFeature(key));

            int noOverlap = 0, partial = 0, full = 0;
            int firing = 0;
            int pendingStm = 0;      // P2: neurons still carrying unconsolidated learning
            double sumCoverage = 0;
            var deltas = new List<double>(neurons.Count);

            foreach (var n in neurons)
            {
                int hits = 0;
                foreach (var id in inputIds)
                    if (n.InputWeights.ContainsKey(id)) hits++;

                var coverage = inputIds.Count == 0 ? 0 : (double)hits / inputIds.Count;
                sumCoverage += coverage;
                if (hits == 0) noOverlap++;
                else if (coverage > 0.95) full++;
                else partial++;

                var delta = n.CurrentPotential - n.RestingPotential;
                deltas.Add(delta);
                if (delta > 2.0) firing++;
                if (n.HasPendingStm) pendingStm++;
            }

            deltas.Sort();
            var median = deltas.Count > 0 ? deltas[deltas.Count / 2] : 0;
            var p10 = deltas.Count > 0 ? deltas[deltas.Count / 10] : 0;
            Console.WriteLine($"   🔬 RF[{concept}]: neurons={neurons.Count} inputs={inputIds.Count} " +
                              $"coverage[none={noOverlap} partial={partial} full={full} avg={sumCoverage / Math.Max(1, neurons.Count):P0}] " +
                              $"delta[p10={p10:F2} med={median:F2} max={(deltas.Count > 0 ? deltas[^1] : 0):F2}] " +
                              $"firing={firing}/{neurons.Count} pendingStm={pendingStm}");
        }

        private Dictionary<string, double> BuildTrainingFeatures(double[] conceptVector, Dictionary<string, double> contextFeatures)
        {
            var result = new Dictionary<string, double>(ConceptFeatureDims + contextFeatures.Count);

            // Concept identity: deterministic top-K dims by magnitude, so the same
            // word always drives the same input lines regardless of sentence.
            //
            // Rectified into ON/OFF channels (P1.6k). FeatureEncoder emits a
            // unit-norm vector with SIGNED components, but TrainNeuronWithFeatures
            // initializes weights positive (1.5-4.5) — an assumption inherited from
            // the old all-non-negative sentence features. Feeding signed values
            // through positive weights cancels: mean delta fell 4.1 -> 2.04 and
            // delta_min went negative (-6.84) for the first time.
            // Splitting each dim into a positive and a negative channel keeps all
            // inputs non-negative while preserving sign information. Only one
            // channel per dim is ever emitted, so sparsity is unchanged.
            var topDims = Enumerable.Range(0, conceptVector.Length)
                .OrderByDescending(i => Math.Abs(conceptVector[i]))
                .ThenBy(i => i)                       // stable tie-break
                .Take(ConceptFeatureDims);
            foreach (var dim in topDims)
            {
                var v = conceptVector[dim];
                if (v >= 0) result[$"cf_{dim}_p"] = v;
                else        result[$"cf_{dim}_n"] = -v;
            }

            // Context, down-weighted: modulates but does not define the field.
            foreach (var kv in contextFeatures)
            {
                result[$"ctx_{kv.Key}"] = kv.Value * ContextFeatureWeight;
            }

            return result;
        }

        /// <summary>
        /// Minimum cosine match for a neuron to count as recalled. Cosine is a
        /// genuine similarity, so this is now a meaningful bar rather than a
        /// threshold on an unbounded sum.
        /// </summary>
        private const double RecallMatchThreshold = 0.5;

        /// <summary>
        /// Minimum cosine match to count as co-active for Hebbian wiring. On the
        /// same [0,1] scale as recall, deliberately lower: wiring should tolerate
        /// weaker participation than recall asserts.
        /// </summary>
        private const double HebbianCoactivationThreshold = 0.3;

        // P2.1 competitive-learning constants (lateral inhibition)
        private const double CompetitiveWinnerFraction = 0.25;  // top quarter of the assembly learns
        private const int MinCompetitiveWinners = 4;
        private const double CompetitiveLearningRate = 0.05;    // Kohonen step toward the winning pattern

        /// <summary>
        /// Wire a neuron to any of this pattern's input lines it is missing, using
        /// its deterministic sparse subset. Split out of the old training method so
        /// wiring happens before the competition rather than as a side effect of it.
        /// </summary>
        private void EnsureFeatureWiring(HybridNeuron neuron, Dictionary<string, double> features)
        {
            foreach (var feature in features)
            {
                if (!NeuronSamplesFeature(neuron.Id, feature.Key)) continue;

                var featureNeuronId = _featureMapper.GetNeuronIdForFeature(feature.Key);
                if (!neuron.InputWeights.ContainsKey(featureNeuronId))
                {
                    neuron.InputWeights[featureNeuronId] =
                        (_random.NextDouble() + 0.5) * 3.0 / ReceptiveFieldDensity;
                }
            }
        }

        private async Task TrainNeuronWithFeatures(HybridNeuron neuron, Dictionary<string, double> features)
        {
            // Convert features to consistent neuron inputs
            var inputs = _featureMapper.ConvertFeaturesToNeuronInputs(features);

            // Wire up any feature input this neuron is missing.
            //
            // P1.6m — this used to be `if (!neuron.InputWeights.Any())`, which was
            // the bug behind the immovable ~22% Hebbian pass rate.
            // `InputWeights` holds TWO kinds of key: feature-input IDs (the
            // receptive field) and other neurons' IDs (synapses, written by
            // HybridNeuron.ConnectTo and restored by ProceduralNeuronRegenerator).
            // NeuronCluster.GrowForConcept connects each new neuron to 3 random
            // peers, so the dictionary was almost always non-empty — and the
            // neuron then NEVER received feature weights at all.
            // Measured consequence (LogReceptiveFieldOverlap, every sample):
            //   coverage[none=62 partial=0 full=16] — binary, and exactly one
            //   16-neuron cohort per concept had a receptive field. Everything
            //   else was dead weight that could never fire, which is why the pass
            //   rate ignored clustering, concept features, and rectification alike.
            foreach (var feature in features)
            {
                // P1.7: each neuron listens to a deterministic SPARSE SUBSET of the
                // concept's inputs. Wiring every neuron to every input made all
                // neurons in an assembly functionally identical (100% fired, median
                // delta within 2 of max) — one neuron replicated N times, no
                // distributed code, and N copies of the same receptive field on disk.
                if (!NeuronSamplesFeature(neuron.Id, feature.Key)) continue;

                var featureNeuronId = _featureMapper.GetNeuronIdForFeature(feature.Key);
                if (!neuron.InputWeights.ContainsKey(featureNeuronId))
                {
                    // Scale by 1/density so EXPECTED activation is unchanged
                    // (~17 above resting) while the variance across neurons is now
                    // real. Keeps thresholds, the tanh(delta/20) gate and decay
                    // calibration untouched; the only thing that changed is which
                    // inputs a given neuron can see.
                    neuron.InputWeights[featureNeuronId] =
                        (_random.NextDouble() + 0.5) * 3.0 / ReceptiveFieldDensity;
                }
            }
            
            // Process inputs and get output
            var output = neuron.ProcessInputs(inputs);
            
            // Train regardless of output (supervised learning)
            foreach (var feature in features)
            {
                var featureNeuronId = _featureMapper.GetNeuronIdForFeature(feature.Key);
                // Use a target activation of 0.8 for concept learning
                neuron.Learn(featureNeuronId, feature.Value, 0.8, output);
            }
            
            await Task.CompletedTask;
        }

        private async Task CreateConceptualConnections(string concept, Dictionary<string, double> features)
        {
            // Find related concepts and create synaptic connections
            var relatedClusters = await FindRelevantClusters(features.Keys);
            
            foreach (var cluster in relatedClusters.Take(2))
            {
                var clusterNeurons = await cluster.GetNeuronsAsync();
                var conceptCluster = await FindOrCreateClusterForConcept(concept);
                var conceptNeurons = await conceptCluster.GetNeuronsAsync();
                
                // Create a few random connections
                for (int i = 0; i < Math.Min(3, Math.Min(clusterNeurons.Count, conceptNeurons.Count)); i++)
                {
                    var sourceNeuron = clusterNeurons.Values.ElementAt(_random.Next(clusterNeurons.Count));
                    var targetNeuron = conceptNeurons.Values.ElementAt(_random.Next(conceptNeurons.Count));
                    
                    var synapse = new Synapse(sourceNeuron.Id, targetNeuron.Id, _random.NextDouble() * 0.2 - 0.1);
                    _synapses[synapse.Id] = synapse;
                }
            }
        }

        private Dictionary<Guid, double> ConvertFeaturesToNeuronInputs(Dictionary<string, double> features)
        {
            return _featureMapper.ConvertFeaturesToNeuronInputs(features);
        }

        /// <summary>
        /// Phase 1: Load EXISTING trained neurons for a concept (don't create new ones)
        /// This enables synaptic propagation by ensuring queries use the same neurons as training
        /// </summary>
        private async Task<(Dictionary<Guid, double> neurons, List<(NeuronCluster cluster, double score)> clusters)> 
            LoadTrainedNeuronsForConcept(string concept)
        {
            var activatedNeurons = new Dictionary<Guid, double>();
            var relevantClusters = new List<(NeuronCluster, double)>();
            
            // Encode concept to feature vector
            var featureVector = _featureEncoder.Encode(concept);
            
            // Get region ID for this pattern (VQ code or LSH hash)
            var regionId = GetRegionId(featureVector);
            
            DebugLog.Debug($"   🔍 LoadTrainedNeuronsForConcept('{concept}'): regionId={regionId}, total mappings={_regionToClusterMapping.Count}");
            
            // Find clusters that were TRAINED on patterns in this region
            var clusterIds = _regionToClusterMapping.GetValueOrDefault(regionId, new List<Guid>());
            
            DebugLog.Debug($"   🔍 Region {regionId}: found {clusterIds.Count} trained clusters");
            
            if (!clusterIds.Any())
            {
                // No trained clusters for this region - truly novel pattern
                return (activatedNeurons, relevantClusters);
            }
            
            // Load neurons from trained clusters (lazy loading)
            foreach (var clusterId in clusterIds.Take(3)) // Top 3 clusters per concept
            {
                // Try to get cluster from loaded cache
                if (!_loadedClusters.TryGetValue(clusterId, out var cluster))
                {
                    // Cluster not in memory - load it from storage using hierarchical partitioning
                    try
                    {
                        // Create hierarchical loader function (same pattern used in InitializeAsync)
                        Func<string, Task<List<NeuronSnapshot>>> hierLoad = id =>
                            _storage.LoadClusterWithPartitioningAsync(id, new BrainContext
                            {
                                AllNeurons = new Dictionary<Guid, HybridNeuron>(),
                                AnalysisTime = DateTime.UtcNow
                            });
                        
                        // Reconstruct NeuronCluster with lazy loading capability
                        cluster = new NeuronCluster($"pattern_{clusterId}", clusterId, hierLoad, _storage.SaveClusterAsync);
                        
                        // CRITICAL: Restore centroid from persisted metadata for pattern matching
                        var metadata = _storage.GetClusterMetadata(clusterId);
                        if (metadata?.Centroid != null && metadata.Centroid.Length > 0)
                        {
                            // Restore centroid directly without recalculation
                            cluster.RestoreCentroid(metadata.Centroid, metadata.CentroidNeuronCount);
                        }
                        
                        // Restore concept label for queryability
                        if (!string.IsNullOrEmpty(metadata?.ConceptLabel))
                        {
                            cluster.ConceptLabel = metadata.ConceptLabel;
                            _conceptClusterCache[metadata.ConceptLabel] = clusterId;
                        }
                        
                        // Cache loaded cluster
                        var evictionResult = _loadedClusters.Add(clusterId, cluster);
                        _clusterAccessTimes[clusterId] = DateTime.UtcNow;
                        if (evictionResult.evicted && evictionResult.key != default)
                        {
                            await HandleClusterEvictionAsync(evictionResult.key, evictionResult.value!);
                        }
                        
                        if ((_configForLogging?.Verbosity ?? 0) >= 2)
                        {
                            Console.WriteLine($"💾 Loaded cluster {clusterId:N} from storage for query processing");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Cluster could not be loaded - skip it
                        if ((_configForLogging?.Verbosity ?? 0) >= 1)
                        {
                            Console.WriteLine($"⚠️ Failed to load cluster {clusterId:N}: {ex.Message}");
                        }
                        continue;
                    }
                }
                
                // Get EXISTING neurons in this cluster (don't create new ones)
                if (cluster == null) continue;
                var neurons = await cluster.GetNeuronsAsync();
                
                if (neurons.Count == 0) continue;
                
                // Calculate activation for each neuron based on feature similarity
                double clusterActivationSum = 0;
                int neuronsActivated = 0;
                
                foreach (var kvp in neurons)
                {
                    var neuronId = kvp.Key;
                    var neuron = kvp.Value;
                    
                    // Activation = similarity between input features and neuron's learned features
                    // For now, use simple feature overlap (can be enhanced with attention later)
                    double activation = CalculateNeuronActivation(neuron, featureVector);
                    
                    if (activation > RecallMatchThreshold) // cosine match, not raw sum
                    {
                        activatedNeurons[neuronId] = activation;
                        clusterActivationSum += activation;
                        neuronsActivated++;
                        
                        // Limit neurons per cluster (sparse activation)
                        if (neuronsActivated >= 50)
                            break;
                    }
                }
                
                if (neuronsActivated > 0)
                {
                    var clusterScore = clusterActivationSum / neuronsActivated;
                    relevantClusters.Add((cluster, clusterScore));
                }
            }
            
            return (activatedNeurons, relevantClusters);
        }
        
        /// <summary>
        /// Calculate how strongly a neuron should activate given input features
        /// Uses cosine similarity between input and neuron's learned patterns
        /// </summary>
        /// <summary>
        /// Activation of a neuron given an input pattern.
        ///
        /// P2 FIX: this used to ignore `featureVector` entirely and return
        /// `0.3 + importance*0.5` — a function of the neuron alone. Consequences,
        /// all confirmed by the first fidelity run:
        ///   • every neuron in a cluster returned the same value for ANY cue;
        ///   • the nonsense controls "qwertyuiop"/"zxcvbnmasd" activated exactly
        ///     as strongly as "the" and "water" (top act 0.54/0.63);
        ///   • recall through this path was never pattern-based, which retroactively
        ///     invalidates the novelty-detection claims in
        ///     docs/SYNAPTIC_NOVELTY_DETECTION.md.
        ///
        /// Now uses the same neuron model as training: build the concept's input
        /// lines, run them through ProcessInputs, and report activation above
        /// resting on the same tanh(delta/20) scale as the Hebbian gate — so probe
        /// and training agree about what "active" means.
        ///
        /// Context features are deliberately omitted: a probe carries concept
        /// identity only, so recall is tested against the concept rather than
        /// against a remembered sentence.
        /// </summary>
        private double CalculateNeuronActivation(HybridNeuron neuron, double[] featureVector)
        {
            var probeFeatures = BuildTrainingFeatures(featureVector, EmptyContext);
            var inputs = _featureMapper.ConvertFeaturesToNeuronInputs(probeFeatures);

            // P2.1: match quality, not raw sum. The unnormalised weighted sum let a
            // partially-overlapping pattern saturate the neuron, which is how
            // "qwertyuiop" scored 0.993. Cosine asks how ALIGNED the input is with
            // what this neuron is tuned to.
            return neuron.MatchQuality(inputs);
        }

        private static readonly Dictionary<string, double> EmptyContext = new();

        /// <summary>
        /// Phase 2: Propagate activation through synaptic graph in cascading layers
        /// This is the biological "spreading activation" that distinguishes trained from novel patterns
        /// Trained pathways have strong synapses → deep cascade → many neurons
        /// Novel patterns have weak/no synapses → shallow cascade → few neurons
        /// </summary>
        private async Task<PropagationResult> PropagateActivationThroughSynapticGraph(
            Dictionary<Guid, double> seedNeurons,
            int maxDepth = 3)
        {
            const int EMERGENCY_BRAKE = 50000; // Safety limit
            const double PROPAGATION_DECAY = 0.9; // Activation decays each layer (higher = less decay)
            const double ACTIVATION_THRESHOLD = 0.01; // Minimum to continue propagating (lowered for small weights)
            
            var allActivations = new Dictionary<Guid, double>(seedNeurons);
            var currentLayer = new Dictionary<Guid, double>(seedNeurons);
            var layerSizes = new List<int> { seedNeurons.Count }; // Track neurons per layer
            var maxDepthReached = 0;
            
            Console.WriteLine($"🌊 Starting synaptic cascade from {seedNeurons.Count} seed neurons...");
            
            for (int depth = 1; depth <= maxDepth; depth++)
            {
                if (currentLayer.Count == 0) break;
                if (allActivations.Count >= EMERGENCY_BRAKE)
                {
                    if ((_configForLogging?.Verbosity ?? 0) >= 1)
                    {
                        Console.WriteLine($"⚠️ Emergency brake: {allActivations.Count} neurons activated");
                    }
                    break;
                }
                
                var nextLayer = new Dictionary<Guid, double>();
                
                int synapsesChecked = 0;
                int synapsesFound = 0;
                int neuronsWithSynapses = 0;
                
                // Propagate from each neuron in current layer through its synapses
                foreach (var (sourceNeuronId, sourceActivation) in currentLayer)
                {
                    if (sourceActivation < ACTIVATION_THRESHOLD) continue;
                    
                    synapsesChecked++;
                    
                    // CRITICAL FIX: Use _synapticGraph instead of _synapses
                    // The SparseSynapticGraph records Hebbian learning during training
                    var outgoingSynapses = _synapticGraph.GetOutgoingSynapses(sourceNeuronId);
                    
                    if (outgoingSynapses.Count > 0)
                    {
                        neuronsWithSynapses++;
                        synapsesFound += outgoingSynapses.Count;
                    }
                    
                    foreach (var (targetNeuronId, weight) in outgoingSynapses)
                    {
                        // Calculate propagated activation through this synapse
                        // Activation = source_activation * synapse_weight * decay
                        var propagatedActivation = sourceActivation * weight * PROPAGATION_DECAY;
                        
                        if (propagatedActivation < ACTIVATION_THRESHOLD) continue;
                        
                        // Dendritic integration: sum activations from multiple sources
                        if (nextLayer.ContainsKey(targetNeuronId))
                        {
                            // Multiple synapses converging - integrate (sum with saturation)
                            nextLayer[targetNeuronId] = Math.Min(1.0, 
                                nextLayer[targetNeuronId] + propagatedActivation * 0.5);
                        }
                        else if (!allActivations.ContainsKey(targetNeuronId))
                        {
                            // New neuron activated in this layer
                            nextLayer[targetNeuronId] = propagatedActivation;
                        }
                        else
                        {
                            // Already activated in previous layer - boost it
                            allActivations[targetNeuronId] = Math.Min(1.0,
                                allActivations[targetNeuronId] + propagatedActivation * 0.3);
                        }
                    }
                }
                
                // Add next layer neurons to total activations
                foreach (var (neuronId, activation) in nextLayer)
                {
                    allActivations[neuronId] = activation;
                }
                
                // Track cascade metrics
                layerSizes.Add(nextLayer.Count);
                if (nextLayer.Count > 0)
                {
                    maxDepthReached = depth;
                }
                
                Console.WriteLine($"   Layer {depth}: checked {synapsesChecked} neurons, {neuronsWithSynapses} had synapses ({synapsesFound} total), activated {nextLayer.Count} new neurons");
                
                // Set up for next iteration
                currentLayer = nextLayer;
            }
            
            Console.WriteLine($"🎯 Cascade complete: {seedNeurons.Count} seed → {allActivations.Count} total neurons (max depth: {maxDepthReached})\n");
            
            return new PropagationResult
            {
                AllActivations = allActivations,
                MaxDepthReached = maxDepthReached,
                LayerSizes = layerSizes
            };
        }

        /// <summary>
        /// Phase 3: Calculate novelty score from cascade metrics
        /// Biological principle: Familiar = deep cascade through trained paths, Novel = shallow/no cascade
        /// </summary>
        private double CalculateNoveltyFromCascade(
            int seedCount,
            int totalActivated,
            int maxDepth,
            List<int> layerGrowth)
        {
            // Metric 1: Cascade growth ratio (how much did it spread?)
            var growthRatio = seedCount > 0 ? (totalActivated - seedCount) / (double)seedCount : 0.0;
            
            // Metric 2: Cascade depth (how many layers?)
            var depthNormalized = maxDepth / 3.0; // Normalize to 0-1 (max depth = 3)
            
            // Metric 3: Average layer growth
            var avgLayerGrowth = layerGrowth.Count > 1
                ? layerGrowth.Skip(1).Average() / Math.Max(1, layerGrowth[0])
                : 0.0;
            
            // Combine metrics into novelty score
            // HIGH novelty (0.7-1.0) = no cascade, shallow depth, low growth
            // LOW novelty (0.0-0.3) = deep cascade, many layers, high growth
            var familiarityScore = (growthRatio * 0.4) + (depthNormalized * 0.4) + (avgLayerGrowth * 0.2);
            var noveltyScore = Math.Max(0.0, Math.Min(1.0, 1.0 - familiarityScore)); // Invert
            
            Console.WriteLine($"📊 Novelty Analysis:");
            Console.WriteLine($"   Seeds: {seedCount}, Total activated: {totalActivated}, New neurons: {totalActivated - seedCount}");
            Console.WriteLine($"   Growth ratio: {growthRatio:F2} (weight 0.4)");
            Console.WriteLine($"   Depth: {maxDepth}/3 layers = {depthNormalized:F2} normalized (weight 0.4)");
            Console.WriteLine($"   Avg layer growth: {avgLayerGrowth:F2} (weight 0.2)");
            Console.WriteLine($"   → Familiarity score: {familiarityScore:F2}");
            Console.WriteLine($"   → Novelty score: {noveltyScore:F2} (0=familiar, 1=novel)\n");
            
            return noveltyScore;
        }

        /// <summary>
        /// Result of synaptic propagation cascade
        /// </summary>
        private class PropagationResult
        {
            public Dictionary<Guid, double> AllActivations { get; set; } = new();
            public int MaxDepthReached { get; set; }
            public List<int> LayerSizes { get; set; } = new();
        }

        private string[] ExtractConcepts(string input)
        {
            // Simple concept extraction - in practice would use NLP
            return input.ToLowerInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(word => word.Length > 2)
                .Distinct()
                .ToArray();
        }

        private string GenerateResponse(Dictionary<Guid, double> neuronOutputs, string[] concepts, List<NeuronCluster> activatedClusters, double novelty)
        {
            if (!neuronOutputs.Any())
                return "I don't recognize this.";
            
            var avgActivation = neuronOutputs.Values.Average();
            var maxActivation = neuronOutputs.Values.Max();
            var activationCount = neuronOutputs.Count;
            var conceptsList = string.Join(", ", concepts.Take(3));
            
            // Generate response based on novelty score (Phase 3)
            string responseText;
            if (novelty < 0.3)
            {
                // Familiar - strong cascade through trained pathways
                responseText = $"This is familiar to me. Related to: {conceptsList} ({activationCount} neurons, {activatedClusters.Count} clusters)";
            }
            else if (novelty > 0.7)
            {
                // Novel - little to no cascade
                responseText = $"This is completely novel - I have no trained associations for: {conceptsList} ({activationCount} neurons, {activatedClusters.Count} clusters)";
            }
            else
            {
                // Moderate - some cascade but not deep
                responseText = $"Moderate activation for this input. Related to: {conceptsList} ({activationCount} neurons, {activatedClusters.Count} clusters)";
            }
            
            return responseText;
        }

        private double CalculatePatternFamiliarity(Dictionary<Guid, double> neuronOutputs, string[] concepts, List<NeuronCluster> activatedClusters)
        {
            // Check HEBBIAN CO-ACTIVATION: trained neurons have synapses TO EACH OTHER
            // Trained patterns: neurons densely interconnected (learned together through STDP)
            // Garbage: neurons isolated/weakly connected (random feature overlap, never trained together)
            
            if (!activatedClusters.Any() || !neuronOutputs.Any())
                return 0.0;
            
            // Sample activated neurons and check their interconnectivity
            var sampledNeurons = neuronOutputs.Keys.Take(50).ToList(); // Sample 50 neurons
            int interconnections = 0;
            int totalChecked = 0;
            
            for (int i = 0; i < Math.Min(20, sampledNeurons.Count); i++)
            {
                var neuronA = sampledNeurons[i];
                var outgoing = _synapticGraph.GetOutgoingSynapses(neuronA);
                
                // Check if this neuron connects to OTHER activated neurons
                foreach (var (targetId, weight) in outgoing)
                {
                    if (sampledNeurons.Contains(targetId) && weight > 0.3)
                    {
                        interconnections++;
                    }
                }
                totalChecked++;
            }
            
            // Interconnection ratio: trained concepts have ~20-50% of neurons connected to each other
            // Garbage: <5% interconnection (isolated neurons)
            var interconnectionRatio = totalChecked > 0 ? (double)interconnections / totalChecked : 0.0;
            
            // Also check activation statistics
            var avgActivation = neuronOutputs.Values.Average();
            var maxActivation = neuronOutputs.Values.Max();
            var strongActivations = neuronOutputs.Values.Count(v => v > 0.5);
            var activationRatio = neuronOutputs.Count / (double)activatedClusters.Sum(c => c.NeuronCount);
            
            double familiarity = 0.0;
            
            // INTERCONNECTION (most important - proves neurons trained together)
            if (interconnectionRatio > 0.3) // >30% interconnected = trained
                familiarity += 0.6;
            else if (interconnectionRatio > 0.15) // >15% = moderate training
                familiarity += 0.4;
            else if (interconnectionRatio > 0.05) // >5% = weak training
                familiarity += 0.2;
            else
                familiarity -= 0.3; // <5% = garbage (isolated neurons)
            
            // MAX ACTIVATION (confident match)
            if (maxActivation > 0.7)
                familiarity += 0.2;
            else if (maxActivation < 0.4)
                familiarity -= 0.2;
            
            // ACTIVATION RATIO (focused vs diffuse)
            if (activationRatio < 0.08) // <8% = focused
                familiarity += 0.1;
            else if (activationRatio > 0.15) // >15% = too diffuse
                familiarity -= 0.1;
            
            Console.WriteLine($"   🔗 Co-activation analysis ({sampledNeurons.Count} neurons sampled):");
            Console.WriteLine($"      • Hebbian interconnections: {interconnections} ({interconnectionRatio*100:F1}%)");
            Console.WriteLine($"      • Avg activation: {avgActivation:F3} | Max: {maxActivation:F3}");
            Console.WriteLine($"      • Strong activations (>0.5): {strongActivations} ({100.0*strongActivations/neuronOutputs.Count:F1}%)");
            Console.WriteLine($"      • Activation focus: {activationRatio*100:F1}% of cluster neurons");
            Console.WriteLine($"      • Familiarity score: {Math.Clamp(familiarity, 0.0, 1.0):F3}");
            
            return Math.Clamp(familiarity, 0.0, 1.0);
        }

        private double CalculateConfidence(Dictionary<Guid, double> neuronOutputs, List<NeuronCluster> activatedClusters)
        {
            if (!neuronOutputs.Any()) return 0.0;
            
            var avgActivation = neuronOutputs.Values.Average();
            var maxActivation = neuronOutputs.Values.Max();
            
            // Simple confidence based on activation strength
            // (Novelty detection disabled - requires architecture changes to properly track trained patterns)
            return (avgActivation + maxActivation) / 2.0;
        }

        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays >= 1)
                return $"{timeSpan.Days}d {timeSpan.Hours}h";
            else if (timeSpan.TotalHours >= 1)
                return $"{timeSpan.Hours}h {timeSpan.Minutes}m";
            else
                return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
        }
        
        /// <summary>
        /// Calculate required neurons based on concept complexity
        /// EMERGENT, STOCHASTIC, BIOLOGICALLY-REALISTIC allocation
        /// 
        /// BIOLOGICAL BASIS:
        /// - Neural competition for resources (scarce cortical real estate)
        /// - Stochastic developmental processes (genetic + environmental noise)
        /// - Non-linear emergence from neural interactions
        /// - Context-dependent plasticity and adaptation
        /// - Dynamic resource allocation based on current brain state
        /// 
        /// COMPUTATIONAL SCALING:
        /// - CPU speed advantage: ~2,000,000x faster than biological neurons
        /// - Target: 50-5,000 neurons per concept (vs biological 1K-500K+)
        /// - Massive variability for emergence: ±200-500% variation allowed
        /// - Resource competition: concepts compete for finite neural pools
        /// </summary>
        private int CalculateRequiredNeurons(string concept, Dictionary<string, double> features)
        {
            // === BIOLOGICAL STOCHASTICITY & EMERGENCE ===
            var random = new Random(concept.GetHashCode() + DateTime.Now.Millisecond); // Concept-specific but variable seed
            
            // Base allocation with developmental variability (biological: ±50-200% variation)
            var baseNeurons = 50 + random.Next(-20, 80); // 30-130 baseline with stochastic variation
            var emergenceScore = 0.0;
            
            // === DYNAMIC NEURAL COMPETITION ===
            // Concepts compete for finite neural resources (biological reality)
            var totalNeuronsInUse = _loadedClusters.GetValues().Sum(c => c.NeuronCount);
            var resourcePressure = Math.Max(0.5, Math.Min(3.0, totalNeuronsInUse / 10000.0)); // Pressure increases with usage
            
            // === STOCHASTIC COMPLEXITY FACTORS ===
            
            // 1. Frequency with biological noise (high-frequency ≠ uniform allocation)
            var frequencyFactor = CalculateStochasticFrequency(concept, random);
            emergenceScore -= frequencyFactor; // More efficient, but with variability
            
            // 2. Feature interactions (non-linear, emergent)
            var featureEmergence = CalculateFeatureEmergence(features, random);
            emergenceScore += featureEmergence;
            
            // 3. Semantic network position (dynamic based on current brain state)
            var networkPosition = CalculateNetworkPosition(concept, random);
            emergenceScore += networkPosition;
            
            // 4. Developmental timing effects (concepts learned at different "ages" vary)
            var developmentalFactor = CalculateDevelopmentalVariation(concept, random);
            emergenceScore += developmentalFactor;
            
            // 5. Contextual plasticity (current brain state influences allocation)
            var contextualDemand = CalculateContextualDemand(concept, features, random);
            emergenceScore += contextualDemand;
            
            // 6. Stochastic gene expression simulation (biological: identical twins differ)
            var geneticVariation = (random.NextDouble() - 0.5) * 50.0; // ±25 neurons random variation
            emergenceScore += geneticVariation;
            
            // === NON-LINEAR EMERGENCE CALCULATION ===
            // Biological: neural allocation follows power laws, not linear scaling
            var powerLawExponent = 1.3 + (random.NextDouble() * 0.4); // 1.3-1.7 (biological range)
            var emergentComplexity = Math.Pow(Math.Abs(emergenceScore), powerLawExponent) * Math.Sign(emergenceScore);
            
            // Apply resource pressure (scarcity breeds efficiency and competition)
            var adjustedComplexity = emergentComplexity / resourcePressure;
            
            // === FINAL ALLOCATION WITH MASSIVE VARIABILITY ===
            var neuronsNeeded = (int)Math.Ceiling(baseNeurons + adjustedComplexity);
            
            // Biologically realistic range: 50-5,000 neurons (vs biological 1K-500K+)
            // MASSIVE VARIATION: ±200-500% possible (essential for emergence)
            // Simple concepts: 50-800 neurons (huge variation)
            // Complex concepts: 200-5,000 neurons (massive variation)
            // Competition effects: scarce resources create adaptive pressure
            return Math.Max(50, Math.Min(5000, neuronsNeeded));
        }

        /// <summary>
        /// STOCHASTIC FREQUENCY CALCULATION
        /// Biological: Even high-frequency words show massive individual variation
        /// </summary>
        private double CalculateStochasticFrequency(string concept, Random random)
        {
            // Base frequency categories with biological variation
            var tier1Words = new[] { "the", "of", "and", "a", "to", "in", "is", "you", "that", "it", "he", "was", "for", "on", "are", "as", "with", "his", "they", "I", "at", "be", "this", "have", "from", "or", "one", "had", "by", "word", "but", "not", "what", "all", "were", "we", "when", "your", "can", "said", "there", "each", "which", "she", "do", "how", "their", "if", "will", "up", "other", "about", "out", "many", "then", "them", "these", "so", "some", "her", "would", "make", "like", "into", "him", "time", "has", "two", "more", "go", "no", "way", "could", "my", "than", "first", "water", "been", "call", "who", "its", "now", "find", "long", "down", "day", "did", "get", "come", "made", "may", "part" };
            
            var baseEfficiency = 0.0;
            if (tier1Words.Contains(concept.ToLowerInvariant())) 
                baseEfficiency = 15.0; // High efficiency baseline
            else if (concept.Length <= 6) 
                baseEfficiency = 8.0;  // Medium efficiency
            else if (concept.Length <= 4) 
                baseEfficiency = 5.0;  // Some efficiency
            
            // BIOLOGICAL STOCHASTICITY: ±50-200% variation even for identical concepts
            var variationFactor = 0.5 + (random.NextDouble() * 1.5); // 0.5x to 2.0x multiplier
            var noiseAddition = (random.NextDouble() - 0.5) * 10.0; // ±5 neurons noise
            
            return (baseEfficiency * variationFactor) + noiseAddition;
        }

        /// <summary>
        /// EMERGENT FEATURE INTERACTIONS
        /// Biological: Features interact non-linearly, creating emergent complexity
        /// </summary>
        private double CalculateFeatureEmergence(Dictionary<string, double> features, Random random)
        {
            if (features.Count == 0) return random.NextDouble() * 20.0; // Base variability
            
            var emergence = 0.0;
            
            // 1. Non-linear feature interactions (biological: network effects)
            var featureProduct = 1.0;
            var featureSum = 0.0;
            foreach (var feature in features.Values.Take(8)) // Limit to prevent explosion
            {
                featureProduct *= (1.0 + feature * 0.1); // Multiplicative interactions
                featureSum += feature;
            }
            
            // Emergent complexity from feature interactions
            emergence += Math.Log(featureProduct) * 25.0; // Log scaling for realism
            emergence += featureSum * (2.0 + random.NextDouble() * 3.0); // Stochastic linear component
            
            // 2. Feature conflict/harmony effects
            var featureConflicts = CalculateFeatureConflicts(features, random);
            emergence += featureConflicts;
            
            // 3. Dimensional curse effects (high-dimensional spaces are weird)
            if (features.Count > 10)
                emergence += (features.Count - 10) * random.NextDouble() * 8.0;
            
            // 4. Stochastic resonance effects
            emergence += (random.NextDouble() - 0.5) * 40.0; // ±20 neurons random emergence
            
            return emergence;
        }

        /// <summary>
        /// DYNAMIC NETWORK POSITION
        /// Biological: Position in semantic network affects resource needs
        /// </summary>
        private double CalculateNetworkPosition(string concept, Random random)
        {
            var position = 0.0;
            
            // 1. Hub vs. peripheral concepts (biological: hubs need more resources)
            var hubness = concept.Length < 8 ? random.NextDouble() * 30.0 : random.NextDouble() * 15.0;
            position += hubness;
            
            // 2. Network density around this concept
            var localDensity = random.NextDouble() * 25.0; // Simulated local connectivity
            position += localDensity;
            
            // 3. Cross-domain bridging (concepts that bridge domains need more resources)
            var bridging = random.NextDouble() < 0.3 ? random.NextDouble() * 40.0 : 0.0;
            position += bridging;
            
            // 4. Dynamic network evolution effects
            position += (random.NextDouble() - 0.5) * 30.0; // Network is constantly changing
            
            return position;
        }

        /// <summary>
        /// DEVELOPMENTAL VARIATION
        /// Biological: When concepts are learned affects their neural representation
        /// </summary>
        private double CalculateDevelopmentalVariation(string concept, Random random)
        {
            var variation = 0.0;
            
            // 1. Critical period effects (earlier = more plastic, variable allocation)
            var earlyLearning = concept.Length <= 6 && !concept.Contains("_");
            if (earlyLearning)
                variation += random.NextDouble() * 60.0; // High plasticity, high variation
            else
                variation += random.NextDouble() * 25.0; // Later learning, more constrained
            
            // 2. Maturational constraints (simulated "age" effects)
            var maturationNoise = (random.NextDouble() - 0.5) * 35.0;
            variation += maturationNoise;
            
            // 3. Experience-dependent plasticity
            var experienceEffect = random.NextDouble() * 20.0;
            variation += experienceEffect;
            
            return variation;
        }

        /// <summary>
        /// CONTEXTUAL DEMAND
        /// Biological: Current brain state influences resource allocation
        /// </summary>
        private double CalculateContextualDemand(string concept, Dictionary<string, double> features, Random random)
        {
            var demand = 0.0;
            
            // 1. Current cognitive load (simulated)
            var cognitiveLoad = _loadedClusters.Count > 100 ? random.NextDouble() * 30.0 : random.NextDouble() * 15.0;
            demand += cognitiveLoad;
            
            // 2. Attention state simulation
            var attentionFocus = random.NextDouble() < 0.4 ? random.NextDouble() * 25.0 : random.NextDouble() * 10.0;
            demand += attentionFocus;
            
            // 3. Instinctual/motivational state effects
            var emotionalContext = features.ContainsKey("emotional") ? random.NextDouble() * 35.0 : random.NextDouble() * 10.0;
            demand += emotionalContext;
            
            // 4. Working memory pressure
            var workingMemoryPressure = (random.NextDouble() - 0.5) * 20.0;
            demand += workingMemoryPressure;
            
            return demand;
        }

        /// <summary>
        /// FEATURE CONFLICTS AND HARMONIES
        /// Biological: Conflicting features require more neural arbitration
        /// </summary>
        private double CalculateFeatureConflicts(Dictionary<string, double> features, Random random)
        {
            var conflicts = 0.0;
            
            // Known semantic conflicts (biology: require more neural arbitration)
            var conflictPairs = new[]
            {
                ("abstract", "concrete"),
                ("positive", "negative"), 
                ("simple", "complex"),
                ("rational", "emotional"),
                ("individual", "collective"),
                ("static", "dynamic")
            };
            
            foreach (var (feat1, feat2) in conflictPairs)
            {
                if (features.ContainsKey(feat1) && features.ContainsKey(feat2))
                {
                    var conflictStrength = features[feat1] * features[feat2];
                    conflicts += conflictStrength * random.NextDouble() * 15.0; // Conflicts need arbitration
                }
            }
            
            // Random semantic tensions
            conflicts += random.NextDouble() * 25.0;
            
            return conflicts;
        }

        private double CalculateMultiModalDemand(Dictionary<string, double> features)
        {
            double multiModalScore = 0.0;
            
            // Visual cortex involvement (objects, colors, spatial)
            var visualFeatures = new[] { "visual", "color", "shape", "spatial", "image", "bright", "dark", "visible", "appearance", "size", "form" };
            multiModalScore += visualFeatures.Count(vf => features.ContainsKey(vf)) * 2.0;
            
            // Auditory cortex involvement (sounds, music, speech)
            var auditoryFeatures = new[] { "sound", "music", "loud", "quiet", "noise", "voice", "audio", "acoustic", "hearing", "phonetic" };
            multiModalScore += auditoryFeatures.Count(af => features.ContainsKey(af)) * 2.0;
            
            // Motor cortex involvement (movement, action, manipulation)
            var motorFeatures = new[] { "movement", "action", "motor", "manipulation", "gesture", "physical_action", "body_part", "motion", "kinesthetic" };
            multiModalScore += motorFeatures.Count(mf => features.ContainsKey(mf)) * 2.5;
            
            // Somatosensory involvement (touch, texture, temperature)
            var somatosensoryFeatures = new[] { "touch", "texture", "temperature", "tactile", "soft", "hard", "smooth", "rough", "pressure", "sensation" };
            multiModalScore += somatosensoryFeatures.Count(sf => features.ContainsKey(sf)) * 1.8;
            
            // Olfactory/gustatory involvement (smell, taste)
            var chemicalFeatures = new[] { "smell", "taste", "flavor", "odor", "scent", "sweet", "bitter", "sour", "salty", "aromatic" };
            multiModalScore += chemicalFeatures.Count(cf => features.ContainsKey(cf)) * 1.5;
            
            // Cross-modal integration (concepts requiring multiple sensory modalities)
            var crossModalFeatures = new[] { "experience", "environment", "interaction", "perception", "sensation", "multi_sensory" };
            multiModalScore += crossModalFeatures.Count(cmf => features.ContainsKey(cmf)) * 3.0;
            
            return multiModalScore;
        }

        // ─── P2: regeneration-fidelity support ───────────────────────────────
        // The whole thesis in one measurement: can an evicted region, rebuilt
        // procedurally from VQ codes + persisted synapses, reproduce the
        // activation it had before eviction?

        /// <summary>
        /// Activation probe with NO side effects: no growth, no training, no
        /// Hebbian recording, no capacity adjustment. Returns the top-k neuron
        /// IDs a cue activates, which is the unit of comparison for P2.
        /// </summary>
        public async Task<List<(Guid neuronId, double activation)>> ProbeConceptAsync(string concept, int topK = 16)
        {
            var (activated, _) = await LoadTrainedNeuronsForConcept(concept);
            return activated
                .OrderByDescending(kvp => kvp.Value)
                .ThenBy(kvp => kvp.Key)          // deterministic tie-break
                .Take(topK)
                .Select(kvp => (kvp.Key, kvp.Value))
                .ToList();
        }

        /// <summary>
        /// Force every loaded cluster out of memory, persisting first. After this
        /// the next probe must rebuild from disk — which is exactly the path P2
        /// is testing. Returns the number of clusters evicted.
        /// </summary>
        public async Task<int> EvictAllClustersAsync()
        {
            var keys = _loadedClusters.GetKeys();
            int evicted = 0;
            foreach (var clusterId in keys)
            {
                if (_loadedClusters.TryGetValue(clusterId, out var cluster) && cluster != null)
                {
                    await HandleClusterEvictionAsync(clusterId, cluster);
                    _loadedClusters.Remove(clusterId);
                    evicted++;
                }
            }
            _loadedClusters.Clear();
            _clusterAccessTimes.Clear();
            return evicted;
        }

        public void AttachConfiguration(CerebroConfiguration config)
        {
            _configForLogging = config;
            _storage.MaxParallelSaves = config.MaxParallelSaves;
            _storage.CompressClusters = config.CompressClusters;
            DebugLog.Level = config.Verbosity; // gate high-volume diagnostics globally
        }

        /// <summary>
        /// Sample a few clusters to validate membership vs bank hydration.
        /// </summary>
        public async Task RunIntegritySamplerAsync(int sampleClusters = 5)
        {
            var sw = Stopwatch.StartNew();
            var clusters = _loadedClusters.GetValues().OrderBy(_ => _reportRand.Next()).Take(Math.Max(1, sampleClusters)).ToList();
            int ok = 0, bad = 0;
            foreach (var c in clusters)
            {
                if (c == null) continue;
                var neurons = await c.GetNeuronsAsync();
                var ctx = new BrainContext { AllNeurons = neurons, AnalysisTime = DateTime.UtcNow };
                var (m, h) = await _storage.InspectClusterMembershipAsync(c, ctx);
                if (m == h) ok++; else bad++;
            }
            Console.WriteLine($"🔎 Integrity sampler: OK={ok}, Mismatch={bad} in {sw.Elapsed.TotalSeconds:F2}s");
        }
        
        /// <summary>
    }

    // Result classes
    public class LearningResult
    {
        public string Concept { get; set; } = "";
        public Guid ClusterId { get; set; }
        public bool Success { get; set; }
        public int NeuronsCreated { get; set; }
        public int NeuronsInvolved { get; set; }
    }

    public class ProcessingResult
    {
        public string Input { get; set; } = "";
        public string Response { get; set; } = "";
        public List<Guid> ActivatedClusters { get; set; } = new();
        public int ActivatedNeurons { get; set; }
        public double Confidence { get; set; }
    }

    public class BrainStats
    {
        public int LoadedClusters { get; set; }
        public int TotalClusters { get; set; }
        public int TotalSynapses { get; set; }
        public int TotalNeuronsCreated { get; set; }
        public string StorageSizeFormatted { get; set; } = "";
        public string UptimeFormatted { get; set; } = "";
    }

    public class EnhancedBrainStats
    {
        public BrainStats BaseStats { get; set; } = new();
        public EnhancedStorageStats StorageStats { get; set; } = new();
        public double PartitionEfficiency { get; set; }
        public Dictionary<string, PartitionStats> TopPartitions { get; set; } = new();
    }

    public class CognitionStats
    {
        public bool IsConscious { get; set; } = false;
        public int CognitionIterations { get; set; } = 0;
        public DateTime LastThought { get; set; } = DateTime.UtcNow;
        public string CurrentFocus { get; set; } = "";
        public double WisdomSeeking { get; set; } = 0.0;
        public double UniversalCompassion { get; set; } = 0.0;
        public double CreativeContribution { get; set; } = 0.0;
        public double CooperativeSpirit { get; set; } = 0.0;
        public double BenevolentCuriosity { get; set; } = 0.0;
        public TimeSpan CognitionFrequency { get; set; } = TimeSpan.Zero;
        
        // Enhanced: Instinctual state information
        public string DominantEmotion { get; set; } = "";
        public double InstinctualBalance { get; set; } = 0.0;
        public double InstinctualClarity { get; set; } = 0.0;
        
        // Enhanced: Goal system information
        public int ActiveGoals { get; set; } = 0;
        public int CompletedGoals { get; set; } = 0;
        public double AverageGoalProgress { get; set; } = 0.0;
        
        // Provide formatted summaries expected by Program.cs
        public string EthicalState =>
            $"Wisdom {WisdomSeeking:P1} | Compassion {UniversalCompassion:P1} | Creative {CreativeContribution:P1} | Cooperative {CooperativeSpirit:P1} | Curiosity {BenevolentCuriosity:P1}";
        
        public string InstinctualStatus =>
            (string.IsNullOrWhiteSpace(DominantEmotion)
                ? $"Balance {InstinctualBalance:F2} | Clarity {InstinctualClarity:F2}"
                : $"{DominantEmotion} | Balance {InstinctualBalance:F2} | Clarity {InstinctualClarity:F2}");
        
        public string GoalStatus =>
            $"{ActiveGoals} active | {CompletedGoals} completed | Avg {AverageGoalProgress:P1}";
        
        public string Status => IsConscious ? "Awake & Processing" : "Dormant";
    }
}