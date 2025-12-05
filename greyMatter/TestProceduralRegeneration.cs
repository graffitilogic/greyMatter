using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreyMatter.Core;

namespace GreyMatter
{
    /// <summary>
    /// Phase 6B: Test procedural neuron regeneration.
    /// Validates "No Man's Sky principle" - can we regenerate neurons from VQ codes?
    /// 
    /// Test procedure:
    /// 1. Train on sample data, creating neurons with VQ codes
    /// 2. Save neurons to full snapshots
    /// 3. Convert to compact procedural representations
    /// 4. Delete original neurons
    /// 5. Regenerate from procedural data
    /// 6. Compare activation patterns: original vs regenerated
    /// 
    /// Success: >95% pattern match accuracy
    /// </summary>
    public class TestProceduralRegeneration
    {
        public static async Task RunTest()
        {
            Console.WriteLine("🧪 Phase 6B: Procedural Neuron Regeneration Test");
            Console.WriteLine("=".PadRight(60, '='));
            Console.WriteLine();
            
            // Step 1: Initialize Cerebro with small dataset
            Console.WriteLine("Step 1: Training small dataset...");
            var config = new CerebroConfiguration
            {
                MaxClusters = 50,
                ClusterSizeRange = (5, 100),
                StoragePath = "/Volumes/jarvis/brainData/hierarchical",
                EnableHierarchicalStorage = true
            };
            
            var cerebro = new Cerebro(config);
            
            // Train on 20 sentences
            var trainingSentences = new[]
            {
                "the cat sat on the mat",
                "dogs are loyal animals",
                "birds can fly in the sky",
                "fish swim in the water",
                "the sun is bright and warm",
                "rain falls from the clouds",
                "trees grow tall and strong",
                "flowers bloom in spring",
                "winter brings cold and snow",
                "summer is hot and sunny",
                "apples are red or green",
                "bananas are yellow fruit",
                "carrots are orange vegetables",
                "bread is made from wheat",
                "milk comes from cows",
                "cheese is made from milk",
                "pizza is a popular food",
                "coffee keeps people awake",
                "tea is a soothing drink",
                "water is essential for life"
            };
            
            foreach (var sentence in trainingSentences)
            {
                await cerebro.LearnFromSentenceAsync(sentence);
            }
            
            Console.WriteLine($"✅ Trained on {trainingSentences.Length} sentences");
            Console.WriteLine();
            
            // Step 2: Run queries and capture original activation patterns
            Console.WriteLine("Step 2: Capturing original activation patterns...");
            var testQueries = new[] { "what do cats do?", "describe fruit", "talk about weather" };
            var originalPatterns = new Dictionary<string, Dictionary<Guid, double>>();
            
            foreach (var query in testQueries)
            {
                var result = await cerebro.ProcessInputAsync(query, new Dictionary<string, double>());
                var neuronActivations = new Dictionary<Guid, double>();
                
                // Extract neuron activations from clusters
                foreach (var clusterId in result.ActivatedClusters)
                {
                    var cluster = cerebro._loadedClusters.GetValueOrDefault(clusterId);
                    if (cluster != null)
                    {
                        var neurons = await cluster.GetNeuronsAsync();
                        foreach (var neuron in neurons.Values)
                        {
                            if (neuron.IsActive)
                            {
                                neuronActivations[neuron.Id] = neuron.CurrentPotential;
                            }
                        }
                    }
                }
                
                originalPatterns[query] = neuronActivations;
                Console.WriteLine($"   Query: \"{query}\" → {neuronActivations.Count} neurons activated");
            }
            
            Console.WriteLine($"✅ Captured {originalPatterns.Sum(p => p.Value.Count)} total activations");
            Console.WriteLine();
            
            // Step 3: Convert neurons to compact procedural representations
            Console.WriteLine("Step 3: Converting to procedural representations...");
            var compactNeurons = new List<ProceduralNeuronData>();
            var fullSizeBytes = 0L;
            var compactSizeBytes = 0L;
            
            foreach (var cluster in cerebro._loadedClusters.Values)
            {
                var neurons = await cluster.GetNeuronsAsync();
                foreach (var neuron in neurons.Values)
                {
                    // Create full snapshot (what we currently store)
                    var fullSnapshot = neuron.CreateSnapshot();
                    fullSizeBytes += EstimateSnapshotSize(fullSnapshot);
                    
                    // Extract VQ code for this neuron's pattern
                    // For now, use a simple hash-based code assignment
                    // In production, this would come from the actual VQ encoding during training
                    int vqCode = Math.Abs(neuron.ConceptTag.GetHashCode()) % 512;
                    
                    // Convert to compact representation
                    var compactData = ProceduralNeuronData.FromSnapshot(fullSnapshot, vqCode, cluster.ClusterId);
                    compactNeurons.Add(compactData);
                    compactSizeBytes += compactData.EstimatedBytes();
                }
            }
            
            double compressionRatio = (double)fullSizeBytes / compactSizeBytes;
            Console.WriteLine($"   Full storage: {fullSizeBytes:N0} bytes");
            Console.WriteLine($"   Compact storage: {compactSizeBytes:N0} bytes");
            Console.WriteLine($"   Compression ratio: {compressionRatio:F2}x");
            Console.WriteLine($"✅ Converted {compactNeurons.Count} neurons");
            Console.WriteLine();
            
            // Step 4: Delete original neurons and regenerate from procedural data
            Console.WriteLine("Step 4: Regenerating neurons from procedural data...");
            var regenerator = new ProceduralNeuronRegenerator(cerebro._quantizer, cerebro._featureEncoder);
            var regeneratedNeurons = new Dictionary<Guid, HybridNeuron>();
            
            foreach (var compactData in compactNeurons)
            {
                var regenerated = regenerator.RegenerateNeuron(compactData);
                regeneratedNeurons[regenerated.Id] = regenerated;
            }
            
            Console.WriteLine($"✅ Regenerated {regeneratedNeurons.Count} neurons");
            Console.WriteLine();
            
            // Step 5: Replace neurons in clusters with regenerated versions
            Console.WriteLine("Step 5: Replacing neurons with regenerated versions...");
            foreach (var cluster in cerebro._loadedClusters.Values)
            {
                var originalNeurons = await cluster.GetNeuronsAsync();
                var neuronIds = originalNeurons.Keys.ToList();
                
                // Remove originals
                foreach (var neuronId in neuronIds)
                {
                    await cluster.RemoveNeuronAsync(neuronId);
                }
                
                // Add regenerated
                foreach (var neuronId in neuronIds)
                {
                    if (regeneratedNeurons.TryGetValue(neuronId, out var regenerated))
                    {
                        await cluster.AddNeuronAsync(regenerated);
                    }
                }
            }
            
            Console.WriteLine("✅ Neurons replaced");
            Console.WriteLine();
            
            // Step 6: Re-run queries and compare activation patterns
            Console.WriteLine("Step 6: Validating regenerated activation patterns...");
            var regeneratedPatterns = new Dictionary<string, Dictionary<Guid, double>>();
            
            foreach (var query in testQueries)
            {
                var result = await cerebro.ProcessInputAsync(query, new Dictionary<string, double>());
                var neuronActivations = new Dictionary<Guid, double>();
                
                foreach (var clusterId in result.ActivatedClusters)
                {
                    var cluster = cerebro._loadedClusters.GetValueOrDefault(clusterId);
                    if (cluster != null)
                    {
                        var neurons = await cluster.GetNeuronsAsync();
                        foreach (var neuron in neurons.Values)
                        {
                            if (neuron.IsActive)
                            {
                                neuronActivations[neuron.Id] = neuron.CurrentPotential;
                            }
                        }
                    }
                }
                
                regeneratedPatterns[query] = neuronActivations;
            }
            
            // Step 7: Calculate pattern match accuracy
            Console.WriteLine();
            Console.WriteLine("📊 Regeneration Accuracy Analysis:");
            Console.WriteLine("-".PadRight(60, '-'));
            
            double totalAccuracy = 0.0;
            int queryCount = 0;
            
            foreach (var query in testQueries)
            {
                var original = originalPatterns[query];
                var regenerated = regeneratedPatterns[query];
                
                // Calculate Jaccard similarity: intersection / union
                var intersection = original.Keys.Intersect(regenerated.Keys).Count();
                var union = original.Keys.Union(regenerated.Keys).Count();
                var jaccardSimilarity = union > 0 ? (double)intersection / union : 0.0;
                
                // Calculate activation strength correlation
                var commonNeurons = original.Keys.Intersect(regenerated.Keys).ToList();
                double correlation = 0.0;
                if (commonNeurons.Count > 0)
                {
                    var strengthDiffs = commonNeurons.Select(id => 
                        Math.Abs(original[id] - regenerated[id])).ToList();
                    var avgDiff = strengthDiffs.Average();
                    correlation = 1.0 - Math.Min(1.0, avgDiff / 100.0); // Normalize
                }
                
                var accuracy = (jaccardSimilarity + correlation) / 2.0;
                totalAccuracy += accuracy;
                queryCount++;
                
                Console.WriteLine($"Query: \"{query}\"");
                Console.WriteLine($"   Original neurons: {original.Count}");
                Console.WriteLine($"   Regenerated neurons: {regenerated.Count}");
                Console.WriteLine($"   Overlap: {intersection}/{union} ({jaccardSimilarity:P1})");
                Console.WriteLine($"   Strength correlation: {correlation:P1}");
                Console.WriteLine($"   Accuracy: {accuracy:P1}");
                Console.WriteLine();
            }
            
            var overallAccuracy = totalAccuracy / queryCount;
            Console.WriteLine("=".PadRight(60, '='));
            Console.WriteLine($"📊 Overall Regeneration Accuracy: {overallAccuracy:P1}");
            Console.WriteLine($"📦 Compression Ratio: {compressionRatio:F2}x");
            Console.WriteLine();
            
            if (overallAccuracy >= 0.95)
            {
                Console.WriteLine("✅ SUCCESS: >95% accuracy achieved!");
                Console.WriteLine("   Procedural regeneration validated ✓");
            }
            else if (overallAccuracy >= 0.80)
            {
                Console.WriteLine("⚠️  PARTIAL: 80-95% accuracy");
                Console.WriteLine("   Regeneration works but needs tuning");
            }
            else
            {
                Console.WriteLine("❌ FAILED: <80% accuracy");
                Console.WriteLine("   Regeneration algorithm needs revision");
            }
            
            Console.WriteLine();
            Console.WriteLine("✅ Test complete!");
        }
        
        private static int EstimateSnapshotSize(NeuronSnapshot snapshot)
        {
            // Rough estimate of NeuronSnapshot size in bytes
            int baseSize = 100; // GUID, timestamps, primitives
            int conceptsSize = snapshot.AssociatedConcepts.Sum(c => c.Length * 2); // UTF-16
            int weightsSize = snapshot.InputWeights.Count * (16 + 8); // GUID + double
            return baseSize + conceptsSize + weightsSize;
        }
    }
}
