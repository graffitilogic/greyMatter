using System;
using System.IO;

namespace GreyMatter.Core
{
    /// <summary>
    /// Configuration settings for the brain system
    /// Supports configurable storage paths and system parameters
    /// </summary>
    public class CerebroConfiguration
    {
        /// <summary>
        /// Path where brain data (neurons, clusters, synapses) is stored
        /// </summary>
        public string BrainDataPath { get; set; } = "/Volumes/jarvis/brainData";
        
        /// <summary>
        /// Root path for training data and learning resources
        /// </summary>
        public string TrainingDataRoot { get; set; } = "/Volumes/jarvis/trainData";
        
        /// <summary>
        /// Working drive for large-scale storage (NAS, external drive, etc.)
        /// </summary>
        public string WorkingDrivePath { get; set; } = "";
        
        /// <summary>
        /// Maximum size for brain data before suggesting migration to working drive
        /// </summary>
        public long MaxLocalStorageBytes { get; set; } = 10L * 1024 * 1024 * 1024; // 10GB
        
        /// <summary>
        /// Enable interactive mode for conversational interface
        /// </summary>
        public bool InteractiveMode { get; set; } = false;
        
        /// <summary>
        /// Enable voice synthesis for verbal responses
        /// </summary>
        public bool VoiceEnabled { get; set; } = false;

        /// <summary>
        /// Verbosity level for logging: 0=normal, 1=verbose, 2=debug
        /// </summary>
        public int Verbosity { get; set; } = 0;

        /// <summary>
        /// Max parallel cluster saves (use low for NAS/spindles)
        /// </summary>
        public int MaxParallelSaves { get; set; } = 2;

        /// <summary>
        /// Compress cluster payloads (gzip) to reduce NAS I/O
        /// </summary>
        public bool CompressClusters { get; set; } = true;
        
        /// <summary>
        /// Create configuration from command line arguments
        /// </summary>
        public static CerebroConfiguration FromCommandLine(string[] args)
        {
            var config = new CerebroConfiguration();
            
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "--brain-data":
                    case "-bd":
                        if (i + 1 < args.Length)
                            config.BrainDataPath = args[++i];
                        break;
                        
                    case "--training-data":
                    case "-td":
                        if (i + 1 < args.Length)
                            config.TrainingDataRoot = args[++i];
                        break;
                        
                    case "--working-drive":
                    case "-wd":
                        if (i + 1 < args.Length)
                            config.WorkingDrivePath = args[++i];
                        break;
                        
                    case "--interactive":
                    case "-i":
                        config.InteractiveMode = true;
                        break;
                        
                    case "--voice":
                        // Note: -v reused for verbosity; keep --voice explicit
                        config.VoiceEnabled = true;
                        break;

                    case "--verbosity":
                    case "-log":
                        if (i + 1 < args.Length && int.TryParse(args[i+1], out var v))
                        {
                            config.Verbosity = Math.Max(0, Math.Min(2, v));
                            i++;
                        }
                        break;

                    case "--max-parallel-saves":
                    case "-mps":
                        if (i + 1 < args.Length && int.TryParse(args[i+1], out var mps))
                        {
                            config.MaxParallelSaves = Math.Max(1, Math.Min(8, mps));
                            i++;
                        }
                        break;

                    case "--compress-clusters":
                    case "-cc":
                        if (i + 1 < args.Length && bool.TryParse(args[i+1], out var cc))
                        {
                            config.CompressClusters = cc;
                            i++;
                        }
                        break;
                }
            }
            
            return config;
        }
        
        /// <summary>
        /// Validate and setup paths with improved data storage handling
        /// </summary>
        public void ValidateAndSetup()
        {
            // Environment variable overrides
            var envBD = Environment.GetEnvironmentVariable("BRAIN_DATA_PATH");
            var envTD = Environment.GetEnvironmentVariable("TRAINING_DATA_ROOT");
            if (!string.IsNullOrWhiteSpace(envBD)) BrainDataPath = envBD!;
            if (!string.IsNullOrWhiteSpace(envTD)) TrainingDataRoot = envTD!;

            // Optional env overrides for perf/logging
            var envVerb = Environment.GetEnvironmentVariable("VERBOSITY");
            if (int.TryParse(envVerb, out var v)) Verbosity = Math.Max(0, Math.Min(2, v));
            var envMps = Environment.GetEnvironmentVariable("MAX_PARALLEL_SAVES");
            if (int.TryParse(envMps, out var mps)) MaxParallelSaves = Math.Max(1, Math.Min(8, mps));
            var envCc = Environment.GetEnvironmentVariable("COMPRESS_CLUSTERS");
            if (bool.TryParse(envCc, out var cc)) CompressClusters = cc;

            // If working drive specified, prefer that
            if (!string.IsNullOrEmpty(WorkingDrivePath))
            {
                if (!Directory.Exists(WorkingDrivePath))
                {
                    throw new DirectoryNotFoundException($"Working drive path not found: {WorkingDrivePath}");
                }
                BrainDataPath = Path.Combine(WorkingDrivePath, "brainData");
                TrainingDataRoot = Path.Combine(WorkingDrivePath, "training_library");
            }
            else
            {
                // CRITICAL: Never store data in project directory
                // Only use project directory as absolute last resort
                var projectDir = Directory.GetCurrentDirectory();
                var isInProjectDir = BrainDataPath.Contains(projectDir) || TrainingDataRoot.Contains(projectDir);

                if (isInProjectDir)
                {
                    Console.WriteLine("⚠️  WARNING: Data paths are currently pointing to project directory!");
                    Console.WriteLine("    This is not recommended for production use.");
                    Console.WriteLine("    Set BRAIN_DATA_PATH and TRAINING_DATA_ROOT environment variables");
                    Console.WriteLine("    or use --brain-data and --training-data command line arguments");
                    Console.WriteLine("    to point to proper NAS/external storage locations.");
                    Console.WriteLine();
                }

                // Prefer NAS defaults if user hasn't explicitly set paths
                bool bdWasDefault = string.Equals(BrainDataPath, "/Volumes/jarvis/brainData", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(BrainDataPath);
                bool tdWasDefault = string.Equals(TrainingDataRoot, "/tmp/brain_library", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(TrainingDataRoot);

                if (bdWasDefault || tdWasDefault)
                {
                    var candidateRoots = new[]
                    {
                        @"\\\\doddnas\\jarvis",   // Preferred UNC
                        @"\\\\dodnas\\jarvis",    // Alternate UNC (typo-safe)
                        "/Volumes/jarvis"            // macOS mount point
                    };

                    foreach (var root in candidateRoots)
                    {
                        try
                        {
                            if (Directory.Exists(root))
                            {
                                if (bdWasDefault) BrainDataPath = Path.Combine(root, "brainData");
                                if (tdWasDefault) TrainingDataRoot = Path.Combine(root, "trainData");
                                break;
                            }
                        }
                        catch
                        {
                            // Ignore and try next root
                        }
                    }
                }
            }

            // Ensure directories exist; if creation fails, fall back to local
            var actualBrainPath = EnsureDirectoryOrFallback(BrainDataPath, Path.Combine(Path.GetTempPath(), "greyMatter_brainData"), "external brain data path");
            var actualTrainingPath = EnsureDirectoryOrFallback(TrainingDataRoot, Path.Combine(Path.GetTempPath(), "greyMatter_trainingData"), "external training data path");

            // Update paths to reflect what's actually being used
            BrainDataPath = actualBrainPath;
            TrainingDataRoot = actualTrainingPath;

            Console.WriteLine($"📁 Brain Data Path: {Path.GetFullPath(BrainDataPath)}");
            Console.WriteLine($"📚 Training Data Root: {Path.GetFullPath(TrainingDataRoot)}");

            if (!string.IsNullOrEmpty(WorkingDrivePath))
            {
                Console.WriteLine($"💾 Working Drive: {WorkingDrivePath}");
            }

            if (Verbosity > 0)
            {
                Console.WriteLine($"🛠️  Save tuning → MaxParallelSaves={MaxParallelSaves}, CompressClusters={CompressClusters}");
                Console.WriteLine($"🔊 Verbosity: {Verbosity}");
            }
        }

        private string EnsureDirectoryOrFallback(string desiredPath, string fallbackRelative, string label)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(desiredPath))
                {
                    Directory.CreateDirectory(desiredPath);
                    // Verify we can actually write to it
                    var testFile = Path.Combine(desiredPath, ".write_test");
                    File.WriteAllText(testFile, "test");
                    File.Delete(testFile);
                    return desiredPath;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Could not use {label}: '{desiredPath}' - {ex.Message}");
                Console.WriteLine($"    Falling back to '{fallbackRelative}'.");
            }

            try
            {
                Directory.CreateDirectory(fallbackRelative);
                return fallbackRelative;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Could not create fallback directory '{fallbackRelative}' - {ex.Message}");
                // Last resort: current directory
                return Directory.GetCurrentDirectory();
            }
        }
        
        /// <summary>
        /// Check if current brain data size exceeds local storage limits
        /// </summary>
        public bool ShouldMigrateToWorkingDrive()
        {
            if (!string.IsNullOrEmpty(WorkingDrivePath))
                return false; // Already using working drive
                
            try
            {
                var brainDataSize = GetDirectorySize(BrainDataPath);
                return brainDataSize > MaxLocalStorageBytes;
            }
            catch
            {
                return false;
            }
        }
        
        private long GetDirectorySize(string path)
        {
            if (!Directory.Exists(path))
                return 0;
                
            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            long size = 0;
            
            foreach (var file in files)
            {
                try
                {
                    size += new FileInfo(file).Length;
                }
                catch
                {
                    // Skip files we can't access
                }
            }
            
            return size;
        }
        
        /// <summary>
        /// Display usage information
        /// </summary>
        public static void DisplayUsage()
        {
            Console.WriteLine("🧠 **BRAIN CONFIGURATION OPTIONS**");
            Console.WriteLine("==================================");
            Console.WriteLine();
            Console.WriteLine("Demo Modes:");
            Console.WriteLine("  --developmental-demo         Run developmental learning demonstration");
            Console.WriteLine("  --emotional-demo             Run emotional intelligence demonstration");
            Console.WriteLine("  --language-foundations       Run foundational language learning");
            Console.WriteLine("  --comprehensive-language     Run comprehensive language training (2000+ words)");
            Console.WriteLine("  --preschool-train            Compile a small curriculum, learn, and run a quick cloze baseline");
            Console.WriteLine("  --save-only                  Initialize and immediately save brain state (no learning)");
            Console.WriteLine();
            Console.WriteLine("Storage Configuration:");
            Console.WriteLine("  --brain-data, -bd <path>     Brain data storage location");
            Console.WriteLine("  --training-data, -td <path>  Training data root directory");
            Console.WriteLine("  --working-drive, -wd <path>  Working drive for large-scale storage");
            Console.WriteLine("  env BRAIN_DATA_PATH          Override brain data path");
            Console.WriteLine("  env TRAINING_DATA_ROOT       Override training data path");
            Console.WriteLine("  Defaults attempt NAS: \\doddnas\\jarvis\\brainData and \\doddnas\\jarvis\\trainData (or /Volumes/jarvis on macOS)");
            Console.WriteLine();
            Console.WriteLine("Performance & Logging:");
            Console.WriteLine("  --verbosity, -log <0|1|2>   Log level (0=normal,1=verbose,2=debug)");
            Console.WriteLine("  --max-parallel-saves, -mps N Limit concurrent cluster saves (default 2)");
            Console.WriteLine("  --compress-clusters, -cc <true|false>  Gzip cluster files (default true)");
            Console.WriteLine("  env VERBOSITY, MAX_PARALLEL_SAVES, COMPRESS_CLUSTERS");
            Console.WriteLine();
            Console.WriteLine("Interactive Options:");
            Console.WriteLine("  --interactive, -i            Enable conversational mode");
            Console.WriteLine("  --voice                      Enable voice synthesis");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  # Use NAS paths explicitly");
            Console.WriteLine("  dotnet run -- -bd \\doddnas\\jarvis\\brainData -td \\doddnas\\jarvis\\trainData");
            Console.WriteLine();
            Console.WriteLine("  # macOS + verbose timings + tuned saves");
            Console.WriteLine("  dotnet run -- --preschool-train -bd /Volumes/jarvis/brainData -td /Volumes/jarvis/trainData -log 1 -mps 1 -cc true");
            Console.WriteLine();
            Console.WriteLine("  # Save-only re-persist");
            Console.WriteLine("  dotnet run -- --save-only -bd /Volumes/jarvis/brainData -td /Volumes/jarvis/trainData");
            Console.WriteLine();
            Console.WriteLine("  # Use external drive for storage");
            Console.WriteLine("  dotnet run -- -wd /Volumes/MyDrive");
            Console.WriteLine();
            Console.WriteLine("  # Custom paths");
            Console.WriteLine("  dotnet run -- -bd /custom/brain -td /custom/training");
            Console.WriteLine();
            Console.WriteLine("  # Interactive conversation mode");
            Console.WriteLine("  dotnet run -- --interactive --voice");
        }
    }
}
