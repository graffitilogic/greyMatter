using GreyMatter.Poc.Pipeline;

namespace GreyMatter.Poc.Eval;

/// <summary>
/// The P4 control set, replacing the ported mash/pseudoword lists.
///
/// **Why the ported controls had to go.** P0.3 and P2.3 measured two independent
/// disqualifying properties:
///   • rule 5 fails — the strongest control sits at 0.741 similarity while the
///     median vocabulary word has a neighbour at 0.833, so the ported controls are
///     *easier* than ordinary corpus words;
///   • all 8 are out-of-vocabulary, all 14 trained cues are in-vocabulary, so any
///     encoder with a distributional component separates them at AUC 1.000 by
///     detecting vocabulary membership. Subtracting the ceiling does not rescue
///     this: both terms carry the same artifact.
///
/// **What replaces them.** Controls are real corpus words, frequency-matched to the
/// trained cues, and held out of training — the trainer skips their tokens. So a
/// control is:
///   • in-vocabulary and morphologically ordinary (no OOV artifact);
///   • of comparable frequency (no base-rate artifact);
///   • given a perfectly valid, regenerable assembly by the runtime (§4.3 — nothing
///     needs to have been stored for a cue to activate).
///
/// The ONLY thing distinguishing a control from a trained cue is that no learning
/// ever ran on it. That makes the recall gate a test of the architecture rather
/// than of the encoder, which is the whole point of rule 6.
/// </summary>
public static class ControlSets
{
    public sealed record Split(
        IReadOnlyList<string> Trained,
        IReadOnlyList<string> Controls,
        IReadOnlyDictionary<string, int> Frequency)
    {
        public HashSet<string> HeldOut => Controls.ToHashSet();

        /// <summary>Frequency-match quality — reported every run, per rule 5.</summary>
        public (double trainedMedian, double controlMedian, double ratio) FrequencyMatch()
        {
            double t = Median(Trained.Select(w => (double)Frequency.GetValueOrDefault(w)).ToList());
            double c = Median(Controls.Select(w => (double)Frequency.GetValueOrDefault(w)).ToList());
            return (t, c, c > 0 ? t / c : double.NaN);
        }

        private static double Median(List<double> xs)
        {
            if (xs.Count == 0) return 0;
            xs.Sort();
            return xs[xs.Count / 2];
        }
    }

    /// <summary>
    /// Build a frequency-matched trained/control split from the corpus.
    ///
    /// Candidates are drawn from a mid-frequency band. Very frequent words ("the",
    /// "to") appear in nearly every sentence, so holding one out would mutilate the
    /// corpus rather than hold out a word; very rare words give the trained arm too
    /// little to learn from. Pairs are then formed by adjacent frequency rank and
    /// split alternately, which matches the two arms by construction rather than by
    /// hoping.
    /// </summary>
    public static Split Build(Corpus corpus, string dataset, int sentences, int pairs = 16,
                              int minCount = 5, int maxCount = 200)
    {
        var frequency = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var s in corpus.Sentences(dataset, sentences))
            foreach (var w in Corpus.Tokenize(s))
                frequency[w] = frequency.GetValueOrDefault(w) + 1;

        // Alphabetic only: punctuation-attached variants ("world." vs "world:") are
        // near-duplicates of each other and would let a control be the trained cue
        // in disguise.
        var candidates = frequency
            .Where(kv => kv.Value >= minCount && kv.Value <= maxCount)
            .Where(kv => kv.Key.All(char.IsLetter) && kv.Key.Length >= 3)
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.Ordinal)   // deterministic ties
            .Select(kv => kv.Key)
            .ToList();

        var trained = new List<string>();
        var controls = new List<string>();

        // Adjacent ranks alternate between the arms, so the two lists interleave
        // through the frequency band instead of one taking the head of it.
        for (int i = 0; i + 1 < candidates.Count && trained.Count < pairs; i += 2)
        {
            trained.Add(candidates[i]);
            controls.Add(candidates[i + 1]);
        }

        return new Split(trained, controls, frequency);
    }
}
