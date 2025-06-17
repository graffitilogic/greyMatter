using GreyMatter.Core;

// Simple test to debug one input
var brain = new BrainInJar();
await brain.InitializeAsync();

// Test with red features - should match what was learned
var result = await brain.ProcessInputAsync("What is red?", 
    new Dictionary<string, double> 
    { 
        ["wavelength"] = 0.7, 
        ["warmth"] = 0.8, 
        ["intensity"] = 0.9, 
        ["brightness"] = 0.6 
    });

Console.WriteLine($"Result: {result.Response}");
Console.WriteLine($"Confidence: {result.Confidence:F2}");
