using greyMatter.Core;
using greyMatter.Learning;
using System;
using System.Linq;

namespace greyMatter
{
    /// <summary>
    /// Phase 3 demo: Learning from actual text using the ephemeral brain
    /// </summary>
    public class TextLearningDemo
    {
        public static void RunDemo()
        {
            Console.WriteLine("=== Text Learning Demo (Phase 3) ===");
            Console.WriteLine("Learning concepts from actual text using ephemeral brain\n");

            var brain = new SimpleEphemeralBrain();
            var parser = new SimpleTextParser();

            // Sample children's book style text
            var sampleText = @"
                The red apple is sweet. The cat sees the apple on the tree.
                The big dog runs fast. The little bird flies high in the blue sky.
                The green grass grows in the garden. The yellow sun shines bright.
                The girl likes the sweet apple. The boy plays with the red ball.
                The happy cat sleeps on the soft bed. The tall tree has green leaves.
            ";

            Console.WriteLine("Sample text to learn from:");
            Console.WriteLine(sampleText.Trim());
            Console.WriteLine("\n" + new string('=', 60));

            // Parse the text into lessons
            Console.WriteLine("\n1. Parsing text into learning lessons:");
            var lessons = parser.ParseText(sampleText);
            
            foreach (var lesson in lessons.Take(3)) // Show first 3 lessons
            {
                Console.WriteLine($"\nSentence: {lesson.OriginalText}");
                Console.WriteLine($"Main concepts: {string.Join(", ", lesson.MainConcepts)}");
                Console.WriteLine($"Relationships: {lesson.ConceptRelationships.Count} found");
                
                foreach (var rel in lesson.ConceptRelationships.Take(2))
                {
                    Console.WriteLine($"  {rel.Concept1} --{rel.RelationType}--> {rel.Concept2} (strength: {rel.Strength})");
                }
            }

            Console.WriteLine($"\nTotal lessons extracted: {lessons.Count}");
            Console.WriteLine("\n" + new string('=', 60));

            // Learn from the text
            Console.WriteLine("\n2. Teaching the brain from text:");
            LearnFromLessons(brain, lessons);

            Console.WriteLine("\n" + new string('=', 60));

            // Test what the brain learned
            Console.WriteLine("\n3. Testing what the brain learned:");
            TestLearning(brain);

            Console.WriteLine("\n" + new string('=', 60));

            // Show brain state
            Console.WriteLine("\n4. Final brain state:");
            brain.ShowBrainScan();
            brain.ShowMemoryEfficiency();

            Console.WriteLine("\n=== Phase 3 Features Demonstrated ===");
            Console.WriteLine("✓ Text parsing: Extract concepts from real sentences");
            Console.WriteLine("✓ Relationship detection: Find connections between concepts");
            Console.WriteLine("✓ Incremental learning: Build concept networks from text");
            Console.WriteLine("✓ Association testing: Verify learned connections");
            Console.WriteLine("\nReady for Phase 4: Visualization and scale demonstration!");
        }

        private static void LearnFromLessons(SimpleEphemeralBrain brain, System.Collections.Generic.List<LearningLesson> lessons)
        {
            foreach (var lesson in lessons)
            {
                Console.WriteLine($"\nLearning from: {lesson.OriginalText}");
                
                // Learn main concepts first
                foreach (var concept in lesson.MainConcepts)
                {
                    // Find related concepts from relationships
                    var relatedConcepts = lesson.ConceptRelationships
                        .Where(r => r.Concept1 == concept || r.Concept2 == concept)
                        .SelectMany(r => new[] { r.Concept1, r.Concept2 })
                        .Where(c => c != concept)
                        .Distinct()
                        .ToArray();
                    
                    if (relatedConcepts.Any())
                    {
                        brain.Learn(concept, relatedConcepts);
                    }
                    else
                    {
                        brain.Learn(concept);
                    }
                }
                
                // Learn strong relationships as additional connections
                foreach (var relationship in lesson.ConceptRelationships.Where(r => r.Strength > 0.7))
                {
                    // Strengthen the connection by learning the reverse relationship
                    brain.Learn(relationship.Concept2, new[] { relationship.Concept1 });
                }
            }
        }

        private static void TestLearning(SimpleEphemeralBrain brain)
        {
            var testConcepts = new[] { "apple", "red", "cat", "tree", "sky", "dog" };
            
            foreach (var concept in testConcepts)
            {
                Console.WriteLine($"\nTesting recall for '{concept}':");
                var related = brain.Recall(concept);
                
                if (related.Any())
                {
                    Console.WriteLine($"  Associated with: {string.Join(", ", related.Take(3))}");
                }
                else
                {
                    Console.WriteLine("  No associations found");
                }
            }
            
            // Test some specific associations we expect
            Console.WriteLine("\n--- Expected Association Tests ---");
            TestSpecificAssociation(brain, "red", "apple", "Color-object association");
            TestSpecificAssociation(brain, "cat", "tree", "Animal-object association");
            TestSpecificAssociation(brain, "blue", "sky", "Color-object association");
        }

        private static void TestSpecificAssociation(SimpleEphemeralBrain brain, string concept1, string concept2, string description)
        {
            var related1 = brain.Recall(concept1);
            var related2 = brain.Recall(concept2);
            
            var association1to2 = related1.Any(r => r.Contains(concept2));
            var association2to1 = related2.Any(r => r.Contains(concept1));
            
            var result = association1to2 || association2to1 ? "✓ FOUND" : "✗ NOT FOUND";
            Console.WriteLine($"{description}: {concept1} ↔ {concept2} - {result}");
        }
    }
}
