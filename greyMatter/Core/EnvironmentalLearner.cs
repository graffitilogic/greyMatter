using System;
using System.Collections.Generic;
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
        private readonly BrainInJar _brain;
        private readonly BrainConfiguration _config;

        public EnvironmentalLearner(BrainInJar brain, BrainConfiguration config)
        {
            _brain = brain;
            _config = config;
        }

        public async Task<int> LearnAsync(IEnumerable<CurriculumCompiler.LessonItem> lessons, int maxItems = 1000)
        {
            int count = 0;
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
                }

                count++;
            }
            return count;
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
