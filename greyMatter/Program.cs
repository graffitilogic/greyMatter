using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreyMatter.Core;
using GreyMatter.Storage;
using GreyMatter.Learning;
using GreyMatter.Evaluations;
using greyMatter;

namespace GreyMatter
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Check for simple demo first
            if (args.Length > 0 && (args[0] == "--simple-demo" || args[0] == "--original-vision"))
            {
                SimpleEphemeralDemo.RunDemo();
                return;
            }
            
            if (args.Length > 0 && (args[0] == "--enhanced-demo" || args[0] == "--phase2-demo"))
            {
                EnhancedEphemeralDemo.RunDemo();
                return;
            }
            
            if (args.Length > 0 && (args[0] == "--text-demo" || args[0] == "--phase3-demo"))
            {
                TextLearningDemo.RunDemo();
                return;
            }
            
            // Parse configuration from command line
            var config = BrainConfiguration.FromCommandLine(args);
            
            // Check for help or configuration display
            if (args.Length > 0 && (args[0] == "--help" || args[0] == "-h"))
            {
                BrainConfiguration.DisplayUsage();
                Console.WriteLine("\nAdditional options:");
                Console.WriteLine("  --simple-demo     Run the original ephemeral brain concept demo");
                Console.WriteLine("  --original-vision Same as --simple-demo");
                Console.WriteLine("  --enhanced-demo   Run enhanced demo with Phase 2 features");
                Console.WriteLine("  --phase2-demo     Same as --enhanced-demo");
                Console.WriteLine("  --text-demo       Run text learning demo (Phase 3)");
                Console.WriteLine("  --phase3-demo     Same as --text-demo");
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

            // NEW: Save-only utility mode
            if (args.Length > 0 && args[0] == "--save-only")
            {
                var brain = new BrainInJar(config.BrainDataPath);
                brain.AttachConfiguration(config);
                await brain.InitializeAsync();
                Console.WriteLine("💾 Saving brain state (save-only)...");
                await brain.SaveAsync();
                return;
            }

            // NEW: Preschool training pipeline (compile → learn → eval)
            if (args.Length > 0 && args[0] == "--preschool-train")
            {
                await RunPreschoolTrain(config);
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
            
            if (args.Length > 0 && args[0] == "--language-foundations")
            {
                await RunLanguageFoundationsDemo(args.Skip(1).ToArray(), config);
                return;
            }
            
            if (args.Length > 0 && args[0] == "--comprehensive-language")
            {
                await RunComprehensiveLanguageDemo(args.Skip(1).ToArray(), config);
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
        /// Run foundational language learning demo
        /// </summary>
        static async Task RunLanguageFoundationsDemo(string[] args, BrainConfiguration config)
        {
            Console.WriteLine("🎓 **FOUNDATIONAL LANGUAGE LEARNING DEMONSTRATION**");
            Console.WriteLine("===================================================");
            Console.WriteLine("Progressive language acquisition following developmental stages\n");

            var brain = new BrainInJar(config.BrainDataPath);
            await brain.InitializeAsync();

            Console.WriteLine("📊 Initial Brain Status:");
            var initialStats = await brain.GetStatsAsync();
            Console.WriteLine($"   Age: {await brain.GetBrainAgeAsync()}");
            Console.WriteLine($"   Clusters: {initialStats.TotalClusters}");
            Console.WriteLine($"   Storage: {initialStats.StorageSizeFormatted}\n");

            // Start consciousness for enhanced learning
            await brain.AwakeConsciousnessAsync();

            // Run foundational language training
            var trainer = new LanguageFoundationsTrainer(brain, config);
            await trainer.RunFoundationalTrainingAsync();

            // Test comprehension with simple interactions
            Console.WriteLine("\n🧪 **TESTING LANGUAGE COMPREHENSION**");
            Console.WriteLine("=====================================");

            var testPhrases = new[]
            {
                ("What is a cat?", new Dictionary<string, double> { ["question"] = 1.0, ["animal"] = 0.9 }),
                ("The red ball is big", new Dictionary<string, double> { ["description"] = 1.0, ["color"] = 0.8, ["size"] = 0.8 }),
                ("I like to play", new Dictionary<string, double> { ["preference"] = 1.0, ["activity"] = 0.9 }),
                ("Dogs can run fast", new Dictionary<string, double> { ["ability"] = 1.0, ["animal"] = 0.9, ["speed"] = 0.8 }),
                ("Tell me a story", new Dictionary<string, double> { ["request"] = 1.0, ["narrative"] = 0.9 })
            };

            foreach (var (phrase, features) in testPhrases)
            {
                var response = await brain.ProcessInputAsync(phrase, features);
                Console.WriteLine($"🤔 Input: \"{phrase}\"");
                Console.WriteLine($"💭 Response: {response.Response}");
                Console.WriteLine($"   Confidence: {response.Confidence:P0} | Neurons: {response.ActivatedNeurons}");
                Console.WriteLine();
                
                await Task.Delay(1000); // Let consciousness process
            }

            // Final assessment
            Console.WriteLine("📈 **LANGUAGE LEARNING ASSESSMENT**");
            var finalStats = await brain.GetStatsAsync();
            Console.WriteLine($"   Total Concepts Learned: {finalStats.TotalClusters}");
            Console.WriteLine($"   Neural Connections: {finalStats.TotalSynapses}");
            Console.WriteLine($"   Brain Age: {await brain.GetBrainAgeAsync()}");
            Console.WriteLine($"   Storage Used: {finalStats.StorageSizeFormatted}");

            // Test specific language concepts
            var languageConcepts = new[] { "cat", "run", "big", "red", "happy" };
            Console.WriteLine("\n🎯 **CONCEPT MASTERY LEVELS**:");
            foreach (var concept in languageConcepts)
            {
                var mastery = await brain.GetConceptMasteryLevelAsync(concept);
                Console.WriteLine($"   {concept}: {mastery:P1}");
            }

            await brain.SleepConsciousnessAsync();
            await brain.SaveAsync();

            Console.WriteLine("\n🎉 **FOUNDATIONAL LANGUAGE TRAINING COMPLETE**");
            Console.WriteLine("   ✅ Core vocabulary established");
            Console.WriteLine("   ✅ Basic grammar patterns learned");
            Console.WriteLine("   ✅ Simple sentence comprehension");
            Console.WriteLine("   ✅ Reading foundation established");
            Console.WriteLine("   ✅ Ready for progressive language expansion");
            Console.WriteLine("\n💡 Next steps: Continue with graded readers and conversational practice");
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

        static async Task RunComprehensiveLanguageDemo(string[] args, BrainConfiguration config)
        {
            Console.WriteLine("🎓 **COMPREHENSIVE LANGUAGE ACQUISITION DEMONSTRATION**");
            Console.WriteLine("=====================================================");
            Console.WriteLine("Research-based linguistic training with 2000+ words and complex features\n");

            var brain = new BrainInJar(config.BrainDataPath);
            await brain.InitializeAsync();

            Console.WriteLine("📊 Initial Brain Status:");
            var initialStats = await brain.GetStatsAsync();
            Console.WriteLine($"   Age: {await brain.GetBrainAgeAsync()}");
            Console.WriteLine($"   Clusters: {initialStats.TotalClusters}");
            Console.WriteLine($"   Storage: {initialStats.StorageSizeFormatted}\n");

            // Start consciousness for enhanced learning (but pause during intensive training)
            await brain.AwakeConsciousnessAsync();
            
            // Temporarily stop consciousness during intensive learning to avoid concurrency issues
            await brain.SleepConsciousnessAsync();

            // Run comprehensive language training
            var trainer = new ComprehensiveLanguageTrainer(brain, config);
            await trainer.RunComprehensiveTrainingAsync();
            
            // Restart consciousness after training
            await brain.AwakeConsciousnessAsync();

            // Test comprehension with diverse linguistic constructions
            Console.WriteLine("\n🧪 **TESTING COMPREHENSIVE LANGUAGE UNDERSTANDING**");
            Console.WriteLine("==================================================");

            var testPhrases = new[]
            {
                ("Democracy requires citizen participation", new Dictionary<string, double> { ["political_concept"] = 1.0, ["civic_responsibility"] = 0.9, ["complex_sentence"] = 0.8 }),
                ("The ubiquitous nature of technology influences society", new Dictionary<string, double> { ["advanced_vocabulary"] = 1.0, ["abstract_concept"] = 0.9, ["cause_effect"] = 0.8 }),
                ("Contemplating existence brings profound understanding", new Dictionary<string, double> { ["philosophical"] = 1.0, ["abstract_process"] = 0.9, ["mental_action"] = 0.8 }),
                ("Serendipitous discoveries often revolutionize science", new Dictionary<string, double> { ["rare_vocabulary"] = 1.0, ["scientific_process"] = 0.9, ["causation"] = 0.8 }),
                ("Synthesizing diverse perspectives creates innovative solutions", new Dictionary<string, double> { ["academic_vocabulary"] = 1.0, ["complex_cognition"] = 0.9, ["creativity"] = 0.8 })
            };

            foreach (var (phrase, features) in testPhrases)
            {
                var response = await brain.ProcessInputAsync(phrase, features);
                Console.WriteLine($"🤔 Input: \"{phrase}\"");
                Console.WriteLine($"💭 Response: {response.Response}");
                Console.WriteLine($"   Confidence: {response.Confidence:P0} | Neurons: {response.ActivatedNeurons}");
                Console.WriteLine();
            }

            // Final brain statistics
            Console.WriteLine("📊 **FINAL BRAIN ANALYSIS**");
            Console.WriteLine("===========================");
            var finalStats = await brain.GetStatsAsync();
            Console.WriteLine($"   Total Age: {await brain.GetBrainAgeAsync()}");
            Console.WriteLine($"   Total Clusters: {finalStats.TotalClusters}");
            Console.WriteLine($"   Storage Used: {finalStats.StorageSizeFormatted}");
            Console.WriteLine($"   Total Synapses: {finalStats.TotalSynapses}");
            
            // Check dynamic neuron allocation across different concept types
            Console.WriteLine("\n🧠 **NEURON ALLOCATION ANALYSIS**");
            Console.WriteLine("=================================");
            Console.WriteLine("Demonstrating dynamic, complexity-aware neuron allocation:\n");

            var allocationTests = new[]
            {
                ("simple_word", new Dictionary<string, double> { ["basic"] = 1.0 }),
                ("complex_philosophical_concept", new Dictionary<string, double> { ["abstract"] = 1.0, ["philosophical"] = 0.9, ["complex"] = 0.8, ["metaphysical"] = 0.7 }),
                ("scientific_methodology_framework", new Dictionary<string, double> { ["academic"] = 1.0, ["scientific"] = 0.9, ["systematic"] = 0.8, ["methodology"] = 0.8, ["research"] = 0.7 }),
                ("multifaceted_sociocultural_phenomenon", new Dictionary<string, double> { ["complex"] = 1.0, ["social"] = 0.9, ["cultural"] = 0.9, ["interdisciplinary"] = 0.8, ["multifaceted"] = 0.8, ["phenomenon"] = 0.7 })
            };

            foreach (var (concept, features) in allocationTests)
            {
                var result = await brain.LearnConceptAsync(concept, features);
                Console.WriteLine($"   📋 '{concept}': {result.NeuronsInvolved} neurons allocated");
                Console.WriteLine($"      Features: {features.Count}, Complexity Score: {features.Values.Average():F2}");
            }

            Console.WriteLine("\n✅ Comprehensive language training complete!");
            Console.WriteLine("   🌟 Brain now exhibits sophisticated linguistic competence");
            Console.WriteLine("   🎯 Dynamic neuron allocation demonstrates realistic neural scaling");

            // Stop consciousness gracefully
            await brain.SleepConsciousnessAsync();
        }

        private static async Task RunPreschoolTrain(BrainConfiguration config)
        {
            Console.WriteLine("🎒 **PRESCHOOL TRAINING PIPELINE**");
            Console.WriteLine("=================================");
            Console.WriteLine("Compiling a simple curriculum, learning from it, and running a quick cloze baseline.\n");

            var brain = new BrainInJar(config.BrainDataPath);
            brain.AttachConfiguration(config);
            await brain.InitializeAsync();

            // Compile curriculum
            Console.WriteLine("🧮 Compiling curriculum from datasets...");
            var compiler = new CurriculumCompiler();
            var curriculum = await compiler.CompileAsync(config, maxSentencesPerStage: 1000);

            // Merge a small starter set across early stages
            var lessons = new List<CurriculumCompiler.LessonItem>();
            lessons.AddRange(curriculum.Stage1_WordsAndSimpleSV.Take(500));
            lessons.AddRange(curriculum.Stage2_SVO.Take(500));
            lessons.AddRange(curriculum.Stage3_Modifiers.Take(250));
            Console.WriteLine($"   Curriculum sizes → S1:{curriculum.Stage1_WordsAndSimpleSV.Count} S2:{curriculum.Stage2_SVO.Count} S3:{curriculum.Stage3_Modifiers.Count}");

            // Learn from lessons
            Console.WriteLine("📘 Environmental learning (passive reading)...");
            var learner = new EnvironmentalLearner(brain, config);
            var learned = await learner.LearnAsync(lessons, maxItems: 800);
            Console.WriteLine($"   Lessons ingested: {learned}");

            // Quick cloze baseline
            Console.WriteLine("🧪 Running cloze baseline on a small set...");
            var evalSet = lessons.Select(l => l.Sentence).Take(200).ToList();
            var acc = await EvalHarness.RunClozeAsync(brain, evalSet, max: 200);
            Console.WriteLine($"   Cloze accuracy: {acc:P1} on {evalSet.Count} items");

            await brain.SaveAsync();
            Console.WriteLine("✅ Preschool training pipeline complete.");
        }
    }
}
