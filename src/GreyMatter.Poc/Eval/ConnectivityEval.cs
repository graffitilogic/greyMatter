using GreyMatter.Poc.Encoding;
using GreyMatter.Poc.Pipeline;
using GreyMatter.Poc.Runtime;

namespace GreyMatter.Poc.Eval;

/// <summary>
/// P8a mechanism check, registered in RESULTS P8a.0.
///
/// P7.2.8 measured that `to → be` has 7,164 synapses across 256 fully-resident
/// assembly members and ZERO edges into `be`'s assembly, while `it → is` has 1,490.
/// Whether two words have a direct synaptic path is close to a lottery, and Hebbian
/// learning cannot encode an association across neuron sets that never form an edge.
///
/// This reports that quantity directly: over frequent bigrams, what fraction of
/// (cue, successor) pairs have at least one direct edge, and how many. It has to
/// move before any order result can be credited to shared substrate rather than to
/// chance.
///
/// The null is the same measurement on **frequency-matched non-co-occurring pairs**
/// (rule 2 / A-R1). Raising connectivity for everything is assembly merging — the
/// P4.3 defect-4 failure mode — not association. Only the gap means anything.
/// </summary>
public static class ConnectivityEval
{
    public static void Run(Config cfg, Args args)
    {
        int train = args.Int("--train", 2000);
        int pairCount = args.Int("--pairs", 40);
        var corpus = new Corpus(cfg.TrainingDataRoot, args.Has("--local-sample"));

        Console.WriteLine("🔬 P8a CONNECTIVITY — do co-occurring words have direct synaptic paths?");
        Console.WriteLine("=======================================================================\n");
        Console.WriteLine($"source: {corpus.Describe(cfg.Dataset)}");
        Console.WriteLine($"train: {train:N0}   AssemblyOverlap: {cfg.AssemblyOverlap}   " +
                          $"seed {cfg.Seed}\n");

        var sentences = corpus.Sentences(cfg.Dataset, train).ToList();

        var bigram = new Dictionary<(string, string), int>();
        var unigram = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var s in sentences)
        {
            var w = Corpus.Tokenize(s);
            for (int i = 0; i < w.Count; i++)
            {
                unigram[w[i]] = unigram.GetValueOrDefault(w[i]) + 1;
                if (i + 1 < w.Count) { var k = (w[i], w[i + 1]); bigram[k] = bigram.GetValueOrDefault(k) + 1; }
            }
        }

        var cooccurring = bigram.Where(kv => kv.Value >= 3)
                                .OrderByDescending(kv => kv.Value)
                                .ThenBy(kv => kv.Key.Item1 + " " + kv.Key.Item2, StringComparer.Ordinal)
                                .Take(pairCount).Select(kv => kv.Key).ToList();

        // Null: pairs drawn from the same words, frequency-matched by construction
        // (both endpoints are cue-set and successor-set members), that never occur
        // adjacently in the corpus.
        var cues = cooccurring.Select(p => p.Item1).Distinct().ToList();
        var targets = cooccurring.Select(p => p.Item2).Distinct().ToList();
        var control = new List<(string, string)>();
        foreach (var c in cues)
        {
            foreach (var t in targets)
            {
                if (bigram.ContainsKey((c, t)) || c == t) continue;
                control.Add((c, t));
                if (control.Count >= pairCount) break;
            }
            if (control.Count >= pairCount) break;
        }

        var encoder = new ContextEncoder(cfg);
        Trainer.AccumulateContext(encoder, sentences);
        using var scope = new ActivationScope(cfg);
        new Trainer(cfg, scope, encoder).Run(sentences, quiet: true);
        scope.ConsolidateAll();

        var co = Measure(cfg, scope, encoder, cooccurring);
        var nul = Measure(cfg, scope, encoder, control);

        // P8c mechanism check: does edge mass track the TARGET'S OWN FREQUENCY?
        // Hebbian accumulates in proportion to count(s,t), so a frequent target gains
        // weight from every cue it appears with. That is the measured reason cascade
        // mass ranks by frequency, and base-rate depression must drive it toward 0
        // while leaving the co-occurrence correlation intact.
        var massByPair = cooccurring.Select(pr => PairMass(cfg, scope, encoder, pr)).ToList();
        var targetFreq = cooccurring.Select(pr => (double)unigram.GetValueOrDefault(pr.Item2)).ToList();
        var pairFreq = cooccurring.Select(pr => (double)bigram[pr]).ToList();
        double rFreq = Harness.Spearman(massByPair, targetFreq);
        double rCooc = Harness.Spearman(massByPair, pairFreq);

        Console.WriteLine("| pair set | pairs | with ≥1 edge | connectivity | median edges | mean mass |");
        Console.WriteLine("|---|---|---|---|---|---|");
        Console.WriteLine($"| co-occurring | {co.n} | {co.connected} | **{co.Connectivity:P1}** | {co.MedianEdges} | {co.MeanMass:F2} |");
        Console.WriteLine($"| non-co-occurring (null) | {nul.n} | {nul.connected} | {nul.Connectivity:P1} | {nul.MedianEdges} | {nul.MeanMass:F2} |");

        Console.WriteLine($"\nCONNECTIVITY_COOCCUR: {co.Connectivity:P1}");
        Console.WriteLine($"CONNECTIVITY_NULL:    {nul.Connectivity:P1}");
        Console.WriteLine($"CONNECTIVITY_GAP:     {co.Connectivity - nul.Connectivity:+0.000;-0.000}   " +
                          "(> 0 means paths track co-occurrence rather than merely existing)");
        Console.WriteLine($"MEAN_MASS_RATIO:      {(nul.MeanMass > 1e-9 ? co.MeanMass / nul.MeanMass : double.NaN):F2}   " +
                          "(co-occurring ÷ null edge mass)");

        Console.WriteLine($"\nWEIGHT_VS_TARGETFREQ: {rFreq:+0.000;-0.000}   " +
                          "(Spearman(edge mass, target unigram count) — the defect; must fall toward 0)");
        Console.WriteLine($"WEIGHT_VS_COOCCUR:    {rCooc:+0.000;-0.000}   " +
                          "(Spearman(edge mass, bigram count) — the signal; must survive)");
        Console.WriteLine($"SPECIFICITY:          {rCooc - rFreq:+0.000;-0.000}   (co-occurrence − frequency)");
        Console.WriteLine($"BASE_RATE_SUPPRESSED: {scope.Synapses.BaseRateSuppressed:F1} weight units");

        Console.WriteLine($"\nAssembly member overlap actually realised:");
        var sample = cooccurring.Take(4).ToList();
        foreach (var (a, b) in sample)
        {
            var ma = Assembly.Members(encoder.Encode(a), cfg.BaselineNeuronCount, cfg.AssemblyOverlap).ToHashSet();
            var mb = Assembly.Members(encoder.Encode(b), cfg.BaselineNeuronCount, cfg.AssemblyOverlap);
            int shared = mb.Count(ma.Contains);
            int dimShared = encoder.Encode(a).Overlap(encoder.Encode(b));
            Console.WriteLine($"   {a,-8} ~ {b,-8} shared members {shared,3}/{mb.Length}   " +
                              $"shared dims {dimShared,2}/{cfg.Sparsity}");
        }

        Console.WriteLine($"\nCOMMAND: {args.CommandLine}");
    }

    private static double PairMass(Config cfg, ActivationScope scope, ContextEncoder encoder,
                                   (string, string) pair)
    {
        var from = Assembly.Members(encoder.Encode(pair.Item1), cfg.BaselineNeuronCount, cfg.AssemblyOverlap);
        var to = Assembly.Members(encoder.Encode(pair.Item2), cfg.BaselineNeuronCount, cfg.AssemblyOverlap).ToHashSet();
        double mass = 0;
        foreach (var vid in from)
        {
            if (!scope.Recipes.TryGetValue(vid, out var r)) continue;
            for (int i = 0; i < r.SynapseTargets.Length; i++)
                if (to.Contains(r.SynapseTargets[i])) mass += r.SynapseWeights[i];
        }
        return mass;
    }

    private sealed record Stats(int n, int connected, int MedianEdges, double MeanMass)
    {
        public double Connectivity => n > 0 ? (double)connected / n : 0;
    }

    private static Stats Measure(Config cfg, ActivationScope scope, ContextEncoder encoder,
                                 List<(string, string)> pairs)
    {
        var edgeCounts = new List<int>();
        var masses = new List<double>();
        int connected = 0;

        foreach (var (a, b) in pairs)
        {
            var from = Assembly.Members(encoder.Encode(a), cfg.BaselineNeuronCount, cfg.AssemblyOverlap);
            var to = Assembly.Members(encoder.Encode(b), cfg.BaselineNeuronCount, cfg.AssemblyOverlap).ToHashSet();

            int edges = 0;
            double mass = 0;
            foreach (var vid in from)
            {
                if (!scope.Recipes.TryGetValue(vid, out var recipe)) continue;
                for (int i = 0; i < recipe.SynapseTargets.Length; i++)
                    if (to.Contains(recipe.SynapseTargets[i])) { edges++; mass += recipe.SynapseWeights[i]; }
            }

            if (edges > 0) connected++;
            edgeCounts.Add(edges);
            masses.Add(mass);
        }

        edgeCounts.Sort();
        int median = edgeCounts.Count > 0 ? edgeCounts[edgeCounts.Count / 2] : 0;
        return new Stats(pairs.Count, connected, median, Harness.Mean(masses));
    }
}
