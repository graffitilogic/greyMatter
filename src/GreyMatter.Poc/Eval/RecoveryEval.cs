using GreyMatter.Poc.Encoding;
using GreyMatter.Poc.Runtime;

namespace GreyMatter.Poc.Eval;

/// <summary>Recovery instruments. Fixtures are correctness checks, never learning evidence.</summary>
public static class RecoveryEval
{
    public sealed record Check(string Name, bool Passed, string Evidence);

    public static int Run(Config cfg, Args args)
    {
        if (args.Value("--mode", "fixtures") == "storage") return RelayStorageEval.Run(args);
        if (args.Value("--mode", "fixtures") == "relay") return RelayEval.Run(args);
        if (args.Value("--mode", "fixtures") == "routes") return RouteReview.Run(args);
        if (args.Value("--mode", "fixtures") == "travel-time") return TravelTimeReview.Run(args);
        if (args.Value("--mode", "fixtures") == "travel") return TravelReview.Run(args);
        if (args.Value("--mode", "fixtures") == "learning") return RecoveryLearning.Run(cfg,args);
        if (args.Value("--mode", "fixtures") != "fixtures")
            throw new ArgumentException("Implemented recovery modes: fixtures, learning");
        var checks = Fixtures();
        foreach (var c in checks) Console.WriteLine($"{(c.Passed ? "PASS" : "FAIL")} {c.Name}: {c.Evidence}");
        Console.WriteLine($"COMMAND: {args.CommandLine}");
        return checks.All(c => c.Passed) ? 0 : 1;
    }

    public static IReadOnlyList<Check> Fixtures()
    {
        var checks = new List<Check>();
        var code = new SparseCode(new[] { 7 });
        var cfg = new Config { Sparsity = 1, WorkingSetMax = 64, ActivationWidth = 32,
            ActivationDepth = 1, PropagatedWinnerQuota = 0 };
        using var scope = new ActivationScope(cfg);
        var members = Assembly.Members(code, cfg.BaselineNeuronCount);
        foreach (var id in members) scope.Materialize(id);
        uint[] outside = Enumerable.Range(900001, 6).Select(i => (uint)i).Except(members).Take(4).ToArray();
        foreach (var id in outside) scope.Materialize(id);
        uint root = members[0], a = outside[0], b = outside[1], c = outside[2], distractor = outside[3];
        int Slot(uint id) => scope.Pool.Find(id);
        void Wire(uint from, params uint[] to) => scope.Synapses.Hydrate(Slot(from), to, to.Select(_ => 1f).ToArray());
        float Drive(Cascade run, uint id) => run.DeliveredDrive[Slot(id)];
        Wire(root, a); Wire(a, b); Wire(b, c);
        var cascade = new Cascade(cfg, scope);
        var before = scope.Pool.Familiarity.Take(scope.Pool.Count).ToArray();
        cascade.Run(code, false);
        checks.Add(new("one-hop-boundary", Drive(cascade,a) == 1 && Drive(cascade,b) == 0 && Drive(cascade,c) == 0,
            $"a={Drive(cascade,a)} b={Drive(cascade,b)} c={Drive(cascade,c)}"));
        checks.Add(new("recall-is-read-only", before.SequenceEqual(scope.Pool.Familiarity.Take(scope.Pool.Count)),
            "familiarity before/after query"));
        cfg.ActivationDepth = 2;
        cascade.Run(code, false);
        checks.Add(new("two-hop-boundary", Drive(cascade,b) == 1 && Drive(cascade,c) == 0,
            $"b={Drive(cascade,b)} c={Drive(cascade,c)}"));
        cfg.ActivationDepth = 3;
        cascade.Run(code, false);
        checks.Add(new("three-hop-and-distractor", Drive(cascade,c) == 1 && Drive(cascade,distractor) == 0,
            $"c={Drive(cascade,c)} distractor={Drive(cascade,distractor)}"));
        Wire(root); Wire(a,root); Wire(b); Wire(c);
        cfg.ActivationDepth = 1;
        cascade.Run(code,false);
        checks.Add(new("direction", Drive(cascade,a) == 0, "reverse-only edge must not activate a"));
        Wire(root,a,b); Wire(a); Wire(b);
        cascade.Run(code,false);
        checks.Add(new("branch-tie", Drive(cascade,a) == .5f && Drive(cascade,b) == .5f,
            $"a={Drive(cascade,a)} b={Drive(cascade,b)}"));
        Wire(root,a); Wire(a,root);
        cfg.ActivationDepth = 2;
        cascade.Run(code,false);
        var first = cascade.DeliveredDrive.ToArray();
        checks.Add(new("cycle-step-bound", Drive(cascade,root) == 1, $"returned={Drive(cascade,root)}"));
        cascade.Run(new SparseCode(new[] { 8 }),false);
        cascade.Run(code,false);
        checks.Add(new("query-reset", first.SequenceEqual(cascade.DeliveredDrive.ToArray()), "same drive after intervening cue"));
        return checks;
    }
}
