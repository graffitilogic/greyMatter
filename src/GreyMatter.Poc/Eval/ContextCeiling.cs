using GreyMatter.Poc.Encoding;
using GreyMatter.Poc.Pipeline;

namespace GreyMatter.Poc.Eval;

/// <summary>
/// plan.md P2 gate — the ceiling measured on the CONTEXT stage, and the
/// surface-vs-context comparison.
///
/// Deliberately a separate type from <see cref="EncoderCeiling"/> rather than a
/// generalisation of it. <c>EncoderCeiling.Run</c> reproduces the legacy harness
/// bit-for-bit and is the recorded P0 baseline; parameterising it to serve both
/// stages would put that reproduction at risk for no gain (rule 4: no
/// restructuring of a phase that has passed its gate). The modest duplication
/// buys a guarantee that the baseline cannot drift.
/// </summary>
public sealed class ContextCeilingResult
{
    public required double SurfaceAuc { get; init; }
    public required double ContextAuc { get; init; }
    public required double SurfaceDPrime { get; init; }
    public required double ContextDPrime { get; init; }
    public required int SentencesObserved { get; init; }
    public required int VocabSize { get; init; }
    public required int ContextVocab { get; init; }
    public required double CollisionPctK32 { get; init; }
    public required int SeparatedPairs { get; init; }
    public required int ConfusedPairsTested { get; init; }
    public required bool GatePass { get; init; }
}

public static class ContextCeiling
{
    public static ContextCeilingResult Run(Config cfg, Args args)
    {
        var accumulate = args.Int("--accumulate", 5000);
        var vocabCap = args.Int("--vocab", 3000);
        var corpus = new Corpus(cfg.TrainingDataRoot, args.Has("--local-sample"));
        var surface = new SurfaceEncoder(cfg.SurfaceDimensions);

        Console.WriteLine("\n\n🔬 CONTEXT CEILING — what the distributional stage adds");
        Console.WriteLine("========================================================\n");
        Console.WriteLine($"source: {corpus.Describe(cfg.Dataset)}");
        Console.WriteLine($"accumulate: {accumulate:N0} sentences   β={cfg.ContextBlend}   " +
                          $"n={cfg.PatternSize} k={cfg.Sparsity}   window=±{ContextEncoder.Window}\n");

        // ── Accumulation pass ───────────────────────────────────────────────
        var encoder = new ContextEncoder(cfg, surface);
        int seen = 0;
        foreach (var sentence in corpus.Sentences(cfg.Dataset, accumulate))
        {
            encoder.Observe(Corpus.Tokenize(sentence));
            if (++seen % 1000 == 0) Console.WriteLine($"   observed {seen:N0} sentences   " +
                                                      $"context vocabulary {encoder.VocabularyObserved:N0}");
        }
        Console.WriteLine($"   observed {seen:N0} sentences   context vocabulary {encoder.VocabularyObserved:N0}\n");

        // A β=0 twin, to isolate what context contributes from what merely changing
        // the code space contributes. Both run the identical measurement path, so
        // any difference between them is the context accumulation and nothing else.
        var nullCfg = Clone(cfg);
        nullCfg.ContextBlend = 0.0;
        var nullEncoder = new ContextEncoder(nullCfg, surface);

        // ── A. Trained-vs-control separation, both stages ───────────────────
        Console.WriteLine("── A. trained-vs-control separation ──");
        var (surfAuc, surfD) = Separation(nullEncoder);
        var (ctxAuc, ctxD) = Separation(encoder);

        Console.WriteLine($"   SURFACE_AUC (β=0): {surfAuc:F3}   d′ {surfD:F2}");
        Console.WriteLine($"   CONTEXT_AUC (β={cfg.ContextBlend}): {ctxAuc:F3}   d′ {ctxD:F2}");
        Console.WriteLine($"   CONTEXT_LIFT: {ctxAuc - surfAuc:+0.000;-0.000}");

        // ── B. Vocabulary and code capacity ─────────────────────────────────
        var vocab = corpus.Sentences(cfg.Dataset, accumulate)
            .SelectMany(s => s.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(w => w.Length > 1).Distinct().Take(vocabCap).ToList();

        var codes = vocab.Select(encoder.Encode).ToList();
        var distinct = codes.Select(c => c.Hash()).Distinct().Count();
        var collisionPct = 100.0 * (codes.Count - distinct) / Math.Max(1, codes.Count);

        Console.WriteLine($"\n── B. realised capacity of the blended k-of-n code ──");
        Console.WriteLine($"   vocabulary: {vocab.Count:N0} distinct words");
        Console.WriteLine($"   TOPK_COLLISIONS k={cfg.Sparsity}: {distinct:N0}/{codes.Count:N0} distinct " +
                          $"({collisionPct:F1}% collide)");

        // ── C. Does context separate pairs the surface stage confuses? ──────
        //
        // The plan's example pair (sleep/sleeps) is void: P0 measured it at
        // cos 0.143, already well separated. The pairs the surface stage ACTUALLY
        // confuses are the ones worth testing, so they are discovered from the
        // corpus rather than assumed.
        Console.WriteLine($"\n── C. pairs the surface stage confuses ──");
        var confused = FindConfusedPairs(vocab, surface, limit: 12);

        // Each stage is weighted by rarity measured on ITS OWN codes. Sharing one
        // table across stages would compare arms differing by two factors: the
        // blend AND the weighting (rule 8 — arms differ by exactly one factor).
        var ctxIdf = BuildRarity(cfg, codes);
        var nullIdf = BuildRarity(cfg, vocab.Select(nullEncoder.Encode).ToList());

        int separated = 0;
        var confusedDeltas = new List<double>();
        Console.WriteLine($"   {"pair",-30} {"surface",9} {"context",9} {"Δ",8}");
        foreach (var (a, b, _) in confused)
        {
            var ctxSim = encoder.Encode(a).WeightedSimilarity(encoder.Encode(b), ctxIdf);
            var nullSim = nullEncoder.Encode(a).WeightedSimilarity(nullEncoder.Encode(b), nullIdf);
            bool wins = ctxSim < nullSim - 0.02f;
            if (wins) separated++;
            confusedDeltas.Add(ctxSim - nullSim);
            Console.WriteLine($"   {a + " ~ " + b,-30} {nullSim,9:F3} {ctxSim,9:F3} " +
                              $"{ctxSim - nullSim,8:+0.000;-0.000} {(wins ? "✓" : "")}");
        }
        Console.WriteLine($"   (surface cosine of the most-confused pair: {confused[0].sim:F3})");
        Console.WriteLine($"   SEPARATED: {separated}/{confused.Count} confused pairs pushed apart by context");

        // ── C-control. Does context separate CONFUSED pairs specifically? ────
        //
        // "12/12 pairs pushed apart" is not evidence of discrimination if context
        // pushes EVERY pair apart — that is a stage adding noise, not signal. Rule
        // 2 requires the null be scored on comparable pairs, so the same Δ is
        // measured on random vocabulary pairs. Only the GAP between the two means
        // anything.
        var randomDeltas = new List<double>();
        var rnd = new Random(cfg.Seed);
        for (int i = 0; i < 200; i++)
        {
            var a = vocab[rnd.Next(vocab.Count)];
            var b = vocab[rnd.Next(vocab.Count)];
            if (a == b) continue;
            randomDeltas.Add(encoder.Encode(a).WeightedSimilarity(encoder.Encode(b), ctxIdf)
                           - nullEncoder.Encode(a).WeightedSimilarity(nullEncoder.Encode(b), nullIdf));
        }

        var confusedMean = Harness.Mean(confusedDeltas);
        var randomMean = Harness.Mean(randomDeltas);
        Console.WriteLine($"\n   DELTA_CONFUSED: {confusedMean:+0.000;-0.000} (mean over {confusedDeltas.Count} confused pairs)");
        Console.WriteLine($"   DELTA_RANDOM:   {randomMean:+0.000;-0.000} (mean over {randomDeltas.Count} random pairs)");
        Console.WriteLine($"   SELECTIVITY:    {confusedMean - randomMean:+0.000;-0.000} " +
                          "(confused − random; negative ⇒ context targets confusion specifically)");
        Console.WriteLine(confusedMean - randomMean < -0.02
            ? "   ✅ Context separates confused pairs MORE than it separates arbitrary pairs."
            : "   ⚠️  Context separates confused and arbitrary pairs alike. The 'separation' above\n" +
              "       is indiscriminate displacement, not discrimination — it carries no evidence\n" +
              "       that the stage encodes distributional structure.");

        // ── Gate ─────────────────────────────────────────────────────────────
        bool collisionOk = collisionPct == 0.0;
        bool separationOk = separated >= 1;
        bool pass = collisionOk && separationOk;

        Console.WriteLine($"\n── P2 gate ──");
        Console.WriteLine($"   GATE_COLLISIONS: {(collisionOk ? "PASS" : "FAIL")} " +
                          $"(k={cfg.Sparsity} collision {collisionPct:F1}%, requires 0.0%)");
        Console.WriteLine($"   GATE_SEPARATION: {(separationOk ? "PASS" : "FAIL")} " +
                          $"({separated} confused pair(s) separated, requires ≥ 1)");
        Console.WriteLine($"   P2_GATE: {(pass ? "PASS" : "FAIL")}");

        return new ContextCeilingResult
        {
            SurfaceAuc = surfAuc,
            ContextAuc = ctxAuc,
            SurfaceDPrime = surfD,
            ContextDPrime = ctxD,
            SentencesObserved = seen,
            VocabSize = vocab.Count,
            ContextVocab = encoder.VocabularyObserved,
            CollisionPctK32 = collisionPct,
            SeparatedPairs = separated,
            ConfusedPairsTested = confused.Count,
            GatePass = pass
        };
    }

    /// <summary>
    /// Trained-vs-control AUC on the same statistic as the surface ceiling:
    /// max similarity to any OTHER trained cue, self excluded.
    /// </summary>
    private static (double auc, double dPrime) Separation(ContextEncoder enc)
    {
        var trained = CueSets.Trained.ToDictionary(w => w, enc.Encode);
        var controls = CueSets.AllControls();

        double MaxToTrained(string w, SparseCode v)
        {
            double best = -1;
            foreach (var kv in trained)
            {
                if (kv.Key.Equals(w, StringComparison.OrdinalIgnoreCase)) continue;
                var s = v.Similarity(kv.Value);
                if (s > best) best = s;
            }
            return best;
        }

        var t = CueSets.Trained.Select(w => MaxToTrained(w, trained[w])).ToList();
        var c = controls.Select(w => MaxToTrained(w, enc.Encode(w))).ToList();
        return (Harness.Auc(t, c), Harness.DPrime(t, c));
    }

    /// <summary>
    /// The vocabulary pairs the surface stage confuses most, measured rather than
    /// assumed. Returns descending by surface cosine.
    /// </summary>
    private static List<(string a, string b, double sim)> FindConfusedPairs(
        List<string> vocab, SurfaceEncoder surface, int limit)
    {
        var vecs = vocab.Select(surface.Encode).ToList();
        var pairs = new List<(string, string, double)>();
        for (int i = 0; i < vecs.Count; i++)
            for (int j = i + 1; j < vecs.Count; j++)
            {
                var c = Harness.Cosine(vecs[i], vecs[j]);
                if (c > 0.93) pairs.Add((vocab[i], vocab[j], c));
            }
        return pairs.OrderByDescending(p => p.Item3).Take(limit).ToList();
    }

    private static float[] BuildRarity(Config cfg, List<SparseCode> codes)
    {
        var table = new RarityTable(cfg.PatternSize);
        foreach (var c in codes) table.Observe(c);
        return table.Idf();
    }

    private static Config Clone(Config c) => c.Clone();
}
