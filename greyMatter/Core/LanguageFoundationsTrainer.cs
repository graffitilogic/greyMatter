using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GreyMatter.Storage;

namespace GreyMatter.Core
{
    /// <summary>
    /// Fundamental language comprehension trainer
    /// Focuses on preschool through elementary language acquisition patterns
    /// </summary>
    public class LanguageFoundationsTrainer
    {
        private readonly Cerebro _brain;
        private readonly CerebroConfiguration _config;
        
        public LanguageFoundationsTrainer(Cerebro brain, CerebroConfiguration config)
        {
            _brain = brain;
            _config = config;
        }

        /// <summary>
        /// Progressive language learning curriculum following developmental stages
        /// </summary>
        public async Task RunFoundationalTrainingAsync()
        {
            Console.WriteLine("🎓 **FOUNDATIONAL LANGUAGE COMPREHENSION TRAINING**");
            Console.WriteLine("==================================================");
            Console.WriteLine("Following developmental language acquisition patterns\n");

            // Stage 1: Basic Vocabulary (Ages 2-4 equivalent)
            await Stage1_BasicVocabulary();
            
            // Stage 2: Simple Sentences (Ages 4-6 equivalent)
            await Stage2_SimpleSentences();
            
            // Stage 3: Grammar Patterns (Ages 6-8 equivalent)
            await Stage3_GrammarPatterns();
            
            // Stage 4: Reading Comprehension (Ages 8-10 equivalent)
            await Stage4_ReadingComprehension();
            
            // Stage 5: Complex Language (Ages 10+ equivalent)
            await Stage5_ComplexLanguage();
        }

        /// <summary>
        /// Stage 1: Core vocabulary building (2000 most common words)
        /// </summary>
        private async Task Stage1_BasicVocabulary()
        {
            Console.WriteLine("📚 **STAGE 1: BASIC VOCABULARY** (Core 2000 Words)");
            Console.WriteLine("Learning fundamental nouns, verbs, adjectives\n");

            // Core nouns (things children learn first)
            var coreNouns = new Dictionary<string, Dictionary<string, double>>
            {
                ["apple"] = new() { ["fruit"] = 1.0, ["food"] = 0.9, ["red"] = 0.7, ["round"] = 0.6 },
                ["ball"] = new() { ["toy"] = 1.0, ["round"] = 0.9, ["play"] = 0.8, ["bounce"] = 0.7 },
                ["cat"] = new() { ["animal"] = 1.0, ["pet"] = 0.9, ["furry"] = 0.8, ["meow"] = 0.7 },
                ["dog"] = new() { ["animal"] = 1.0, ["pet"] = 0.9, ["bark"] = 0.8, ["loyal"] = 0.7 },
                ["house"] = new() { ["building"] = 1.0, ["home"] = 0.9, ["shelter"] = 0.8, ["family"] = 0.7 },
                ["car"] = new() { ["vehicle"] = 1.0, ["transport"] = 0.9, ["wheels"] = 0.8, ["drive"] = 0.7 },
                ["book"] = new() { ["reading"] = 1.0, ["knowledge"] = 0.9, ["pages"] = 0.8, ["story"] = 0.7 },
                ["water"] = new() { ["liquid"] = 1.0, ["drink"] = 0.9, ["wet"] = 0.8, ["clear"] = 0.7 },
                ["sun"] = new() { ["star"] = 1.0, ["bright"] = 0.9, ["warm"] = 0.8, ["yellow"] = 0.7 },
                ["tree"] = new() { ["plant"] = 1.0, ["green"] = 0.9, ["tall"] = 0.8, ["leaves"] = 0.7 }
            };

            // Core verbs (actions children learn first)
            var coreVerbs = new Dictionary<string, Dictionary<string, double>>
            {
                ["run"] = new() { ["action"] = 1.0, ["fast"] = 0.9, ["legs"] = 0.8, ["movement"] = 0.7 },
                ["eat"] = new() { ["action"] = 1.0, ["food"] = 0.9, ["mouth"] = 0.8, ["hunger"] = 0.7 },
                ["sleep"] = new() { ["action"] = 1.0, ["rest"] = 0.9, ["bed"] = 0.8, ["tired"] = 0.7 },
                ["play"] = new() { ["action"] = 1.0, ["fun"] = 0.9, ["toys"] = 0.8, ["happy"] = 0.7 },
                ["walk"] = new() { ["action"] = 1.0, ["slow"] = 0.8, ["legs"] = 0.8, ["movement"] = 0.7 },
                ["jump"] = new() { ["action"] = 1.0, ["up"] = 0.9, ["legs"] = 0.8, ["high"] = 0.7 },
                ["sing"] = new() { ["action"] = 1.0, ["voice"] = 0.9, ["music"] = 0.8, ["happy"] = 0.7 },
                ["look"] = new() { ["action"] = 1.0, ["eyes"] = 0.9, ["see"] = 0.8, ["observe"] = 0.7 },
                ["give"] = new() { ["action"] = 1.0, ["share"] = 0.9, ["kind"] = 0.8, ["transfer"] = 0.7 },
                ["help"] = new() { ["action"] = 1.0, ["assist"] = 0.9, ["kind"] = 0.8, ["support"] = 0.7 }
            };

            // Basic adjectives (descriptive words)
            var coreAdjectives = new Dictionary<string, Dictionary<string, double>>
            {
                ["big"] = new() { ["size"] = 1.0, ["large"] = 0.9, ["huge"] = 0.8, ["opposite_small"] = 0.7 },
                ["small"] = new() { ["size"] = 1.0, ["little"] = 0.9, ["tiny"] = 0.8, ["opposite_big"] = 0.7 },
                ["red"] = new() { ["color"] = 1.0, ["bright"] = 0.8, ["apple"] = 0.7, ["fire"] = 0.6 },
                ["blue"] = new() { ["color"] = 1.0, ["sky"] = 0.8, ["water"] = 0.7, ["calm"] = 0.6 },
                ["happy"] = new() { ["emotion"] = 1.0, ["joy"] = 0.9, ["smile"] = 0.8, ["good"] = 0.7 },
                ["sad"] = new() { ["emotion"] = 1.0, ["cry"] = 0.9, ["unhappy"] = 0.8, ["bad"] = 0.7 },
                ["hot"] = new() { ["temperature"] = 1.0, ["warm"] = 0.8, ["fire"] = 0.7, ["summer"] = 0.6 },
                ["cold"] = new() { ["temperature"] = 1.0, ["cool"] = 0.8, ["ice"] = 0.7, ["winter"] = 0.6 },
                ["fast"] = new() { ["speed"] = 1.0, ["quick"] = 0.9, ["run"] = 0.8, ["opposite_slow"] = 0.7 },
                ["slow"] = new() { ["speed"] = 1.0, ["walk"] = 0.8, ["turtle"] = 0.7, ["opposite_fast"] = 0.6 }
            };

            // Train core vocabulary
            var allWords = new[] { coreNouns, coreVerbs, coreAdjectives };
            foreach (var wordCategory in allWords)
            {
                foreach (var (word, features) in wordCategory)
                {
                    var result = await _brain.LearnConceptAsync(word, features);
                    Console.WriteLine($"   ✅ {word}: {result.NeuronsInvolved} neurons");
                }
            }

            Console.WriteLine($"✅ Stage 1 Complete: {allWords.Sum(w => w.Count)} core words learned\n");
        }

        /// <summary>
        /// Stage 2: Simple sentence patterns (Subject-Verb-Object)
        /// </summary>
        private async Task Stage2_SimpleSentences()
        {
            Console.WriteLine("📝 **STAGE 2: SIMPLE SENTENCES** (Basic Grammar Patterns)");
            Console.WriteLine("Learning subject-verb-object patterns\n");

            var simpleSentences = new[]
            {
                ("the cat sleeps", new Dictionary<string, double> { ["subject_verb"] = 1.0, ["animal_action"] = 0.9, ["simple"] = 0.8 }),
                ("I eat food", new Dictionary<string, double> { ["subject_verb_object"] = 1.0, ["first_person"] = 0.9, ["basic_need"] = 0.8 }),
                ("the ball is red", new Dictionary<string, double> { ["subject_be_adjective"] = 1.0, ["description"] = 0.9, ["color"] = 0.8 }),
                ("dogs run fast", new Dictionary<string, double> { ["subject_verb_adverb"] = 1.0, ["animals"] = 0.9, ["movement"] = 0.8 }),
                ("children play games", new Dictionary<string, double> { ["subject_verb_object"] = 1.0, ["people_action"] = 0.9, ["fun"] = 0.8 }),
                ("the sun is bright", new Dictionary<string, double> { ["subject_be_adjective"] = 1.0, ["nature"] = 0.9, ["light"] = 0.8 }),
                ("mom gives hugs", new Dictionary<string, double> { ["subject_verb_object"] = 1.0, ["family"] = 0.9, ["love"] = 0.8 }),
                ("birds can fly", new Dictionary<string, double> { ["subject_modal_verb"] = 1.0, ["ability"] = 0.9, ["animals"] = 0.8 }),
                ("water is wet", new Dictionary<string, double> { ["subject_be_adjective"] = 1.0, ["property"] = 0.9, ["liquid"] = 0.8 }),
                ("books have pages", new Dictionary<string, double> { ["subject_verb_object"] = 1.0, ["possession"] = 0.9, ["reading"] = 0.8 })
            };

            foreach (var (sentence, features) in simpleSentences)
            {
                var result = await _brain.ProcessInputAsync(sentence, features);
                Console.WriteLine($"   📝 \"{sentence}\": confidence {result.Confidence:P0}");
            }

            Console.WriteLine($"✅ Stage 2 Complete: {simpleSentences.Length} sentence patterns learned\n");
        }

        /// <summary>
        /// Stage 3: Grammar patterns and word relationships
        /// </summary>
        private async Task Stage3_GrammarPatterns()
        {
            Console.WriteLine("📐 **STAGE 3: GRAMMAR PATTERNS** (Word Relationships)");
            Console.WriteLine("Learning plurals, tenses, prepositions\n");

            // Plurals
            var pluralPairs = new[]
            {
                ("cat", "cats", new Dictionary<string, double> { ["singular_plural"] = 1.0, ["animal"] = 0.9 }),
                ("book", "books", new Dictionary<string, double> { ["singular_plural"] = 1.0, ["object"] = 0.9 }),
                ("child", "children", new Dictionary<string, double> { ["irregular_plural"] = 1.0, ["person"] = 0.9 }),
                ("mouse", "mice", new Dictionary<string, double> { ["irregular_plural"] = 1.0, ["animal"] = 0.9 })
            };

            // Tenses
            var tensePairs = new[]
            {
                ("I walk", "I walked", new Dictionary<string, double> { ["present_past"] = 1.0, ["movement"] = 0.9 }),
                ("she eats", "she ate", new Dictionary<string, double> { ["present_past"] = 1.0, ["action"] = 0.9 }),
                ("we run", "we ran", new Dictionary<string, double> { ["irregular_past"] = 1.0, ["movement"] = 0.9 }),
                ("they go", "they went", new Dictionary<string, double> { ["irregular_past"] = 1.0, ["movement"] = 0.9 })
            };

            // Prepositions (spatial relationships)
            var prepositions = new[]
            {
                ("the cat is on the table", new Dictionary<string, double> { ["spatial_on"] = 1.0, ["above"] = 0.9 }),
                ("the ball is under the bed", new Dictionary<string, double> { ["spatial_under"] = 1.0, ["below"] = 0.9 }),
                ("the book is in the box", new Dictionary<string, double> { ["spatial_in"] = 1.0, ["inside"] = 0.9 }),
                ("the dog is beside the house", new Dictionary<string, double> { ["spatial_beside"] = 1.0, ["next_to"] = 0.9 })
            };

            // Process all grammar patterns
            foreach (var (singular, plural, features) in pluralPairs)
            {
                await _brain.LearnConceptAsync($"plural_{singular}_{plural}", features);
                Console.WriteLine($"   📊 {singular} → {plural}");
            }

            foreach (var (present, past, features) in tensePairs)
            {
                await _brain.LearnConceptAsync($"tense_{present}_{past}", features);
                Console.WriteLine($"   ⏰ {present} → {past}");
            }

            foreach (var (sentence, features) in prepositions)
            {
                var result = await _brain.ProcessInputAsync(sentence, features);
                Console.WriteLine($"   📍 \"{sentence}\": {result.Confidence:P0}");
            }

            Console.WriteLine("✅ Stage 3 Complete: Grammar patterns established\n");
        }

        /// <summary>
        /// Stage 4: Reading comprehension with simple stories
        /// </summary>
        private async Task Stage4_ReadingComprehension()
        {
            Console.WriteLine("📖 **STAGE 4: READING COMPREHENSION** (Simple Stories)");
            Console.WriteLine("Learning narrative structure and comprehension\n");

            var simpleStories = new[]
            {
                new {
                    Title = "The Happy Cat",
                    Text = "There was a cat named Fluffy. Fluffy was very happy. She liked to play with a red ball. Every day, Fluffy would run and jump. She was the happiest cat in the neighborhood.",
                    Features = new Dictionary<string, double> { ["narrative"] = 1.0, ["character"] = 0.9, ["emotion"] = 0.8, ["action_sequence"] = 0.7 }
                },
                new {
                    Title = "The Little Tree",
                    Text = "A small tree grew in the garden. It was green and had many leaves. Birds liked to sit on its branches. The tree gave shade on hot days. Everyone loved the little tree.",
                    Features = new Dictionary<string, double> { ["nature_story"] = 1.0, ["description"] = 0.9, ["relationships"] = 0.8, ["purpose"] = 0.7 }
                },
                new {
                    Title = "Going to School",
                    Text = "Sam gets ready for school. He puts on his clothes and eats breakfast. Then he walks to school with his friends. At school, Sam learns new things. He likes reading and math.",
                    Features = new Dictionary<string, double> { ["routine"] = 1.0, ["sequence"] = 0.9, ["learning"] = 0.8, ["social"] = 0.7 }
                }
            };

            foreach (var story in simpleStories)
            {
                Console.WriteLine($"   📚 Reading: \"{story.Title}\"");
                var result = await _brain.ProcessInputAsync(story.Text, story.Features);
                Console.WriteLine($"      Comprehension: {result.Confidence:P0} | Neurons: {result.ActivatedNeurons}");
                
                // Extract key concepts from the story
                var concepts = ExtractStoryConcepts(story.Text);
                foreach (var concept in concepts)
                {
                    await _brain.LearnConceptAsync(concept.Key, concept.Value);
                }
            }

            Console.WriteLine("✅ Stage 4 Complete: Story comprehension developed\n");
        }

        /// <summary>
        /// Stage 5: Complex language patterns and abstract concepts
        /// </summary>
        private async Task Stage5_ComplexLanguage()
        {
            Console.WriteLine("🎯 **STAGE 5: COMPLEX LANGUAGE** (Abstract Concepts)");
            Console.WriteLine("Learning metaphors, idioms, and abstract thinking\n");

            var complexConcepts = new[]
            {
                ("time flies when you're having fun", new Dictionary<string, double> { ["metaphor"] = 1.0, ["time_perception"] = 0.9, ["enjoyment"] = 0.8 }),
                ("it's raining cats and dogs", new Dictionary<string, double> { ["idiom"] = 1.0, ["heavy_rain"] = 0.9, ["expression"] = 0.8 }),
                ("knowledge is power", new Dictionary<string, double> { ["metaphor"] = 1.0, ["abstract_comparison"] = 0.9, ["wisdom"] = 0.8 }),
                ("actions speak louder than words", new Dictionary<string, double> { ["proverb"] = 1.0, ["behavior"] = 0.9, ["truth"] = 0.8 }),
                ("the early bird catches the worm", new Dictionary<string, double> { ["proverb"] = 1.0, ["punctuality"] = 0.9, ["success"] = 0.8 })
            };

            foreach (var (phrase, features) in complexConcepts)
            {
                var result = await _brain.ProcessInputAsync(phrase, features);
                Console.WriteLine($"   🎭 \"{phrase}\": {result.Confidence:P0}");
            }

            Console.WriteLine("✅ Stage 5 Complete: Advanced language patterns acquired\n");
            Console.WriteLine("🎉 **FOUNDATIONAL LANGUAGE TRAINING COMPLETE**");
            Console.WriteLine("   • Core vocabulary: 30+ essential words");
            Console.WriteLine("   • Grammar patterns: plurals, tenses, prepositions");
            Console.WriteLine("   • Sentence structure: S-V-O patterns");
            Console.WriteLine("   • Reading comprehension: narrative understanding");
            Console.WriteLine("   • Complex language: metaphors and idioms");
        }

        /// <summary>
        /// Extract key concepts from story text for learning
        /// </summary>
        private Dictionary<string, Dictionary<string, double>> ExtractStoryConcepts(string text)
        {
            // Simple concept extraction based on common patterns
            var concepts = new Dictionary<string, Dictionary<string, double>>();
            
            // This would be enhanced with NLP in a real implementation
            var words = text.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var word in words.Where(w => w.Length > 3))
            {
                if (!concepts.ContainsKey(word))
                {
                    concepts[word] = new Dictionary<string, double>
                    {
                        ["story_element"] = 0.8,
                        ["vocabulary"] = 0.7,
                        ["context"] = 0.6
                    };
                }
            }
            
            return concepts;
        }
    }
}
