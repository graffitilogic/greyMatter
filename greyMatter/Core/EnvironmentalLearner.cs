using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GreyMatter.Core;
using GreyMatter.Learning;

namespace GreyMatter.Core
{
    /// <summary>
    /// Consumes Curriculum lesson items and feeds them to the Brain for incremental learning.
    /// </summary>
    public class EnvironmentalLearner
    {
        private readonly Cerebro _brain;
        private readonly CerebroConfiguration _config;

        public EnvironmentalLearner(Cerebro brain, CerebroConfiguration config)
        {
            _brain = brain;
            _config = config;
        }

        public async Task<int> LearnAsync(IEnumerable<CurriculumCompiler.LessonItem> lessons, int maxItems = 1000)
        {
            int count = 0;
            // Determine total lessons if available
            int totalLessons = -1;
            if (lessons is ICollection<CurriculumCompiler.LessonItem> col)
                totalLessons = Math.Min(maxItems, col.Count);

            // Progress tracking
            var sw = Stopwatch.StartNew();
            int batchInterval = 50; // lessons per progress update
            int nextReport = batchInterval;
            long conceptOps = 0;
            var uniqueConcepts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var lesson in lessons)
            {
                if (count >= maxItems) break;

                // Map to small set of durable concepts, not per-sentence keys
                var conceptKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var f in lesson.FocusConcepts)
                {
                    if (IsSafeConcept(f)) conceptKeys.Add(f);
                }

                // Add a simple pattern tag for structure (keeps cluster set tiny)
                var pattern = ExtractPatternTag(lesson.Sentence);
                if (pattern != null) conceptKeys.Add(pattern);

                // Features for training
                var baseFeatures = new Dictionary<string, double>
                {
                    ["difficulty"] = lesson.Difficulty,
                    ["source:tatoeba_wiki"] = 1.0
                };
                foreach (var f in lesson.FocusConcepts)
                {
                    baseFeatures[$"focus:{f}"] = 1.0;
                }

                foreach (var key in conceptKeys)
                {
                    await _brain.LearnConceptAsync(key, baseFeatures);
                    conceptOps++;
                    uniqueConcepts.Add(key);
                }

                count++;

                // Batched progress output
                if (count >= nextReport || (totalLessons > 0 && count == totalLessons))
                {
                    var elapsed = sw.Elapsed;
                    var lessonsPerSec = count > 0 ? count / Math.Max(0.001, elapsed.TotalSeconds) : 0.0;
                    string etaStr = "?";
                    string pctStr = "n/a";
                    string totalStr = totalLessons > 0 ? totalLessons.ToString() : "?";
                    if (totalLessons > 0 && lessonsPerSec > 0.0001)
                    {
                        var remaining = Math.Max(0, totalLessons - count);
                        var eta = TimeSpan.FromSeconds(remaining / lessonsPerSec);
                        etaStr = FormatTime(eta);
                        pctStr = ((double)count / totalLessons).ToString("P0");
                    }
                    Console.WriteLine($"   📦 Progress: {count}/{totalStr} lessons ({pctStr}) | concepts: {conceptOps} unique:{uniqueConcepts.Count} | elapsed {FormatTime(elapsed)} | rate {lessonsPerSec:F1} lps | ETA {etaStr}");
                    nextReport += batchInterval;
                }
            }
            return count;
        }

        private static string FormatTime(TimeSpan ts)
        {
            if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}h {ts.Minutes}m";
            if (ts.TotalMinutes >= 1) return $"{ts.Minutes}m {ts.Seconds}s";
            return $"{ts.Seconds}s";
        }

        private static bool IsSafeConcept(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = s.Trim();
            if (s.Length > 32) return false; // avoid long strings
            // allow alpha and simple hyphen/apostrophe
            return Regex.IsMatch(s, "^[A-Za-z][A-Za-z\\-']*$");
        }

        private static string? ExtractPatternTag(string sentence)
        {
            var s = sentence.Trim();
            if (Regex.IsMatch(s, "\\?$")) return "pattern:question";
            if (Regex.IsMatch(s, "\\b[A-Za-z]+\\s+(is|are|runs|run|jumps|jump|sleeps|sleep)\\b", RegexOptions.IgnoreCase)) return "pattern:sv";
            if (Regex.IsMatch(s, "\\b[A-Za-z]+\\s+(is|are|was|were|likes|like|has|have|sees|see|eats|eat|makes|make)\\s+[A-Za-z]+\\b", RegexOptions.IgnoreCase)) return "pattern:svo";
            return null;
        }
    }
}
