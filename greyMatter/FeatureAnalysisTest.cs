using System;
using System.Linq;
using GreyMatter.Core;

public class FeatureAnalysisTest
{
    public static async Task RunAsync()
    {
        Console.WriteLine("🔍 **FEATURE EXTRACTION ANALYSIS**");
        Console.WriteLine("=====================================");
        
        var encoder = new SparseConceptEncoder();
        
        // Test what features are being extracted for different words
        Console.WriteLine("\n📝 **FEATURE EXTRACTION TEST**");
        
        TestWordFeatures(encoder, "cat", "animal context");
        TestWordFeatures(encoder, "dog", "animal context");
        TestWordFeatures(encoder, "car", "vehicle context");
        TestWordFeatures(encoder, "bank", "financial context");
        TestWordFeatures(encoder, "bank", "river context");
    }
    
    private static void TestWordFeatures(SparseConceptEncoder encoder, string word, string context)
    {
        Console.WriteLine($"\n🔤 Word: '{word}' in '{context}'");
        
        // We need to create a reflection method to access private methods
        // For now, let's create patterns and see what happens
        var pattern = encoder.EncodeWord(word, context);
        Console.WriteLine($"   Pattern: {pattern.ActiveBits.Length} bits, strength: {pattern.ActivationStrength}");
        Console.WriteLine($"   First 15 bits: [{string.Join(", ", pattern.ActiveBits.Take(15))}]");
    }
}
