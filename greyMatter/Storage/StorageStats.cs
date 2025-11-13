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
    }
}
