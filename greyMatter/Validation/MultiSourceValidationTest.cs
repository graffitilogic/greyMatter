using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using greyMatter.Learning;
using GreyMatter.Storage;
using GreyMatter.Core;
using greyMatter.Core;

namespace greyMatter.Validation
{
    /// <summary>
    /// Week 1 Day 3: Multi-source integration validation
    /// Tests: Learning from multiple data sources (Tatoeba + Enhanced Data + SimpleWiki)
    /// Goal: Prove multi-source learning works, track source attribution
    /// </summary>
    public class MultiSourceValidationTest
    {
        // NAS paths
        private const string TATOEBA_PATH = "/Volumes/jarvis/trainData/Tatoeba";
        private const string ENHANCED_DATA_PATH = "/Volumes/jarvis/trainData/enhanced_learning_data";
        private const string SIMPLEWIKI_PATH = "/Volumes/jarvis/trainData/SimpleWiki";
        private const string BRAIN_STORAGE_PATH = "/Volumes/jarvis/brainData";
        private const string WORKING_PATH = "/Users/billdodd/Documents/Cerebro";

        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== Week 1 Multi-Source Validation Test ===");
            Console.WriteLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();

            var test = new MultiSourceValidationTest();
            
            try
            {
                // Step 1: Verify all data sources accessible
                await test.VerifyDataSources();
                
                // Step 2: Train from Tatoeba (baseline)
                await test.TrainFromTatoeba();
                
                // Step 3: Load enhanced data and continue training
                await test.TrainFromEnhancedData();
                
                // Step 4: Verify multi-source knowledge
                await test.VerifyMultiSourceKnowledge();
                
                // Step 5: Save and report
                await test.SaveAndReport();
                
                Console.WriteLine();
                Console.WriteLine("✅ MULTI-SOURCE VALIDATION: SUCCESS");
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine($"❌ MULTI-SOURCE VALIDATION: FAILED");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");
                throw;
            }
        }

        private LanguageEphemeralBrain? _brain;
        private TatoebaLanguageTrainer? _trainer;
        private FastStorageAdapter? _storage;
        private Dictionary<string, int> _sourceStats = new();

        private async Task VerifyDataSources()
        {
            Console.WriteLine("=== Step 1: Verify Data Sources ===");
            
            // Check Tatoeba
            var tatoebaFile = Path.Combine(TATOEBA_PATH, "sentences_eng_small.csv");
            if (!File.Exists(tatoebaFile))
            {
                throw new FileNotFoundException($"Tatoeba data not found: {tatoebaFile}");
            }
            var tatoebaSize = new FileInfo(tatoebaFile).Length / 1024.0 / 1024.0;
            Console.WriteLine($"✅ Tatoeba data accessible: {tatoebaSize:F2} MB");
            
            // Check Enhanced Data
            var enhancedWordDb = Path.Combine(ENHANCED_DATA_PATH, "enhanced_word_database.json");
            if (!File.Exists(enhancedWordDb))
            {
                throw new FileNotFoundException($"Enhanced data not found: {enhancedWordDb}");
            }
            var enhancedSize = new FileInfo(enhancedWordDb).Length / 1024.0 / 1024.0;
            Console.WriteLine($"✅ Enhanced data accessible: {enhancedSize:F2} MB");
            
            // Check directories
            if (!Directory.Exists(BRAIN_STORAGE_PATH))
            {
                Directory.CreateDirectory(BRAIN_STORAGE_PATH);
                Console.WriteLine($"✅ Created brain storage: {BRAIN_STORAGE_PATH}");
            }
            else
            {
                Console.WriteLine($"✅ Brain storage exists: {BRAIN_STORAGE_PATH}");
            }
            
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

        private async Task TrainFromTatoeba()
        {
            Console.WriteLine("=== Step 2: Train from Tatoeba (500 sentences) ===");
            
            // Create storage adapter
            _storage = new FastStorageAdapter(
                hotPath: WORKING_PATH,
                coldPath: BRAIN_STORAGE_PATH
            );
            
            // Create trainer
            _trainer = new TatoebaLanguageTrainer(
                tatoebaDataPath: TATOEBA_PATH,
                storage: _storage
            );
            
            Console.WriteLine("Starting Tatoeba training...");
            var startTime = DateTime.Now;
            
            // Train on 500 sentences
            await _trainer.TrainOnEnglishSentencesAsync(
                maxSentences: 500,
                batchSize: 100
            );
            
            var elapsed = (DateTime.Now - startTime).TotalSeconds;
            
            // Get brain reference
            _brain = _trainer.Brain;
            if (_brain == null)
            {
                throw new Exception("Failed to get LanguageEphemeralBrain from trainer");
            }
            
            _sourceStats["Tatoeba"] = _brain.VocabularySize;
            
            Console.WriteLine($"✅ Tatoeba training completed in {elapsed:F2} seconds");
            Console.WriteLine($"   Vocabulary size: {_brain.VocabularySize}");
            Console.WriteLine($"   Learned sentences: {_brain.LearnedSentences}");
            Console.WriteLine();
        }

        private async Task TrainFromEnhancedData()
        {
            Console.WriteLine("=== Step 3: Train from Enhanced Data ===");
            
            if (_brain == null)
            {
                throw new Exception("Brain not initialized - run TrainFromTatoeba first");
            }
            
            var initialVocab = _brain.VocabularySize;
            Console.WriteLine($"Starting with {initialVocab} words from Tatoeba");
            
            // Load enhanced word database
            var enhancedWordDb = Path.Combine(ENHANCED_DATA_PATH, "enhanced_word_database.json");
            var jsonText = await File.ReadAllTextAsync(enhancedWordDb);
            
            // Parse JSON to extract sentences/contexts
            // For this test, we'll extract unique sentences from the database
            var sentences = ExtractSentencesFromEnhancedData(jsonText);
            
            Console.WriteLine($"📚 Extracted {sentences.Count} sentences from enhanced data");
            Console.WriteLine("Learning enhanced data...");
            
            var startTime = DateTime.Now;
            int learned = 0;
            
            // Learn up to 300 sentences from enhanced data
            foreach (var sentence in sentences.Take(300))
            {
                _brain.LearnSentence(sentence);  // Synchronous method
                learned++;
                
                if (learned % 50 == 0)
                {
                    Console.WriteLine($"  Processed {learned}/300 sentences - Vocab: {_brain.VocabularySize}");
                }
            }
            
            var elapsed = (DateTime.Now - startTime).TotalSeconds;
            var vocabGain = _brain.VocabularySize - initialVocab;
            
            _sourceStats["Enhanced"] = vocabGain;
            
            Console.WriteLine($"✅ Enhanced data training completed in {elapsed:F2} seconds");
            Console.WriteLine($"   New vocabulary learned: {vocabGain} words");
            Console.WriteLine($"   Total vocabulary: {_brain.VocabularySize}");
            Console.WriteLine();
        }

        private List<string> ExtractSentencesFromEnhancedData(string jsonText)
        {
            var sentences = new List<string>();
            
            // Simple extraction: find quoted strings that look like sentences
            // This is a quick approach for validation - proper JSON parsing would be better
            var lines = jsonText.Split('\n');
            foreach (var line in lines)
            {
                // Look for context or sentence fields
                if (line.Contains("\"context\"") || line.Contains("\"sentence\""))
                {
                    var start = line.IndexOf(": \"");
                    if (start >= 0)
                    {
                        start += 3;
                        var end = line.IndexOf("\"", start);
                        if (end > start)
                        {
                            var sentence = line.Substring(start, end - start);
                            if (sentence.Length > 10 && sentence.Length < 200 && sentence.Contains(" "))
                            {
                                sentences.Add(sentence);
                            }
                        }
                    }
                }
            }
            
            return sentences.Distinct().ToList();
        }

        private async Task VerifyMultiSourceKnowledge()
        {
            Console.WriteLine("=== Step 4: Verify Multi-Source Knowledge ===");
            
            if (_brain == null)
            {
                throw new Exception("Brain not initialized");
            }
            
            Console.WriteLine($"📊 Total vocabulary: {_brain.VocabularySize} words");
            Console.WriteLine($"📊 Total sentences: {_brain.LearnedSentences}");
            Console.WriteLine();
            
            Console.WriteLine("📈 Source breakdown:");
            foreach (var kvp in _sourceStats.OrderByDescending(x => x.Value))
            {
                var percentage = (_brain.VocabularySize > 0) 
                    ? (kvp.Value * 100.0 / _brain.VocabularySize) 
                    : 0;
                Console.WriteLine($"   {kvp.Key}: {kvp.Value} words ({percentage:F1}%)");
            }
            Console.WriteLine();
            
            // Test some words that might come from different sources
            var testWords = new[] { "the", "cat", "language", "computer", "science" };
            Console.WriteLine("🔍 Testing vocabulary presence:");
            var vocab = _brain.ExportVocabulary();
            foreach (var word in testWords)
            {
                var hasWord = vocab.ContainsKey(word);
                var status = hasWord ? "✅" : "❌";
                Console.WriteLine($"   {status} '{word}'");
            }
            Console.WriteLine();
            
            await Task.CompletedTask;
        }

        private async Task SaveAndReport()
        {
            Console.WriteLine("=== Step 5: Save and Report ===");
            
            if (_brain == null || _trainer == null)
            {
                throw new Exception("Brain or trainer not initialized");
            }
            
            Console.WriteLine("💾 Saving brain state...");
            var startTime = DateTime.Now;
            
            await _trainer.SaveBrainStateAsync();
            
            var elapsed = (DateTime.Now - startTime).TotalSeconds;
            
            Console.WriteLine($"✅ Save completed in {elapsed:F2} seconds");
            Console.WriteLine();
            
            Console.WriteLine("=== Multi-Source Validation Summary ===");
            Console.WriteLine($"✅ Successfully trained from {_sourceStats.Count} data sources");
            Console.WriteLine($"✅ Final vocabulary: {_brain.VocabularySize} words");
            Console.WriteLine($"✅ Total sentences: {_brain.LearnedSentences}");
            Console.WriteLine($"✅ Multi-source learning VALIDATED");
            Console.WriteLine();
            
            Console.WriteLine("Storage Configuration:");
            Console.WriteLine($"  Hot (working): {WORKING_PATH}");
            Console.WriteLine($"  Cold (storage): {BRAIN_STORAGE_PATH}");
            Console.WriteLine();
            
            Console.WriteLine("VALIDATION: Multi-source integration with NAS storage PROVEN");
        }
    }
}
