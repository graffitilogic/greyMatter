using greyMatter.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace greyMatter.Visualization
{
    /// <summary>
    /// FMRI-like visualization of brain activity
    /// Shows real-time cluster activation, neuron sharing, and memory usage
    /// </summary>
    public class BrainScanVisualizer
    {
        private readonly SimpleEphemeralBrain _brain;
        private readonly Dictionary<string, List<double>> _activationHistory;
        private readonly int _maxHistoryLength;

        public BrainScanVisualizer(SimpleEphemeralBrain brain, int maxHistoryLength = 20)
        {
            _brain = brain;
            _activationHistory = new Dictionary<string, List<double>>();
            _maxHistoryLength = maxHistoryLength;
        }

        /// <summary>
        /// Real-time brain scan display
        /// </summary>
        public void ShowRealTimeBrainScan()
        {
            Console.Clear();
            Console.WriteLine("🧠 === REAL-TIME BRAIN SCAN ===");
            Console.WriteLine("(FMRI-like visualization of neural cluster activity)\n");

            // Get current brain state (would need access to internal state)
            var activeClusters = GetCurrentActiveClusters();
            var totalNeurons = activeClusters.Sum(c => c.NeuronCount);
            var activeRegions = activeClusters.Count(c => c.ActivationLevel > 0.1);

            // Header stats
            Console.WriteLine($"📊 Brain Stats: {activeClusters.Count} clusters, {totalNeurons} neurons, {activeRegions} active regions");
            Console.WriteLine($"🧮 Memory: {totalNeurons} active neurons (efficient: O(active_concepts))");
            Console.WriteLine();

            // Activation heat map
            ShowActivationHeatMap(activeClusters);
            
            // Neuron sharing network
            ShowNeuronSharingNetwork(activeClusters);
            
            // Memory usage over time
            ShowMemoryUsageGraph();
            
            // Active concept list
            ShowActiveConceptsList(activeClusters);
        }

        /// <summary>
        /// Interactive brain query tool
        /// </summary>
        public void RunInteractiveBrainQuery()
        {
            Console.WriteLine("🔍 === Interactive Brain Query ===");
            Console.WriteLine("Ask: 'What's thinking about X?' or type 'quit' to exit\n");

            while (true)
            {
                Console.Write("Query > ");
                var input = Console.ReadLine();
                
                if (string.IsNullOrEmpty(input) || input.ToLower() == "quit")
                    break;
                
                ProcessBrainQuery(input);
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Learning session visualization
        /// </summary>
        public void VisualizeLearningSession(Action learningAction, string sessionName)
        {
            Console.WriteLine($"🎓 === Learning Session: {sessionName} ===");
            Console.WriteLine("Watch the brain activity as learning occurs...\n");

            // Capture before state
            var beforeClusters = GetCurrentActiveClusters();
            
            // Run learning with periodic updates
            learningAction();
            
            // Capture after state  
            var afterClusters = GetCurrentActiveClusters();
            
            // Show learning impact
            ShowLearningImpact(beforeClusters, afterClusters);
        }

        /// <summary>
        /// Concept network visualization
        /// </summary>
        public void ShowConceptNetwork(string[] focusConcepts = null)
        {
            Console.WriteLine("🕸️  === Concept Network ===");
            Console.WriteLine("Showing how concepts are interconnected through shared neurons\n");

            var clusters = GetCurrentActiveClusters();
            
            if (focusConcepts != null)
            {
                clusters = clusters.Where(c => focusConcepts.Contains(c.Concept)).ToList();
            }

            // Show network connections
            foreach (var cluster in clusters)
            {
                Console.WriteLine($"🧠 {cluster.Concept} ({cluster.NeuronCount} neurons)");
                
                var connections = GetConceptConnections(cluster.Concept, clusters);
                foreach (var connection in connections.Take(5))
                {
                    var strength = connection.SharedNeurons / (double)Math.Max(cluster.NeuronCount, connection.TargetNeuronCount);
                    var strengthBar = CreateStrengthBar(strength);
                    Console.WriteLine($"   ├─ {strengthBar} {connection.TargetConcept} ({connection.SharedNeurons} shared)");
                }
                
                if (connections.Count > 5)
                {
                    Console.WriteLine($"   └─ ... and {connections.Count - 5} more connections");
                }
                Console.WriteLine();
            }
        }

        // Private visualization methods
        private void ShowActivationHeatMap(List<ClusterInfo> clusters)
        {
            Console.WriteLine("🔥 Activation Heat Map:");
            
            var maxActivation = clusters.Any() ? clusters.Max(c => c.ActivationLevel) : 1.0;
            
            foreach (var cluster in clusters.OrderByDescending(c => c.ActivationLevel).Take(10))
            {
                var normalizedActivation = cluster.ActivationLevel / maxActivation;
                var heatBar = CreateHeatBar(normalizedActivation);
                var activationPercent = (cluster.ActivationLevel * 100);
                
                Console.WriteLine($"   {cluster.Concept,-15} {heatBar} {activationPercent:F0}%");
            }
            Console.WriteLine();
        }

        private void ShowNeuronSharingNetwork(List<ClusterInfo> clusters)
        {
            Console.WriteLine("🔗 Neuron Sharing Network:");
            
            var strongConnections = new List<(string, string, int)>();
            
            for (int i = 0; i < clusters.Count; i++)
            {
                for (int j = i + 1; j < clusters.Count; j++)
                {
                    var shared = CalculateSharedNeurons(clusters[i], clusters[j]);
                    if (shared > 5) // Only show strong connections
                    {
                        strongConnections.Add((clusters[i].Concept, clusters[j].Concept, shared));
                    }
                }
            }
            
            foreach (var connection in strongConnections.OrderByDescending(c => c.Item3).Take(8))
            {
                var connectionStrength = CreateConnectionStrength(connection.Item3);
                Console.WriteLine($"   {connection.Item1} {connectionStrength} {connection.Item2} ({connection.Item3} neurons)");
            }
            Console.WriteLine();
        }

        private void ShowMemoryUsageGraph()
        {
            Console.WriteLine("📈 Memory Usage Trend:");
            
            // Simulate memory usage over time (would be real data in full implementation)
            var usage = new[] { 45, 52, 48, 61, 58, 63, 67, 71, 69, 74 };
            var maxUsage = usage.Max();
            
            for (int i = 0; i < usage.Length; i++)
            {
                var normalizedUsage = (double)usage[i] / maxUsage;
                var bar = CreateUsageBar(normalizedUsage);
                Console.WriteLine($"   T-{9-i:D2} {bar} {usage[i]} neurons");
            }
            Console.WriteLine();
        }

        private void ShowActiveConceptsList(List<ClusterInfo> clusters)
        {
            Console.WriteLine("🎯 Currently Active Concepts:");
            
            var activeClusters = clusters.Where(c => c.ActivationLevel > 0.1).OrderByDescending(c => c.ActivationLevel);
            
            foreach (var cluster in activeClusters.Take(8))
            {
                var status = GetConceptStatus(cluster);
                Console.WriteLine($"   • {cluster.Concept} - {status}");
            }
            
            if (activeClusters.Count() > 8)
            {
                Console.WriteLine($"   ... and {activeClusters.Count() - 8} more active concepts");
            }
            Console.WriteLine();
        }

        private void ProcessBrainQuery(string query)
        {
            var queryLower = query.ToLower();
            
            if (queryLower.StartsWith("what's thinking about"))
            {
                var concept = ExtractConceptFromQuery(query);
                if (!string.IsNullOrEmpty(concept))
                {
                    ShowThinkingAbout(concept);
                }
                else
                {
                    Console.WriteLine("❓ Couldn't understand the concept. Try: 'What's thinking about apple?'");
                }
            }
            else if (queryLower.Contains("connections") || queryLower.Contains("related"))
            {
                var concept = ExtractConceptFromQuery(query);
                if (!string.IsNullOrEmpty(concept))
                {
                    ShowConceptConnections(concept);
                }
            }
            else if (queryLower.Contains("brain scan") || queryLower.Contains("activity"))
            {
                ShowRealTimeBrainScan();
            }
            else
            {
                Console.WriteLine("❓ Available queries:");
                Console.WriteLine("   • What's thinking about [concept]?");
                Console.WriteLine("   • Show connections for [concept]");
                Console.WriteLine("   • Show brain scan");
            }
        }

        private void ShowThinkingAbout(string concept)
        {
            Console.WriteLine($"🧠 What's thinking about '{concept}':");
            
            // Simulate brain recall
            var related = _brain.Recall(concept);
            
            if (related.Any())
            {
                Console.WriteLine("   Activated regions:");
                foreach (var rel in related.Take(5))
                {
                    Console.WriteLine($"   🔥 {rel}");
                }
                
                Console.WriteLine($"\n   Spreading activation to {related.Count} related concepts");
            }
            else
            {
                Console.WriteLine($"   ❌ No active neural patterns found for '{concept}'");
                Console.WriteLine("   💡 Try learning about this concept first");
            }
        }

        private void ShowConceptConnections(string concept)
        {
            Console.WriteLine($"🔗 Neural connections for '{concept}':");
            ShowConceptNetwork(new[] { concept });
        }

        private void ShowLearningImpact(List<ClusterInfo> before, List<ClusterInfo> after)
        {
            Console.WriteLine("📊 Learning Impact Analysis:");
            
            var newClusters = after.Count - before.Count;
            var totalNeuronsBefore = before.Sum(c => c.NeuronCount);
            var totalNeuronsAfter = after.Sum(c => c.NeuronCount);
            var newNeurons = totalNeuronsAfter - totalNeuronsBefore;
            
            Console.WriteLine($"   • New clusters formed: {newClusters}");
            Console.WriteLine($"   • New neurons allocated: {newNeurons}");
            Console.WriteLine($"   • Memory efficiency: {(double)after.Count / totalNeuronsAfter:F3} clusters/neuron");
            
            if (newClusters > 0)
            {
                Console.WriteLine("   • New concepts learned:");
                var newConcepts = after.Select(a => a.Concept).Except(before.Select(b => b.Concept));
                foreach (var concept in newConcepts.Take(5))
                {
                    Console.WriteLine($"     - {concept}");
                }
            }
        }

        // Helper methods for visualization
        private string CreateHeatBar(double intensity, int width = 20)
        {
            var filled = (int)(intensity * width);
            var empty = width - filled;
            
            var colors = new[] { "░", "▒", "▓", "█" };
            var colorIndex = Math.Min((int)(intensity * colors.Length), colors.Length - 1);
            
            return new string(colors[colorIndex][0], filled) + new string('░', empty);
        }

        private string CreateConnectionStrength(int sharedNeurons)
        {
            if (sharedNeurons > 50) return "═══";
            if (sharedNeurons > 20) return "═══";
            if (sharedNeurons > 10) return "───";
            return "┄┄┄";
        }

        private string CreateUsageBar(double usage, int width = 15)
        {
            var filled = (int)(usage * width);
            return "█".PadRight(filled) + "░".PadRight(width - filled);
        }

        private string CreateStrengthBar(double strength)
        {
            var bars = new[] { "▁", "▂", "▃", "▄", "▅", "▆", "▇", "█" };
            var index = Math.Min((int)(strength * bars.Length), bars.Length - 1);
            return bars[index].PadRight(3);
        }

        private string GetConceptStatus(ClusterInfo cluster)
        {
            if (cluster.ActivationLevel > 0.8) return "🔥 Highly Active";
            if (cluster.ActivationLevel > 0.5) return "⚡ Active";
            if (cluster.ActivationLevel > 0.2) return "💭 Thinking";
            return "💤 Resting";
        }

        private string ExtractConceptFromQuery(string query)
        {
            // Simple extraction - in real implementation would be more sophisticated
            var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return words.LastOrDefault()?.Trim('?', '.', '!');
        }

        // Mock data methods - would be replaced with real brain state access
        private List<ClusterInfo> GetCurrentActiveClusters()
        {
            // This would access real brain state
            return new List<ClusterInfo>
            {
                new ClusterInfo { Concept = "apple", ActivationLevel = 0.8, NeuronCount = 45 },
                new ClusterInfo { Concept = "red", ActivationLevel = 0.6, NeuronCount = 32 },
                new ClusterInfo { Concept = "fruit", ActivationLevel = 0.4, NeuronCount = 28 },
                new ClusterInfo { Concept = "sweet", ActivationLevel = 0.3, NeuronCount = 22 },
                new ClusterInfo { Concept = "tree", ActivationLevel = 0.2, NeuronCount = 38 }
            };
        }

        private List<ConceptConnection> GetConceptConnections(string concept, List<ClusterInfo> clusters)
        {
            // Mock implementation - would calculate real shared neurons
            return clusters.Where(c => c.Concept != concept)
                          .Select(c => new ConceptConnection 
                          { 
                              TargetConcept = c.Concept, 
                              SharedNeurons = new Random().Next(5, 25),
                              TargetNeuronCount = c.NeuronCount
                          })
                          .OrderByDescending(c => c.SharedNeurons)
                          .ToList();
        }

        private int CalculateSharedNeurons(ClusterInfo cluster1, ClusterInfo cluster2)
        {
            // Mock calculation - would use real neuron overlap
            return new Random(cluster1.Concept.GetHashCode() + cluster2.Concept.GetHashCode()).Next(0, 30);
        }
    }

    /// <summary>
    /// Cluster information for visualization
    /// </summary>
    public class ClusterInfo
    {
        public required string Concept { get; set; }
        public double ActivationLevel { get; set; }
        public int NeuronCount { get; set; }
    }

    /// <summary>
    /// Concept connection information
    /// </summary>
    public class ConceptConnection
    {
        public required string TargetConcept { get; set; }
        public int SharedNeurons { get; set; }
        public int TargetNeuronCount { get; set; }
    }
}
