// TEMPORARY STUBS FOR DEPRECATED DEMOS - Phase 0 Cleanup
// These stubs exist only to allow compilation during transition period
// TODO: Remove all callers and delete this file

using System;
using System.Threading.Tasks;

namespace GreyMatter.Demos
{
    /// <summary>
    /// Temporary stub for deleted DevelopmentalLearningDemo
    /// </summary>
    public static class DevelopmentalLearningDemo
    {
        public static Task RunDemo(string[] args)
        {
            Console.WriteLine("⚠️  DevelopmentalLearningDemo is deprecated (Phase 0 cleanup)");
            Console.WriteLine("   Use: dotnet run -- --continuous-learning");
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Temporary stub for deleted LanguageFoundationsDemo  
    /// </summary>
    public static class LanguageFoundationsDemo
    {
        public static Task RunDemo()
        {
            Console.WriteLine("⚠️  LanguageFoundationsDemo is deprecated (Phase 0 cleanup)");
            Console.WriteLine("   Use: dotnet run -- --llm-teacher");
            return Task.CompletedTask;
        }

        public static void RunQuickTest()
        {
            Console.WriteLine("⚠️  LanguageFoundationsDemo is deprecated (Phase 0 cleanup)");
            Console.WriteLine("   Use: dotnet run -- --tatoeba-hybrid-1k");
        }

        public static Task RunMinimalDemo()
        {
            Console.WriteLine("⚠️  LanguageFoundationsDemo is deprecated (Phase 0 cleanup)");
            Console.WriteLine("   Use: dotnet run -- --tatoeba-hybrid-1k");
            return Task.CompletedTask;
        }

        public static Task RunFullScaleTraining()
        {
            Console.WriteLine("⚠️  LanguageFoundationsDemo is deprecated (Phase 0 cleanup)");
            Console.WriteLine("   Use: dotnet run -- --enhanced-learning --max-words 50000");
            return Task.CompletedTask;
        }

        public static Task RunRandomSampleTraining(int sampleSize, bool resetBrain = false)
        {
            Console.WriteLine("⚠️  LanguageFoundationsDemo is deprecated (Phase 0 cleanup)");
            Console.WriteLine($"   Use: dotnet run -- --enhanced-learning --max-words {sampleSize}");
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Temporary stub for deleted IterativeGrowthTest
    /// </summary>
    public static class IterativeGrowthTest
    {
        public static Task RunIterativeGrowthAnalysis(int sampleSize, int iterations)
        {
            Console.WriteLine("⚠️  IterativeGrowthTest is deprecated (Phase 0 cleanup)");
            Console.WriteLine("   Test functionality moved to test suite");
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Temporary stub for deleted InteractiveConversation
    /// </summary>
    public class InteractiveConversation
    {
        public InteractiveConversation(object brain, object config)
        {
            Console.WriteLine("⚠️  InteractiveConversation is deprecated (Phase 0 cleanup)");
            Console.WriteLine("   Feature not yet available in production");
        }

        public Task StartAsync()
        {
            return Task.CompletedTask;
        }

        public Task StartConversationAsync()
        {
            return Task.CompletedTask;
        }
    }
}
