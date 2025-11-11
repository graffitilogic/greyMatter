using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using greyMatter.Core;  // IntegratedTrainer lives here (lowercase namespace)
using GreyMatter.Learning;
using GreyMatter.Storage;

namespace GreyMatter.Demos
{
    /// <summary>
    /// Actually interesting demo: Shows attention mechanisms dynamically prioritizing
    /// novel/interesting content while skipping repetitive boring stuff
    /// </summary>
    public class AttentionShowcase
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  ATTENTION SHOWCASE: Dynamic Learning with Smart Focus           ║");
            Console.WriteLine("║  Watch how the system learns to ignore boring repetitive stuff   ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════╝\n");

            // Use centralized production storage
            var storage = new ProductionStorageManager();
            
            var brain = new LanguageEphemeralBrain();
            
            // Create three trainers with different attention strategies
            var baselineTrainer = new IntegratedTrainer(
                brain,
                enableColumnProcessing: true,
                enableTraditionalLearning: true,
                enableIntegration: true,
                enableAttention: false,
                enableEpisodicMemory: false
            );

            var attentionTrainer = new IntegratedTrainer(
                brain,
                enableColumnProcessing: true,
                enableTraditionalLearning: true,
                enableIntegration: true,
                enableAttention: true,
                enableEpisodicMemory: false,
                attentionThreshold: 0.5 // Medium selectivity
            );

            var smartTrainer = new IntegratedTrainer(
                brain,
                enableColumnProcessing: true,
                enableTraditionalLearning: true,
                enableIntegration: true,
                enableAttention: true,
                enableEpisodicMemory: true,
                attentionThreshold: 0.5,
                episodicMemoryPath: storage.GetEpisodicMemoryPath()
            );

            // Scenario 1: Boring repetitive data (attention should skip most of it)
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine("SCENARIO 1: Boring Repetitive Content");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════\n");
            
            var boringData = GenerateBoringRepetitiveData();
            Console.WriteLine($"Dataset: {boringData.Count} sentences (99% repetitive)\n");
            
            await CompareTrainers("Boring Repetitive", boringData, baselineTrainer, attentionTrainer, smartTrainer);

            // Scenario 2: Novel interesting data (attention should process everything)
            Console.WriteLine("\n═══════════════════════════════════════════════════════════════════");
            Console.WriteLine("SCENARIO 2: Novel Interesting Content");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════\n");
            
            var novelData = GenerateNovelInterestingData();
            Console.WriteLine($"Dataset: {novelData.Count} sentences (100% unique)\n");
            
            await CompareTrainers("Novel Content", novelData, baselineTrainer, attentionTrainer, smartTrainer);

            // Scenario 3: Mixed data stream (realistic - some new, lots of noise)
            Console.WriteLine("\n═══════════════════════════════════════════════════════════════════");
            Console.WriteLine("SCENARIO 3: Realistic Mixed Stream (Signal + Noise)");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════\n");
            
            var mixedData = GenerateMixedDataStream();
            Console.WriteLine($"Dataset: {mixedData.Count} sentences (20% novel, 80% repetitive noise)\n");
            
            await CompareTrainers("Mixed Stream", mixedData, baselineTrainer, attentionTrainer, smartTrainer);

            // Scenario 4: Concept drift (patterns change over time - attention adapts)
            Console.WriteLine("\n═══════════════════════════════════════════════════════════════════");
            Console.WriteLine("SCENARIO 4: Concept Drift (Patterns Change Over Time)");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════\n");
            
            var driftData = GenerateConceptDriftData();
            Console.WriteLine($"Dataset: {driftData.Count} sentences (patterns evolve from topic A → B → C)\n");
            
            await CompareTrainers("Concept Drift", driftData, baselineTrainer, attentionTrainer, smartTrainer);

            Console.WriteLine("\n╔═══════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  KEY INSIGHTS:                                                    ║");
            Console.WriteLine("║  • Attention saves time on boring repetitive data                 ║");
            Console.WriteLine("║  • Attention has minimal overhead on novel interesting data       ║");
            Console.WriteLine("║  • Episodic memory helps track what's been seen before            ║");
            Console.WriteLine("║  • System adapts to concept drift by refocusing attention         ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════╝\n");
        }

        static async Task CompareTrainers(
            string scenarioName,
            List<string> sentences,
            IntegratedTrainer baseline,
            IntegratedTrainer attention,
            IntegratedTrainer smart)
        {
            var sw = Stopwatch.StartNew();
            foreach (var sent in sentences)
                await baseline.TrainOnSentenceAsync(sent);
            var baselineMs = sw.Elapsed.TotalMilliseconds;
            var baselineStats = baseline.GetStats();

            sw.Restart();
            foreach (var sent in sentences)
                await attention.TrainOnSentenceAsync(sent);
            var attentionMs = sw.Elapsed.TotalMilliseconds;
            var attentionStats = attention.GetStats();

            sw.Restart();
            foreach (var sent in sentences)
                await smart.TrainOnSentenceAsync(sent);
            var smartMs = sw.Elapsed.TotalMilliseconds;
            var smartStats = smart.GetStats();

            // Extract attention metrics
            var attentionSkipped = attentionStats.PatternsSkippedByAttention;
            var attentionTotal = attentionSkipped + attentionStats.PatternsDetected;
            var attentionSkipPct = attentionTotal > 0 ? (attentionSkipped * 100.0 / attentionTotal) : 0;

            var smartSkipped = smartStats.PatternsSkippedByAttention;
            var smartTotal = smartSkipped + smartStats.PatternsDetected;
            var smartSkipPct = smartTotal > 0 ? (smartSkipped * 100.0 / smartTotal) : 0;

            Console.WriteLine($"┌─────────────────┬──────────────┬──────────────┬──────────────┐");
            Console.WriteLine($"│ Trainer         │ Time (ms)    │ Speedup      │ Skip Rate    │");
            Console.WriteLine($"├─────────────────┼──────────────┼──────────────┼──────────────┤");
            Console.WriteLine($"│ Baseline        │ {baselineMs,12:F1} │ {1.0,12:F2}x │ {0,11:F1}%  │");
            Console.WriteLine($"│ Attention       │ {attentionMs,12:F1} │ {baselineMs/attentionMs,12:F2}x │ {attentionSkipPct,11:F1}%  │");
            Console.WriteLine($"│ Smart (Attn+Ep) │ {smartMs,12:F1} │ {baselineMs/smartMs,12:F2}x │ {smartSkipPct,11:F1}%  │");
            Console.WriteLine($"└─────────────────┴──────────────┴──────────────┴──────────────┘");

            // Show what was actually learned
            Console.WriteLine($"\n💡 Patterns detected:");
            Console.WriteLine($"   Baseline: {baselineStats.PatternsDetected}");
            Console.WriteLine($"   Attention: {attentionStats.PatternsDetected} " +
                            $"(skipped {attentionSkipped})");
            Console.WriteLine($"   Smart: {smartStats.PatternsDetected} " +
                            $"(skipped {smartSkipped}, episodes: {smartStats.EpisodesRecorded})");
        }

        // Generate boring repetitive data (99% same sentences)
        static List<string> GenerateBoringRepetitiveData()
        {
            var data = new List<string>();
            
            // Add the same boring sentence 95 times
            for (int i = 0; i < 95; i++)
            {
                data.Add("The cat sat on the mat.");
            }
            
            // Add a few variations
            data.Add("The dog sat on the mat.");
            data.Add("The cat stood on the mat.");
            data.Add("The cat sat on the rug.");
            data.Add("The kitten sat on the mat.");
            data.Add("The cat sat on the floor.");
            
            return Shuffle(data);
        }

        // Generate novel interesting data (100% unique)
        static List<string> GenerateNovelInterestingData()
        {
            return new List<string>
            {
                "Quantum entanglement defies classical physics.",
                "The Higgs boson gives particles their mass.",
                "Dark matter comprises most of the universe.",
                "Photosynthesis converts light into chemical energy.",
                "DNA encodes genetic information in base pairs.",
                "Neural networks learn through backpropagation.",
                "Blockchain creates immutable distributed ledgers.",
                "CRISPR allows precise gene editing.",
                "Fusion reactions power stars like our sun.",
                "Black holes warp spacetime infinitely.",
                "Neurons communicate via synaptic transmission.",
                "Mitochondria generate cellular energy.",
                "Gravitational waves ripple through spacetime.",
                "Antibodies recognize specific antigens.",
                "Enzymes catalyze biochemical reactions.",
                "Chromosomes carry hereditary information.",
                "Transistors amplify electronic signals.",
                "Proteins fold into functional shapes.",
                "Galaxies cluster in cosmic filaments.",
                "Neurons form complex networks.",
                "Electrons orbit atomic nuclei.",
                "RNA translates genetic instructions.",
                "Supernovae forge heavy elements.",
                "Circuits process binary information.",
                "Neurons fire action potentials.",
                "Ribosomes synthesize protein chains.",
                "Neutron stars spin incredibly fast.",
                "Algorithms solve computational problems.",
                "Dendrites receive synaptic inputs.",
                "Nucleotides form DNA strands.",
                "Pulsars emit radio beams.",
                "Networks transmit data packets.",
                "Axons conduct neural signals.",
                "Codons specify amino acids.",
                "Quasars shine across cosmic distances.",
                "Processors execute machine instructions.",
                "Synapses strengthen with repetition.",
                "Genes express specific proteins.",
                "Comets orbit the outer solar system.",
                "Memory stores information patterns.",
                "Evolution shapes living organisms.",
                "Asteroids impact planetary surfaces.",
                "Learning modifies neural connections.",
                "Mutations introduce genetic variation.",
                "Planets orbit their parent stars.",
                "Cognition emerges from neural activity.",
                "Selection favors adaptive traits.",
                "Moons orbit their planets.",
                "Intelligence reflects problem-solving ability.",
                "Heredity passes traits to offspring.",
                "Nebulae birth new star systems."
            };
        }

        // Generate mixed stream (20% signal, 80% noise)
        static List<string> GenerateMixedDataStream()
        {
            var data = new List<string>();
            
            // 80% boring noise
            var noise = new[] 
            { 
                "Hello world.",
                "How are you?",
                "Nice weather today.",
                "I like coffee.",
                "The sky is blue."
            };
            
            for (int i = 0; i < 80; i++)
            {
                data.Add(noise[i % noise.Length]);
            }
            
            // 20% interesting signal
            data.Add("Superconductors conduct electricity without resistance.");
            data.Add("Quantum computers use qubits instead of bits.");
            data.Add("Graphene is stronger than steel.");
            data.Add("Antimatter annihilates with matter.");
            data.Add("Stem cells differentiate into specialized cells.");
            data.Add("Prions are misfolded proteins that cause disease.");
            data.Add("Telomeres protect chromosome ends.");
            data.Add("Neurotransmitters cross synaptic clefts.");
            data.Add("Ribozymes are catalytic RNA molecules.");
            data.Add("Exoplanets orbit distant stars.");
            data.Add("Magnetars have incredibly strong magnetic fields.");
            data.Add("Topological insulators conduct on their surface.");
            data.Add("Epigenetics modifies gene expression.");
            data.Add("Metamaterials have engineered properties.");
            data.Add("Myelin sheaths insulate neural axons.");
            data.Add("Organoids are miniature organ models.");
            data.Add("Perovskites enable efficient solar cells.");
            data.Add("Microbiomes influence host health.");
            data.Add("Optogenetics controls neurons with light.");
            data.Add("Nanobots could deliver targeted therapies.");
            
            return Shuffle(data);
        }

        // Generate concept drift (patterns change: A→B→C)
        static List<string> GenerateConceptDriftData()
        {
            var data = new List<string>();
            
            // Phase A: Animal behavior (repeated 20x)
            var phaseA = new[] 
            {
                "The lion hunts at night.",
                "Birds migrate in winter.",
                "Dolphins communicate with clicks."
            };
            for (int i = 0; i < 20; i++)
                data.Add(phaseA[i % phaseA.Length]);
            
            // Phase B: Technology (repeated 20x)
            var phaseB = new[] 
            {
                "Computers process information rapidly.",
                "Satellites orbit the Earth.",
                "Lasers emit coherent light."
            };
            for (int i = 0; i < 20; i++)
                data.Add(phaseB[i % phaseB.Length]);
            
            // Phase C: Space exploration (repeated 20x)
            var phaseC = new[] 
            {
                "Rockets launch payloads into orbit.",
                "Rovers explore the Martian surface.",
                "Telescopes observe distant galaxies."
            };
            for (int i = 0; i < 20; i++)
                data.Add(phaseC[i % phaseC.Length]);
            
            // Don't shuffle - concept drift is temporal!
            return data;
        }

        static List<string> Shuffle(List<string> list)
        {
            var rng = new Random(42); // Fixed seed for reproducibility
            return list.OrderBy(_ => rng.Next()).ToList();
        }
    }
}
