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
        private readonly Dictionary<Guid, NeuronCluster> _loadedClusters = new();
        private readonly Dictionary<Guid, Synapse> _synapses = new();
        private readonly FeatureMapper _featureMapper = new();
        private readonly Random _random = new();
        private readonly ConceptDependencyGraph _dependencyGraph = new();
        private ContinuousProcessor? _continuousProcessor; // Add consciousness processor
        
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

        private bool ShouldSampleLog() => (_configForLogging?.Verbosity ?? 0) >= 2 && _reportRand.NextDouble() <= _reportingSampleRate;
        private void ReportSampler(string concept, int neuronsUsed, Guid clusterId)
        {
            if (ShouldSampleLog())
            {
                Console.WriteLine($"🎓 Learning concept: {concept}");
                Console.WriteLine($"✅ Learned '{concept}' using {neuronsUsed} neurons in cluster {clusterId:N}");
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
        // Per-run growth hit counters (simple frequency gating)
        private readonly Dictionary<string, int> _conceptGrowthHits = new(StringComparer.OrdinalIgnoreCase);

        // Growth controls (tunable)
        private int MaxAddPerConceptPerRun = 64;        // cap growth per concept per run
        private int GrowthHitThreshold = 3;             // require N hits before allowing growth

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
            var seed = StableHash(concept);
            var random = new Random(seed);
            var baseNeurons = 50 + random.Next(-20, 80);
            double emergenceScore = 0.0;

            var frequencyFactor = CalculateStochasticFrequency(concept, random);
            emergenceScore -= frequencyFactor;

            var featureEmergence = CalculateFeatureEmergence(features, random);
            emergenceScore += featureEmergence;

            var networkPosition = CalculateNetworkPosition(concept, random);
            emergenceScore += networkPosition;

            var developmentalFactor = CalculateDevelopmentalVariation(concept, random);
            emergenceScore += developmentalFactor;

            var contextualDemand = CalculateContextualDemand(concept, features, random);
            emergenceScore += contextualDemand;

            var geneticVariation = (random.NextDouble() - 0.5) * 50.0;
            emergenceScore += geneticVariation;

            var powerLawExponent = 1.3 + (random.NextDouble() * 0.4);
            var emergentComplexity = Math.Pow(Math.Abs(emergenceScore), powerLawExponent) * Math.Sign(emergenceScore);
            var adjustedComplexity = emergentComplexity; // no resource pressure factor in base target
            var neuronsNeeded = (int)Math.Ceiling(baseNeurons + adjustedComplexity);
            return Math.Max(MinConceptNeurons, Math.Min(MaxConceptNeurons, neuronsNeeded));
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

        public Cerebro(string storagePath = "brain_data")
        {
            _storage = new EnhancedBrainStorage(storagePath);
            _continuousProcessor = new ContinuousProcessor(this); // Initialize consciousness
        }

        /// <summary>
        /// Initialize the brain - load existing clusters and synapses
        /// </summary>
        public async Task InitializeAsync()
        {
            Console.WriteLine("🧠 Initializing Brain in Jar...");
            
            // Load feature mappings first
            var featureMappings = await _storage.LoadFeatureMappingsAsync();
            _featureMapper.RestoreFromSnapshot(featureMappings);
            Console.WriteLine($"Loaded {featureMappings.FeatureMappings.Count} feature mappings");
            
            // Load synapses
            var synapseSnapshots = await _storage.LoadSynapsesAsync();
            foreach (var snapshot in synapseSnapshots)
            {
                _synapses[snapshot.Id] = Synapse.FromSnapshot(snapshot);
            }
            
            Console.WriteLine($"Loaded {_synapses.Count} synapses");
            
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

            // Reset per-run growth hits and cache
            _conceptGrowthHits.Clear();
            _conceptClusterCache.Clear();
        }

        /// <summary>
        /// Learn a new concept by creating or growing relevant clusters
        /// </summary>
        public async Task<LearningResult> LearnConceptAsync(string concept, Dictionary<string, double> features)
        {
            var result = new LearningResult { Concept = concept };

            // Growth gating counter
            var hits = _conceptGrowthHits.TryGetValue(concept, out var h) ? (h + 1) : 1;
            _conceptGrowthHits[concept] = hits;

            // Timing buckets
            var tAll = Stopwatch.StartNew();
            var tFind = Stopwatch.StartNew();

            // Find or create relevant cluster
            var cluster = await FindOrCreateClusterForConcept(concept);
            tFind.Stop();
            result.ClusterId = cluster.ClusterId;

            // Get neurons for this concept
            var tLookup = Stopwatch.StartNew();
            var conceptNeurons = await cluster.FindNeuronsByConcept(concept);
            tLookup.Stop();

            // Capacity and growth
            var tCapacity = Stopwatch.StartNew();
            var target = GetTargetNeuronsForConcept(concept, features);
            int grew = 0;

            // Apply growth gating and per-run cap
            if (conceptNeurons.Count < target)
            {
                var needed = target - conceptNeurons.Count;
                if (hits < GrowthHitThreshold)
                {
                    needed = 0; // defer growth until concept is seen enough times
                }
                else
                {
                    needed = Math.Min(needed, Math.Max(0, MaxAddPerConceptPerRun));
                }
                if (needed > 0)
                {
                    var tGrow = Stopwatch.StartNew();
                    var newNeurons = await cluster.GrowForConcept(concept, conceptNeurons.Count + needed);
                    tGrow.Stop();
                    // GrowForConcept(targetSize) returns created neurons; ensure we track only added
                    grew = newNeurons.Count;
                    conceptNeurons.AddRange(newNeurons);
                    TotalNeuronsCreated += grew;
                    if (ShouldSampleLog()) Console.WriteLine($"   ◽ grow(gated): +{grew} (target {target}, hits {hits}) in {tGrow.Elapsed.TotalMilliseconds:F1} ms");
                }
            }
            tCapacity.Stop();

            // Training pass
            var tTrain = Stopwatch.StartNew();
            foreach (var neuron in conceptNeurons)
            {
                await TrainNeuronWithFeatures(neuron, features);
            }
            tTrain.Stop();

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
            
            // Extract concepts from input
            var inputConcepts = ExtractConcepts(input);
            
            // Find relevant clusters
            var relevantClusters = await FindRelevantClusters(inputConcepts);
            
            foreach (var cluster in relevantClusters.Take(5)) // Limit to top 5 clusters
            {
                var clusterOutputs = await cluster.ProcessInputAsync(ConvertFeaturesToNeuronInputs(features));
                
                foreach (var output in clusterOutputs)
                {
                    neuronOutputs[output.Key] = output.Value;
                }
                
                activatedClusters.Add(cluster.ClusterId);
            }
            
            // Generate response based on activated neurons
            result.Response = GenerateResponse(neuronOutputs, inputConcepts);
            result.ActivatedClusters = activatedClusters;
            result.ActivatedNeurons = neuronOutputs.Count;
            result.Confidence = CalculateConfidence(neuronOutputs);
            
            // Enhanced: Integrate emotional processing if consciousness is active
            // BUT avoid recursive loops for internal consciousness processing
            if (_continuousProcessor != null && _continuousProcessor.IsProcessing && 
                !IsInternalCognitionInput(input))
            {
                // Let the emotional processor analyze this experience
                var emotionalProcessor = _continuousProcessor.GetEmotionalProcessor();
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
            Console.WriteLine("💾 Saving brain state with enhanced partitioning...");
            var swTotal = Stopwatch.StartNew();

            // Create brain context for partitioning decisions
            var sw = Stopwatch.StartNew();
            var allNeurons = new Dictionary<Guid, HybridNeuron>();
            foreach (var cluster in _loadedClusters.Values)
            {
                var neurons = await cluster.GetNeuronsAsync();
                foreach (var neuron in neurons.Values)
                {
                    allNeurons[neuron.Id] = neuron;
                }
            }
            if ((_configForLogging?.Verbosity ?? 0) > 0)
                Console.WriteLine($"   ⏱️  Gathered neuron context in {sw.Elapsed.TotalSeconds:F2}s");
            
            var context = new BrainContext
            {
                AllNeurons = allNeurons,
                AnalysisTime = DateTime.UtcNow
            };

            // STM->LTM consolidation with collection
            sw.Restart();
            int totalPromoted = 0;
            int clustersTouched = 0;
            int budgetPerCluster = Math.Max(5, Math.Min(50, (_configForLogging?.MaxParallelSaves ?? 1) * 5));
            var changedByCluster = new Dictionary<Guid, List<HybridNeuron>>();
            foreach (var cluster in _loadedClusters.Values)
            {
                var changed = await cluster.ConsolidateStmCollectAsync(budgetPerCluster);
                if (changed.Count > 0)
                {
                    totalPromoted += changed.Count;
                    clustersTouched++;
                    changedByCluster[cluster.ClusterId] = changed;
                }
            }
            if ((_configForLogging?.Verbosity ?? 0) > 0)
                Console.WriteLine($"   🧠 Consolidation: promoted {totalPromoted} neurons across {clustersTouched} clusters in {sw.Elapsed.TotalSeconds:F2}s (budget/cluster={budgetPerCluster})");

            // Save feature mappings
            sw.Restart();
            var featureMappingSnapshot = _featureMapper.CreateSnapshot();
            await _storage.SaveFeatureMappingsAsync(featureMappingSnapshot);
            if ((_configForLogging?.Verbosity ?? 0) > 0)
                Console.WriteLine($"   ⏱️  Saved feature mappings in {sw.Elapsed.TotalSeconds:F2}s");
            
            // Persist changed neurons to neuron banks ONLY (batched by partition)
            sw.Restart();
            var changeTuples = changedByCluster.Select(kvp => (_loadedClusters[kvp.Key], kvp.Value.AsEnumerable()));
            await _storage.SaveNeuronBanksInBatchesAsync(changeTuples, context);
            var neuronsPersisted = changedByCluster.Sum(kvp => kvp.Value.Count);
            if ((_configForLogging?.Verbosity ?? 0) > 0)
                Console.WriteLine($"   💾 Persisted neuron banks in batches; ~{neuronsPersisted} neurons updated in {sw.Elapsed.TotalSeconds:F2}s");

            // Determine clusters requiring membership/metadata save
            var dirtyClusters = _loadedClusters.Values
                .Where(c => c.HasUnsavedChanges)
                .Distinct()
                .ToList();
            if ((_configForLogging?.Verbosity ?? 0) > 0)
                Console.WriteLine($"   🧮 Clusters: total={_loadedClusters.Count}, dirty={dirtyClusters.Count}, skipped={_loadedClusters.Count - dirtyClusters.Count}");

            // Save clusters (membership + metadata) with throttling
            sw.Restart();
            await _storage.SaveClustersEfficientlyAsync(dirtyClusters, context);
            if ((_configForLogging?.Verbosity ?? 0) > 0)
                Console.WriteLine($"   ⏱️  Saved {dirtyClusters.Count} clusters in {sw.Elapsed.TotalSeconds:F2}s (parallel={_storage.MaxParallelSaves}, gzip={_storage.CompressClusters})");
            
            // Save cluster index
            sw.Restart();
            var clusterSnapshots = _loadedClusters.Values.Select(c => c.CreateSnapshot()).ToList();
            await _storage.SaveClusterIndexAsync(clusterSnapshots);
            if ((_configForLogging?.Verbosity ?? 0) > 0)
                Console.WriteLine($"   ⏱️  Saved cluster index in {sw.Elapsed.TotalSeconds:F2}s");
            
            // Save synapses
            sw.Restart();
            var synapseSnapshots = _synapses.Values.Select(s => s.CreateSnapshot()).ToList();
            await _storage.SaveSynapsesAsync(synapseSnapshots);
            if ((_configForLogging?.Verbosity ?? 0) > 0)
                Console.WriteLine($"   ⏱️  Saved {synapseSnapshots.Count} synapses in {sw.Elapsed.TotalSeconds:F2}s");
            
            // Persist concept capacities at end of save
            try { await _storage.SaveConceptCapacitiesAsync(_conceptCapacities); } catch { /* best effort */ }
            if ((_configForLogging?.Verbosity ?? 0) > 0)
            {
                var m = _storage.GetAndResetLastSaveMetrics();
                Console.WriteLine($"   📈 Save metrics: clustersExamined={m.ClustersExamined}, changedMembership={m.ClustersChangedMembership}, packsWritten={m.MembershipPacksWritten}, packsSkipped={m.MembershipPacksSkipped}, bankPartitions={m.NeuronBankPartitions}, neuronsUpserted={m.NeuronsUpserted}");
                Console.WriteLine($"   ⏱️  Total save time {swTotal.Elapsed.TotalSeconds:F2}s");
            }
            Console.WriteLine("✅ Brain state saved with hierarchical partitioning");

            // Optional quick integrity sampler when verbose
            if ((_configForLogging?.Verbosity ?? 0) > 0)
            {
                try { await RunIntegritySamplerAsync(5); } catch { /* best effort */ }
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
            var clustersToUnload = _loadedClusters.Values
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
            if (_continuousProcessor != null)
            {
                await _continuousProcessor.StartCognitionAsync();
            }
        }

        /// <summary>
        /// Stop continuous consciousness processing
        /// </summary>
        public async Task SleepCognitionAsync()
        {
            if (_continuousProcessor != null)
            {
                await _continuousProcessor.StopCognitionAsync();
            }
        }

        /// <summary>
        /// Get consciousness status and statistics
        /// </summary>
        public CognitionStats GetCognitionStats()
        {
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
            var emotionalState = _continuousProcessor.CurrentEmotionalState;
            stats.DominantEmotion = emotionalState.DominantEmotion;
            stats.EmotionalBalance = emotionalState.EmotionalBalance;
            stats.EmotionalClarity = emotionalState.EmotionalClarity;

            // Add goal system information
            var goalStatus = _continuousProcessor.CurrentGoalStatus;
            stats.ActiveGoals = goalStatus.ActiveGoalCount;
            stats.CompletedGoals = goalStatus.CompletedGoalCount;
            stats.AverageGoalProgress = goalStatus.AverageProgress;

            // Formatted summaries are computed via read-only properties now; no direct assignment needed
            
            return stats;
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

        // Private helper methods

        private async Task<NeuronCluster> FindOrCreateClusterForConcept(string concept)
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
                _loadedClusters[cachedId] = clusterFromCache;
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
                _loadedClusters[existing.ClusterId] = existing;
                _conceptClusterCache[concept] = existing.ClusterId;
                return existing;
            }

            // Create new cluster
            var newCluster = new NeuronCluster(concept, _storage.LoadClusterAsync, _storage.SaveClusterAsync);
            _loadedClusters[newCluster.ClusterId] = newCluster;
            _conceptClusterCache[concept] = newCluster.ClusterId;
            TotalClustersCreated++;
            return newCluster;
        }

        private async Task<List<NeuronCluster>> FindRelevantClusters(IEnumerable<string> concepts)
        {
            var allClusters = new List<NeuronCluster>(_loadedClusters.Values);

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
                        _loadedClusters[cid] = cached;
                    }
                    allClusters.Add(cached);
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
                        _loadedClusters[cluster.ClusterId] = cluster;
                        allClusters.Add(cluster);
                    }
                }
            }
            
            // Fallback to legacy cluster index search if needed
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
            
            return allClusters
                .Where(c => c != null)
                .OrderByDescending(c => c.CalculateRelevance(concepts))
                .ToList();
        }

        private async Task TrainNeuronWithFeatures(HybridNeuron neuron, Dictionary<string, double> features)
        {
            // Convert features to consistent neuron inputs
            var inputs = _featureMapper.ConvertFeaturesToNeuronInputs(features);
            
            // Initialize connections if neuron has no weights
            if (!neuron.InputWeights.Any())
            {
                foreach (var feature in features)
                {
                    var featureNeuronId = _featureMapper.GetNeuronIdForFeature(feature.Key);
                    // Initialize with much larger weights for guaranteed activation
                    neuron.InputWeights[featureNeuronId] = (_random.NextDouble() + 0.5) * 3.0; // Range: 1.5 to 4.5
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

        private string[] ExtractConcepts(string input)
        {
            // Simple concept extraction - in practice would use NLP
            return input.ToLowerInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(word => word.Length > 2)
                .Distinct()
                .ToArray();
        }

        private string GenerateResponse(Dictionary<Guid, double> neuronOutputs, string[] concepts)
        {
            if (!neuronOutputs.Any())
                return "I need to learn more about this.";
            
            var avgActivation = neuronOutputs.Values.Average();
            var maxActivation = neuronOutputs.Values.Max();
            var activationCount = neuronOutputs.Count;
            
            // Build response based on activation patterns
            var responseBuilder = new List<string>();
            
            if (maxActivation > 0.7)
            {
                responseBuilder.Add($"I recognize this strongly!");
                responseBuilder.Add($"It relates to: {string.Join(", ", concepts.Take(3))}");
            }
            else if (maxActivation > 0.4)
            {
                responseBuilder.Add($"This seems familiar to me.");
                responseBuilder.Add($"I associate it with: {string.Join(", ", concepts.Take(2))}");
            }
            else if (avgActivation > 0.2)
            {
                responseBuilder.Add($"I have some knowledge about this.");
                responseBuilder.Add($"Possibly related to: {concepts.FirstOrDefault() ?? "unknown"}");
            }
            else
            {
                responseBuilder.Add("This is quite new to me.");
                responseBuilder.Add("I'm learning from this input...");
            }
            
            // Add activation details
            if (activationCount > 0)
            {
                responseBuilder.Add($"({activationCount} neurons activated)");
            }
            
            return string.Join(" ", responseBuilder);
        }

        private double CalculateConfidence(Dictionary<Guid, double> neuronOutputs)
        {
            if (!neuronOutputs.Any()) return 0.0;
            
            var avgActivation = neuronOutputs.Values.Average();
            var maxActivation = neuronOutputs.Values.Max();
            
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
            var totalNeuronsInUse = _loadedClusters.Values.Sum(c => c.NeuronCount);
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
            
            // 3. Emotional/motivational state effects
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

        public void AttachConfiguration(CerebroConfiguration config)
        {
            _configForLogging = config;
            _storage.MaxParallelSaves = config.MaxParallelSaves;
            _storage.CompressClusters = config.CompressClusters;
            // Optionally allow overrides via config env vars later
        }

        /// <summary>
        /// Sample a few clusters to validate membership vs bank hydration.
        /// </summary>
        public async Task RunIntegritySamplerAsync(int sampleClusters = 5)
        {
            var sw = Stopwatch.StartNew();
            var clusters = _loadedClusters.Values.OrderBy(_ => _reportRand.Next()).Take(Math.Max(1, sampleClusters)).ToList();
            int ok = 0, bad = 0;
            foreach (var c in clusters)
            {
                var neurons = await c.GetNeuronsAsync();
                var ctx = new BrainContext { AllNeurons = neurons, AnalysisTime = DateTime.UtcNow };
                var (m, h) = await _storage.InspectClusterMembershipAsync(c, ctx);
                if (m == h) ok++; else bad++;
            }
            Console.WriteLine($"🔎 Integrity sampler: OK={ok}, Mismatch={bad} in {sw.Elapsed.TotalSeconds:F2}s");
        }
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
        
        // Enhanced: Emotional state information
        public string DominantEmotion { get; set; } = "";
        public double EmotionalBalance { get; set; } = 0.0;
        public double EmotionalClarity { get; set; } = 0.0;
        
        // Enhanced: Goal system information
        public int ActiveGoals { get; set; } = 0;
        public int CompletedGoals { get; set; } = 0;
        public double AverageGoalProgress { get; set; } = 0.0;
        
        // Provide formatted summaries expected by Program.cs
        public string EthicalState =>
            $"Wisdom {WisdomSeeking:P1} | Compassion {UniversalCompassion:P1} | Creative {CreativeContribution:P1} | Cooperative {CooperativeSpirit:P1} | Curiosity {BenevolentCuriosity:P1}";
        
        public string EmotionalStatus =>
            (string.IsNullOrWhiteSpace(DominantEmotion)
                ? $"Balance {EmotionalBalance:F2} | Clarity {EmotionalClarity:F2}"
                : $"{DominantEmotion} | Balance {EmotionalBalance:F2} | Clarity {EmotionalClarity:F2}");
        
        public string GoalStatus =>
            $"{ActiveGoals} active | {CompletedGoals} completed | Avg {AverageGoalProgress:P1}";
        
        public string Status => IsConscious ? "Awake & Processing" : "Dormant";
    }
}