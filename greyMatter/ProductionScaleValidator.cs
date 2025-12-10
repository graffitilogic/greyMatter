using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GreyMatter.Core;

namespace GreyMatter
{
    public static class ProductionScaleValidator
    {
        public static async Task RunProductionScaleTest(string brainPath, int sentenceCount)
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  PRODUCTION SCALE TEST - Phase 6B Validation             ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════╝\n");
            
            Console.WriteLine($"📊 Test Parameters:");
            Console.WriteLine($"   Brain Path: {brainPath}");
            Console.WriteLine($"   Target Sentences: {sentenceCount}");
            Console.WriteLine($"   Procedural Save: ENABLED");
            Console.WriteLine();
            
            // Generate diverse training data
            var trainingData = GenerateProductionDataset(sentenceCount);
            Console.WriteLine($"✅ Generated {trainingData.Length} training sentences");
            Console.WriteLine($"   Unique concepts: {trainingData.Select(s => s.Split(' ')[0]).Distinct().Count()}");
            Console.WriteLine();
            
            // Create and configure brain
            var config = new CerebroConfiguration
            {
                BrainDataPath = brainPath,
                Verbosity = 1,
                UseProceduralSave = true,
                MaxParallelSaves = 2,
                CompressClusters = true
            };
            config.ValidateAndSetup();
            
            var cerebro = new Cerebro(brainPath);
            cerebro.AttachConfiguration(config);
            await cerebro.InitializeAsync();
            
            Console.WriteLine("🧠 Brain initialized with procedural save ENABLED\n");
            
            // Training phase
            Console.WriteLine($"📚 Phase 1: Training on {trainingData.Length} sentences...");
            var startTime = DateTime.UtcNow;
            int processed = 0;
            
            foreach (var sentence in trainingData)
            {
                var features = new Dictionary<string, double>();
                await cerebro.LearnConceptAsync(sentence, features);
                processed++;
                
                if (processed % 100 == 0)
                {
                    var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
                    var rate = processed / elapsed;
                    Console.WriteLine($"   Progress: {processed}/{trainingData.Length} ({processed * 100 / trainingData.Length}%) - {rate:F1} sentences/sec");
                }
            }
            
            var totalTime = (DateTime.UtcNow - startTime).TotalSeconds;
            Console.WriteLine($"✅ Training complete in {totalTime:F1}s ({trainingData.Length / totalTime:F1} sentences/sec)\n");
            
            // Get stats before save
            var stats = await cerebro.GetStatsAsync();
            Console.WriteLine($"📊 Brain Statistics:");
            Console.WriteLine($"   Neurons: {stats.TotalNeuronsCreated:N0}");
            Console.WriteLine($"   Clusters: {stats.TotalClusters}");
            Console.WriteLine($"   Synapses: {stats.TotalSynapses}");
            Console.WriteLine();
            
            // Save with procedural compression
            Console.WriteLine("💾 Phase 2: Saving brain state with PROCEDURAL COMPRESSION...");
            var saveStart = DateTime.UtcNow;
            await cerebro.SaveAsync();
            var saveTime = (DateTime.UtcNow - saveStart).TotalSeconds;
            Console.WriteLine($"✅ Save complete in {saveTime:F2}s\n");
            
            // Analyze storage
            Console.WriteLine("📦 Phase 3: Analyzing compression results...");
            AnalyzeStorage(brainPath);
            Console.WriteLine();
            
            // Test queries
            Console.WriteLine("🔍 Phase 4: Testing query performance...");
            var testQueries = new[]
            {
                "neural networks",
                "machine learning",
                "artificial intelligence",
                "deep learning",
                "pattern recognition"
            };
            
            var queryResults = new List<(string query, int neurons, double confidence)>();
            foreach (var query in testQueries)
            {
                var features = new Dictionary<string, double>();
                var result = await cerebro.ProcessInputAsync(query, features);
                queryResults.Add((query, result.ActivatedNeurons, result.Confidence));
                Console.WriteLine($"   '{query}': {result.ActivatedNeurons} neurons, confidence {result.Confidence:F3}");
            }
            Console.WriteLine();
            
            // Load test
            Console.WriteLine("🔄 Phase 5: Testing procedural load cycle...");
            var loadStart = DateTime.UtcNow;
            var loadedBrain = new Cerebro(brainPath);
            loadedBrain.AttachConfiguration(config);
            await loadedBrain.InitializeAsync();
            var loadTime = (DateTime.UtcNow - loadStart).TotalSeconds;
            Console.WriteLine($"✅ Brain loaded in {loadTime:F2}s\n");
            
            // Verify queries on loaded brain
            Console.WriteLine("✅ Phase 6: Verifying query accuracy after load...");
            int perfectMatches = 0;
            foreach (var (query, baselineNeurons, baselineConf) in queryResults)
            {
                var features = new Dictionary<string, double>();
                var result = await loadedBrain.ProcessInputAsync(query, features);
                var match = result.ActivatedNeurons == baselineNeurons && 
                           Math.Abs(result.Confidence - baselineConf) < 0.001;
                if (match) perfectMatches++;
                
                var status = match ? "✅" : "⚠️";
                Console.WriteLine($"   {status} '{query}': {result.ActivatedNeurons} vs {baselineNeurons} neurons");
            }
            
            var accuracy = (perfectMatches * 100.0) / queryResults.Count;
            Console.WriteLine($"\n🎯 Accuracy: {perfectMatches}/{queryResults.Count} perfect matches ({accuracy:F1}%)");
            Console.WriteLine();
            
            // Final summary
            Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  PRODUCTION SCALE TEST - RESULTS                          ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
            Console.WriteLine($"   Sentences trained: {trainingData.Length:N0}");
            Console.WriteLine($"   Neurons created: {stats.TotalNeuronsCreated:N0}");
            Console.WriteLine($"   Clusters created: {stats.TotalClusters}");
            Console.WriteLine($"   Training rate: {trainingData.Length / totalTime:F1} sentences/sec");
            Console.WriteLine($"   Save time: {saveTime:F2}s");
            Console.WriteLine($"   Load time: {loadTime:F2}s");
            Console.WriteLine($"   Query accuracy: {accuracy:F1}%");
            
            if (accuracy >= 95.0)
            {
                Console.WriteLine("\n✅ PRODUCTION TEST PASSED - Ready for deployment!");
            }
            else
            {
                Console.WriteLine("\n⚠️  PRODUCTION TEST NEEDS INVESTIGATION");
            }
            Console.WriteLine("═══════════════════════════════════════════════════════════\n");
        }
        
        private static void AnalyzeStorage(string brainPath)
        {
            var hierarchicalPath = Path.Combine(brainPath, "hierarchical");
            if (!Directory.Exists(hierarchicalPath))
            {
                Console.WriteLine("   ⚠️  Hierarchical storage not found");
                return;
            }
            
            var proceduralFiles = Directory.GetFiles(hierarchicalPath, "*procedural*.msgpack.gz", SearchOption.AllDirectories);
            var standardFiles = Directory.GetFiles(hierarchicalPath, "neurons.bank_*.msgpack.gz", SearchOption.AllDirectories)
                .Where(f => !f.Contains("procedural")).ToArray();
            
            long proceduralSize = proceduralFiles.Sum(f => new FileInfo(f).Length);
            long standardSize = standardFiles.Sum(f => new FileInfo(f).Length);
            
            Console.WriteLine($"   Procedural files: {proceduralFiles.Length} files, {FormatBytes(proceduralSize)}");
            Console.WriteLine($"   Standard files: {standardFiles.Length} files, {FormatBytes(standardSize)}");
            
            if (standardSize > 0)
            {
                var ratio = (double)standardSize / proceduralSize;
                var savings = standardSize - proceduralSize;
                Console.WriteLine($"   Compression ratio: {ratio:F2}x");
                Console.WriteLine($"   Space saved: {FormatBytes(savings)} ({savings * 100.0 / standardSize:F1}%)");
            }
            
            var totalSize = GetDirectorySize(hierarchicalPath);
            Console.WriteLine($"   Total storage: {FormatBytes(totalSize)}");
        }
        
        private static long GetDirectorySize(string path)
        {
            return Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                .Sum(f => new FileInfo(f).Length);
        }
        
        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:F2} {sizes[order]}";
        }
        
        private static string[] GenerateProductionDataset(int count)
        {
            var concepts = new[]
            {
                "neural networks", "machine learning", "deep learning", "artificial intelligence",
                "pattern recognition", "computer vision", "natural language processing",
                "reinforcement learning", "supervised learning", "unsupervised learning",
                "convolutional networks", "recurrent networks", "transformers", "attention mechanisms",
                "gradient descent", "backpropagation", "optimization algorithms",
                "data preprocessing", "feature engineering", "model evaluation",
                "overfitting", "underfitting", "regularization", "dropout",
                "batch normalization", "activation functions", "loss functions",
                "neural architecture", "hyperparameter tuning", "transfer learning",
                "ensemble methods", "decision trees", "random forests",
                "support vector machines", "k-means clustering", "dimensionality reduction",
                "principal component analysis", "autoencoders", "generative models",
                "gan networks", "variational autoencoders", "semantic segmentation",
                "object detection", "image classification", "sentiment analysis",
                "named entity recognition", "machine translation", "speech recognition",
                "time series analysis", "anomaly detection", "recommendation systems"
            };
            
            var templates = new[]
            {
                "{0} enable intelligent systems",
                "{0} process complex patterns",
                "{0} learn from data automatically",
                "{0} improve prediction accuracy",
                "{0} extract meaningful features",
                "{0} optimize model performance",
                "{0} handle large-scale datasets",
                "{0} generalize to new examples",
                "{0} minimize prediction errors",
                "{0} maximize classification accuracy"
            };
            
            var sentences = new List<string>();
            var random = new Random(42);
            
            for (int i = 0; i < count; i++)
            {
                var concept = concepts[random.Next(concepts.Length)];
                var template = templates[random.Next(templates.Length)];
                sentences.Add(string.Format(template, concept));
            }
            
            return sentences.ToArray();
        }
    }
}
