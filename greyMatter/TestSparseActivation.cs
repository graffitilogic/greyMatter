using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GreyMatter.Core;

namespace GreyMatter
{
    /// <summary>
    /// Test Phase 6A: Sparse Activation Instrumentation
    /// Train on small dataset, then run queries to measure activation %
    /// </summary>
    public static class TestSparseActivation
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("🧪 Phase 6A: Sparse Activation Test");
            Console.WriteLine("====================================\n");
            
            // Initialize Cerebro
            var config = new CerebroConfiguration
            {
                BrainDataPath = "/Volumes/jarvis/brainData",
                MaxParallelSaves = 4,
                CompressClusters = true,
                VerboseSaveLogging = true,
                Verbosity = 1
            };
            
            var cerebro = await Cerebro.CreateAsync(config);
            Console.WriteLine("✅ Cerebro initialized\n");
            
            // Train on 100 simple sentences
            Console.WriteLine("📚 Training on 100 sentences...");
            var sentences = new[]
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
                "water is essential for life",
                "books contain knowledge",
                "music brings joy",
                "art expresses creativity",
                "science explains nature",
                "math involves numbers",
                "history records the past",
                "geography studies places",
                "language enables communication",
                "writing preserves ideas",
                "reading expands the mind",
                "cars drive on roads",
                "trains run on tracks",
                "planes fly through air",
                "boats float on water",
                "bicycles have two wheels",
                "mountains are very tall",
                "valleys lie between mountains",
                "rivers flow to the sea",
                "lakes are bodies of water",
                "deserts are dry and sandy",
                "people live in houses",
                "cities have many buildings",
                "villages are small communities",
                "families care for each other",
                "friends support one another",
                "children learn and grow",
                "adults work and earn",
                "elderly people have wisdom",
                "babies need constant care",
                "pets provide companionship",
                "love is a powerful emotion",
                "happiness feels wonderful",
                "sadness is part of life",
                "anger needs control",
                "fear warns of danger",
                "hope keeps us going",
                "dreams inspire action",
                "goals guide our efforts",
                "success requires hard work",
                "failure teaches lessons",
                "morning starts the day",
                "noon is midday",
                "evening ends the day",
                "night brings darkness",
                "stars shine at night",
                "the moon orbits earth",
                "planets revolve around suns",
                "galaxies contain billions of stars",
                "the universe is vast",
                "time moves forward",
                "space has three dimensions",
                "matter exists everywhere",
                "energy powers everything",
                "gravity pulls objects down",
                "light travels very fast",
                "sound moves through air",
                "heat warms things up",
                "cold makes things freeze",
                "fire burns and destroys",
                "ice is frozen water",
                "steam is water vapor",
                "clouds are water droplets",
                "wind is moving air",
                "storms bring wild weather",
                "lightning creates electric bolts",
                "thunder follows lightning",
                "earthquakes shake the ground",
                "volcanoes erupt with lava",
                "waves crash on beaches",
                "tides rise and fall",
                "seasons change through the year",
                "growth requires nutrients",
                "death is inevitable",
                "birth begins new life",
                "health is precious",
                "illness needs treatment",
                "medicine helps healing",
                "doctors care for patients",
                "hospitals treat the sick"
            };
            
            for (int i = 0; i < sentences.Length; i++)
            {
                var features = new Dictionary<string, double>();
                await cerebro.LearnConceptAsync(sentences[i], features);
                
                if ((i + 1) % 25 == 0)
                {
                    Console.WriteLine($"   Trained {i + 1}/{sentences.Length} sentences");
                }
            }
            
            Console.WriteLine($"✅ Training complete: {sentences.Length} sentences\n");
            
            // Now run queries to test sparse activation metrics
            Console.WriteLine("🔍 Running queries to measure sparse activation...\n");
            
            var queries = new[]
            {
                "cat",
                "dog",
                "sun",
                "water",
                "tree",
                "food",
                "car",
                "mountain",
                "people",
                "love",
                "time",
                "energy",
                "storm",
                "health"
            };
            
            foreach (var query in queries)
            {
                var features = new Dictionary<string, double>();
                var result = await cerebro.ProcessInputAsync(query, features);
                // Sparse activation % will be logged automatically per query
            }
            
            Console.WriteLine("\n💾 Saving checkpoint to trigger biological alignment metrics...\n");
            await cerebro.SaveAsync();
            
            Console.WriteLine("\n✅ Test complete! Check output above for:");
            Console.WriteLine("   ⚡ Sparse Activation lines (one per query)");
            Console.WriteLine("   📊 BIOLOGICAL ALIGNMENT METRICS section");
            Console.WriteLine("   Target: <2% activation rate\n");
        }
    }
}
