using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GreyMatter.Core;
using GreyMatter.Storage;

namespace GreyMatter
{
    /// <summary>
    /// Demo showcasing Phase 2: Procedural Generation of Cortical Columns
    /// Inspired by No Man's Sky's procedural planet generation
    /// </summary>
    public class ProceduralGenerationDemo
    {
        private readonly ProceduralCorticalColumnGenerator _generator;
        private readonly HybridPersistenceManager _persistenceManager;

        public ProceduralGenerationDemo()
        {
            _generator = new ProceduralCorticalColumnGenerator();
            _persistenceManager = new HybridPersistenceManager("./brain_data", _generator);
        }

        public async Task RunDemoAsync()
        {
            Console.WriteLine("🚀 **PHASE 2: PROCEDURAL CORTICAL COLUMN GENERATION**");
            Console.WriteLine("==================================================");
            Console.WriteLine("Inspired by No Man's Sky - Generating neural structures on-demand");
            Console.WriteLine();

            var stopwatch = Stopwatch.StartNew();

            // Demo 1: Generate columns for different semantic domains
            await DemonstrateSemanticDomainGenerationAsync();

            // Demo 2: Show coordinate-based generation consistency
            await DemonstrateCoordinateConsistencyAsync();

            // Demo 3: Demonstrate hybrid persistence decisions
            await DemonstrateHybridPersistenceAsync();

            // Demo 4: Scale demonstration - generate many columns
            await DemonstrateScaleGenerationAsync();

            stopwatch.Stop();

            // Final statistics
            await ShowFinalStatisticsAsync();

            Console.WriteLine($"\n⏱️ **PROCEDURAL GENERATION COMPLETE** - Total time: {stopwatch.Elapsed.TotalMinutes:F1} minutes");
        }

        /// <summary>
        /// Demonstrate generating columns for different semantic domains
        /// </summary>
        private async Task DemonstrateSemanticDomainGenerationAsync()
        {
            Console.WriteLine("🧠 **DEMO 1: SEMANTIC DOMAIN GENERATION**");
            Console.WriteLine("Generating cortical columns for different knowledge domains...");

            var domains = new[]
            {
                ("technical", "machine_learning", "neural_network_training"),
                ("scientific", "quantum_physics", "wave_function_collapse"),
                ("literary", "poetry", "metaphor_analysis"),
                ("conversational", "daily_dialogue", "social_interaction"),
                ("mathematical", "calculus", "derivative_computation")
            };

            foreach (var (domain, topic, context) in domains)
            {
                var coordinates = new SemanticCoordinates
                {
                    Domain = domain,
                    Topic = topic,
                    Context = context,
                    IsAbstract = domain == "scientific" || domain == "mathematical",
                    PolysemyCount = domain == "literary" ? 3 : 1
                };

                var requirements = new TaskRequirements
                {
                    Complexity = domain == "technical" ? "high" : "medium",
                    Precision = domain == "scientific" ? "high" : "medium",
                    ExpectedLoad = 1000
                };

                var column = await _persistenceManager.GetColumnAsync("semantic", coordinates, requirements);

                Console.WriteLine($"  ✅ Generated {domain} column: {column.Id}");
                Console.WriteLine($"     Size: {column.Size}, Sparsity: {column.Sparsity:F4}, Complexity: {column.Complexity:F2}");
                Console.WriteLine($"     Patterns: {column.NeuralPatterns.Count}");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrate that the same coordinates generate consistent columns
        /// </summary>
        private async Task DemonstrateCoordinateConsistencyAsync()
        {
            Console.WriteLine("🎯 **DEMO 2: COORDINATE CONSISTENCY**");
            Console.WriteLine("Same semantic coordinates should generate identical columns...");

            var coordinates = new SemanticCoordinates
            {
                Domain = "technical",
                Topic = "neural_network",
                Context = "backpropagation",
                IsAbstract = true,
                PolysemyCount = 2
            };

            var requirements = new TaskRequirements
            {
                Complexity = "high",
                Precision = "high",
                ExpectedLoad = 2000
            };

            // Generate the same column multiple times
            var columns = new List<ProceduralCorticalColumn>();
            for (int i = 0; i < 3; i++)
            {
                var column = await _persistenceManager.GetColumnAsync("semantic", coordinates, requirements);
                columns.Add(column);
                Console.WriteLine($"  🔄 Generation {i + 1}: {column.Id} (Size: {column.Size})");
            }

            // Verify consistency
            var firstColumn = columns[0];
            var allConsistent = columns.All(c =>
                c.Id == firstColumn.Id &&
                c.Size == firstColumn.Size &&
                c.Sparsity == firstColumn.Sparsity);

            Console.WriteLine($"  ✅ Coordinate consistency: {(allConsistent ? "VERIFIED" : "FAILED")}");
            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrate hybrid persistence decisions
        /// </summary>
        private async Task DemonstrateHybridPersistenceAsync()
        {
            Console.WriteLine("💾 **DEMO 3: HYBRID PERSISTENCE DECISIONS**");
            Console.WriteLine("System decides what to persist vs generate on-demand...");

            var testCoordinates = new[]
            {
                new SemanticCoordinates { Domain = "common", Topic = "hello", Context = "greeting" },
                new SemanticCoordinates { Domain = "scientific", Topic = "quantum_entanglement", Context = "physics" },
                new SemanticCoordinates { Domain = "technical", Topic = "machine_learning", Context = "ai" }
            };

            foreach (var coordinates in testCoordinates)
            {
                // Generate column multiple times to simulate usage
                for (int i = 0; i < 5; i++)
                {
                    await _persistenceManager.GetColumnAsync("semantic", coordinates,
                        new TaskRequirements { Complexity = "medium" });
                }

                var columnId = $"{coordinates.Domain}_{coordinates.Topic}".Replace(" ", "_");
                var decision = await GetPersistenceDecisionAsync(columnId);

                Console.WriteLine($"  📊 {coordinates.Domain}: {decision?.Reason ?? "generated"}");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrate scaling to many columns (like No Man's Sky's galaxy)
        /// </summary>
        private async Task DemonstrateScaleGenerationAsync()
        {
            Console.WriteLine("🌌 **DEMO 4: SCALE GENERATION**");
            Console.WriteLine("Generating many columns to demonstrate scalability...");

            var scaleStopwatch = Stopwatch.StartNew();
            var generatedColumns = new List<ProceduralCorticalColumn>();

            // Generate columns across a "semantic space" (like coordinates in No Man's Sky)
            for (int x = 0; x < 5; x++) // Domain dimension
            {
                for (int y = 0; y < 4; y++) // Topic dimension
                {
                    for (int z = 0; z < 3; z++) // Context dimension
                    {
                        var coordinates = new SemanticCoordinates
                        {
                            Domain = $"domain_{x}",
                            Topic = $"topic_{x}_{y}",
                            Context = $"context_{x}_{y}_{z}",
                            IsAbstract = (x + y + z) % 3 == 0,
                            PolysemyCount = 1 + (x + y + z) % 3
                        };

                        var column = await _persistenceManager.GetColumnAsync(
                            new[] { "phonetic", "semantic", "syntactic", "contextual" }[(x + y + z) % 4],
                            coordinates,
                            new TaskRequirements { Complexity = "medium" });

                        generatedColumns.Add(column);
                    }
                }
            }

            scaleStopwatch.Stop();

            Console.WriteLine($"  ✅ Generated {generatedColumns.Count} unique cortical columns");
            Console.WriteLine($"     Generation time: {scaleStopwatch.Elapsed.TotalSeconds:F2} seconds");
            Console.WriteLine($"     Average time per column: {scaleStopwatch.Elapsed.TotalMilliseconds / generatedColumns.Count:F2} ms");
            Console.WriteLine($"     Total neural patterns: {generatedColumns.Sum(c => c.NeuralPatterns.Count)}");

            // Show diversity
            var columnTypes = generatedColumns.GroupBy(c => c.Type)
                .Select(g => $"{g.Key}: {g.Count()}");
            Console.WriteLine($"     Column type distribution: {string.Join(", ", columnTypes)}");

            Console.WriteLine();
        }

        /// <summary>
        /// Show final statistics
        /// </summary>
        private async Task ShowFinalStatisticsAsync()
        {
            Console.WriteLine("📊 **FINAL STATISTICS**");

            var genStats = _generator.GetStats();
            var persistStats = _persistenceManager.GetStats();

            Console.WriteLine("Procedural Generation:");
            Console.WriteLine($"  • Total columns generated: {genStats.TotalColumnsGenerated}");
            Console.WriteLine($"  • Active columns: {genStats.ActiveColumns}");
            Console.WriteLine($"  • Average access count: {genStats.AverageAccessCount:F1}");

            Console.WriteLine("Hybrid Persistence:");
            Console.WriteLine($"  • Total columns processed: {persistStats.TotalColumns}");
            Console.WriteLine($"  • Persisted columns: {persistStats.PersistedColumns}");
            Console.WriteLine($"  • Generated columns: {persistStats.GeneratedColumns}");
            Console.WriteLine($"  • Persistence ratio: {persistStats.PersistenceRatio:P1}");
            Console.WriteLine($"  • Avg structural importance: {persistStats.AverageStructuralImportance:F2}");

            // Save persistence decisions
            await _persistenceManager.SavePersistenceDecisionsAsync();
        }

        /// <summary>
        /// Helper to get persistence decision for a column
        /// </summary>
        private async Task<PersistenceDecision> GetPersistenceDecisionAsync(string columnId)
        {
            // This is a simplified version - in practice, we'd need to expose this from the manager
            return new PersistenceDecision
            {
                ColumnId = columnId,
                ShouldPersist = false,
                Reason = "demo_generated",
                StructuralImportance = 0.5,
                LastPersisted = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Run the procedural generation demo
        /// </summary>
        public static async Task RunAsync()
        {
            var demo = new ProceduralGenerationDemo();
            await demo.RunDemoAsync();
        }
    }
}
