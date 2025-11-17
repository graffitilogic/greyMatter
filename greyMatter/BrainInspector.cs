using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;

namespace GreyMatter
{
    /// <summary>
    /// Simple brain inspection tool - shows what's actually stored
    /// </summary>
    public class BrainInspector
    {
        public static async Task Run(string[] args)
        {
            var brainPath = "/Volumes/jarvis/brainData";
            
            Console.WriteLine("════════════════════════════════════════════════════════════════════════════════");
            Console.WriteLine("BRAIN INSPECTOR - Direct Storage Analysis");
            Console.WriteLine("════════════════════════════════════════════════════════════════════════════════\n");
            
            // Check what's on disk
            Console.WriteLine($"📁 Brain storage path: {brainPath}\n");
            
            // Cluster index
            var clusterIndexPath = Path.Combine(brainPath, "hierarchical", "cluster_index.json");
            if (File.Exists(clusterIndexPath))
            {
                var json = await File.ReadAllTextAsync(clusterIndexPath);
                using var doc = JsonDocument.Parse(json);
                var clusters = doc.RootElement.EnumerateArray().ToList();
                
                Console.WriteLine($"📊 Found {clusters.Count:N0} clusters in storage\n");
                
                // Show top 20 by neuron count
                Console.WriteLine("Top 20 clusters by size:");
                Console.WriteLine("────────────────────────────────────────────────────────────────────────────────");
                
                var sortedClusters = clusters
                    .Select(c => new {
                        Domain = c.GetProperty("ConceptDomain").GetString() ?? "unknown",
                        NeuronCount = c.GetProperty("NeuronCount").GetInt32(),
                        Importance = c.GetProperty("AverageImportance").GetDouble(),
                        RegionId = c.TryGetProperty("PrimaryRegionId", out var r) ? r.GetString() : "N/A"
                    })
                    .OrderByDescending(c => c.NeuronCount)
                    .Take(20)
                    .ToList();
                
                int rank = 1;
                foreach (var cluster in sortedClusters)
                {
                    var regionDisplay = cluster.RegionId != null && cluster.RegionId.Length >= 8 
                        ? cluster.RegionId.Substring(0, 8) 
                        : (cluster.RegionId ?? "N/A");
                    
                    Console.WriteLine($"{rank,3}. {cluster.Domain,-25} Neurons: {cluster.NeuronCount,6}  " +
                                    $"Importance: {cluster.Importance:F4}  Region: {regionDisplay}");
                    rank++;
                }
                
                // Sample some random concepts
                Console.WriteLine($"\n📝 Sample of learned concepts (random 30):");
                Console.WriteLine("────────────────────────────────────────────────────────────────────────────────");
                
                var random = new Random();
                var samples = clusters
                    .OrderBy(x => random.Next())
                    .Take(30)
                    .Select(c => c.GetProperty("ConceptDomain").GetString())
                    .ToList();
                
                for (int i = 0; i < samples.Count; i++)
                {
                    Console.Write($"{samples[i],-15}");
                    if ((i + 1) % 5 == 0) Console.WriteLine();
                }
                Console.WriteLine();
                
                // Statistics by concept length
                var conceptStats = clusters
                    .GroupBy(c => 
                    {
                        var domain = c.GetProperty("ConceptDomain").GetString() ?? "";
                        var len = domain.Length;
                        if (len < 5) return "Short (1-4)";
                        if (len < 10) return "Medium (5-9)";
                        if (len < 15) return "Long (10-14)";
                        return "Very Long (15+)";
                    })
                    .Select(g => new { Category = g.Key, Count = g.Count() })
                    .OrderBy(x => x.Category)
                    .ToList();
                
                Console.WriteLine($"\n📈 Concept distribution by length:");
                Console.WriteLine("────────────────────────────────────────────────────────────────────────────────");
                foreach (var stat in conceptStats)
                {
                    Console.WriteLine($"  {stat.Category,-20} {stat.Count,6:N0} concepts");
                }
            }
            else
            {
                Console.WriteLine("❌ No cluster index found");
            }
            
            // Checkpoint info
            var checkpointsPath = Path.Combine(brainPath, "checkpoints");
            if (Directory.Exists(checkpointsPath))
            {
                var checkpoints = Directory.GetFiles(checkpointsPath, "checkpoint_*.json")
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .ToList();
                
                Console.WriteLine($"\n💾 Found {checkpoints.Count} checkpoints");
                if (checkpoints.Any())
                {
                    var latest = checkpoints.First();
                    var checkpointData = await File.ReadAllTextAsync(latest);
                    using var doc = JsonDocument.Parse(checkpointData);
                    var root = doc.RootElement;
                    
                    Console.WriteLine($"\n📍 Latest checkpoint: {Path.GetFileName(latest)}");
                    Console.WriteLine("────────────────────────────────────────────────────────────────────────────────");
                    Console.WriteLine($"  Timestamp:          {root.GetProperty("Timestamp").GetDateTime():yyyy-MM-dd HH:mm:ss}");
                    Console.WriteLine($"  Sentences Processed: {root.GetProperty("SentencesProcessed").GetInt64():N0}");
                    Console.WriteLine($"  Vocabulary Size:     {root.GetProperty("VocabularySize").GetInt32():N0}");
                    Console.WriteLine($"  Synapse Count:       {root.GetProperty("SynapseCount").GetInt32():N0}");
                    Console.WriteLine($"  Training Hours:      {root.GetProperty("TrainingHours").GetDouble():F2}");
                    Console.WriteLine($"  Memory Usage:        {root.GetProperty("MemoryUsageGB").GetDouble():F2} GB");
                }
            }
            
            // Storage breakdown
            Console.WriteLine($"\n💾 Storage breakdown:");
            Console.WriteLine("────────────────────────────────────────────────────────────────────────────────");
            
            var directories = new[] { "hierarchical", "checkpoints", "episodic_memory", "live", "metrics" };
            foreach (var dir in directories)
            {
                var dirPath = Path.Combine(brainPath, dir);
                if (Directory.Exists(dirPath))
                {
                    var size = GetDirectorySize(new DirectoryInfo(dirPath));
                    Console.WriteLine($"  {dir,-20} {FormatBytes(size),15}");
                }
            }
            
            Console.WriteLine("\n════════════════════════════════════════════════════════════════════════════════");
        }
        
        private static long GetDirectorySize(DirectoryInfo dir)
        {
            try
            {
                return dir.GetFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
            }
            catch
            {
                return 0;
            }
        }
        
        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:F2} {sizes[order]}";
        }
    }
}
