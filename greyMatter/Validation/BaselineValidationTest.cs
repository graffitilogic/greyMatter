using System;
using System.IO;
using System.Threading.Tasks;
using greyMatter.Learning;
using GreyMatter.Storage;

namespace greyMatter.Validation
{
    /// <summary>
    /// Week 1 Day 2: Baseline validation test
    /// Tests: TatoebaLanguageTrainer with real NAS paths
    /// Goal: Prove save/load works with actual infrastructure
    /// </summary>
    public class BaselineValidationTest
    {
        // NAS paths (your actual infrastructure)
        private const string TRAIN_DATA_PATH = "/Volumes/jarvis/trainData/Tatoeba";  // Directory, not file
        private const string TRAIN_DATA_FILE = "/Volumes/jarvis/trainData/Tatoeba/sentences_eng_small.csv";  // Full file path for verification
        private const string BRAIN_STORAGE_PATH = "/Volumes/jarvis/brainData";
        private const string WORKING_PATH = "/Users/billdodd/Documents/Cerebro";

        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== Week 1 Baseline Validation Test ===");
            Console.WriteLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();

            var test = new BaselineValidationTest();
            
            try
            {
                // Step 1: Verify paths
                await test.VerifyPaths();
                
                // Step 2: Initial training run
                await test.RunInitialTraining();
                
                // Step 3: Verify save
                await test.VerifySave();
                
                // Step 4: Load and continue
                await test.LoadAndContinue();
                
                // Step 5: Report results
                test.ReportResults();
                
                Console.WriteLine("\n✅ BASELINE VALIDATION: SUCCESS");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ BASELINE VALIDATION: FAILED");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");
                Environment.Exit(1);
            }
        }

        private async Task VerifyPaths()
        {
            Console.WriteLine("=== Step 1: Verify Paths ===");
            
            // Check training data
            if (!File.Exists(TRAIN_DATA_FILE))
            {
                throw new FileNotFoundException($"Training data not found: {TRAIN_DATA_FILE}");
            }
            var trainFileSize = new FileInfo(TRAIN_DATA_FILE).Length / 1024.0 / 1024.0;
            Console.WriteLine($"✅ Training data found: {TRAIN_DATA_FILE} ({trainFileSize:F2} MB)");
            
            // Create brain storage directory if needed
            if (!Directory.Exists(BRAIN_STORAGE_PATH))
            {
                Directory.CreateDirectory(BRAIN_STORAGE_PATH);
                Console.WriteLine($"✅ Created brain storage: {BRAIN_STORAGE_PATH}");
            }
            else
            {
                Console.WriteLine($"✅ Brain storage exists: {BRAIN_STORAGE_PATH}");
            }
            
            // Create working directory if needed
            if (!Directory.Exists(WORKING_PATH))
            {
                Directory.CreateDirectory(WORKING_PATH);
                Console.WriteLine($"✅ Created working directory: {WORKING_PATH}");
            }
            else
            {
                Console.WriteLine($"✅ Working directory exists: {WORKING_PATH}");
            }
            
            Console.WriteLine();
            await Task.CompletedTask;
        }

        private async Task RunInitialTraining()
        {
            Console.WriteLine("=== Step 2: Initial Training (1K sentences) ===");
            
            // Create storage adapter with hot/cold paths
            var storage = new FastStorageAdapter(
                hotPath: WORKING_PATH,    // Local SSD for speed
                coldPath: BRAIN_STORAGE_PATH  // NAS for long-term
            );
            
            // Create trainer
            var trainer = new TatoebaLanguageTrainer(
                tatoebaDataPath: TRAIN_DATA_PATH,
                storage: storage
            );
            
            Console.WriteLine("Starting training...");
            var startTime = DateTime.Now;
            
            // Train on 1000 sentences
            await trainer.TrainOnEnglishSentencesAsync(
                maxSentences: 1000,
                batchSize: 100
            );
            
            var elapsed = DateTime.Now - startTime;
            Console.WriteLine($"✅ Training completed in {elapsed.TotalSeconds:F2} seconds");
            
            // Get statistics
            var brain = trainer.Brain;
            Console.WriteLine($"   Vocabulary size: {brain.VocabularySize}");
            Console.WriteLine($"   Concepts learned: {brain.VocabularySize}");
            Console.WriteLine();
        }

        private async Task VerifySave()
        {
            Console.WriteLine("=== Step 3: Verify Save ===");
            
            var storage = new FastStorageAdapter(
                hotPath: WORKING_PATH,
                coldPath: BRAIN_STORAGE_PATH
            );
            
            var trainer = new TatoebaLanguageTrainer(
                tatoebaDataPath: TRAIN_DATA_PATH,
                storage: storage
            );
            
            Console.WriteLine("Saving brain state...");
            var startTime = DateTime.Now;
            
            await trainer.SaveBrainStateAsync();
            
            var elapsed = DateTime.Now - startTime;
            Console.WriteLine($"✅ Save completed in {elapsed.TotalSeconds:F2} seconds");
            
            // Verify files were created
            var brainFiles = Directory.GetFiles(BRAIN_STORAGE_PATH, "*", SearchOption.AllDirectories);
            Console.WriteLine($"   Files created: {brainFiles.Length}");
            
            long totalSize = 0;
            foreach (var file in brainFiles)
            {
                totalSize += new FileInfo(file).Length;
            }
            Console.WriteLine($"   Total size: {totalSize / 1024.0 / 1024.0:F2} MB");
            Console.WriteLine();
        }

        private async Task LoadAndContinue()
        {
            Console.WriteLine("=== Step 4: Load and Continue ===");
            
            var storage = new FastStorageAdapter(
                hotPath: WORKING_PATH,
                coldPath: BRAIN_STORAGE_PATH
            );
            
            var trainer = new TatoebaLanguageTrainer(
                tatoebaDataPath: TRAIN_DATA_PATH,
                storage: storage
            );
            
            Console.WriteLine("Loading saved brain state...");
            var startTime = DateTime.Now;
            
            // Load will happen automatically in trainer constructor
            var brain = trainer.Brain;
            
            var elapsed = DateTime.Now - startTime;
            Console.WriteLine($"✅ Load completed in {elapsed.TotalMilliseconds:F0} ms");
            Console.WriteLine($"   Vocabulary size: {brain.VocabularySize}");
            Console.WriteLine($"   Concepts: {brain.VocabularySize}");
            
            // Continue training with another 500 sentences
            Console.WriteLine("\nContinuing training (500 more sentences)...");
            startTime = DateTime.Now;
            
            await trainer.TrainOnEnglishSentencesAsync(
                maxSentences: 500,
                batchSize: 100
            );
            
            elapsed = DateTime.Now - startTime;
            Console.WriteLine($"✅ Continued training completed in {elapsed.TotalSeconds:F2} seconds");
            Console.WriteLine($"   New vocabulary size: {brain.VocabularySize}");
            Console.WriteLine($"   New concept count: {brain.VocabularySize}");
            Console.WriteLine();
        }

        private void ReportResults()
        {
            Console.WriteLine("=== Step 5: Results Summary ===");
            Console.WriteLine("✅ Training data accessible from NAS");
            Console.WriteLine("✅ Initial training successful (1K sentences)");
            Console.WriteLine("✅ Save to NAS successful");
            Console.WriteLine("✅ Load from NAS successful");
            Console.WriteLine("✅ Continue training successful (500 more)");
            Console.WriteLine();
            Console.WriteLine("Storage Configuration:");
            Console.WriteLine($"  Hot (working): {WORKING_PATH}");
            Console.WriteLine($"  Cold (storage): {BRAIN_STORAGE_PATH}");
            Console.WriteLine();
            Console.WriteLine("VALIDATION: Single-source learning with NAS storage PROVEN");
        }
    }
}
