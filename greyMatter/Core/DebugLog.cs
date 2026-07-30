using System;

namespace GreyMatter.Core
{
    /// <summary>
    /// Global log-level gate for high-volume diagnostic output.
    /// Levels: 0 = normal (progress, checkpoints, histograms),
    ///         1 = verbose (evictions, per-partition summaries),
    ///         2 = debug (per-cluster / per-neuron / per-region traces).
    /// Initialized from GREYMATTER_VERBOSITY env var; CerebroConfiguration.Verbosity
    /// overrides it when a Cerebro is constructed with a config.
    /// </summary>
    public static class DebugLog
    {
        public static int Level { get; set; }

        static DebugLog()
        {
            var env = Environment.GetEnvironmentVariable("GREYMATTER_VERBOSITY");
            if (int.TryParse(env, out var v))
                Level = Math.Max(0, Math.Min(2, v));
        }

        public static void Verbose(string message)
        {
            if (Level >= 1) Console.WriteLine(message);
        }

        public static void Debug(string message)
        {
            if (Level >= 2) Console.WriteLine(message);
        }
    }
}
