using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GreyMatter.Core;

namespace GreyMatter.Learning
{
    /// <summary>
    /// Compiles a staged curriculum of simple, concrete sentences from available corpora.
    /// Heuristic-first; avoids heavy NLP. Feeds EnvironmentalLearner.
    /// </summary>
    public class CurriculumCompiler
    {
        public record LessonItem(
            string Sentence,
            IReadOnlyList<string> FocusConcepts,
            double Difficulty,
            string Source,
            IDictionary<string, double>? LightweightTargets = null
        );

        public class Curriculum
        {
            public List<LessonItem> Stage1_WordsAndSimpleSV { get; } = new();
            public List<LessonItem> Stage2_SVO { get; } = new();
            public List<LessonItem> Stage3_Modifiers { get; } = new();
            public List<LessonItem> Stage4_Questions { get; } = new();
            public List<LessonItem> Stage5_Narratives { get; } = new();
        }

        // Basic regexes and small stoplists for scoring
        private static readonly Regex Tokenizer = new("[A-Za-z']+|[?.!,]", RegexOptions.Compiled);
        private static readonly HashSet<string> Stop = new(StringComparer.OrdinalIgnoreCase)
        {
            "a","an","the","and","or","to","of","in","is","it","i","you","he","she","we","they","on","at","for","with","as"
        };

        public async Task<Curriculum> CompileAsync(BrainConfiguration config, int maxSentencesPerStage = 2000)
        {
            var curriculum = new Curriculum();

            // 1) Source sentences primarily from Tatoeba (fast to parse)
            var tatoeba = new TatoebaReader();
            var tatoebaPath = Path.Combine(config.TrainingDataRoot, "Tatoeba", "sentences.csv");
            IEnumerable<string> engSentences = Enumerable.Empty<string>();
            if (File.Exists(tatoebaPath))
            {
                engSentences = tatoeba.ReadEnglishSentences(tatoebaPath).Take(100_000);
            }

            // 2) Optionally append Simple English Wikipedia short sentences
            var simpleWiki = Path.Combine(config.TrainingDataRoot, "SimpleWiki", "simplewiki-latest-pages-articles-multistream.xml");
            IEnumerable<string> wikiShort = Enumerable.Empty<string>();
            if (File.Exists(simpleWiki))
            {
                try
                {
                    var wikiReader = new WikipediaStreamReader(simpleWiki);
                    wikiShort = wikiReader.ReadArticles()
                        .SelectMany(a => SplitIntoSentences(a))
                        .Where(s => WordCount(s) <= 10 && WordCount(s) >= 3)
                        .Take(50_000);
                }
                catch { /* ignore parse issues for now */ }
            }

            var allCandidates = engSentences.Concat(wikiShort)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0 && s.Length < 200);

            // 3) Score, stage, and de-duplicate
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in allCandidates)
            {
                if (!seen.Add(s)) continue;
                var score = ScoreSentence(s, out var focus);

                // Stage assignment by pattern heuristics
                if (IsSimpleSV(s)) curriculum.Stage1_WordsAndSimpleSV.Add(ToLesson(s, focus, score, "tatoeba/wiki"));
                else if (IsSVO(s)) curriculum.Stage2_SVO.Add(ToLesson(s, focus, score + 0.2, "tatoeba/wiki"));
                else if (HasModifier(s)) curriculum.Stage3_Modifiers.Add(ToLesson(s, focus, score + 0.3, "tatoeba/wiki"));
                else if (IsQuestion(s)) curriculum.Stage4_Questions.Add(ToLesson(s, focus, score + 0.4, "tatoeba/wiki"));
                else curriculum.Stage5_Narratives.Add(ToLesson(s, focus, score + 0.5, "tatoeba/wiki"));

                // Truncate to caps to keep things small at first
                if (curriculum.Stage1_WordsAndSimpleSV.Count >= maxSentencesPerStage &&
                    curriculum.Stage2_SVO.Count >= maxSentencesPerStage &&
                    curriculum.Stage3_Modifiers.Count >= maxSentencesPerStage &&
                    curriculum.Stage4_Questions.Count >= maxSentencesPerStage &&
                    curriculum.Stage5_Narratives.Count >= maxSentencesPerStage)
                {
                    break;
                }
            }

            // Coarse shuffle for variety
            Shuffle(curriculum.Stage1_WordsAndSimpleSV);
            Shuffle(curriculum.Stage2_SVO);
            Shuffle(curriculum.Stage3_Modifiers);
            Shuffle(curriculum.Stage4_Questions);
            Shuffle(curriculum.Stage5_Narratives);

            await Task.CompletedTask;
            return curriculum;
        }

        private static LessonItem ToLesson(string s, IReadOnlyList<string> focus, double difficulty, string source)
        {
            var targets = new Dictionary<string, double>();
            foreach (var f in focus) targets[f] = 1.0;
            return new LessonItem(s, focus, Math.Clamp(difficulty, 0, 1), source, targets);
        }

        private static double ScoreSentence(string s, out IReadOnlyList<string> focus)
        {
            var toks = Tokenizer.Matches(s).Select(m => m.Value).ToList();
            var words = toks.Where(t => char.IsLetter(t.FirstOrDefault())).ToList();
            var len = words.Count;

            // Penalize long sentences, reward short concrete tokens
            double score = 0.5;
            if (len <= 5) score += 0.2; else if (len <= 10) score += 0.05; else score -= 0.2;

            // Focus concepts: top content words (very naive)
            var content = words
                .Where(w => !Stop.Contains(w))
                .Select(w => w.ToLowerInvariant())
                .GroupBy(w => w)
                .OrderByDescending(g => g.Count())
                .Take(2)
                .Select(g => g.Key)
                .ToList();

            focus = content;
            return Math.Clamp(score, 0, 1);
        }

        private static bool IsQuestion(string s) => s.TrimEnd().EndsWith("?");
        private static bool HasModifier(string s) => Regex.IsMatch(s, "\\b(very|really|so|quite|red|blue|big|small|happy|sad)\\b", RegexOptions.IgnoreCase);
        private static bool IsSVO(string s) => Regex.IsMatch(s, "\\b[A-Za-z]+\\s+(is|are|was|were|likes|like|has|have|sees|see|eats|eat|makes|make)\\s+[A-Za-z]+\\b");
        private static bool IsSimpleSV(string s) => Regex.IsMatch(s, "\\b[A-Za-z]+\\s+(is|are|runs|run|jumps|jump|sleeps|sleep)\\b");
        private static int WordCount(string s) => Tokenizer.Matches(s).Count(m => char.IsLetter(m.Value.FirstOrDefault()));
        private static IEnumerable<string> SplitIntoSentences(string text)
        {
            // Very naive splitter
            return Regex.Split(text, @"(?<=[.!?])\s+").Where(x => !string.IsNullOrWhiteSpace(x));
        }

        private static void Shuffle<T>(IList<T> list)
        {
            var rng = new Random(12345);
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
