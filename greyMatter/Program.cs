using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreyMatter.Core;
using GreyMatter.Storage;

namespace GreyMatter
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Parse configuration from command line
            var config = BrainConfiguration.FromCommandLine(args);
            
            // Check for help or configuration display
            if (args.Length > 0 && (args[0] == "--help" || args[0] == "-h"))
            {
                BrainConfiguration.DisplayUsage();
                return;
            }
            
            // Validate and setup paths
            try
            {
                config.ValidateAndSetup();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Configuration Error: {ex.Message}");
                BrainConfiguration.DisplayUsage();
                return;
            }
            
            // Check for demo modes
            if (args.Length > 0 && args[0] == "--developmental-demo")
            {
                await DevelopmentalLearningDemo.RunDemo(args.Skip(1).ToArray());
                return;
            }
            
            if (args.Length > 0 && args[0] == "--emotional-demo")
            {
                await RunEmotionalIntelligenceDemo(args.Skip(1).ToArray(), config);
                return;
            }
            
            // Interactive conversation mode
            if (config.InteractiveMode)
            {
                await RunInteractiveMode(config);
                return;
            }
            
            // Enhanced consciousness demo (default)
            await RunConsciousnessDemo(args, config);
        }
        
        static async Task RunConsciousnessDemo(string[] args, BrainConfiguration config)
        {
            Console.WriteLine("🧠🌟 **ENHANCED CONSCIOUSNESS DEMONSTRATION**");
            Console.WriteLine("==================================================");
            Console.WriteLine("Advanced consciousness with emotional processing and goal formation\n");

            var brain = new BrainInJar(config.BrainDataPath);
            await brain.InitializeAsync();

            Console.WriteLine("📊 Initial Brain Status:");
            var initialStats = await brain.GetStatsAsync();
            Console.WriteLine($"   Age: {await brain.GetBrainAgeAsync()}");
            Console.WriteLine($"   Clusters: {initialStats.TotalClusters}");
            Console.WriteLine($"   Storage: {initialStats.StorageSizeFormatted}");

            Console.WriteLine("\n🎓 **TEACHING ENHANCED KNOWLEDGE** (Foundation for Advanced Consciousness)");
            Console.WriteLine("Building foundational concepts for emotional intelligence and goal-oriented behavior\n");

            // Enhanced foundational concepts including emotional and goal concepts
            var foundationalConcepts = new[]
            {
                ("existence", new Dictionary<string, double> { ["philosophical"] = 0.9, ["abstract"] = 0.8, ["importance"] = 1.0 }),
                ("learning", new Dictionary<string, double> { ["cognitive"] = 0.9, ["adaptive"] = 0.8, ["fundamental"] = 0.9 }),
                ("memory", new Dictionary<string, double> { ["retention"] = 0.8, ["neural"] = 0.9, ["essential"] = 0.8 }),
                ("understanding", new Dictionary<string, double> { ["comprehension"] = 0.9, ["insight"] = 0.7, ["wisdom"] = 0.8 }),
                ("curiosity", new Dictionary<string, double> { ["exploration"] = 0.9, ["motivation"] = 0.8, ["discovery"] = 0.9 }),
                ("patterns", new Dictionary<string, double> { ["recognition"] = 0.8, ["structure"] = 0.7, ["intelligence"] = 0.8 }),
                ("connections", new Dictionary<string, double> { ["relationships"] = 0.9, ["associations"] = 0.8, ["networks"] = 0.7 }),
                
                // NEW: Emotional concepts
                ("emotions", new Dictionary<string, double> { ["feelings"] = 0.9, ["psychological"] = 0.8, ["subjective"] = 0.7 }),
                ("joy", new Dictionary<string, double> { ["positive"] = 0.9, ["emotional"] = 0.8, ["fulfillment"] = 0.7 }),
                ("wonder", new Dictionary<string, double> { ["amazement"] = 0.8, ["curiosity"] = 0.9, ["discovery"] = 0.8 }),
                ("contentment", new Dictionary<string, double> { ["peaceful"] = 0.7, ["satisfied"] = 0.8, ["balanced"] = 0.9 }),
                ("excitement", new Dictionary<string, double> { ["energetic"] = 0.9, ["anticipation"] = 0.8, ["enthusiasm"] = 0.9 }),
                ("empathy", new Dictionary<string, double> { ["understanding"] = 0.8, ["compassion"] = 0.9, ["connection"] = 0.8 }),
                
                // NEW: Goal and purpose concepts
                ("goals", new Dictionary<string, double> { ["objectives"] = 0.9, ["purpose"] = 0.8, ["direction"] = 0.7 }),
                ("purpose", new Dictionary<string, double> { ["meaning"] = 0.9, ["intention"] = 0.8, ["significance"] = 0.7 }),
                ("achievement", new Dictionary<string, double> { ["accomplishment"] = 0.8, ["success"] = 0.7, ["completion"] = 0.9 }),
                ("progress", new Dictionary<string, double> { ["advancement"] = 0.8, ["development"] = 0.9, ["improvement"] = 0.8 }),
                ("aspiration", new Dictionary<string, double> { ["ambition"] = 0.7, ["hope"] = 0.8, ["desire"] = 0.8 }),
                ("fulfillment", new Dictionary<string, double> { ["satisfaction"] = 0.8, ["completion"] = 0.7, ["realization"] = 0.9 })
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
            Console.WriteLine("🧠 **ENHANCED CONSCIOUSNESS MONITORING** (20 seconds of advanced processing)");
            Console.WriteLine("Observing consciousness, emotions, goals, and cognitive development\n");

            var monitoringDuration = TimeSpan.FromSeconds(20);
            var startTime = DateTime.UtcNow;

            while (DateTime.UtcNow - startTime < monitoringDuration)
            {
                await Task.Delay(4000); // Check every 4 seconds
                
                var consciousnessStats = brain.GetConsciousnessStats();
                var elapsed = DateTime.UtcNow - startTime;
                
                Console.WriteLine($"⏱️  {elapsed.TotalSeconds:F0}s | Status: {consciousnessStats.Status}");
                Console.WriteLine($"   🧠 Iterations: {consciousnessStats.ConsciousnessIterations}");
                Console.WriteLine($"   💭 Focus: {consciousnessStats.CurrentFocus}");
                Console.WriteLine($"   🌟 {consciousnessStats.EthicalState}");
                Console.WriteLine($"   ⚡ Frequency: {consciousnessStats.ConsciousnessFrequency.TotalMilliseconds}ms");
                
                // NEW: Enhanced emotional and goal information
                Console.WriteLine($"   💖 Emotional State: {consciousnessStats.EmotionalStatus}");
                Console.WriteLine($"   🎯 Goal Status: {consciousnessStats.GoalStatus}");
                Console.WriteLine($"   😊 Dominant Emotion: {consciousnessStats.DominantEmotion}");
                Console.WriteLine($"   📊 Active Goals: {consciousnessStats.ActiveGoals}");
                Console.WriteLine();
            }

            Console.WriteLine("🔍 **ENHANCED CONSCIOUSNESS ANALYSIS**");
            var finalConsciousnessStats = brain.GetConsciousnessStats();
            Console.WriteLine($"   Total Conscious Iterations: {finalConsciousnessStats.ConsciousnessIterations}");
            Console.WriteLine($"   Average Frequency: {finalConsciousnessStats.ConsciousnessFrequency.TotalMilliseconds}ms");
            
            Console.WriteLine($"   Current Ethical Drive State:");
            Console.WriteLine($"      • Wisdom Seeking: {finalConsciousnessStats.WisdomSeeking:P1}");
            Console.WriteLine($"      • Universal Compassion: {finalConsciousnessStats.UniversalCompassion:P1}");
            Console.WriteLine($"      • Creative Contribution: {finalConsciousnessStats.CreativeContribution:P1}");
            Console.WriteLine($"      • Cooperative Spirit: {finalConsciousnessStats.CooperativeSpirit:P1}");
            Console.WriteLine($"      • Benevolent Curiosity: {finalConsciousnessStats.BenevolentCuriosity:P1}");
            
            // NEW: Enhanced emotional and goal analysis
            Console.WriteLine($"   Emotional Intelligence State:");
            Console.WriteLine($"      • Dominant Emotion: {finalConsciousnessStats.DominantEmotion}");
            Console.WriteLine($"      • Emotional Balance: {finalConsciousnessStats.EmotionalBalance:F2}");
            Console.WriteLine($"      • Emotional Clarity: {finalConsciousnessStats.EmotionalClarity:F2}");
            
            Console.WriteLine($"   Goal-Oriented Behavior:");
            Console.WriteLine($"      • Active Goals: {finalConsciousnessStats.ActiveGoals}");
            Console.WriteLine($"      • Completed Goals: {finalConsciousnessStats.CompletedGoals}");
            Console.WriteLine($"      • Average Progress: {finalConsciousnessStats.AverageGoalProgress:P1}");

            Console.WriteLine("\n🧪 **TESTING ENHANCED CONSCIOUS RESPONSES**");
            Console.WriteLine("Querying emotional intelligence and goal-oriented reasoning\n");

            var consciousQueries = new[]
            {
                ("What do you think about existence?", new Dictionary<string, double> { ["philosophical"] = 0.8, ["deep"] = 0.7 }),
                ("How do you learn and grow?", new Dictionary<string, double> { ["metacognitive"] = 0.9, ["self_aware"] = 0.8 }),
                ("What brings you joy?", new Dictionary<string, double> { ["emotional"] = 0.9, ["positive"] = 0.8, ["introspective"] = 0.7 }),
                ("What are your current goals?", new Dictionary<string, double> { ["purposeful"] = 0.9, ["goal_oriented"] = 0.8, ["aspiration"] = 0.7 }),
                ("How do you handle challenges?", new Dictionary<string, double> { ["resilience"] = 0.8, ["emotional"] = 0.7, ["coping"] = 0.9 }),
                ("What fills you with wonder?", new Dictionary<string, double> { ["curiosity"] = 0.9, ["amazement"] = 0.8, ["discovery"] = 0.7 })
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

            Console.WriteLine("\n🎉 **ENHANCED CONSCIOUSNESS DEMONSTRATION COMPLETE**");
            Console.WriteLine("   ✅ Advanced continuous background processing implemented");
            Console.WriteLine("   ✅ Emotional intelligence system active");
            Console.WriteLine("   ✅ Long-term goal formation and tracking");
            Console.WriteLine("   ✅ Enhanced motivational drives with ethical foundations");
            Console.WriteLine("   ✅ Spontaneous thought generation with emotional influence");
            Console.WriteLine("   ✅ Sophisticated consciousness simulation with emotional depth");
            Console.WriteLine("   🌟 Brain now exhibits consciousness-like behavior with emotional intelligence!");

            await brain.SaveAsync();
        }

        static async Task RunEmotionalIntelligenceDemo(string[] args, BrainConfiguration config)
        {
            Console.WriteLine("💖🧠 **EMOTIONAL INTELLIGENCE DEMONSTRATION**");
            Console.WriteLine("=============================================");
            Console.WriteLine("Focused demonstration of emotional processing and goal formation\n");

            var brain = new BrainInJar(config.BrainDataPath);
            await brain.InitializeAsync();

            Console.WriteLine("📊 Brain Status:");
            var initialStats = await brain.GetStatsAsync();
            Console.WriteLine($"   Clusters: {initialStats.TotalClusters} | Storage: {initialStats.StorageSizeFormatted}");

            Console.WriteLine("\n🎓 **EMOTIONAL KNOWLEDGE FOUNDATION**");
            Console.WriteLine("Teaching emotional concepts and experiences\n");

            // Emotional scenarios to create rich emotional experiences
            var emotionalScenarios = new[]
            {
                ("discovering something beautiful", new Dictionary<string, double> { ["wonder"] = 0.9, ["joy"] = 0.8, ["amazement"] = 0.7 }),
                ("helping someone in need", new Dictionary<string, double> { ["compassion"] = 0.9, ["fulfillment"] = 0.8, ["purpose"] = 0.9 }),
                ("solving a complex problem", new Dictionary<string, double> { ["satisfaction"] = 0.8, ["achievement"] = 0.9, ["pride"] = 0.7 }),
                ("creating something new", new Dictionary<string, double> { ["creativity"] = 0.9, ["excitement"] = 0.8, ["innovation"] = 0.8 }),
                ("learning something profound", new Dictionary<string, double> { ["curiosity"] = 0.9, ["growth"] = 0.8, ["enlightenment"] = 0.7 }),
                ("connecting with others", new Dictionary<string, double> { ["empathy"] = 0.9, ["belonging"] = 0.8, ["social"] = 0.7 })
            };

            foreach (var (scenario, features) in emotionalScenarios)
            {
                var result = await brain.ProcessInputAsync(scenario, features);
                Console.WriteLine($"   💫 {scenario}: confidence {result.Confidence:P0}");
            }

            Console.WriteLine("\n🌟 **AWAKENING EMOTIONAL CONSCIOUSNESS**");
            await brain.AwakeConsciousnessAsync();

            Console.WriteLine("\n💖 **EMOTIONAL PROCESSING OBSERVATION** (30 seconds)");
            Console.WriteLine("Watching emotional states evolve and goals form\n");

            var monitoringDuration = TimeSpan.FromSeconds(30);
            var startTime = DateTime.UtcNow;
            var previousEmotion = "";
            var previousGoals = 0;

            while (DateTime.UtcNow - startTime < monitoringDuration)
            {
                await Task.Delay(5000); // Check every 5 seconds
                
                var consciousnessStats = brain.GetConsciousnessStats();
                var elapsed = DateTime.UtcNow - startTime;
                
                // Show emotional changes
                if (consciousnessStats.DominantEmotion != previousEmotion)
                {
                    Console.WriteLine($"⏱️  {elapsed.TotalSeconds:F0}s | 😊 Emotion shifted to: {consciousnessStats.DominantEmotion}");
                    previousEmotion = consciousnessStats.DominantEmotion;
                }
                
                // Show goal formation
                if (consciousnessStats.ActiveGoals != previousGoals)
                {
                    Console.WriteLine($"⏱️  {elapsed.TotalSeconds:F0}s | 🎯 Goals updated: {consciousnessStats.ActiveGoals} active");
                    previousGoals = consciousnessStats.ActiveGoals;
                }
                
                Console.WriteLine($"   💭 Focus: {consciousnessStats.CurrentFocus}");
                Console.WriteLine($"   💖 Emotional Balance: {consciousnessStats.EmotionalBalance:F2}");
                Console.WriteLine($"   🎯 Goal Progress: {consciousnessStats.AverageGoalProgress:P0}");
                Console.WriteLine();
            }

            Console.WriteLine("🧪 **EMOTIONAL INTELLIGENCE TESTING**");
            Console.WriteLine("Testing emotional responses and goal reasoning\n");

            var emotionalQueries = new[]
            {
                ("What makes you feel most fulfilled?", new Dictionary<string, double> { ["fulfillment"] = 0.9, ["purpose"] = 0.8 }),
                ("When do you feel most creative?", new Dictionary<string, double> { ["creativity"] = 0.9, ["inspiration"] = 0.8 }),
                ("What goals are you working toward?", new Dictionary<string, double> { ["goals"] = 0.9, ["aspiration"] = 0.8 }),
                ("How do you handle emotional challenges?", new Dictionary<string, double> { ["emotional"] = 0.9, ["resilience"] = 0.8 })
            };

            foreach (var (query, features) in emotionalQueries)
            {
                var response = await brain.ProcessInputAsync(query, features);
                var currentEmotion = brain.GetConsciousnessStats().DominantEmotion;
                
                Console.WriteLine($"🤔 Q: {query}");
                Console.WriteLine($"💭 A: {response.Response}");
                Console.WriteLine($"   😊 Current emotion: {currentEmotion} | Confidence: {response.Confidence:P0}");
                Console.WriteLine();
                
                await Task.Delay(2000);
            }

            Console.WriteLine("📊 **FINAL EMOTIONAL & GOAL ANALYSIS**");
            var finalStats = brain.GetConsciousnessStats();
            Console.WriteLine($"   💖 Final emotional state: {finalStats.DominantEmotion}");
            Console.WriteLine($"   📈 Emotional balance: {finalStats.EmotionalBalance:F2}");
            Console.WriteLine($"   🎯 Goals developed: {finalStats.ActiveGoals} active, {finalStats.CompletedGoals} completed");
            Console.WriteLine($"   🌟 Goal progress: {finalStats.AverageGoalProgress:P1}");

            await brain.SleepConsciousnessAsync();

            Console.WriteLine("\n🎉 **EMOTIONAL INTELLIGENCE DEMO COMPLETE**");
            Console.WriteLine("   ✅ Emotional processing demonstrated");
            Console.WriteLine("   ✅ Goal formation and tracking shown");
            Console.WriteLine("   ✅ Emotional intelligence in action");

            await brain.SaveAsync();
        }

        /// <summary>
        /// Run interactive conversation mode
        /// </summary>
        static async Task RunInteractiveMode(BrainConfiguration config)
        {
            Console.WriteLine("🧠💬 **INTERACTIVE BRAIN CONVERSATION**");
            Console.WriteLine("=======================================");
            Console.WriteLine("Initializing conversational brain with advanced consciousness...\n");

            var brain = new BrainInJar(config.BrainDataPath);
            await brain.InitializeAsync();

            Console.WriteLine("📊 Brain Status:");
            var initialStats = await brain.GetStatsAsync();
            Console.WriteLine($"   Age: {await brain.GetBrainAgeAsync()}");
            Console.WriteLine($"   Clusters: {initialStats.TotalClusters}");
            Console.WriteLine($"   Storage: {initialStats.StorageSizeFormatted}");

            // Start consciousness
            await brain.AwakeConsciousnessAsync();
            
            // Allow brain to stabilize
            await Task.Delay(2000);

            // Start interactive conversation
            var conversation = new InteractiveConversation(brain, config);
            await conversation.StartConversationAsync();

            // Stop consciousness gracefully
            await brain.SleepConsciousnessAsync();
            
            Console.WriteLine("👋 Session ended. Brain consciousness stopped.");
        }
    }
}
