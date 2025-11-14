using System;
using System.Linq;
using System.Threading.Tasks;
using GreyMatter.Core;

namespace GreyMatter
{
    /// <summary>
    /// Simple CLI for querying trained Cerebro brain
    /// </summary>
    public class CerebroQueryCLI
    {
        public static async Task Run(string[] args)
        {
            if (args.Length < 2)
            {
                ShowUsage();
                return;
            }

            var brainPath = "/Volumes/jarvis/brainData";
            Console.WriteLine($"🧠 Loading trained Cerebro from {brainPath}...\n");

            var cerebro = new Cerebro(brainPath);
            await cerebro.InitializeAsync();

            var command = args[1].ToLower();

            try
            {
                switch (command)
                {
                    case "stats":
                        await ShowStats(cerebro);
                        break;

                    case "think":
                    case "query":
                        if (args.Length < 3)
                        {
                            Console.WriteLine("❌ Usage: --cerebro-query think <concept>");
                            return;
                        }
                        await QueryConcept(cerebro, args[2]);
                        break;

                    case "clusters":
                        await ShowClusters(cerebro, args.Length > 2 ? int.Parse(args[2]) : 20);
                        break;

                    default:
                        Console.WriteLine($"❌ Unknown command: {command}");
                        ShowUsage();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine($"   {ex.StackTrace}");
            }
        }

        private static void ShowUsage()
        {
            Console.WriteLine(@"
════════════════════════════════════════════════════════════════════════════════
CEREBRO QUERY CLI - Query Trained Neural Network
════════════════════════════════════════════════════════════════════════════════

Usage: dotnet run -- --cerebro-query <command> [arguments]

Commands:

  stats                    Show brain statistics
    Example: dotnet run -- --cerebro-query stats

  think <concept>          Query what brain thinks about a concept
    Example: dotnet run -- --cerebro-query think cat
    Example: dotnet run -- --cerebro-query think pizza

  clusters [limit]         List learned concept clusters
    Example: dotnet run -- --cerebro-query clusters 50

════════════════════════════════════════════════════════════════════════════════
");
        }

        private static async Task ShowStats(Cerebro cerebro)
        {
            Console.WriteLine("📊 CEREBRO BRAIN STATISTICS");
            Console.WriteLine("════════════════════════════════════════════════════════════════════════════════\n");

            var stats = await cerebro.GetStatsAsync();
            
            Console.WriteLine($"  Total Neurons Created:      {stats.TotalNeuronsCreated:N0}");
            Console.WriteLine($"  Total Clusters:             {stats.TotalClusters:N0}");
            Console.WriteLine($"  Loaded Clusters:            {stats.LoadedClusters:N0}");
            Console.WriteLine($"  Total Synapses:             {stats.TotalSynapses:N0}");
            Console.WriteLine($"  Storage Size:               {stats.StorageSizeFormatted}");
            Console.WriteLine($"  Uptime:                     {stats.UptimeFormatted}");
            
            Console.WriteLine("\n════════════════════════════════════════════════════════════════════════════════\n");
        }

        private static async Task QueryConcept(Cerebro cerebro, string concept)
        {
            Console.WriteLine($"🔍 Querying concept: '{concept}'");
            Console.WriteLine("════════════════════════════════════════════════════════════════════════════════\n");

            // Create features for the concept (simple word features)
            var features = new Dictionary<string, double>
            {
                { $"word:{concept.ToLower()}", 1.0 },
                { "query_mode", 1.0 }
            };

            try
            {
                var result = await cerebro.ProcessInputAsync(concept, features);

                if (result.ActivatedNeurons == 0)
                {
                    Console.WriteLine($"⚠️  No neural response for '{concept}'");
                    Console.WriteLine("   (Concept may not have been learned yet)\n");
                    return;
                }

                Console.WriteLine($"✅ Neural response:");
                Console.WriteLine($"   Activated neurons:   {result.ActivatedNeurons:N0}");
                Console.WriteLine($"   Activated clusters:  {result.ActivatedClusters.Count}");
                Console.WriteLine($"   Confidence:          {result.Confidence:F2}");
                Console.WriteLine($"\n   Response: {result.Response}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error querying concept: {ex.Message}");
            }
            
            Console.WriteLine("\n════════════════════════════════════════════════════════════════════════════════\n");
        }

        private static async Task ShowClusters(Cerebro cerebro, int limit)
        {
            Console.WriteLine($"🗂️  TOP {limit} CONCEPT CLUSTERS");
            Console.WriteLine("════════════════════════════════════════════════════════════════════════════════\n");

            var stats = await cerebro.GetStatsAsync();
            Console.WriteLine($"Total clusters in brain: {stats.TotalClusters:N0}\n");

            // Load cluster index directly
            var clusterIndexPath = "/Volumes/jarvis/brainData/hierarchical/cluster_index.json";
            if (!File.Exists(clusterIndexPath))
            {
                Console.WriteLine("❌ Cluster index not found");
                return;
            }

            var jsonContent = File.ReadAllText(clusterIndexPath);
            var clusters = System.Text.Json.JsonSerializer.Deserialize<List<System.Text.Json.JsonElement>>(jsonContent);

            if (clusters == null)
            {
                Console.WriteLine("❌ Could not parse cluster index");
                return;
            }

            Console.WriteLine($"Showing top {limit} clusters by neuron count:\n");
            
            var sortedClusters = clusters
                .OrderByDescending(c => c.GetProperty("NeuronCount").GetInt32())
                .Take(limit)
                .ToList();

            int rank = 1;
            foreach (var cluster in sortedClusters)
            {
                var domain = cluster.GetProperty("ConceptDomain").GetString();
                var neuronCount = cluster.GetProperty("NeuronCount").GetInt32();
                var importance = cluster.GetProperty("AverageImportance").GetDouble();
                
                Console.WriteLine($"{rank,3}. {domain,-20}  Neurons: {neuronCount,5}  Importance: {importance:F4}");
                rank++;
            }
            
            Console.WriteLine($"\n💡 To query a concept: dotnet run -- --cerebro-query think <concept>");
            Console.WriteLine($"   Example: dotnet run -- --cerebro-query think {sortedClusters.FirstOrDefault().GetProperty("ConceptDomain").GetString()}");
            
            Console.WriteLine("\n════════════════════════════════════════════════════════════════════════════════\n");
        }
    }
}
