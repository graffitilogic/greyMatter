using System.Diagnostics;
using GreyMatter.Poc.Encoding;
using GreyMatter.Poc.Engrams;
using GreyMatter.Poc.Pipeline;
using GreyMatter.Poc.Runtime;

namespace GreyMatter.Poc.Eval;

/// <summary>
/// plan.md P6 — the Prompt.md success experiment.
///
/// Sweeps <c>BaselineNeuronCount</c> × <c>ActivationDepth</c> × <c>ActivationWidth</c>
/// on a fixed corpus and seed set, and reports per cell: recall lift over an
/// identically-configured untrained brain, an order diagnostic, ms/sentence, bytes
/// on disk, and memory.
///
/// The point is the CURVE, not any single cell: where does recall start costing
/// something as the virtual space grows past what the working set can hold? That
/// trade is the thesis, and §4.4 makes it explicit — beyond `WorkingSetMax` the
/// cascade truncates, and truncation is counted here rather than hidden.
/// </summary>
public static class ScaleSweep
{
    public sealed record Cell(
        int Neurons, int Depth, int Width,
        double SystemAuc, double UntrainedAuc, double Lift, double LiftLo, double LiftHi,
        bool Separated, double GradedRho, double OrderRPmi, double OrderSupport,
        double MsPerSentence, long BytesOnDisk, long ManagedHeapBytes, long ProcessRssBytes,
        int WorkingSetHighWater, long Truncations, long Synapses, int Recipes);

    public static List<Cell> Run(Config cfg, Args args)
    {
        int repeats = args.Int("--repeats", 5);
        int trainSentences = args.Int("--train", 500);
        int orderRepeats = args.Int("--order-repeats", 3);
        int orderTrain = args.Int("--order-train", 1000);
        var root = args.Value("--sweep-path", Path.Combine(Path.GetTempPath(), "gm_sweep"));

        // Pruned grid, 14 cells: a full scale × depth plane at the default width,
        // plus two width probes at the reference scale. §5 asks for ≥12.
        var neuronCounts = new[] { 10_000, 100_000, 1_000_000, 10_000_000 };
        var depths = new[] { 2, 4, 8 };
        var cells = new List<(int n, int d, int w)>();
        foreach (var n in neuronCounts)
            foreach (var d in depths)
                cells.Add((n, d, 256));
        cells.Add((1_000_000, 4, 64));
        cells.Add((1_000_000, 4, 1024));

        Console.WriteLine("🔬 SCALE SWEEP — plan.md P6 / Prompt.md success criterion");
        Console.WriteLine("=========================================================\n");
        Console.WriteLine($"cells: {cells.Count}   recall repeats: {repeats} @ {trainSentences} sentences");
        Console.WriteLine($"order: {orderRepeats} repeats @ {orderTrain} sentences (DIAGNOSTIC — rule 1 needs 5)");
        Console.WriteLine($"corpus: {cfg.Dataset}   seed base: {cfg.Seed}   working set: {cfg.WorkingSetMax:N0}\n");

        var results = new List<Cell>();
        var sw = Stopwatch.StartNew();

        for (int i = 0; i < cells.Count; i++)
        {
            var (n, d, w) = cells[i];
            Console.WriteLine($"─── cell {i + 1}/{cells.Count}: neurons={n:N0} depth={d} width={w} " +
                              $"[{sw.Elapsed.TotalMinutes:F1}m elapsed] ───");

            var cellPath = Path.Combine(root, $"n{n}_d{d}_w{w}");
            if (Directory.Exists(cellPath)) Directory.Delete(cellPath, recursive: true);

            var cellCfg = With(cfg, n, d, w, cellPath);
            results.Add(RunCell(cellCfg, repeats, trainSentences, orderRepeats, orderTrain));

            var r = results[^1];
            Console.WriteLine($"    lift {r.Lift:+0.000;-0.000}  ρ {r.GradedRho:+0.00;-0.00}  " +
                              $"R_PMI {r.OrderRPmi:+0.000;-0.000}  {r.MsPerSentence:F1} ms/sent  " +
                              $"{r.BytesOnDisk / 1024.0 / 1024.0:F1} MB  trunc {r.Truncations:N0}");

            // Scratch brains must not accumulate: rule 7, an experiment must not
            // mutate what it measures, and 14 cells of store would fill the disk.
            if (Directory.Exists(cellPath)) Directory.Delete(cellPath, recursive: true);
        }

        Report(results, cfg, args, sw.Elapsed);
        return results;
    }

    private static Cell RunCell(Config cfg, int repeats, int trainSentences, int orderRepeats, int orderTrain)
    {
        var corpus = new Corpus(cfg.TrainingDataRoot);
        var split = ControlSets.Build(corpus, cfg.Dataset, trainSentences, pairs: 16);

        var systemAucs = new List<double>();
        var untrainedAucs = new List<double>();
        var lifts = new List<double>();
        var rhos = new List<double>();

        double msPerSentence = 0;
        int highWater = 0, recipes = 0;
        long truncations = 0, synapses = 0, bytes = 0;

        for (int r = 0; r < repeats; r++)
        {
            var runCfg = With(cfg, cfg.BaselineNeuronCount, cfg.ActivationDepth, cfg.ActivationWidth,
                              cfg.BrainDataPath);
            runCfg.Seed = cfg.Seed + r;

            var encoder = new ContextEncoder(runCfg);
            Trainer.AccumulateContext(encoder, corpus.Sentences(runCfg.Dataset, trainSentences), split.HeldOut);

            using var trained = new ActivationScope(runCfg);
            var trainer = new Trainer(runCfg, trained, encoder) { HeldOut = split.HeldOut };
            var stats = trainer.Run(corpus.Sentences(runCfg.Dataset, trainSentences), quiet: true);

            msPerSentence = 1000.0 * stats.Seconds / Math.Max(1, stats.Sentences);
            highWater = Math.Max(highWater, stats.WorkingSetHighWater);
            truncations += stats.Truncations;
            synapses = stats.Synapses;

            var sys = Score(runCfg, trained, encoder, split);

            using var untrained = new ActivationScope(runCfg);
            var unt = Score(runCfg, untrained, encoder, split);

            systemAucs.Add(sys.auc);
            untrainedAucs.Add(unt.auc);
            lifts.Add(sys.auc - unt.auc);
            rhos.Add(sys.rho);

            // Persist once, from the last repeat, to measure bytes on disk.
            if (r == repeats - 1)
            {
                trained.ConsolidateAll();
                Trainer.Persist(runCfg, trained, new LshIndex(runCfg.Seed));
                var store = new EngramStore(runCfg.BrainDataPath);
                bytes = store.TotalBytes();
                recipes = store.Totals().recipes;
            }
        }

        var (orderRPmi, orderSupport) = OrderDiagnostic(cfg, corpus, orderRepeats, orderTrain);

        var sysAgg = Harness.Aggregate(systemAucs);
        var untAgg = Harness.Aggregate(untrainedAucs);
        var liftAgg = Harness.Aggregate(lifts);

        // Managed heap after a forced collection: a per-cell figure, unlike process
        // working set, which is cumulative across a 14-cell run in one process.
        var heap = GC.GetTotalMemory(forceFullCollection: true);
        Process.GetCurrentProcess().Refresh();
        var rss = Process.GetCurrentProcess().WorkingSet64;

        return new Cell(cfg.BaselineNeuronCount, cfg.ActivationDepth, cfg.ActivationWidth,
                        sysAgg.mean, untAgg.mean, liftAgg.mean, liftAgg.lo, liftAgg.hi,
                        Verdicts.RangesSeparated(sysAgg, untAgg), Harness.Mean(rhos),
                        orderRPmi, orderSupport, msPerSentence, bytes, heap, rss,
                        highWater, truncations, synapses, recipes);
    }

    private static (double auc, double rho) Score(Config cfg, ActivationScope scope,
                                                  ContextEncoder encoder, ControlSets.Split split)
    {
        var cascade = new Cascade(cfg, scope);
        var trained = split.Trained.Select(w => (double)cascade.Run(encoder.Encode(w), false).TotalMass).ToList();
        var controls = split.Controls.Select(w => (double)cascade.Run(encoder.Encode(w), false).TotalMass).ToList();
        var freqs = split.Trained.Select(w => (double)split.Frequency.GetValueOrDefault(w)).ToList();
        return (Harness.Auc(trained, controls), Harness.Spearman(trained, freqs));
    }

    /// <summary>
    /// Order as a DIAGNOSTIC NUMBER, not a verdict. Rule 1 forbids a verdict under
    /// 5 repeats, and running the full protocol at every cell would dominate the
    /// sweep's runtime. The 5-repeat verdict is established once, in P5.5.
    /// </summary>
    private static (double rPmi, double support) OrderDiagnostic(Config cfg, Corpus corpus,
                                                                 int repeats, int trainSentences)
    {
        var sentences = corpus.Sentences(cfg.Dataset, trainSentences).ToList();
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

        var successors = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var (a, b) in bigram.Keys)
        {
            if (!successors.TryGetValue(a, out var l)) successors[a] = l = new List<string>();
            l.Add(b);
        }

        var cues = unigram.Where(kv => successors.TryGetValue(kv.Key, out var s) && s.Count >= 8)
                          .OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal)
                          .Take(12).Select(kv => kv.Key).ToList();
        if (cues.Count == 0) return (0, 0);

        var rPmis = new List<double>();
        int supported = 0, pairs = 0;
        long totalTokens = unigram.Values.Sum();

        for (int rep = 0; rep < repeats; rep++)
        {
            var runCfg = With(cfg, cfg.BaselineNeuronCount, cfg.ActivationDepth, cfg.ActivationWidth,
                              cfg.BrainDataPath);
            runCfg.Seed = cfg.Seed + rep;

            var encoder = new ContextEncoder(runCfg);
            Trainer.AccumulateContext(encoder, sentences);
            using var scope = new ActivationScope(runCfg);
            new Trainer(runCfg, scope, encoder).Run(sentences, quiet: true);
            var cascade = new Cascade(runCfg, scope);

            foreach (var cue in cues)
            {
                var targets = successors[cue].Distinct().OrderBy(x => x, StringComparer.Ordinal).ToList();
                if (targets.Count < 3) continue;

                var readout = cascade.Run(encoder.Encode(cue), false);
                var winners = cascade.Winners(readout.WinnerCount);
                var scores = cascade.WinnerScores(readout.WinnerCount);
                var active = new Dictionary<uint, float>();
                for (int i = 0; i < winners.Length; i++) active[scope.Pool.VirtualId[winners[i]]] = scores[i];

                var mass = new List<double>();
                var pmis = new List<double>();
                foreach (var t in targets)
                {
                    double m = 0;
                    foreach (var vid in Assembly.Members(encoder.Encode(t), runCfg.BaselineNeuronCount, runCfg.AssemblyOverlap))
                        if (active.TryGetValue(vid, out var v)) m += v;
                    mass.Add(m);

                    int bc = bigram.GetValueOrDefault((cue, t));
                    if (rep == 0) { pairs++; if (bc > 1) supported++; }

                    double pAB = (double)bc / Math.Max(1, totalTokens);
                    double pA = (double)unigram.GetValueOrDefault(cue) / Math.Max(1, totalTokens);
                    double pB = (double)unigram.GetValueOrDefault(t) / Math.Max(1, totalTokens);
                    pmis.Add(pAB <= 0 || pA <= 0 || pB <= 0 ? 0 : Math.Log(pAB / (pA * pB)));
                }
                rPmis.Add(Harness.Spearman(mass, pmis));
            }
        }

        return (Harness.Mean(rPmis), pairs == 0 ? 0 : (double)supported / pairs);
    }

    private static void Report(List<Cell> cells, Config cfg, Args args, TimeSpan elapsed)
    {
        Console.WriteLine($"\n\n── SCALE SWEEP TABLE ({elapsed.TotalMinutes:F1} minutes) ──\n");
        Console.WriteLine("| neurons | depth | width | sys AUC | untr AUC | lift [min..max] | sep | ρ(freq) | R_PMI | ms/sent | MB disk | heap MB | WS high | trunc |");
        Console.WriteLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (var c in cells)
            Console.WriteLine($"| {c.Neurons:N0} | {c.Depth} | {c.Width} | {c.SystemAuc:F3} | {c.UntrainedAuc:F3} | " +
                              $"{c.Lift:+0.000;-0.000} [{c.LiftLo:+0.000;-0.000}..{c.LiftHi:+0.000;-0.000}] | " +
                              $"{(c.Separated ? "yes" : "no")} | {c.GradedRho:+0.00;-0.00} | {c.OrderRPmi:+0.000;-0.000} | " +
                              $"{c.MsPerSentence:F1} | {c.BytesOnDisk / 1048576.0:F1} | {c.ManagedHeapBytes / 1048576.0:F0} | " +
                              $"{c.WorkingSetHighWater:N0} | {c.Truncations:N0} |");

        // PeakWorkingSet64 returns 0 on macOS/Unix, so peak RSS is sampled per cell
        // and maxed here rather than read from the OS counter.
        Console.WriteLine($"\nPEAK_PROCESS_RSS: {cells.Max(c => c.ProcessRssBytes) / 1048576.0:F0} MB " +
                          "(max of per-cell samples; the OS peak counter is unavailable on this platform)");
        Console.WriteLine($"ORDER_SUPPORT: {Harness.Mean(cells.Select(c => c.OrderSupport).ToList()):P1} " +
                          "(mean; R_PMI is diagnostic only — rule 1 needs 5 repeats for a verdict)");

        bool measurableEverywhere = cells.All(c => c.Separated);
        bool reachedScale = cells.Any(c => c.Neurons >= 1_000_000);
        Console.WriteLine($"\nGATE_RECALL_AT_EVERY_SCALE: {(measurableEverywhere ? "PASS" : "FAIL")}");
        Console.WriteLine($"GATE_BEYOND_HUNDREDS_WIDE:  {(reachedScale ? "PASS" : "FAIL")} " +
                          $"(max {cells.Max(c => c.Neurons):N0} virtual neurons, " +
                          $"max depth {cells.Max(c => c.Depth)}, max width {cells.Max(c => c.Width)})");
        Console.WriteLine($"\nCOMMAND: {args.CommandLine}");
    }

    private static Config With(Config c, int neurons, int depth, int width, string path)
    {
        var cfg = c.Clone();
        cfg.BaselineNeuronCount = neurons;
        cfg.ActivationDepth = depth;
        cfg.ActivationWidth = width;
        cfg.BrainDataPath = path;
        return cfg;
    }
}
