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
                "learn" => NotYet("gm learn", "P4"),
                "probe" => NotYet("gm probe", "P4"),
                "stats" => NotYet("gm stats", "P3"),
                "audit" => NotYet("gm audit", "P3"),
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

            case "recall": return NotYet("gm eval recall", "P4");
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
        if (argv.Length < 2 || argv[1] != "substrate")
        {
            Console.Error.WriteLine("usage: gm bench substrate [--cycles 10000] [--scope 2000]");
            return 1;
        }

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
