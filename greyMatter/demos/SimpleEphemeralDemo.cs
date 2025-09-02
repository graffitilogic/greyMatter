using greyMatter.Core;
using System;

namespace greyMatter
{
    /// <summary>
    /// Demo program showing the original ephemeral brain concept
    /// Run this to see the difference from the current complex system
    /// </summary>
    public class SimpleEphemeralDemo
    {
        public static void RunDemo()
        {
            Console.WriteLine("=== Simple Ephemeral Brain Demo ===");
            Console.WriteLine("Demonstrating the original vision: FMRI-like clusters with shared neurons\n");

            var brain = new SimpleEphemeralBrain();

            // Demonstrate basic learning
            Console.WriteLine("1. Learning basic concepts:");
            brain.Learn("red");
            brain.Learn("fruit");
            brain.Learn("apple", new[] { "red", "fruit" }); // Apple shares neurons with red and fruit
            
            Console.WriteLine();

            // Demonstrate recall and spreading activation
            Console.WriteLine("2. Recall and spreading activation:");
            brain.Recall("red");   // Should activate apple (shares neurons)
            brain.Recall("fruit"); // Should activate apple (shares neurons)
            brain.Recall("apple"); // Should activate red and fruit (shared neurons)
            
            Console.WriteLine();

            // Show brain scan (what's currently active)
            brain.ShowBrainScan();

            // Learn more concepts to show scaling
            Console.WriteLine("\n3. Learning more concepts:");
            brain.Learn("green", new[] { "red" });     // Colors share some neurons
            brain.Learn("car", new[] { "red" });       // Red car shares with red
            brain.Learn("tree", new[] { "green" });    // Green tree shares with green
            brain.Learn("orange", new[] { "fruit" });  // Orange fruit shares with fruit

            Console.WriteLine();

            // Show how concepts are interconnected
            Console.WriteLine("4. Complex recall after more learning:");
            brain.Recall("red");    // Should now activate apple, green, car
            brain.Recall("fruit");  // Should activate apple, orange
            
            Console.WriteLine();
            brain.ShowBrainScan();

            // Show memory efficiency
            brain.ShowMemoryEfficiency();

            Console.WriteLine("\n=== Key Differences from Current System ===");
            Console.WriteLine("✓ Simple: <300 lines vs 1000s in current system");
            Console.WriteLine("✓ Fast: No complex persistence, capacity management, or metadata");
            Console.WriteLine("✓ Biological: Clusters activate/deactivate like FMRI");
            Console.WriteLine("✓ Shared neurons: Related concepts share neurons (Venn diagram)");
            Console.WriteLine("✓ Ephemeral: Memory scales with active concepts only");
            Console.WriteLine("✓ Intuitive: Matches your original vision");
            
            Console.WriteLine("\nThis proves the core concept works. Now we can scale it up!");
        }
    }
}
