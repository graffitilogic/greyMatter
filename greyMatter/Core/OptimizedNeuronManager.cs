using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreyMatter.Core;

namespace GreyMatter.Core
{
    /// <summary>
    /// Optimized neuron manager that implements efficient neuron reuse and reduces memory bloat
    /// Fixes: excessive neuron creation, poor reuse, concept duplication
    /// </summary>
    public class OptimizedNeuronManager
    {
        private readonly Dictionary<string, List<Guid>> _wordToNeurons = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<Guid, string> _neuronToWord = new();
        private readonly Dictionary<string, int> _wordFrequency = new(StringComparer.OrdinalIgnoreCase);
        private readonly int _maxNeuronsPerWord;
        private readonly int _baseNeuronsPerWord;
        private readonly Random _random = new();

        public OptimizedNeuronManager(int baseNeuronsPerWord = 5, int maxNeuronsPerWord = 25)
        {
            _baseNeuronsPerWord = baseNeuronsPerWord;
            _maxNeuronsPerWord = maxNeuronsPerWord;
        }

        /// <summary>
        /// Get or create neurons for a word with intelligent reuse
        /// </summary>
        public async Task<List<HybridNeuron>> GetNeuronsForWord(string word, NeuronCluster cluster)
        {
            word = word.ToLowerInvariant().Trim();
            if (string.IsNullOrWhiteSpace(word)) return new List<HybridNeuron>();

            // Track word frequency for intelligent neuron allocation
            _wordFrequency[word] = _wordFrequency.GetValueOrDefault(word, 0) + 1;

            // Check if we already have neurons for this word
            if (_wordToNeurons.TryGetValue(word, out var existingNeuronIds))
            {
                var existingNeurons = new List<HybridNeuron>();
                foreach (var neuronId in existingNeuronIds)
                {
                    var neuron = await cluster.GetNeuronAsync(neuronId);
                    if (neuron != null)
                    {
                        existingNeurons.Add(neuron);
                    }
                }

                // If we have enough neurons, reuse them
                var targetNeurons = CalculateTargetNeurons(word);
                if (existingNeurons.Count >= targetNeurons)
                {
                    return existingNeurons.Take(targetNeurons).ToList();
                }

                // Need more neurons - add only what's needed
                var neuronsNeeded = targetNeurons - existingNeurons.Count;
                var newNeurons = await CreateNeuronsForWord(word, neuronsNeeded, cluster);
                existingNeurons.AddRange(newNeurons);
                return existingNeurons;
            }

            // First time seeing this word - create initial neurons
            var initialNeurons = CalculateTargetNeurons(word);
            return await CreateNeuronsForWord(word, initialNeurons, cluster);
        }

        /// <summary>
        /// Calculate intelligent target neuron count based on word frequency and characteristics
        /// </summary>
        private int CalculateTargetNeurons(string word)
        {
            var frequency = _wordFrequency.GetValueOrDefault(word, 1);
            
            // High-frequency words (like "the", "to", "is") get more neurons for better representation
            if (frequency > 100) return Math.Min(_maxNeuronsPerWord, _baseNeuronsPerWord + 10);
            if (frequency > 50) return Math.Min(_maxNeuronsPerWord, _baseNeuronsPerWord + 5);
            if (frequency > 10) return Math.Min(_maxNeuronsPerWord, _baseNeuronsPerWord + 2);
            
            // Rare words get fewer neurons initially
            return _baseNeuronsPerWord;
        }

        /// <summary>
        /// Create new neurons for a word and track them
        /// </summary>
        private async Task<List<HybridNeuron>> CreateNeuronsForWord(string word, int count, NeuronCluster cluster)
        {
            var newNeurons = new List<HybridNeuron>();
            var neuronIds = new List<Guid>();

            for (int i = 0; i < count; i++)
            {
                // Use word as concept tag, not domain-prefixed concept
                var neuron = new HybridNeuron(word);
                neuron.AssociateConcept(word);
                
                await cluster.AddNeuronAsync(neuron);
                newNeurons.Add(neuron);
                neuronIds.Add(neuron.Id);
                
                // Track mapping
                _neuronToWord[neuron.Id] = word;
            }

            // Update word-to-neurons mapping
            if (_wordToNeurons.ContainsKey(word))
            {
                _wordToNeurons[word].AddRange(neuronIds);
            }
            else
            {
                _wordToNeurons[word] = neuronIds;
            }

            return newNeurons;
        }

        /// <summary>
        /// Get statistics about neuron usage efficiency
        /// </summary>
        public NeuronEfficiencyStats GetEfficiencyStats()
        {
            var totalWords = _wordToNeurons.Count;
            var totalNeurons = _wordToNeurons.Values.Sum(list => list.Count);
            var averageNeuronsPerWord = totalWords > 0 ? (double)totalNeurons / totalWords : 0;
            
            var topWords = _wordFrequency
                .OrderByDescending(kvp => kvp.Value)
                .Take(10)
                .ToList();

            return new NeuronEfficiencyStats
            {
                TotalWords = totalWords,
                TotalNeurons = totalNeurons,
                AverageNeuronsPerWord = averageNeuronsPerWord,
                ReuseEfficiency = CalculateReuseEfficiency(),
                TopFrequentWords = topWords
            };
        }

        /// <summary>
        /// Calculate how efficiently we're reusing neurons
        /// </summary>
        private double CalculateReuseEfficiency()
        {
            if (_wordFrequency.Count == 0) return 0.0;

            var totalWordOccurrences = _wordFrequency.Values.Sum();
            var totalNeurons = _wordToNeurons.Values.Sum(list => list.Count);
            
            // Higher ratio means better reuse
            return totalWordOccurrences / (double)Math.Max(totalNeurons, 1);
        }

        /// <summary>
        /// Optimize existing neurons by consolidating duplicates
        /// </summary>
        public async Task<int> OptimizeExistingNeurons(NeuronCluster cluster)
        {
            var allNeurons = await cluster.GetNeuronsAsync();
            var optimizedCount = 0;

            // Group neurons by their concept tags
            var neuronsByWord = allNeurons.Values
                .GroupBy(n => n.ConceptTag.ToLowerInvariant())
                .Where(g => !string.IsNullOrWhiteSpace(g.Key))
                .ToList();

            foreach (var wordGroup in neuronsByWord)
            {
                var word = wordGroup.Key;
                var neurons = wordGroup.ToList();
                var targetCount = CalculateTargetNeurons(word);

                // If we have too many neurons for this word, remove the least important ones
                if (neurons.Count > targetCount)
                {
                    var neuronsToRemove = neurons
                        .OrderBy(n => n.ImportanceScore)
                        .Take(neurons.Count - targetCount)
                        .ToList();

                    foreach (var neuron in neuronsToRemove)
                    {
                        await cluster.RemoveNeuronAsync(neuron.Id);
                        optimizedCount++;
                    }
                }

                // Update our tracking
                var remainingNeurons = neurons.Except(neurons.Take(neurons.Count - targetCount)).ToList();
                _wordToNeurons[word] = remainingNeurons.Select(n => n.Id).ToList();
                foreach (var neuron in remainingNeurons)
                {
                    _neuronToWord[neuron.Id] = word;
                }
            }

            return optimizedCount;
        }
    }

    /// <summary>
    /// Statistics about neuron usage efficiency
    /// </summary>
    public class NeuronEfficiencyStats
    {
        public int TotalWords { get; set; }
        public int TotalNeurons { get; set; }
        public double AverageNeuronsPerWord { get; set; }
        public double ReuseEfficiency { get; set; }
        public List<KeyValuePair<string, int>> TopFrequentWords { get; set; } = new();
    }
}
