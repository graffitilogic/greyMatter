using System;
using System.Collections.Generic;
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

                var featureMap = new Dictionary<string, double>
                {
                    ["difficulty"] = lesson.Difficulty,
                    ["source:tatoeba_wiki"] = 1.0
                };
                foreach (var f in lesson.FocusConcepts)
                {
                    featureMap[$"focus:{f}"] = 1.0;
                }

                var result = await _brain.LearnConceptAsync(lesson.Sentence, featureMap);
                count++;
            }
            return count;
        }
    }
}
