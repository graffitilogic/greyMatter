using greyMatter.Learning;

namespace greyMatter.Tests;

/// <summary>
/// Week 4 Task 1: First run of column architecture with comprehensive logging
/// Goal: Execute TrainWithColumnArchitectureAsync(50, 10) and verify it completes without crashes
/// </summary>
public class ColumnArchitectureTestRunner
{
    public static async Task Run()
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  Week 4 Task 1: Column Architecture First Run (Debug Mode)  ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");
        
        Console.WriteLine("🎯 Goal: Verify column architecture runs without crashing");
        Console.WriteLine("📊 Parameters: 50 sentences, batch size 10");
        Console.WriteLine("🔍 Debug mode: Full output capture\n");
        
        var startTime = DateTime.Now;
        bool success = false;
        string? errorMessage = null;
        
        try
        {
            // Locate Tatoeba data
            var tatoebaPath = "/Volumes/jarvis/trainData/Tatoeba";
            if (!Directory.Exists(tatoebaPath))
            {
                Console.WriteLine("⚠️  Warning: NAS not mounted, checking local paths...");
                tatoebaPath = Path.Combine(Directory.GetCurrentDirectory(), "trainData", "Tatoeba");
            }
            
            if (!Directory.Exists(tatoebaPath))
            {
                throw new DirectoryNotFoundException($"Tatoeba data not found at {tatoebaPath}");
            }
            
            Console.WriteLine($"✅ Data source: {tatoebaPath}");
            Console.WriteLine($"🧠 Initializing TatoebaLanguageTrainer...\n");
            
            // Create trainer
            var trainer = new TatoebaLanguageTrainer(tatoebaPath);
            
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine("  COLUMN ARCHITECTURE TRAINING - LIVE OUTPUT");
            Console.WriteLine("═══════════════════════════════════════════════════════════════\n");
            
            // Run column-based training with debug output
            await trainer.TrainWithColumnArchitectureAsync(maxSentences: 50, batchSize: 10);
            
            success = true;
            
            Console.WriteLine("\n═══════════════════════════════════════════════════════════════");
            Console.WriteLine("  TRAINING COMPLETE");
            Console.WriteLine("═══════════════════════════════════════════════════════════════\n");
        }
        catch (Exception ex)
        {
            success = false;
            errorMessage = ex.Message;
            
            Console.WriteLine("\n❌ ═══════════════════════════════════════════════════════════════");
            Console.WriteLine("  ERROR OCCURRED");
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine($"Type: {ex.GetType().Name}");
            Console.WriteLine($"Message: {ex.Message}");
            Console.WriteLine($"\nStack Trace:\n{ex.StackTrace}");
            
            if (ex.InnerException != null)
            {
                Console.WriteLine($"\nInner Exception: {ex.InnerException.Message}");
            }
        }
        
        var elapsed = DateTime.Now - startTime;
        
        // Display test results
        Console.WriteLine("\n╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                      TEST RESULTS                            ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        
        Console.WriteLine($"\n📊 Execution Summary:");
        Console.WriteLine($"   Status: {(success ? "✅ SUCCESS" : "❌ FAILED")}");
        Console.WriteLine($"   Time: {elapsed.TotalSeconds:F2} seconds");
        
        if (!success && errorMessage != null)
        {
            Console.WriteLine($"   Error: {errorMessage}");
        }
        
        Console.WriteLine($"\n🎯 Week 4 Task 1 Status:");
        if (success)
        {
            Console.WriteLine("   ✅ Column architecture runs without crashing");
            Console.WriteLine("   ✅ Training completed successfully");
            Console.WriteLine("   ✅ Ready for Task 2: Communication verification");
        }
        else
        {
            Console.WriteLine("   ❌ Training failed with errors");
            Console.WriteLine("   🔧 Need to debug and fix issues before proceeding");
        }
        
        Console.WriteLine("\n📝 Next Steps:");
        if (success)
        {
            Console.WriteLine("   1. ✅ Task 1 complete - Column runs successfully");
            Console.WriteLine("   2. ⏳ Task 2 - Add inter-column communication logging");
            Console.WriteLine("   3. ⏳ Task 3 - Verify column communication works");
            Console.WriteLine("   4. ⏳ Task 4 - Run baseline comparison (columns vs traditional)");
        }
        else
        {
            Console.WriteLine("   1. 🔧 Debug error: " + (errorMessage ?? "Unknown error"));
            Console.WriteLine("   2. 🔧 Fix crash/error and retry");
            Console.WriteLine("   3. ⏳ Verify successful completion");
        }
        
        Console.WriteLine("\n" + new string('═', 64) + "\n");
    }
}
