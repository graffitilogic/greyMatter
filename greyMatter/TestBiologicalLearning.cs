using System;
using System.Linq;
using greyMatter.Core;
using GreyMatter.Storage;

namespace GreyMatter.Tests
{
    /// <summary>
    /// Simple test to verify biological learning with neuron connections works
    /// </summary>
    public class TestBiologicalLearning
    {
        public static void Run()
        {
            Console.WriteLine("=== TESTING BIOLOGICAL LEARNING ===\n");
            
            // Create a simple ephemeral brain
            var brain = new LanguageEphemeralBrain();
            
            Console.WriteLine("Test 1: Learning simple sentences");
            Console.WriteLine("==================================");
            
            // Learn a few simple sentences
            brain.LearnSentence("the red apple");
            brain.LearnSentence("the green apple");
            brain.LearnSentence("the red car");
            
            Console.WriteLine($"\n📊 After learning 3 sentences:");
            Console.WriteLine($"   Vocabulary: {brain.VocabularySize} words");
            Console.WriteLine($"   Learned sentences: {brain.LearnedSentences}");
            
            // Export neurons and check for connections
            var neurons = brain.ExportNeurons();
            Console.WriteLine($"   Total neurons: {neurons.Count}");
            
            // Count neurons with connections
            int neuronsWithConnections = 0;
            int totalConnections = 0;
            int maxConnections = 0;
            
            foreach (var neuronKvp in neurons)
            {
                var neuronData = neuronKvp.Value as dynamic;
                if (neuronData != null)
                {
                    var weights = neuronData.Weights as System.Collections.Generic.Dictionary<int, double>;
                    if (weights != null && weights.Count > 0)
                    {
                        neuronsWithConnections++;
                        totalConnections += weights.Count;
                        maxConnections = Math.Max(maxConnections, weights.Count);
                        
                        // Show first few neurons with connections
                        if (neuronsWithConnections <= 3)
                        {
                            Console.WriteLine($"\n   🔗 Neuron {neuronData.Id} ({neuronData.ClusterId}):");
                            Console.WriteLine($"      Connections: {weights.Count}");
                            Console.WriteLine($"      Activation count: {neuronData.ActivationCount}");
                            
                            // Show some connection weights
                            var sample = weights.Take(3);
                            foreach (var conn in sample)
                            {
                                Console.WriteLine($"        → Neuron {conn.Key}: weight {conn.Value:F3}");
                            }
                        }
                    }
                }
            }
            
            Console.WriteLine($"\n✨ BIOLOGICAL LEARNING RESULTS:");
            Console.WriteLine($"   Neurons with connections: {neuronsWithConnections} / {neurons.Count} ({neuronsWithConnections * 100.0 / neurons.Count:F1}%)");
            Console.WriteLine($"   Total connections: {totalConnections}");
            Console.WriteLine($"   Average connections per neuron: {(double)totalConnections / Math.Max(1, neuronsWithConnections):F1}");
            Console.WriteLine($"   Max connections on one neuron: {maxConnections}");
            
            // Test word associations
            var appleAssociations = brain.GetWordAssociations("apple", 5);
            Console.WriteLine($"\n🔗 Word associations for 'apple': {string.Join(", ", appleAssociations)}");
            
            var redAssociations = brain.GetWordAssociations("red", 5);
            Console.WriteLine($"🔗 Word associations for 'red': {string.Join(", ", redAssociations)}");
            
            // Verdict
            Console.WriteLine($"\n{'='}=======================================");
            if (neuronsWithConnections > 0)
            {
                Console.WriteLine("✅ BIOLOGICAL LEARNING WORKS!");
                Console.WriteLine("   Neurons are forming connections!");
                Console.WriteLine("   Hebbian learning is active!");
            }
            else
            {
                Console.WriteLine("❌ BIOLOGICAL LEARNING STILL BROKEN");
                Console.WriteLine("   No neuron connections formed");
            }
            Console.WriteLine($"{'='}=======================================\n");
        }
    }
}
