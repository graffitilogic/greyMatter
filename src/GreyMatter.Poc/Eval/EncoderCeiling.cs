using GreyMatter.Poc.Encoding;
using GreyMatter.Poc.Pipeline;

namespace GreyMatter.Poc.Eval;

/// <summary>
/// plan.md P0 gate — what is separable BEFORE any learning happens?
///
/// Ported from legacy <c>RunEncoderCeiling</c>. The legacy finding this must
/// reproduce (§1.3): the trained-vs-control separation the fidelity harness
/// reported (AUC 0.94–1.00 over 40 runs) may be entirely the encoder's doing.
/// Whatever this prints becomes the floor every later recall number is reported
/// as lift over (rule 6).
///
/// Sections A–E are the legacy sections, kept in order and by name so the two
/// outputs can be diffed line for line.
/// </summary>
public sealed class CeilingResult
{
    public required string Stage { get; init; }
    public double Auc { get; init; }
    public double DPrime { get; init; }
    public bool Separable { get; init; }
    public double StrongestControl { get; init; }
    public double WeakestTrained { get; init; }
    public int VocabSize { get; init; }
    public double NnMedian { get; init; }
    public double NnP90 { get; init; }
    public double NnMax { get; init; }
    public Dictionary<int, double> TopKCollisionPct { get; init; } = new();
    public int OverlapMedian { get; init; }
    public int OverlapP90 { get; init; }
    public int OverlapMax { get; init; }
    public int DimsUsed { get; init; }
    public int DimsGeneric { get; init; }
    public int DimsDiscriminative { get; init; }
    public bool ControlsAtLeastAsHard { get; init; }
}

public static class EncoderCeiling
{
    public static CeilingResult Run(Config cfg, Args args)
    {
        var maxSentences = args.Int("--train", 500);
        var vocabCap = args.Int("--vocab", 3000);
        var encoder = new SurfaceEncoder(cfg.SurfaceDimensions);
        var corpus = new Corpus(cfg.TrainingDataRoot, args.Has("--local-sample"));

        Console.WriteLine("🔬 ENCODER CEILING — separability before any learning");
        Console.WriteLine("=====================================================\n");
        Console.WriteLine($"stage: surface   source: {corpus.Describe(cfg.Dataset)}   train: {maxSentences}\n");

        var trainedVecs = CueSets.Trained.ToDictionary(w => w, encoder.Encode);
        var controls = CueSets.AllControls();
        var controlVecs = controls.ToDictionary(w => w, encoder.Encode);

        // ── A. Can raw encoder distance tell a trained word from a control? ──
        //
        // Score = max cosine to any trained cue, self EXCLUDED — scoring a word
        // against itself returns 1.0 and manufactures separation out of nothing.
        Console.WriteLine("── A. max cosine to the trained set ──");
        double MaxToTrained(string w, double[] v) => trainedVecs
            .Where(kv => !kv.Key.Equals(w, StringComparison.OrdinalIgnoreCase))
            .Max(kv => Harness.Cosine(v, kv.Value));

        var trainedScores = new List<double>();
        foreach (var w in CueSets.Trained)
        {
            var s = MaxToTrained(w, trainedVecs[w]);
            trainedScores.Add(s);
            Console.WriteLine($"   {w,-12} {s:F3}  (nearest other trained word)");
        }

        Console.WriteLine();
        var controlScores = new List<double>();
        foreach (var w in controls)
        {
            var s = MaxToTrained(w, controlVecs[w]);
            controlScores.Add(s);
            var nearest = trainedVecs.OrderByDescending(kv => Harness.Cosine(controlVecs[w], kv.Value)).First().Key;
            Console.WriteLine($"   {w,-12} {s:F3}  [{CueSets.Tier(w)}] nearest trained = '{nearest}'");
        }

        var auc = Harness.Auc(trainedScores, controlScores);
        var dPrime = Harness.DPrime(trainedScores, controlScores);
        var separable = controlScores.Max() < trainedScores.Min();

        Console.WriteLine();
        Console.WriteLine($"CEILING_AUC:    {auc:F3}");
        Console.WriteLine($"CEILING_DPRIME: {dPrime:F2}");
        Console.WriteLine($"CEILING_GATE:   {(separable ? "SEPARABLE" : "OVERLAPPING")} " +
                          $"(strongest control {controlScores.Max():F3} vs weakest trained {trainedScores.Min():F3})");

        // ── B. Vocabulary-wide crowding ──
        Console.WriteLine("\n── B. vocabulary-wide nearest-neighbour similarity ──");
        var vocab = corpus.Sentences(cfg.Dataset, maxSentences)
            .SelectMany(s => s.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(w => w.Length > 1)
            .Distinct()
            .Take(vocabCap)
            .ToList();
        var vecs = vocab.Select(encoder.Encode).ToList();
        Console.WriteLine($"   vocabulary: {vocab.Count:N0} distinct words");

        var nn = new List<double>(vecs.Count);
        for (int i = 0; i < vecs.Count; i++)
        {
            double best = -1;
            for (int j = 0; j < vecs.Count; j++)
                if (i != j) { var c = Harness.Cosine(vecs[i], vecs[j]); if (c > best) best = c; }
            nn.Add(best);
        }
        nn.Sort();
        var nnMedian = nn[nn.Count / 2];
        Console.WriteLine($"   NN_SIM: median={nnMedian:F3} p90={Harness.Percentile(nn, 0.9):F3} max={nn[^1]:F3}");
        Console.WriteLine($"   pairs above 0.95: {nn.Count(x => x > 0.95)} | above 0.90: {nn.Count(x => x > 0.90)}");

        // ── C. Realised capacity of the top-k dimension code ──
        //
        // Theoretical capacity C(128,32) is astronomical; the question is whether
        // distinct words actually produce distinct top-k sets. This tests the
        // k-of-n proposal directly, using the encoder's own sparsification.
        Console.WriteLine("\n── C. realised capacity of the top-k dimension code ──");
        var collisions = new Dictionary<int, double>();
        foreach (var k in new[] { 4, 8, 16, 32 })
        {
            var codes = vecs.Select(v => string.Join(",", TopKDims(v, k))).ToList();
            var distinct = codes.Distinct().Count();
            var pct = codes.Count == 0 ? 0 : 100.0 * (codes.Count - distinct) / codes.Count;
            collisions[k] = pct;
            Console.WriteLine($"   TOPK_COLLISIONS k={k,2}: {distinct:N0}/{codes.Count:N0} distinct ({pct:F1}% collide)");
        }

        // ── D. Overlap distribution and per-dimension rarity ──
        //
        // Zero collisions at k=32 only says max overlap ≤ 31. An overlap of 31/32
        // still yields near-identical generated fields, so injectivity alone buys
        // no discrimination — the OVERLAP distribution does. The collision curve
        // above says the highest-magnitude dims are largely SHARED (length, vowel
        // ratio); discrimination lives in the low-magnitude tail. That is fatal
        // for magnitude-weighted receptive fields, and is why §4.2 weights dims by
        // rarity instead.
        int K = cfg.Sparsity;
        var sets = vecs.Select(v => TopKDims(v, K).ToHashSet()).ToList();

        var overlaps = new List<int>(sets.Count);
        for (int i = 0; i < sets.Count; i++)
        {
            int best = 0;
            for (int j = 0; j < sets.Count; j++)
                if (i != j) { var o = sets[i].Count(d => sets[j].Contains(d)); if (o > best) best = o; }
            overlaps.Add(best);
        }
        overlaps.Sort();
        var ovMedian = overlaps.Count > 0 ? overlaps[overlaps.Count / 2] : 0;
        var ovP90 = overlaps.Count > 0 ? overlaps[Math.Min(overlaps.Count - 1, (int)(overlaps.Count * 0.9))] : 0;
        var ovMax = overlaps.Count > 0 ? overlaps[^1] : 0;
        Console.WriteLine($"\n── D. top-{K} overlap with the nearest other word ──");
        Console.WriteLine($"   OVERLAP: median={ovMedian}/{K} p90={ovP90}/{K} max={ovMax}/{K}");
        Console.WriteLine($"   words with ≥30/{K} overlap: {overlaps.Count(o => o >= 30)} | ≥28: {overlaps.Count(o => o >= 28)}");

        var df = new int[cfg.SurfaceDimensions];
        foreach (var s in sets) foreach (var d in s) df[d]++;
        var used = Enumerable.Range(0, cfg.SurfaceDimensions).Where(d => df[d] > 0).ToList();
        var generic = used.Count(d => df[d] > sets.Count * 0.9);
        var rare = used.Count(d => df[d] < sets.Count * 0.1);
        Console.WriteLine($"   DIM_USAGE: {used.Count}/{cfg.SurfaceDimensions} dims used | " +
                          $"in >90% of words: {generic} (generic) | in <10%: {rare} (discriminative)");

        // ── E. Is the control set easier than the real vocabulary? ──
        //
        // If controls sit FURTHER from trained words than ordinary vocabulary
        // words sit from each other, the discrimination gate has been testing the
        // easy case and the real problem — morphological relatives like
        // sleep/sleeps/sleeping — is untested (rule 5).
        Console.WriteLine("\n── E. is the control set easier than the vocabulary? ──");
        var hardest = new List<(string a, string b, double sim)>();
        for (int i = 0; i < vecs.Count; i++)
            for (int j = i + 1; j < vecs.Count; j++)
            {
                var c = Harness.Cosine(vecs[i], vecs[j]);
                if (c > 0.93) hardest.Add((vocab[i], vocab[j], c));
            }
        foreach (var (a, b, s) in hardest.OrderByDescending(x => x.sim).Take(8))
            Console.WriteLine($"   {a,-14} ~ {b,-14} {s:F3}");

        var controlsHardEnough = controlScores.Max() >= nnMedian;
        Console.WriteLine($"   CONTROL_DIFFICULTY: strongest control {controlScores.Max():F3} " +
                          $"vs vocabulary NN median {nnMedian:F3}");
        Console.WriteLine(controlsHardEnough
            ? "   ✅ Controls are at least as hard as typical vocabulary neighbours."
            : "   ⚠️  Controls sit FURTHER from trained words than typical vocabulary words sit\n" +
              "       from each other. The gate tests the easy case; real discrimination is harder.");

        Console.WriteLine("\n── Read this against the system's measured numbers ──");
        Console.WriteLine("   Legacy fidelity harness, 40 runs: AUC 0.94–1.00, d′ 1.76–2.09.");
        Console.WriteLine(auc >= 0.90
            ? "   ⚠️  CEILING_AUC is in the same band as the legacy system's measured AUC. The\n" +
              "       encoder alone accounts for the observed separation; no learning architecture\n" +
              "       can be credited with it until it beats THIS number."
            : "   ✅ CEILING_AUC is below the legacy measured AUC, leaving room for the\n" +
              "       architecture to add separation the encoder does not.");
        if (!separable)
            Console.WriteLine("   ⚠️  Controls overlap the trained range in RAW encoder space, before any\n" +
                              "       learning at all.");

        return new CeilingResult
        {
            Stage = "surface",
            Auc = auc,
            DPrime = dPrime,
            Separable = separable,
            StrongestControl = controlScores.Max(),
            WeakestTrained = trainedScores.Min(),
            VocabSize = vocab.Count,
            NnMedian = nnMedian,
            NnP90 = Harness.Percentile(nn, 0.9),
            NnMax = nn.Count > 0 ? nn[^1] : 0,
            TopKCollisionPct = collisions,
            OverlapMedian = ovMedian,
            OverlapP90 = ovP90,
            OverlapMax = ovMax,
            DimsUsed = used.Count,
            DimsGeneric = generic,
            DimsDiscriminative = rare,
            ControlsAtLeastAsHard = controlsHardEnough
        };
    }

    /// Top-k dimensions by |magnitude|, index-ascending tie-break, returned sorted
    /// — the legacy sparsification, reproduced exactly so codes are comparable.
    internal static int[] TopKDims(double[] v, int k) =>
        Enumerable.Range(0, v.Length)
                  .OrderByDescending(i => Math.Abs(v[i]))
                  .ThenBy(i => i)
                  .Take(k)
                  .OrderBy(i => i)
                  .ToArray();
}
