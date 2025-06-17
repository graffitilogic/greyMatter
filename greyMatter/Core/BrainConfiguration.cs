using System;
using System.IO;

namespace GreyMatter.Core
{
    /// <summary>
    /// Configuration settings for the brain system
    /// Supports configurable storage paths and system parameters
    /// </summary>
    public class BrainConfiguration
    {
        /// <summary>
        /// Path where brain data (neurons, clusters, synapses) is stored
        /// </summary>
        public string BrainDataPath { get; set; } = "brain_data";
        
        /// <summary>
        /// Root path for training data and learning resources
        /// </summary>
        public string TrainingDataRoot { get; set; } = "/tmp/brain_library";
        
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
        /// Create configuration from command line arguments
        /// </summary>
        public static BrainConfiguration FromCommandLine(string[] args)
        {
            var config = new BrainConfiguration();
            
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
                    case "-v":
                        config.VoiceEnabled = true;
                        break;
                }
            }
            
            return config;
        }
        
        /// <summary>
        /// Validate and setup paths
        /// </summary>
        public void ValidateAndSetup()
        {
            // Use working drive for brain data if specified
            if (!string.IsNullOrEmpty(WorkingDrivePath))
            {
                if (!Directory.Exists(WorkingDrivePath))
                {
                    throw new DirectoryNotFoundException($"Working drive path not found: {WorkingDrivePath}");
                }
                
                // Move brain data to working drive
                BrainDataPath = Path.Combine(WorkingDrivePath, "brain_data");
                TrainingDataRoot = Path.Combine(WorkingDrivePath, "training_library");
            }
            
            // Ensure directories exist
            Directory.CreateDirectory(BrainDataPath);
            Directory.CreateDirectory(TrainingDataRoot);
            
            Console.WriteLine($"📁 Brain Data Path: {Path.GetFullPath(BrainDataPath)}");
            Console.WriteLine($"📚 Training Data Root: {Path.GetFullPath(TrainingDataRoot)}");
            
            if (!string.IsNullOrEmpty(WorkingDrivePath))
            {
                Console.WriteLine($"💾 Working Drive: {WorkingDrivePath}");
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
            Console.WriteLine("Storage Configuration:");
            Console.WriteLine("  --brain-data, -bd <path>     Brain data storage location");
            Console.WriteLine("  --training-data, -td <path>  Training data root directory");
            Console.WriteLine("  --working-drive, -wd <path>  Working drive for large-scale storage");
            Console.WriteLine();
            Console.WriteLine("Interactive Options:");
            Console.WriteLine("  --interactive, -i            Enable conversational mode");
            Console.WriteLine("  --voice, -v                  Enable voice synthesis");
            Console.WriteLine();
            Console.WriteLine("Examples:");
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
