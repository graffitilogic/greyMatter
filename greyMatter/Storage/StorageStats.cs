using System;

namespace GreyMatter.Storage
{
    /// <summary>
    /// Simple storage statistics class
    /// </summary>
    public class StorageStats
    {
        public long TotalSizeBytes { get; set; }
        public int ClusterCount { get; set; }
        public int NeuronCount { get; set; }
        public int SynapseCount { get; set; }
        public DateTime LastModified { get; set; }
        
        public string TotalSizeFormatted
        {
            get
            {
                if (TotalSizeBytes < 1024) return $"{TotalSizeBytes} B";
                if (TotalSizeBytes < 1024 * 1024) return $"{TotalSizeBytes / 1024.0:F1} KB";
                if (TotalSizeBytes < 1024 * 1024 * 1024) return $"{TotalSizeBytes / (1024.0 * 1024):F1} MB";
                return $"{TotalSizeBytes / (1024.0 * 1024 * 1024):F2} GB";
            }
        }
    }
}
