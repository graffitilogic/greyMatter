using GreyMatter.Poc.Encoding;
using GreyMatter.Poc.Pipeline;
using GreyMatter.Poc.Runtime;

namespace GreyMatter.Poc.Eval;

/// <summary>
/// plan.md P4 gate — trained-vs-control discrimination, reported as architecture
/// lift (rule 6).
///
/// Two ceilings are reported, because the plan's literal one is the weaker of them:
///
///   • **Encoder ceiling** (the plan's metric): trained-vs-control AUC computed
///     from static sparse-code similarity alone. This is what §5 P4 names.
///   • **Untrained-brain ceiling** (added): the identical pipeline — same encoder,
///     same assemblies, same cascade, same readout — on a brain that has had NO
///     training. This is the stronger control, because it holds every factor
///     constant except learning. A system that beats the encoder ceiling but not
///     the untrained brain has learned nothing; it has only revealed that cascade
///     readout is a different statistic from code overlap.
///
/// Both are reported every run. The gate is judged on the untrained-brain lift,
/// with the encoder lift alongside it, and any disagreement between them is
/// itself the finding.
/// </summary>
public static class RecallEval
{
    public sealed record ArmResult(double Auc, double DPrime, double TrainedMean, double ControlMean)
    {
        /// <summary>trained − control mean truncations per cue; non-zero means a residency confound.</summary>
        public double TruncationGap { get; init; }

        /// <summary>Spearman(mass, corpus frequency) among trained cues only.</summary>
        public double GradedRho { get; init; }
    }

    public sealed record Result(
        int Repeats,
        (double mean, double lo, double hi) SystemAuc,
        (double mean, double lo, double hi) UntrainedAuc,
        (double mean, double lo, double hi) EncoderAuc,
        (double mean, double lo, double hi) LiftVsUntrained,
        (double mean, double lo, double hi) LiftVsEncoder,
        bool Separated, int WorkingSetHighWater, int WorkingSetMax,
        long Truncations, long Synapses, string? Refusal);

    public static Result Run(Config cfg, Args args)
    {
        int repeats = args.Int("--repeats", 5);
        int trainSentences = args.Int("--train", 500);
        int pairs = args.Int("--pairs", 16);

        var corpus = new Corpus(cfg.TrainingDataRoot, args.Has("--local-sample"));

        Console.WriteLine("🔬 RECALL — trained vs held-out in-vocabulary controls");
        Console.WriteLine("======================================================\n");
        Console.WriteLine($"source: {corpus.Describe(cfg.Dataset)}");
        Console.WriteLine($"train: {trainSentences} sentences   repeats: {repeats}   " +
                          $"seed base: {cfg.Seed}\n");

        var split = ControlSets.Build(corpus, cfg.Dataset, trainSentences, pairs);
        if (split.Trained.Count < 4)
        {
            Console.WriteLine($"⚠️  only {split.Trained.Count} frequency-matched pairs available at " +
                              $"--train {trainSentences}. Raise it.");
            return Empty(repeats, "INSUFFICIENT VOCABULARY");
        }

        var (tMed, cMed, ratio) = split.FrequencyMatch();
        Console.WriteLine($"── control set (rule 5: controls at least as hard as vocabulary neighbours) ──");
        Console.WriteLine($"   pairs: {split.Trained.Count}");
        Console.WriteLine($"   trained: {string.Join(", ", split.Trained.Take(8))}…");
        Console.WriteLine($"   control: {string.Join(", ", split.Controls.Take(8))}…");
        Console.WriteLine($"   FREQUENCY_MATCH: trained median {tMed:F0} vs control median {cMed:F0} " +
                          $"(ratio {ratio:F2}; 1.00 is perfect)");

        var systemAucs = new List<double>();
        var gradedRhos = new List<double>();
        var truncGaps = new List<double>();
        var untrainedAucs = new List<double>();
        var encoderAucs = new List<double>();
        var liftsUntrained = new List<double>();
        var liftsEncoder = new List<double>();

        int highWater = 0, workingMax = cfg.WorkingSetMax;
        long truncations = 0, synapses = 0;

        for (int r = 0; r < repeats; r++)
        {
            // Arms differ by exactly one factor (rule 8): the same seed drives the
            // trained and untrained brains, so their assemblies and regenerated
            // fields are identical and only learning differs.
            var runCfg = Clone(cfg);
            runCfg.Seed = cfg.Seed + r;

            Console.WriteLine($"\n═══ repeat {r + 1}/{repeats}  (seed {runCfg.Seed}) ═══");

            var encoder = new ContextEncoder(runCfg);
            Trainer.AccumulateContext(encoder, corpus.Sentences(runCfg.Dataset, trainSentences), split.HeldOut);

            // ── Trained arm ─────────────────────────────────────────────────
            using var trainedScope = new ActivationScope(runCfg);
            var trainer = new Trainer(runCfg, trainedScope, encoder) { HeldOut = split.HeldOut };
            var stats = trainer.Run(corpus.Sentences(runCfg.Dataset, trainSentences), quiet: true);

            Console.WriteLine($"   trained: {stats.Tokens:N0} tokens, {stats.Skipped:N0} held-out skipped, " +
                              $"{stats.Synapses:N0} synapses, {stats.Seconds:F1}s");

            highWater = Math.Max(highWater, stats.WorkingSetHighWater);
            truncations += stats.Truncations;
            synapses = stats.Synapses;

            var system = ScoreArm(runCfg, trainedScope, encoder, split);

            // ── Untrained-brain ceiling: identical everything, zero learning ──
            using var untrainedScope = new ActivationScope(runCfg);
            var untrained = ScoreArm(runCfg, untrainedScope, encoder, split);

            // ── Encoder ceiling: static code similarity, no runtime at all ───
            var encoderCeiling = ScoreEncoder(encoder, split);
            gradedRhos.Add(system.GradedRho);
            truncGaps.Add(system.TruncationGap);

            systemAucs.Add(system.Auc);
            untrainedAucs.Add(untrained.Auc);
            encoderAucs.Add(encoderCeiling.Auc);
            liftsUntrained.Add(system.Auc - untrained.Auc);
            liftsEncoder.Add(system.Auc - encoderCeiling.Auc);

            Console.WriteLine($"   SYSTEM_AUC {system.Auc:F3} (d′ {system.DPrime:F2})   " +
                              $"UNTRAINED {untrained.Auc:F3}   ENCODER {encoderCeiling.Auc:F3}");
            Console.WriteLine($"   trained mass {system.TrainedMean:F3} vs control mass {system.ControlMean:F3}   " +
                              $"graded ρ {system.GradedRho:+0.000;-0.000}   trunc gap {system.TruncationGap:+0.0;-0.0}");
        }

        var sys = Harness.Aggregate(systemAucs);
        var unt = Harness.Aggregate(untrainedAucs);
        var enc = Harness.Aggregate(encoderAucs);
        var liftU = Harness.Aggregate(liftsUntrained);
        var liftE = Harness.Aggregate(liftsEncoder);
        bool separated = Verdicts.RangesSeparated(sys, unt);

        Console.WriteLine($"\n── Result (mean of {repeats} repeats, [min..max]) ──");
        Console.WriteLine($"SYSTEM_AUC:    {sys.mean:F3} [{sys.lo:F3}..{sys.hi:F3}]");
        Console.WriteLine($"UNTRAINED_AUC: {unt.mean:F3} [{unt.lo:F3}..{unt.hi:F3}]   (same pipeline, no learning)");
        Console.WriteLine($"ENCODER_AUC:   {enc.mean:F3} [{enc.lo:F3}..{enc.hi:F3}]   (static code similarity)");
        Console.WriteLine($"LIFT_VS_UNTRAINED: {liftU.mean:+0.000;-0.000} [{liftU.lo:+0.000;-0.000}..{liftU.hi:+0.000;-0.000}]");
        Console.WriteLine($"LIFT_VS_ENCODER:   {liftE.mean:+0.000;-0.000} [{liftE.lo:+0.000;-0.000}..{liftE.hi:+0.000;-0.000}]");
        Console.WriteLine($"SEPARATED: {separated} (system and untrained repeat ranges do not overlap)");
        Console.WriteLine($"WORKING_SET_HIGH_WATER: {highWater:N0} / {workingMax:N0}");
        Console.WriteLine($"CASCADE_TRUNCATIONS: {truncations:N0}");

        var rho = Harness.Aggregate(gradedRhos);
        var gap = Harness.Aggregate(truncGaps);
        Console.WriteLine($"GRADED_RHO: {rho.mean:+0.000;-0.000} [{rho.lo:+0.000;-0.000}..{rho.hi:+0.000;-0.000}]   " +
                          "(mass vs corpus frequency, trained cues only)");
        Console.WriteLine($"TRUNCATION_GAP: {gap.mean:+0.0;-0.0} per cue (trained − control; non-zero ⇒ residency confound)");
        Console.WriteLine(Math.Abs(rho.mean) < 0.20
            ? "   ⚠️  Mass does not track frequency among trained cues. The readout is closer to a\n" +
              "       binary seen/unseen detector than to graded recall — AUC 1.000 would follow from\n" +
              "       ANY system that writes something when it sees a word."
            : "   ✅ Mass tracks corpus frequency among trained cues: the readout is graded, not binary.");

        var refusal = Verdicts.RefuseForRepeats(repeats);
        Console.WriteLine();
        if (refusal is not null)
        {
            Console.WriteLine($"VERDICT: {refusal}");
        }
        else
        {
            bool pass = liftU.mean >= 0.05 && separated && highWater <= workingMax;
            Console.WriteLine(pass
                ? $"VERDICT: ARCHITECTURE LIFT — {liftU.mean:+0.000} over an identically-configured untrained brain, " +
                  "repeat ranges non-overlapping."
                : liftU.mean < 0.05
                    ? $"VERDICT: NO ARCHITECTURE LIFT — {liftU.mean:+0.000} against a required +0.050. " +
                      "Learning is not adding discrimination the untrained pipeline lacks."
                    : "VERDICT: PROMISING BUT NOISY — lift clears the bar but repeat ranges overlap.");
            Console.WriteLine($"P4_GATE: {(pass ? "PASS" : "FAIL")}");
        }

        return new Result(repeats, sys, unt, enc, liftU, liftE, separated,
                          highWater, workingMax, truncations, synapses, refusal);
    }

    /// <summary>
    /// Score one brain: cascade readout for every cue, trained vs control.
    /// The statistic is total post-k-WTA activation mass — an untrained assembly
    /// has no synapses to carry mass past the initial drive.
    /// </summary>
    private static ArmResult ScoreArm(Config cfg, ActivationScope scope, ContextEncoder encoder,
                                      ControlSets.Split split)
    {
        var cascade = new Cascade(cfg, scope);

        var trained = new List<double>();
        var controls = new List<double>();
        double trainedTrunc = 0, controlTrunc = 0;

        foreach (var w in split.Trained)
        {
            var r = cascade.Run(encoder.Encode(w), learningMode: false);
            trained.Add(r.TotalMass);
            trainedTrunc += r.Truncated;
        }
        foreach (var w in split.Controls)
        {
            var r = cascade.Run(encoder.Encode(w), learningMode: false);
            controls.Add(r.TotalMass);
            controlTrunc += r.Truncated;
        }

        // Residency confound check. A trained cue's assembly was materialized during
        // training and may still be resident, while a control's was never resident
        // and must be regenerated into a possibly-full pool. If controls truncate
        // more than trained cues, the statistic is partly measuring RESIDENCY rather
        // than learning. (Verified absent: at WorkingSetMax 500,000 nothing truncates
        // and the result is unchanged, but the diagnostic stays because it is
        // configuration-dependent.)
        var truncGap = (trainedTrunc - controlTrunc) / Math.Max(1, split.Trained.Count);

        // Graded-recall diagnostic: among TRAINED cues only, does activation mass
        // track corpus frequency? A monotone relationship means the readout carries
        // graded information; a flat one means it is a binary seen/unseen detector
        // that happens to be scored as an AUC.
        var freqs = split.Trained.Select(w => (double)split.Frequency.GetValueOrDefault(w)).ToList();
        var gradedRho = Harness.Spearman(trained, freqs);

        return new ArmResult(Harness.Auc(trained, controls), Harness.DPrime(trained, controls),
                             Harness.Mean(trained), Harness.Mean(controls))
        { TruncationGap = truncGap, GradedRho = gradedRho };
    }

    /// <summary>
    /// The plan's literal ceiling: static code similarity, no runtime. Max
    /// similarity to any OTHER trained cue, self excluded — the same statistic form
    /// the P0/P2 ceilings used, so the numbers are comparable.
    /// </summary>
    private static ArmResult ScoreEncoder(ContextEncoder encoder, ControlSets.Split split)
    {
        var trainedCodes = split.Trained.ToDictionary(w => w, encoder.Encode);

        double MaxToTrained(string w)
        {
            var code = encoder.Encode(w);
            double best = -1;
            foreach (var kv in trainedCodes)
            {
                if (kv.Key.Equals(w, StringComparison.Ordinal)) continue;
                var s = code.Similarity(kv.Value);
                if (s > best) best = s;
            }
            return best;
        }

        var t = split.Trained.Select(MaxToTrained).ToList();
        var c = split.Controls.Select(MaxToTrained).ToList();
        return new ArmResult(Harness.Auc(t, c), Harness.DPrime(t, c), Harness.Mean(t), Harness.Mean(c));
    }

    private static Result Empty(int repeats, string refusal) =>
        new(repeats, (0, 0, 0), (0, 0, 0), (0, 0, 0), (0, 0, 0), (0, 0, 0), false, 0, 0, 0, 0, refusal);

    private static Config Clone(Config c) => new()
    {
        BaselineNeuronCount = c.BaselineNeuronCount,
        WorkingSetMax = c.WorkingSetMax,
        SynapseCapPerNeuron = c.SynapseCapPerNeuron,
        ActivationDepth = c.ActivationDepth,
        ActivationWidth = c.ActivationWidth,
        PatternSize = c.PatternSize,
        Sparsity = c.Sparsity,
        ContextBlend = c.ContextBlend,
        SurfaceDimensions = c.SurfaceDimensions,
        VqCodebookSize = c.VqCodebookSize,
        DeviationThreshold = c.DeviationThreshold,
        Seed = c.Seed,
        BrainDataPath = c.BrainDataPath,
        TrainingDataRoot = c.TrainingDataRoot,
        Dataset = c.Dataset
    };
}
