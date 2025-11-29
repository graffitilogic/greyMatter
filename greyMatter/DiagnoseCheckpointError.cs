using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Linq;

namespace GreyMatter
{
    public class DiagnoseCheckpointError
    {
        public static void Main(string[] args)
        {
            var basePath = "/Volumes/jarvis/brainData/hierarchical";
            var problemNeuronId = "7a2cb3e1299f43f9afc2a79f0c792825";
            
            Console.WriteLine($"🔍 Searching for neuron {problemNeuronId}...");
            Console.WriteLine();
            
            // Search all neuron bank files
            var bankFiles = Directory.GetFiles(basePath, "neurons.bank.json.gz", SearchOption.AllDirectories);
            Console.WriteLine($"Found {bankFiles.Length} neuron bank files to search");
            Console.WriteLine();
            
            foreach (var file in bankFiles)
            {
                try
                {
                    using var fs = new FileStream(file, FileMode.Open, FileAccess.Read);
                    using var gz = new GZipStream(fs, CompressionMode.Decompress);
                    using var reader = new StreamReader(gz);
                    var json = reader.ReadToEnd();
                    
                    if (json.Contains(problemNeuronId))
                    {
                        Console.WriteLine($"✅ FOUND in: {file}");
                        Console.WriteLine();
                        
                        // Find the neuron's section
                        var startIdx = json.IndexOf($"\"{problemNeuronId}\"");
                        if (startIdx >= 0)
                        {
                            // Show context around the problematic neuron
                            var contextStart = Math.Max(0, startIdx - 200);
                            var contextEnd = Math.Min(json.Length, startIdx + 2000);
                            var context = json.Substring(contextStart, contextEnd - contextStart);
                            
                            Console.WriteLine("Context:");
                            Console.WriteLine("═══════════════════════════════════════");
                            Console.WriteLine(context);
                            Console.WriteLine("═══════════════════════════════════════");
                            Console.WriteLine();
                            
                            // Look specifically for the "thr5" key
                            if (json.Contains("\"thr5\""))
                            {
                                Console.WriteLine("⚠️  Found 'thr5' key in this file!");
                                var thr5Idx = json.IndexOf("\"thr5\"");
                                var thr5ContextStart = Math.Max(0, thr5Idx - 500);
                                var thr5ContextEnd = Math.Min(json.Length, thr5Idx + 500);
                                var thr5Context = json.Substring(thr5ContextStart, thr5ContextEnd - thr5ContextStart);
                                
                                Console.WriteLine("Context around 'thr5':");
                                Console.WriteLine("═══════════════════════════════════════");
                                Console.WriteLine(thr5Context);
                                Console.WriteLine("═══════════════════════════════════════");
                            }
                        }
                        
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️  Error reading {file}: {ex.Message}");
                }
            }
            
            Console.WriteLine("❌ Neuron not found in any bank files");
        }
    }
}
