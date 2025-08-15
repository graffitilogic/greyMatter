using System;
using System.Linq;
using GreyMatter.Core;

public class PatternAnalysisTest
{
    public static async Task RunAsync()
    {
        Console.WriteLine("🔬 **PATTERN ANALYSIS DEEP DIVE**");
        Console.WriteLine("=====================================");
        
        var encoder = new SparseConceptEncoder();
        
        // Test 1: Examine actual bit patterns
        Console.WriteLine("\n📊 **BIT PATTERN ANALYSIS**");
        var catPattern = encoder.EncodeWord("cat", "animal context");
        var dogPattern = encoder.EncodeWord("dog", "animal context");
        var carPattern = encoder.EncodeWord("car", "vehicle context");
        
        Console.WriteLine($"Cat pattern bits: [{string.Join(", ", catPattern.ActiveBits.Take(10))}...] (total: {catPattern.ActiveBits.Length})");
        Console.WriteLine($"Dog pattern bits: [{string.Join(", ", dogPattern.ActiveBits.Take(10))}...] (total: {dogPattern.ActiveBits.Length})");
        Console.WriteLine($"Car pattern bits: [{string.Join(", ", carPattern.ActiveBits.Take(10))}...] (total: {carPattern.ActiveBits.Length})");
        
        // Test 2: Manual similarity calculation
        Console.WriteLine("\n🔢 **MANUAL SIMILARITY CALCULATIONS**");
        var catDogIntersection = catPattern.ActiveBits.Intersect(dogPattern.ActiveBits).Count();
        var catDogUnion = catPattern.ActiveBits.Union(dogPattern.ActiveBits).Count();
        var catCarIntersection = catPattern.ActiveBits.Intersect(carPattern.ActiveBits).Count();
        var catCarUnion = catPattern.ActiveBits.Union(carPattern.ActiveBits).Count();
        
        Console.WriteLine($"Cat-Dog: Intersection={catDogIntersection}, Union={catDogUnion}, Jaccard={catDogIntersection}/{catDogUnion} = {(double)catDogIntersection/catDogUnion:P2}");
        Console.WriteLine($"Cat-Car: Intersection={catCarIntersection}, Union={catCarUnion}, Jaccard={catCarIntersection}/{catCarUnion} = {(double)catCarIntersection/catCarUnion:P2}");
        
        // Test 3: Context sensitivity 
        Console.WriteLine("\n🎭 **CONTEXT SENSITIVITY ANALYSIS**");
        var bankFinancial = encoder.EncodeWord("bank", "financial institution context");
        var bankRiver = encoder.EncodeWord("bank", "river side context");
        
        Console.WriteLine($"Bank (financial) bits: [{string.Join(", ", bankFinancial.ActiveBits.Take(10))}...] (total: {bankFinancial.ActiveBits.Length})");
        Console.WriteLine($"Bank (river) bits: [{string.Join(", ", bankRiver.ActiveBits.Take(10))}...] (total: {bankRiver.ActiveBits.Length})");
        
        var bankIntersection = bankFinancial.ActiveBits.Intersect(bankRiver.ActiveBits).Count();
        var bankUnion = bankFinancial.ActiveBits.Union(bankRiver.ActiveBits).Count();
        Console.WriteLine($"Bank contexts: Intersection={bankIntersection}, Union={bankUnion}, Jaccard={bankIntersection}/{bankUnion} = {(double)bankIntersection/bankUnion:P2}");
        
        // Test 4: Encoder's built-in similarity
        Console.WriteLine("\n⚡ **ENCODER SIMILARITY COMPARISON**");
        Console.WriteLine($"Encoder Cat-Dog similarity: {encoder.CalculateSimilarity(catPattern, dogPattern):P2}");
        Console.WriteLine($"Encoder Cat-Car similarity: {encoder.CalculateSimilarity(catPattern, carPattern):P2}");
        Console.WriteLine($"Encoder Bank contexts similarity: {encoder.CalculateSimilarity(bankFinancial, bankRiver):P2}");
        
        // Test 5: Unique pattern generation
        Console.WriteLine("\n🔄 **PATTERN UNIQUENESS TEST**");
        var apple1 = encoder.EncodeWord("apple", "fruit context");
        var apple2 = encoder.EncodeWord("apple", "fruit context"); // Same context
        var apple3 = encoder.EncodeWord("apple", "tech context");  // Different context
        
        Console.WriteLine($"Apple1 vs Apple2 (same context): {encoder.CalculateSimilarity(apple1, apple2):P2}");
        Console.WriteLine($"Apple1 vs Apple3 (diff context): {encoder.CalculateSimilarity(apple1, apple3):P2}");
        
        Console.WriteLine($"\nApple1 bits: [{string.Join(", ", apple1.ActiveBits.Take(15))}...]");
        Console.WriteLine($"Apple2 bits: [{string.Join(", ", apple2.ActiveBits.Take(15))}...]");
        Console.WriteLine($"Apple3 bits: [{string.Join(", ", apple3.ActiveBits.Take(15))}...]");
    }
}
