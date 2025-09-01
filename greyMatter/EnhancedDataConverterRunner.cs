using System;
using System.Threading.Tasks;

namespace GreyMatter
{
    /// <summary>
    /// Runner for Enhanced Data Converter
    /// Processes diverse training sources for comprehensive language learning
    /// </summary>
    public class EnhancedDataConverterRunner
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 **ENHANCED DATA CONVERTER RUNNER**");
            Console.WriteLine("===================================");

            try
            {
                // Configure paths
                var dataRoot = "/Volumes/jarvis/trainData";
                var outputPath = "/Volumes/jarvis/trainData/enhanced_learning_data";

                // Create output directory
                if (!System.IO.Directory.Exists(outputPath))
                {
                    System.IO.Directory.CreateDirectory(outputPath);
                }

                Console.WriteLine($"📁 Data root: {dataRoot}");
                Console.WriteLine($"📁 Output path: {outputPath}");

                // Initialize enhanced converter
                var converter = new EnhancedDataConverter(dataRoot, outputPath);

                // Process all enhanced data sources
                await converter.ConvertAllSourcesAsync(50000);

                Console.WriteLine("\n✅ **ENHANCED DATA CONVERSION COMPLETE**");
                Console.WriteLine("🎯 Enhanced learning data is ready for training!");
                Console.WriteLine("");
                Console.WriteLine("📊 Data includes:");
                Console.WriteLine("   • Conversational English (OpenSubtitles)");
                Console.WriteLine("   • Formal language (News headlines)");
                Console.WriteLine("   • Academic writing (Scientific abstracts)");
                Console.WriteLine("   • Narrative content (Children's stories)");
                Console.WriteLine("   • Technical language (Documentation)");
                Console.WriteLine("   • Informal communication (Social media)");
                Console.WriteLine("   • Figurative language (Idioms & expressions)");
                Console.WriteLine("   • Multilingual content (Parallel corpus)");
                Console.WriteLine("");
                Console.WriteLine("🔄 Next: Run enhanced training with this diverse dataset!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
