using GreyMatter.Poc.Eval;

namespace GreyMatter.Poc;

/// <summary>
/// plan.md §4.6 — the single entry point. One binary, no shell scripts.
/// Commands not yet implemented report the phase that delivers them rather than
/// silently doing nothing.
/// </summary>
public static class Cli
{
    public static int Main(string[] argv)
    {
        if (argv.Length == 0) { Usage(); return 1; }

        var args = new Args(argv);
        var cfg = Config.Load(args.Value("--config", null));
        cfg.ApplyOverrides(args);

        try
        {
            return argv[0] switch
            {
                "eval" => Eval(argv, args, cfg),
                "bench" => Bench(argv, args, cfg),
                "learn" => Learn(args, cfg),
                "probe" => Probe(args, cfg),
                "stats" => Stats(args, cfg),
                "audit" => Audit(args, cfg),
                "config" => Dump(cfg),
                "-h" or "--help" or "help" => Usage(),
                _ => Unknown(argv[0])
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"❌ {ex.GetType().Name}: {ex.Message}");
            return 1;
        }
    }

    private static int Eval(string[] argv, Args args, Config cfg)
    {
        if (argv.Length < 2) { Console.Error.WriteLine("usage: gm eval <encoder-ceiling|recall|order|scale>"); return 1; }

        switch (argv[1])
        {
            case "encoder-ceiling":
                // --stage surface reproduces the P0 baseline exactly; both adds the
                // P2 context measurement after it.
                var stage = args.Value("--stage", "surface");
                if (stage is not ("surface" or "context" or "both"))
                {
                    Console.Error.WriteLine($"unknown --stage '{stage}' (surface|context|both)");
                    return 1;
                }

                if (stage is "surface" or "both") EncoderCeiling.Run(cfg, args);

                int rc = 0;
                if (stage is "context" or "both")
                    rc = ContextCeiling.Run(cfg, args).GatePass ? 0 : 1;

                Console.WriteLine($"\nCOMMAND: {args.CommandLine}");
                Console.WriteLine($"SEED:    {cfg.Seed}");
                return rc;

            case "recall":
            {
                var result = RecallEval.Run(cfg, args);
                Console.WriteLine($"\nCOMMAND: {args.CommandLine}");
                return result.Refusal is null
                    && result.LiftVsUntrained.mean >= 0.05
                    && result.Separated ? 0 : 1;
            }
            case "order": return NotYet("gm eval order", "P5");
            case "scale": return NotYet("gm eval scale", "P6");
            default:
                Console.Error.WriteLine($"unknown eval '{argv[1]}'");
                return 1;
        }
    }

    /// <summary>
    /// P1 gate instrument. Named subcommand rather than an ad-hoc flag (rule 6),
    /// and specified by plan.md P1, so it is not a new experiment requiring
    /// registration.
    /// </summary>
    private static int Bench(string[] argv, Args args, Config cfg)
    {
        if (argv.Length < 2 || argv[1] is not ("substrate" or "store"))
        {
            Console.Error.WriteLine("usage: gm bench substrate [--cycles 10000] [--scope 2000]");
            Console.Error.WriteLine("       gm bench store [--recipes 100000] [--drift-scale 1.0]");
            return 1;
        }

        if (argv[1] == "store") return BenchStore(args, cfg);

        var cycles = args.Int("--cycles", 10_000);
        var scope = args.Int("--scope", 2_000);

        Console.WriteLine("⚙️  SUBSTRATE MICROBENCHMARK — plan.md P1 gate");
        Console.WriteLine("==============================================\n");
        Console.WriteLine($"virtual space:  {cfg.BaselineNeuronCount:N0} neurons");
        Console.WriteLine($"working set:    {cfg.WorkingSetMax:N0} max");
        Console.WriteLine($"scope/cycle:    {scope:N0}   depth {cfg.ActivationDepth}   width {cfg.ActivationWidth}");
        Console.WriteLine($"synapse cap:    {cfg.SynapseCapPerNeuron}/neuron   seed {cfg.Seed}\n");

        var r = Substrate.SubstrateBench.Run(cfg, cycles, scope);

        Console.WriteLine($"\n── Throughput ──");
        Console.WriteLine($"CYCLES_PER_SEC:  {r.CyclesPerSecond:F1}   ({r.Cycles:N0} cycles in {r.Seconds:F2}s)");
        Console.WriteLine($"MS_PER_CYCLE:    {1000.0 / r.CyclesPerSecond:F3}");
        Console.WriteLine($"GATE_THROUGHPUT: {(r.CyclesPerSecond >= 50 ? "PASS" : "FAIL")} (requires ≥ 50 cycles/sec)");

        Console.WriteLine($"\n── Garbage collection over the measured window ──");
        Console.WriteLine($"GC_GEN0: {r.Gen0}   GC_GEN1: {r.Gen1}   GC_GEN2: {r.Gen2}");
        Console.WriteLine($"ALLOCATED: {r.AllocatedBytes:N0} bytes total " +
                          $"({(double)r.AllocatedBytes / r.Cycles:F1} B/cycle)");
        Console.WriteLine($"GATE_GEN2: {(r.Gen2 == 0 ? "PASS" : "FAIL")} (requires zero gen2 collections)");

        Console.WriteLine($"\n── Substrate state ──");
        Console.WriteLine($"WORKING_SET_HIGH_WATER: {r.HighWaterMark:N0} / {cfg.WorkingSetMax:N0}");
        Console.WriteLine($"MATERIALIZED: {r.Materialized:N0}   EVICTED: {r.Evicted:N0}");
        Console.WriteLine($"SYNAPSES: {r.Synapses:N0}   created {r.Created:N0}   strengthened {r.Strengthened:N0}");
        Console.WriteLine($"COMPETITION: displaced {r.Displaced:N0}   declined {r.Declined:N0}");

        var pass = r.CyclesPerSecond >= 50 && r.Gen2 == 0 && r.HighWaterMark <= cfg.WorkingSetMax;
        Console.WriteLine($"\nP1_GATE: {(pass ? "PASS" : "FAIL")}");
        Console.WriteLine($"\nCOMMAND: {args.CommandLine}");
        return pass ? 0 : 1;
    }

    private static int Learn(Args args, Config cfg)
    {
        var sentences = args.Int("--sentences", 500);
        var corpus = new Pipeline.Corpus(cfg.TrainingDataRoot, args.Has("--local-sample"));

        Console.WriteLine("🧠 LEARN");
        Console.WriteLine("========\n");
        Console.WriteLine($"source:    {corpus.Describe(cfg.Dataset)}");
        Console.WriteLine($"sentences: {sentences:N0}   seed {cfg.Seed}");
        Console.WriteLine($"space:     {cfg.BaselineNeuronCount:N0} virtual, {cfg.WorkingSetMax:N0} resident");
        Console.WriteLine($"scope:     depth {cfg.ActivationDepth}, width {cfg.ActivationWidth}, " +
                          $"assembly {Runtime.Assembly.Size(cfg.Sparsity)}\n");

        var encoder = new Encoding.ContextEncoder(cfg);
        Console.WriteLine("   accumulating context…");
        Pipeline.Trainer.AccumulateContext(encoder, corpus.Sentences(cfg.Dataset, sentences));

        using var scope = new Runtime.ActivationScope(cfg);
        var trainer = new Pipeline.Trainer(cfg, scope, encoder);
        var stats = trainer.Run(corpus.Sentences(cfg.Dataset, sentences));

        var lsh = new Engrams.LshIndex(cfg.Seed);
        var partitions = Pipeline.Trainer.Persist(cfg, scope, lsh);

        Console.WriteLine($"\n── Learned ──");
        Console.WriteLine($"SENTENCES: {stats.Sentences:N0}   TOKENS: {stats.Tokens:N0}   " +
                          $"{stats.Sentences / Math.Max(1e-9, stats.Seconds):F0} sentences/sec");
        Console.WriteLine($"MS_PER_SENTENCE: {1000.0 * stats.Seconds / Math.Max(1, stats.Sentences):F2}");
        Console.WriteLine($"SYNAPSES: {stats.Synapses:N0}   " +
                          $"within-cue {stats.WithinCueUpdates:N0}   sequence {stats.SequenceUpdates:N0}");
        Console.WriteLine($"WORKING_SET_HIGH_WATER: {stats.WorkingSetHighWater:N0} / {cfg.WorkingSetMax:N0}");
        Console.WriteLine($"CASCADE_TRUNCATIONS: {stats.Truncations:N0}");
        Console.WriteLine($"CONSOLIDATIONS: {stats.Consolidations:N0}   " +
                          $"DEVIATIONS_WRITTEN: {stats.DeviationsWritten:N0}");
        Console.WriteLine($"PARTITIONS_WRITTEN: {partitions:N0} → {cfg.BrainDataPath}");
        Console.WriteLine($"\nCOMMAND: {args.CommandLine}");
        return 0;
    }

    private static int Probe(Args args, Config cfg)
    {
        var cue = args.Value("--cue", null);
        if (cue is null) { Console.Error.WriteLine("usage: gm probe --cue <word> [--topk 16]"); return 1; }

        var topk = args.Int("--topk", 16);
        var sentences = args.Int("--sentences", 500);
        var corpus = new Pipeline.Corpus(cfg.TrainingDataRoot, args.Has("--local-sample"));

        var encoder = new Encoding.ContextEncoder(cfg);
        Pipeline.Trainer.AccumulateContext(encoder, corpus.Sentences(cfg.Dataset, sentences));

        using var scope = new Runtime.ActivationScope(cfg);
        var trainer = new Pipeline.Trainer(cfg, scope, encoder);
        trainer.Run(corpus.Sentences(cfg.Dataset, sentences), quiet: true);

        var code = encoder.Encode(cue);
        var cascade = new Runtime.Cascade(cfg, scope);
        var readout = cascade.Run(code, learningMode: false);

        Console.WriteLine($"CUE:        {cue}");
        Console.WriteLine($"CODE_HASH:  {code.Hash():x16}   dims {code.K}");
        Console.WriteLine($"ASSEMBLY:   {readout.Materialized} materialized, {readout.Truncated} truncated");
        Console.WriteLine($"WINNERS:    {readout.WinnerCount}");
        Console.WriteLine($"TOTAL_MASS: {readout.TotalMass:F3}   mean {readout.MeanWinnerMass:F4}   " +
                          $"self {readout.SelfMass:F3}");

        Console.WriteLine($"\ntop {topk} winners (virtual id → activation):");
        var winners = cascade.Winners(readout.WinnerCount);
        var scores = cascade.WinnerScores(readout.WinnerCount);
        for (int i = 0; i < Math.Min(topk, readout.WinnerCount); i++)
            Console.WriteLine($"   {scope.Pool.VirtualId[winners[i]],10} → {scores[i]:F4}");

        Console.WriteLine($"\nCOMMAND: {args.CommandLine}");
        return 0;
    }

    /// <summary>P3 gate instrument: populate a store and measure it.</summary>
    private static int BenchStore(Args args, Config cfg)
    {
        var recipes = args.Int("--recipes", 100_000);
        var driftScale = args.Double("--drift-scale", 1.0);

        Console.WriteLine("⚙️  ENGRAM STORE BENCHMARK — plan.md P3 gate");
        Console.WriteLine("============================================\n");
        Console.WriteLine($"recipes:   {recipes:N0}");
        Console.WriteLine($"path:      {cfg.BrainDataPath}");
        Console.WriteLine($"codebook:  {cfg.VqCodebookSize} × {cfg.SurfaceDimensions}   " +
                          $"deviation threshold {cfg.DeviationThreshold}   seed {cfg.Seed}");
        Console.WriteLine($"drift scale:    {driftScale} weight units (threshold decides what persists)\n");

        var r = Engrams.StoreBench.Run(cfg, recipes, driftScale);

        Console.WriteLine($"\n── Storage ──");
        Console.WriteLine($"RECIPES:          {r.Recipes:N0}");
        Console.WriteLine($"PARTITIONS:       {r.Partitions:N0}");
        Console.WriteLine($"BYTES_TOTAL:      {r.Bytes:N0}");
        Console.WriteLine($"BYTES_PER_NEURON: {r.BytesPerNeuron:F1}");
        Console.WriteLine($"DEVIATIONS_PER_NEURON: {r.MeanDeviationsPerNeuron:F1} mean, {r.MaxDeviations} max " +
                          $"(of {cfg.SurfaceDimensions} dims)");
        Console.WriteLine($"CODEBOOK_UTILIZATION: {r.CodebookUtilization:P1}");
        Console.WriteLine($"GATE_SIZE: {(r.BytesPerNeuron <= 100 ? "PASS" : "FAIL")} (requires ≤ 100 B/neuron)");

        Console.WriteLine($"\n── Regeneration fidelity ──");
        Console.WriteLine($"WEIGHTS_CHECKED:  {r.FidelityChecked:N0}");
        Console.WriteLine($"VIOLATIONS:       {r.FidelityViolations:N0}");
        Console.WriteLine($"MAX_ABS_ERROR:    {r.MaxAbsError:F6} (threshold {cfg.DeviationThreshold})");
        Console.WriteLine($"GATE_FIDELITY: {(r.FidelityViolations == 0 ? "PASS" : "FAIL")} " +
                          "(requires 100% of weights within DeviationThreshold)");

        var audit = Engrams.StoreAudit.Scan(new Engrams.EngramStore(cfg.BrainDataPath), CorpusVocabulary(cfg));
        Console.WriteLine($"\n── Guardrail ──");
        Console.WriteLine($"STRING_TOKENS: {audit.StringTokens}   CORPUS_WORDS: {audit.CorpusWordHits}   " +
                          $"(letter runs {audit.LetterRuns:N0}, {audit.AscendingExcluded:N0} excluded as sorted dim arrays)");
        Console.WriteLine($"GATE_STRINGS: {(audit.Clean ? "PASS" : "FAIL")} " +
                          $"across {audit.FilesScanned} partitions, {audit.PayloadBytes:N0} payload bytes");
        foreach (var f in audit.Findings.Take(10))
            Console.WriteLine($"   ⚠️  {f.File} @{f.Offset} [{f.Kind}] \"{f.Text}\"");

        bool pass = r.BytesPerNeuron <= 100 && r.FidelityViolations == 0 && audit.Clean;
        Console.WriteLine($"\nP3_GATE: {(pass ? "PASS" : "FAIL")}");
        Console.WriteLine($"\nCOMMAND: {args.CommandLine}");
        return pass ? 0 : 1;
    }

    private static int Stats(Args args, Config cfg)
    {
        var store = new Engrams.EngramStore(cfg.BrainDataPath);
        var files = store.PartitionFiles().ToList();
        var bytes = store.TotalBytes();
        var recipes = files.Count == 0 ? 0 : store.TotalRecipes();

        Console.WriteLine($"BRAIN_DATA_PATH: {cfg.BrainDataPath}");
        Console.WriteLine($"PARTITIONS:      {files.Count:N0}");
        Console.WriteLine($"RECIPES:         {recipes:N0}");
        Console.WriteLine($"BYTES_TOTAL:     {bytes:N0}");
        Console.WriteLine($"BYTES_PER_NEURON: {(recipes > 0 ? (double)bytes / recipes : 0):F1}");
        return 0;
    }

    private static int Audit(Args args, Config cfg)
    {
        if (!args.Has("--strings"))
        {
            Console.Error.WriteLine("usage: gm audit --strings");
            return 1;
        }

        var report = Engrams.StoreAudit.Scan(new Engrams.EngramStore(cfg.BrainDataPath), CorpusVocabulary(cfg));
        Console.WriteLine($"AUDIT_PATH:    {cfg.BrainDataPath}");
        Console.WriteLine($"FILES_SCANNED: {report.FilesScanned:N0}");
        Console.WriteLine($"RAW_BYTES:     {report.RawBytes:N0}");
        Console.WriteLine($"PAYLOAD_BYTES: {report.PayloadBytes:N0} (decompressed — gzip is not a guardrail)");
        Console.WriteLine($"STRING_TOKENS: {report.StringTokens:N0} (exact: a record cannot hold a string without one)");
        Console.WriteLine($"CORPUS_WORDS:  {report.CorpusWordHits:N0} of {report.LetterRuns:N0} letter runs matched training vocabulary\n" +
                          $"               ({report.AscendingExcluded:N0} excluded as strictly-ascending — sorted dim arrays, not text)");

        foreach (var f in report.Findings.Take(25))
            Console.WriteLine($"   ⚠️  {f.File} @{f.Offset} [{f.Kind}] \"{f.Text}\"");

        Console.WriteLine($"\nAUDIT: {(report.Clean ? "CLEAN" : "FAIL — readable text found in the brain store")}");
        return report.Clean ? 0 : 1;
    }

    /// <summary>
    /// Training vocabulary for the audit's semantic check. Words of ≥4 letters
    /// only, matching the minimum run length — shorter ones would flag on
    /// coincidental integer aliasing rather than on stored text.
    /// </summary>
    private static IReadOnlySet<string> CorpusVocabulary(Config cfg, int sentences = 5000)
    {
        try
        {
            var corpus = new Pipeline.Corpus(cfg.TrainingDataRoot);
            return corpus.Sentences(cfg.Dataset, sentences)
                .SelectMany(s => s.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .Select(w => new string(w.Where(char.IsLetter).ToArray()))
                .Where(w => w.Length >= Engrams.StoreAudit.MinRunLength)
                .ToHashSet();
        }
        catch (Exception ex)
        {
            // A missing corpus must not silently downgrade the guardrail to the
            // token-only check without saying so.
            Console.Error.WriteLine($"⚠️  audit: corpus vocabulary unavailable ({ex.Message}); " +
                                    "running the exact string-token check only.");
            return new HashSet<string>();
        }
    }

    private static int Dump(Config cfg) { Console.WriteLine(cfg.ToJson()); return 0; }

    private static int NotYet(string what, string phase)
    {
        Console.Error.WriteLine($"{what} is not implemented yet — it lands in phase {phase} (plan.md §5).");
        return 2;
    }

    private static int Unknown(string cmd)
    {
        Console.Error.WriteLine($"unknown command '{cmd}'");
        Usage();
        return 1;
    }

    private static int Usage()
    {
        Console.WriteLine("""
            gm — greyMatter proof-of-concept

              gm learn  --dataset tatoeba_small --sentences 500 [--config f.json] [--resume]
              gm probe  --cue <word> [--topk 16]
              gm eval   encoder-ceiling [--train 500] [--vocab 3000]
              gm eval   recall | order | scale
              gm bench  substrate [--cycles 10000] [--scope 2000]
              gm stats
              gm audit  --strings
              gm config                      # print the effective configuration

            Common flags:
              --config <file.json>           load a Config; every field is also a --kebab-case flag
              --local-sample                 use the built-in corpus instead of the NAS
              --dataset, --seed, --brain-data-path, --training-data-root, ...
            """);
        return 0;
    }
}
