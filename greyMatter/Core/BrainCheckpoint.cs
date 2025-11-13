using System;

namespace GreyMatter.Core
{
    /// <summary>
    /// Simple checkpoint data structure
    /// </summary>
    public class BrainCheckpoint
    {
        public DateTime Timestamp { get; set; }
        public long SentencesProcessed { get; set; }
        public int VocabularySize { get; set; }
        public int SynapseCount { get; set; }
        public double TrainingHours { get; set; }
        public double AverageTrainingRate { get; set; }
        public double MemoryUsageGB { get; set; }
        public string? DatasetKey { get; set; }
    }
}
