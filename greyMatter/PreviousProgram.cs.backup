using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GreyMatter.Core;

Console.WriteLine("🧠 Welcome to GreyMatter - Small Brain in a Jar (SBIJ)");
Console.WriteLine("=====================================================");

// Initialize the brain
var brain = new BrainInJar("./brain_data");
await brain.InitializeAsync();

// Demonstrate learning concepts
Console.WriteLine("\n📚 Teaching the brain some concepts...\n");

// Teach about colors
await brain.LearnConceptAsync("red", new Dictionary<string, double>
{
    ["wavelength"] = 0.7,
    ["intensity"] = 0.8,
    ["warmth"] = 0.9,
    ["energy"] = 0.6
});

await brain.LearnConceptAsync("blue", new Dictionary<string, double>
{
    ["wavelength"] = 0.4,
    ["intensity"] = 0.7,
    ["warmth"] = 0.2,
    ["energy"] = 0.8
});

// Teach about shapes
await brain.LearnConceptAsync("circle", new Dictionary<string, double>
{
    ["symmetry"] = 1.0,
    ["angles"] = 0.0,
    ["curves"] = 1.0,
    ["area_ratio"] = 0.785
});

await brain.LearnConceptAsync("square", new Dictionary<string, double>
{
    ["symmetry"] = 0.8,
    ["angles"] = 1.0,
    ["curves"] = 0.0,
    ["area_ratio"] = 1.0
});

// Teach about emotions
await brain.LearnConceptAsync("happy", new Dictionary<string, double>
{
    ["positive"] = 0.9,
    ["energy"] = 0.8,
    ["social"] = 0.7,
    ["arousal"] = 0.6
});

// Show brain stats
Console.WriteLine("\n📊 Brain Statistics:");
var stats = await brain.GetStatsAsync();
Console.WriteLine($"   Loaded Clusters: {stats.LoadedClusters}");
Console.WriteLine($"   Total Clusters: {stats.TotalClusters}");
Console.WriteLine($"   Total Synapses: {stats.TotalSynapses}");
Console.WriteLine($"   Neurons Created: {stats.TotalNeuronsCreated}");
Console.WriteLine($"   Storage Size: {stats.StorageSizeFormatted}");
Console.WriteLine($"   Uptime: {stats.UptimeFormatted}");

// Test the brain with different inputs
Console.WriteLine("\n🤔 Testing the brain's responses...\n");

var testInputs = new[]
{
    ("What is red?", new Dictionary<string, double> { ["wavelength"] = 0.7, ["warmth"] = 0.8 }),
    ("Tell me about circles", new Dictionary<string, double> { ["symmetry"] = 1.0, ["curves"] = 0.9 }),
    ("Something purple", new Dictionary<string, double> { ["wavelength"] = 0.5, ["intensity"] = 0.6 }),
    ("Happy feelings", new Dictionary<string, double> { ["positive"] = 0.8, ["energy"] = 0.7 }),
    ("Unknown concept", new Dictionary<string, double> { ["mystery"] = 0.5, ["unknown"] = 0.8 })
};

foreach (var (input, features) in testInputs)
{
    var result = await brain.ProcessInputAsync(input, features);
    Console.WriteLine($"Input: {input}");
    Console.WriteLine($"Response: {result.Response}");
    Console.WriteLine($"Confidence: {result.Confidence:F2}");
    Console.WriteLine($"Activated {result.ActivatedNeurons} neurons in {result.ActivatedClusters.Count} clusters");
    Console.WriteLine();
}

// Demonstrate learning from interaction
Console.WriteLine("🎓 Teaching a new concept from the interaction...\n");
await brain.LearnConceptAsync("purple", new Dictionary<string, double>
{
    ["wavelength"] = 0.5,
    ["intensity"] = 0.6,
    ["warmth"] = 0.4,
    ["energy"] = 0.7,
    ["mix_red"] = 0.5,
    ["mix_blue"] = 0.5
});

// Test again with purple
var purpleTest = await brain.ProcessInputAsync("What about purple now?", 
    new Dictionary<string, double> { ["wavelength"] = 0.5, ["intensity"] = 0.6 });
Console.WriteLine($"Purple test: {purpleTest.Response} (Confidence: {purpleTest.Confidence:F2})");

// Run maintenance
Console.WriteLine("\n🧹 Running brain maintenance...");
await brain.MaintenanceAsync();

// Save the brain state
await brain.SaveAsync();

// Final stats
Console.WriteLine("\n📈 Final Brain Statistics:");
var finalStats = await brain.GetStatsAsync();
Console.WriteLine($"   Loaded Clusters: {finalStats.LoadedClusters}");
Console.WriteLine($"   Total Clusters: {finalStats.TotalClusters}");
Console.WriteLine($"   Total Synapses: {finalStats.TotalSynapses}");
Console.WriteLine($"   Neurons Created: {finalStats.TotalNeuronsCreated}");
Console.WriteLine($"   Storage Size: {finalStats.StorageSizeFormatted}");

Console.WriteLine("\n🎉 SBIJ demonstration complete!");
Console.WriteLine("The brain has learned, stored knowledge, and can respond to new inputs.");
Console.WriteLine("Clusters and neurons were dynamically created and will persist between runs.");
