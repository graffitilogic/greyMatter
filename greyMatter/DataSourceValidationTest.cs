using System;
using System.Linq;
using System.Threading.Tasks;
using GreyMatter.Learning;

namespace GreyMatter.Tests
{
    /// <summary>
    /// Test runner for new IDataSource implementations
    /// Validates Tatoeba, CBT, and Enhanced data sources
    /// </summary>
    public class DataSourceValidationTest
    {
        public static async Task Run()
        {
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine("📊 DATA SOURCE VALIDATION TEST - Week 3 Task 1");
            Console.WriteLine("═══════════════════════════════════════════════════════════════\n");

            // Define data sources
            var tatoebaPath = "/Volumes/jarvis/trainData/Tatoeba/sentences_eng_small.csv";
            var cbtPath = "/Volumes/jarvis/trainData/CBT/CBTest/data/cbt_train.txt";
            var enhancedPath = "/Volumes/jarvis/trainData/enhanced_learning_data/enhanced_word_database.json";

            var dataSources = new IDataSource[]
            {
                new TatoebaDataSource(tatoebaPath),
                new CBTDataSource(cbtPath),
                new EnhancedDataSource(enhancedPath)
            };

            foreach (var dataSource in dataSources)
            {
                await TestDataSource(dataSource);
            }

            Console.WriteLine("\n═══════════════════════════════════════════════════════════════");
            Console.WriteLine("✅ DATA SOURCE VALIDATION COMPLETE");
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
        }

        private static async Task TestDataSource(IDataSource dataSource)
        {
            Console.WriteLine($"\n🔍 Testing: {dataSource.SourceName}");
            Console.WriteLine($"   Description: {dataSource.Description}");
            Console.WriteLine($"   Type: {dataSource.SourceType}");
            Console.WriteLine();

            // Test 1: Validation
            Console.WriteLine("   📋 Step 1: Validating data source...");
            var validation = await dataSource.ValidateAsync();
            
            if (validation.IsValid)
            {
                Console.WriteLine($"      ✅ {validation.Message}");
                if (validation.Warnings.Any())
                {
                    foreach (var warning in validation.Warnings)
                    {
                        Console.WriteLine($"      ⚠️  {warning}");
                    }
                }
            }
            else
            {
                Console.WriteLine($"      ❌ Validation failed: {validation.Message}");
                foreach (var error in validation.Errors)
                {
                    Console.WriteLine($"         ❌ {error}");
                }
                return; // Skip remaining tests if validation failed
            }

            // Test 2: Metadata
            Console.WriteLine("\n   📊 Step 2: Getting metadata...");
            var metadata = await dataSource.GetMetadataAsync();
            Console.WriteLine($"      File: {metadata.FilePath}");
            Console.WriteLine($"      Size: {metadata.FileSizeFormatted}");
            Console.WriteLine($"      Estimated sentences: {metadata.EstimatedSentenceCount:N0}");
            Console.WriteLine($"      Last modified: {metadata.LastModified:yyyy-MM-dd HH:mm}");
            
            if (metadata.AdditionalInfo.Any())
            {
                Console.WriteLine("      Additional info:");
                foreach (var kvp in metadata.AdditionalInfo)
                {
                    Console.WriteLine($"         {kvp.Key}: {kvp.Value}");
                }
            }

            // Test 3: Load sample sentences
            Console.WriteLine("\n   📚 Step 3: Loading sample sentences (max 10)...");
            int count = 0;
            var startTime = DateTime.UtcNow;

            await foreach (var sentence in dataSource.LoadSentencesAsync(maxSentences: 10))
            {
                count++;
                if (count <= 3) // Show first 3 sentences
                {
                    var preview = sentence.Text.Length > 80 
                        ? sentence.Text.Substring(0, 77) + "..." 
                        : sentence.Text;
                    Console.WriteLine($"      {count}. [{sentence.Context}] {preview}");
                }
            }

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            Console.WriteLine($"\n      ✅ Loaded {count} sentences in {elapsed:F1}ms");
            Console.WriteLine($"      📈 Rate: {count / elapsed * 1000:F0} sentences/second");
        }
    }
}
