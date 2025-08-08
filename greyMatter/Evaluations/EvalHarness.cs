using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreyMatter.Core;

namespace GreyMatter.Evaluations
{
    /// <summary>
    /// Basic cloze-style evaluation harness.
    /// Masks one token and asks the brain to process the masked sentence; compares against ground truth.
    /// This is a placeholder for tracking baseline accuracy.
    /// </summary>
    public static class EvalHarness
    {
        private static readonly char[] SplitDelims = new[] { ' ', '\t' };

        public static async Task<double> RunClozeAsync(BrainInJar brain, IEnumerable<string> sentences, int max = 1000)
        {
            int total = 0;
            int correct = 0;
            foreach (var s in sentences)
            {
                if (total >= max) break;
                var toks = s.Split(SplitDelims, StringSplitOptions.RemoveEmptyEntries).ToList();
                if (toks.Count < 4) continue;

                // pick a middle token to mask
                int idx = toks.Count / 2;
                var answer = toks[idx];
                toks[idx] = "____";
                var masked = string.Join(' ', toks);

                var resp = await brain.ProcessInputAsync(masked, new Dictionary<string, double> { ["task:cloze"] = 1.0 });
                var guess = (resp.Response ?? string.Empty).Trim();

                // extremely naive correctness: substring match
                if (guess.Contains(answer, StringComparison.OrdinalIgnoreCase)) correct++;
                total++;
            }

            return total == 0 ? 0.0 : (double)correct / total;
        }
    }
}
