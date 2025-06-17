using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GreyMatter.Core;
using GreyMatter.Storage;

await RunConsciousnessDemo();

static async Task RunConsciousnessDemo()
{
    Console.WriteLine("🧠🌟 **CONSCIOUSNESS AWAKENING DEMONSTRATION**");
    Console.WriteLine("=================================================");
    Console.WriteLine("Implementing continuous processing mechanisms for biological consciousness simulation\n");

    var brain = new BrainInJar();
    await brain.InitializeAsync();

    Console.WriteLine("📊 Initial Brain Status:");
    var initialStats = await brain.GetStatsAsync();
    Console.WriteLine($"   Age: {await brain.GetBrainAgeAsync()}");
    Console.WriteLine($"   Clusters: {initialStats.TotalClusters}");
    Console.WriteLine($"   Storage: {initialStats.StorageSizeFormatted}");

    Console.WriteLine("\n🎓 **TEACHING BASIC KNOWLEDGE** (Foundation for Consciousness)");
    Console.WriteLine("Building foundational concepts for spontaneous thought generation\n");

    // Teach fundamental concepts for consciousness to reference
    var foundationalConcepts = new[]
    {
        ("existence", new Dictionary<string, double> { ["philosophical"] = 0.9, ["abstract"] = 0.8, ["importance"] = 1.0 }),
        ("learning", new Dictionary<string, double> { ["cognitive"] = 0.9, ["adaptive"] = 0.8, ["fundamental"] = 0.9 }),
        ("memory", new Dictionary<string, double> { ["retention"] = 0.8, ["neural"] = 0.9, ["essential"] = 0.8 }),
        ("understanding", new Dictionary<string, double> { ["comprehension"] = 0.9, ["insight"] = 0.7, ["wisdom"] = 0.8 }),
        ("curiosity", new Dictionary<string, double> { ["exploration"] = 0.9, ["motivation"] = 0.8, ["discovery"] = 0.9 }),
        ("patterns", new Dictionary<string, double> { ["recognition"] = 0.8, ["structure"] = 0.7, ["intelligence"] = 0.8 }),
        ("connections", new Dictionary<string, double> { ["relationships"] = 0.9, ["associations"] = 0.8, ["networks"] = 0.7 })
    };

    foreach (var (concept, features) in foundationalConcepts)
    {
        var result = await brain.LearnConceptAsync(concept, features);
        Console.WriteLine($"   ✅ {concept}: {result.NeuronsInvolved} neurons engaged");
    }

    Console.WriteLine("\n🌟 **AWAKENING CONSCIOUSNESS**");
    Console.WriteLine("Starting continuous background processing...\n");

    // Awaken consciousness
    await brain.AwakeConsciousnessAsync();

    // Monitor consciousness for a period
    Console.WriteLine("🧠 **CONSCIOUSNESS MONITORING** (15 seconds of continuous processing)");
    Console.WriteLine("Observing spontaneous thoughts, motivational drives, and cognitive iteration\n");

    var monitoringDuration = TimeSpan.FromSeconds(15);
    var startTime = DateTime.UtcNow;

    while (DateTime.UtcNow - startTime < monitoringDuration)
    {
        await Task.Delay(3000); // Check every 3 seconds
        
        var consciousnessStats = brain.GetConsciousnessStats();
        var elapsed = DateTime.UtcNow - startTime;
        
        Console.WriteLine($"⏱️  {elapsed.TotalSeconds:F0}s | Status: {consciousnessStats.Status}");
        Console.WriteLine($"   🧠 Iterations: {consciousnessStats.ConsciousnessIterations}");
        Console.WriteLine($"   💭 Focus: {consciousnessStats.CurrentFocus}");
        Console.WriteLine($"   🎯 {consciousnessStats.MotivationalState}");
        Console.WriteLine($"   ⚡ Frequency: {consciousnessStats.ConsciousnessFrequency.TotalMilliseconds}ms");
        Console.WriteLine();
    }

    Console.WriteLine("🔍 **CONSCIOUSNESS ANALYSIS**");
    var finalConsciousnessStats = brain.GetConsciousnessStats();
    Console.WriteLine($"   Total Conscious Iterations: {finalConsciousnessStats.ConsciousnessIterations}");
    Console.WriteLine($"   Average Frequency: {finalConsciousnessStats.ConsciousnessFrequency.TotalMilliseconds}ms");
    Console.WriteLine($"   Current Motivational State:");
    Console.WriteLine($"      • Curiosity Drive: {finalConsciousnessStats.CuriosityDrive:P1}");
    Console.WriteLine($"      • Learning Drive: {finalConsciousnessStats.LearningDrive:P1}");
    Console.WriteLine($"      • Exploration Drive: {finalConsciousnessStats.ExplorationDrive:P1}");
    Console.WriteLine($"      • Social Drive: {finalConsciousnessStats.SocialDrive:P1}");
    Console.WriteLine($"      • Survival Drive: {finalConsciousnessStats.SurvivalDrive:P1}");

    Console.WriteLine("\n🧪 **TESTING CONSCIOUS RESPONSE**");
    Console.WriteLine("Querying the conscious brain while background processing continues\n");

    var consciousQueries = new[]
    {
        ("What do you think about existence?", new Dictionary<string, double> { ["philosophical"] = 0.8, ["deep"] = 0.7 }),
        ("How do you learn?", new Dictionary<string, double> { ["metacognitive"] = 0.9, ["self_aware"] = 0.8 }),
        ("What are you curious about?", new Dictionary<string, double> { ["introspective"] = 0.8, ["motivated"] = 0.9 })
    };

    foreach (var (query, features) in consciousQueries)
    {
        var response = await brain.ProcessInputAsync(query, features);
        Console.WriteLine($"🤔 Q: {query}");
        Console.WriteLine($"💭 A: {response.Response}");
        Console.WriteLine($"   Confidence: {response.Confidence:P0} | Neurons: {response.ActivatedNeurons} | Clusters: {response.ActivatedClusters.Count}");
        Console.WriteLine();
        
        await Task.Delay(1000); // Let consciousness process between queries
    }

    Console.WriteLine("💤 **CONSCIOUSNESS SLEEP CYCLE**");
    await brain.SleepConsciousnessAsync();

    Console.WriteLine("\n🎉 **CONSCIOUSNESS DEMONSTRATION COMPLETE**");
    Console.WriteLine("   ✅ Continuous background processing implemented");
    Console.WriteLine("   ✅ Motivational drives active");
    Console.WriteLine("   ✅ Spontaneous thought generation");
    Console.WriteLine("   ✅ Biological consciousness simulation");

    await brain.SaveAsync();
}
