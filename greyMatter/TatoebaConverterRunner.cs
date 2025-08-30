using System;
using System.Threading.Tasks;

namespace GreyMatter
{
    /// <summary>
    /// Simple runner for Tatoeba data conversion
    /// </summary>
    public class TatoebaConverterRunner
    {
        // public static async Task Main(string[] args)
        // {
        //     Console.WriteLine("🔄 **TATOEBA DATA CONVERTER**");
        //     Console.WriteLine("============================");

        //     try
        //     {
        //         var tatoebaPath = "learning_datasets/Tatoeba/sentences.csv";
        //         var outputPath = "learning_datasets/learning_data";

        //         var converter = new TatoebaDataConverter(tatoebaPath, outputPath);
        //         await converter.ConvertAndBuildLearningDataAsync(10000);

        //         Console.WriteLine("\n✅ **DATA CONVERSION COMPLETE**");
        //     }
        //     catch (Exception ex)
        //     {
        //         Console.WriteLine($"❌ Error: {ex.Message}");
        //         Console.WriteLine(ex.StackTrace);
        //     }
        // }
    }
}
