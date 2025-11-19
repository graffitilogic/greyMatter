using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreyMatter.Core;
using GreyMatter.Storage;

namespace GreyMatter
{
    /// <summary>
    /// Command-line tool for querying accumulated knowledge.
    /// Makes learned knowledge accessible and demonstrates what the brain knows.
    /// </summary>
    public class KnowledgeQueryCLI
    {
        public static async Task Run(string[] args)
        {
            var storage = new ProductionStorageManager();
            var query = new ProductionKnowledgeQuery(storage);
            
            if (args.Length < 2)
            {
                ShowUsage();
                return;
            }
            
            var command = args[1].ToLower();
            
            try
            {
                switch (command)
                {
                    case "associations":
                    case "assoc":
                        await ShowWordAssociations(query, args);
                        break;
                    
                    case "patterns":
                    case "pattern":
                        await ShowPatterns(query, args);
                        break;
                    
                    case "search":
                    case "find":
                        await SearchMemory(query, args);
                        break;
                    
                    case "stats":
                    case "vocabulary":
                        await ShowStats(query);
                        break;
                    
                    case "test":
                    case "comprehension":
                        await TestComprehension(query, args);
                        break;
                    
                    default:
                        Console.WriteLine($"❌ Unknown command: {command}");
                        ShowUsage();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }
        
        private static void ShowUsage()
        {
            Console.WriteLine(@"
════════════════════════════════════════════════════════════════════════════════
KNOWLEDGE QUERY CLI - Query Accumulated Brain Knowledge
════════════════════════════════════════════════════════════════════════════════

Usage: dotnet run -- --query <command> [arguments]

Commands:

  associations <word> [limit]     Find words associated with given word
    Example: dotnet run -- --query associations cat 15
  
  patterns <pattern> [limit]      Search for syntactic patterns
    Example: dotnet run -- --query patterns ""subject verb object""
  
  search <query> [limit]          Search episodic memory
    Example: dotnet run -- --query search ""sentences about animals""
  
  stats                           Show vocabulary statistics
    Example: dotnet run -- --query stats
  
  test <question> <answer>        Test comprehension
    Example: dotnet run -- --query test ""What is a cat?"" ""feline animal""

════════════════════════════════════════════════════════════════════════════════
");
        }
        
        private static async Task ShowWordAssociations(IKnowledgeQuery query, string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("❌ Usage: --query associations <word> [limit]");
                return;
            }
            
            var word = args[2];
            var limit = args.Length > 3 && int.TryParse(args[3], out var l) ? l : 10;
            
            Console.WriteLine($"\n🔍 Finding associations for '{word}'...\n");
            
            var associations = await query.GetWordAssociationsAsync(word, limit);
            
            if (associations.Count == 0)
            {
                Console.WriteLine($"⚠️  No associations found for '{word}'");
                Console.WriteLine("   (The brain may not have learned this word yet)");
                return;
            }
            
            Console.WriteLine($" Found {associations.Count} associations:\n");
            
            foreach (var assoc in associations.OrderByDescending(a => a.Confidence))
            {
                var bar = new string('█', (int)(assoc.Confidence * 50));
                Console.WriteLine($"  {assoc.Word,-20} {bar} ({assoc.CoOccurrenceCount} co-occurrences)");
            }
            
            Console.WriteLine();
        }
        
        private static async Task ShowPatterns(IKnowledgeQuery query, string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("❌ Usage: --query patterns <pattern> [limit]");
                return;
            }
            
            var pattern = args[2];
            var limit = args.Length > 3 && int.TryParse(args[3], out var l) ? l : 10;
            
            Console.WriteLine($"\n🔍 Searching for pattern '{pattern}'...\n");
            
            var matches = await query.FindPatternsAsync(pattern, limit);
            
            if (matches.Count == 0)
            {
                Console.WriteLine($"⚠️  No matches found for pattern '{pattern}'");
                return;
            }
            
            Console.WriteLine($" Found {matches.Count} matches:\n");
            
            foreach (var match in matches)
            {
                Console.WriteLine($"  📄 {match.Example}");
                Console.WriteLine();
            }
        }
        
        private static async Task SearchMemory(IKnowledgeQuery query, string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("❌ Usage: --query search <query> [limit]");
                return;
            }
            
            var searchQuery = string.Join(" ", args.Skip(2).TakeWhile(a => !int.TryParse(a, out _)));
            var limit = args.Length > 2 + searchQuery.Split(' ').Length && 
                       int.TryParse(args.Last(), out var l) ? l : 10;
            
            Console.WriteLine($"\n🔍 Searching episodic memory for '{searchQuery}'...\n");
            
            var memories = await query.SearchEpisodicMemoryAsync(searchQuery, limit);
            
            if (memories.Count == 0)
            {
                Console.WriteLine($"⚠️  No memories found matching '{searchQuery}'");
                return;
            }
            
            Console.WriteLine($" Found {memories.Count} relevant memories:\n");
            
            foreach (var memory in memories.OrderByDescending(m => m.Relevance))
            {
                var relevanceBar = new string('█', (int)(memory.Relevance * 30));
                Console.WriteLine($"  📅 {memory.Timestamp:yyyy-MM-dd HH:mm:ss}  {relevanceBar} {memory.Relevance:P0}");
                Console.WriteLine($"     {memory.Description}");
                Console.WriteLine();
            }
        }
        
        private static async Task ShowStats(IKnowledgeQuery query)
        {
            Console.WriteLine("\n📊 VOCABULARY STATISTICS\n");
            
            var stats = await query.GetVocabularyStatsAsync();
            
            Console.WriteLine($"  Total Words Learned:     {stats.TotalWords:N0}");
            Console.WriteLine($"  Unique Vocabulary:       {stats.UniqueWords:N0}");
            Console.WriteLine($"  Sentences Processed:     {stats.TotalSentences:N0}");
            Console.WriteLine($"  Avg Words/Sentence:      {stats.AverageWordFrequency:F2}");
            
            if (stats.MostFrequent.Count > 0)
            {
                Console.WriteLine("\n  Most Frequent Words:");
                foreach (var (word, freq) in stats.MostFrequent.Take(10))
                {
                    Console.WriteLine($"    {word,-15} {freq:N0}");
                }
            }
            
            Console.WriteLine();
        }
        
        private static async Task TestComprehension(IKnowledgeQuery query, string[] args)
        {
            if (args.Length < 4)
            {
                Console.WriteLine("❌ Usage: --query test <question> <expected_answer>");
                return;
            }
            
            var question = args[2];
            var expectedAnswer = args[3];
            
            var questions = new List<ComprehensionQuestion>
            {
                new ComprehensionQuestion
                {
                    Question = question,
                    ExpectedAnswer = expectedAnswer,
                    Context = ""
                }
            };
            
            Console.WriteLine($"\n🧪 Testing comprehension...\n");
            Console.WriteLine($"   Question: {question}");
            Console.WriteLine($"   Expected: {expectedAnswer}\n");
            
            var accuracy = await query.TestComprehensionAsync(questions);
            
            if (accuracy >= 1.0)
            {
                Console.WriteLine(" CORRECT! The brain has learned this.");
            }
            else
            {
                Console.WriteLine("❌ INCORRECT. The brain hasn't learned this yet.");
            }
            
            Console.WriteLine($"\n   Accuracy: {accuracy:P0}\n");
        }
    }
}
