using GreyMatter.Poc.Encoding;
using GreyMatter.Poc.Pipeline;
using GreyMatter.Poc.Runtime;
using GreyMatter.Poc.Substrate;

namespace GreyMatter.Poc.Eval;

/// <summary>
/// plan.md Addendum A, P7.0 — attribution instrumentation. Changes no behaviour.
///
/// Its job is to confirm or kill each A.1 hypothesis with a number, before P7.1
/// changes anything:
///
///   H1  the synaptic budget is consumed by within-assembly edges, which encode
///       only "I fired" — leaving no slots for edges that could carry association
///   H2  displacement is structurally inert, so the graph cannot correct itself
///   H3  the readout is dominated by hop 0
///   H4  width is overpaid, so there is free budget to spend on fixing H1–H3
///
/// H4 is already settled by P6.2 (width 64 ≡ width 256 on every recall metric at 5×
/// the throughput) and is not re-measured here.
///
/// The load-bearing number for P7.1's design is the one flagged before this phase
/// began: **what share of RECALL MASS arrives via within-assembly edges.** If
/// within-assembly wiring is both the thing that encodes nothing and the thing that
/// carries recall, then P7.1 cannot cut it and satisfy A-R3 simultaneously — it has
/// to grow cross-assembly recall first.
/// </summary>
public static class AttributionEval
{
    private static readonly string[] PopNames = { "within-assembly", "cross-assembly", "cross-cue" };

    public static void Run(Config cfg, Args args)
    {
        int trainSentences = args.Int("--train", 4000);
        var corpus = new Corpus(cfg.TrainingDataRoot, args.Has("--local-sample"));

        Console.WriteLine("🔬 P7.0 ATTRIBUTION — where the synaptic budget and the recall mass go");
        Console.WriteLine("======================================================================\n");
        Console.WriteLine($"source: {corpus.Describe(cfg.Dataset)}");
        Console.WriteLine($"train: {trainSentences:N0} sentences   seed {cfg.Seed}   " +
                          $"width {cfg.ActivationWidth}   depth {cfg.ActivationDepth}   " +
                          $"cap {cfg.SynapseCapPerNeuron}/neuron\n");

        var split = ControlSets.Build(corpus, cfg.Dataset, trainSentences, pairs: 16);
        var encoder = new ContextEncoder(cfg);
        Trainer.AccumulateContext(encoder, corpus.Sentences(cfg.Dataset, trainSentences), split.HeldOut);

        using var scope = new ActivationScope(cfg);
        var trainer = new Trainer(cfg, scope, encoder) { HeldOut = split.HeldOut };
        var stats = trainer.Run(corpus.Sentences(cfg.Dataset, trainSentences), quiet: true);

        var syn = scope.Synapses;

        // ── (a) the synaptic budget ─────────────────────────────────────────
        Console.WriteLine("── (a) SYNAPTIC BUDGET by provenance ──\n");
        var (counts, meanWeights) = syn.PopulationCensus(scope.Pool.Count);
        long liveTotal = counts.Sum();

        Console.WriteLine("| population | live slots | share | mean w | proposals | created | strengthened | displaced | declined | decline rate |");
        Console.WriteLine("|---|---|---|---|---|---|---|---|---|---|");
        for (int p = 0; p < 3; p++)
        {
            long proposals = syn.CreatedBy[p] + syn.StrengthenedBy[p] + syn.DisplacedBy[p] + syn.DeclinedBy[p];
            double declineRate = proposals > 0 ? (double)syn.DeclinedBy[p] / proposals : 0;
            double share = liveTotal > 0 ? (double)counts[p] / liveTotal : 0;
            Console.WriteLine($"| {PopNames[p]} | {counts[p]:N0} | {share:P1} | {meanWeights[p]:F3} | " +
                              $"{proposals:N0} | {syn.CreatedBy[p]:N0} | {syn.StrengthenedBy[p]:N0} | " +
                              $"{syn.DisplacedBy[p]:N0} | {syn.DeclinedBy[p]:N0} | {declineRate:P1} |");
        }
        Console.WriteLine($"\nLIVE_SYNAPSES: {liveTotal:N0} of {(long)scope.Pool.Count * cfg.SynapseCapPerNeuron:N0} slots " +
                          $"({(double)liveTotal / Math.Max(1, (long)scope.Pool.Count * cfg.SynapseCapPerNeuron):P1} full)");
        Console.WriteLine($"CROSS_SHARE: {(liveTotal > 0 ? (double)(counts[1] + counts[2]) / liveTotal : 0):P1} " +
                          "(cross-assembly + cross-cue share of live slots — the P7.1 gate needs > 50%)");

        // ── (b) recall mass ─────────────────────────────────────────────────
        Console.WriteLine("\n── (b) RECALL MASS by hop and by delivering population ──\n");
        var cascade = new Cascade(cfg, scope);

        var hopTotals = new double[3];
        var popTotals = new double[3];
        var winnersByHop = new long[3];
        double grandTotal = 0;

        foreach (var w in split.Trained)
        {
            var r = cascade.Run(encoder.Encode(w), learningMode: false);
            for (int i = 0; i < 3; i++)
            {
                hopTotals[i] += r.MassByHop[i];
                popTotals[i] += r.DriveByPopulation[i];
                winnersByHop[i] += r.WinnersByHop[i];
            }
            grandTotal += r.TotalMass;
        }

        Console.WriteLine("| hop | surviving mass | share | k-WTA winners |");
        Console.WriteLine("|---|---|---|---|");
        string[] hopNames = { "0 (cue's own assembly)", "1 (one synapse away)", "2+ (multi-hop)" };
        for (int i = 0; i < 3; i++)
            Console.WriteLine($"| {hopNames[i]} | {hopTotals[i]:F1} | " +
                              $"{(grandTotal > 0 ? hopTotals[i] / grandTotal : 0):P1} | {winnersByHop[i]:N0} |");

        Console.WriteLine($"\nKWTA_SATURATION: assembly is {Runtime.Assembly.Size(cfg.Sparsity)} neurons, " +
                          $"ActivationWidth is {cfg.ActivationWidth} — " +
                          (Runtime.Assembly.Size(cfg.Sparsity) >= cfg.ActivationWidth
                            ? "the cue's own assembly FILLS every k-WTA slot, so no propagated neuron can ever win."
                            : $"{cfg.ActivationWidth - Runtime.Assembly.Size(cfg.Sparsity)} slots remain for propagated neurons."));

        Console.WriteLine($"\nHOP0_SHARE: {(grandTotal > 0 ? hopTotals[0] / grandTotal : 0):P1} " +
                          "(H3: the readout is dominated by hop 0)");
        Console.WriteLine($"MULTIHOP_SHARE: {(grandTotal > 0 ? hopTotals[2] / grandTotal : 0):P1} " +
                          "(mass that working-set pressure could break — P6.3's missing trade)");

        Console.WriteLine("\n| population | drive injected | share |");
        Console.WriteLine("|---|---|---|");
        double delivered = popTotals.Sum();
        for (int p = 0; p < 3; p++)
            Console.WriteLine($"| {PopNames[p]} | {popTotals[p]:F1} | {(delivered > 0 ? popTotals[p] / delivered : 0):P1} |");
        Console.WriteLine("\n(drive injected is summed at delivery, so it counts top-ups to hop-0 neurons —");
        Console.WriteLine(" different units from surviving mass above, and the only correct way to attribute it.)");

        Console.WriteLine($"\nRECALL_VIA_WITHIN_ASSEMBLY: {(delivered > 0 ? popTotals[0] / delivered : 0):P1}");
        Console.WriteLine("   ↑ the load-bearing number for P7.1. If within-assembly edges both encode");
        Console.WriteLine("     nothing (H1) and deliver recall, P7.1 cannot cut them and hold A-R3's");
        Console.WriteLine("     no-regression clause at the same time; cross-assembly recall has to grow first.");

        // ── verdicts on the A.1 hypotheses ──────────────────────────────────
        Console.WriteLine("\n── A.1 hypotheses, judged ──\n");

        double withinShare = liveTotal > 0 ? (double)counts[0] / liveTotal : 0;
        long crossProposals = syn.CreatedBy[1] + syn.StrengthenedBy[1] + syn.DisplacedBy[1] + syn.DeclinedBy[1]
                            + syn.CreatedBy[2] + syn.StrengthenedBy[2] + syn.DisplacedBy[2] + syn.DeclinedBy[2];
        long crossDeclined = syn.DeclinedBy[1] + syn.DeclinedBy[2];
        double crossDeclineRate = crossProposals > 0 ? (double)crossDeclined / crossProposals : 0;

        Say("H1  budget consumed by within-assembly edges",
            withinShare > 0.5,
            $"within-assembly holds {withinShare:P1} of live slots; cross-* proposals decline at {crossDeclineRate:P1}");

        long totalProposals = syn.Created + syn.Strengthened + syn.Displaced + syn.Declined;
        double displaceRate = totalProposals > 0 ? (double)syn.Displaced / totalProposals : 0;
        Say("H2  displacement is structurally inert",
            displaceRate < 0.005,
            $"displacement is {displaceRate:P3} of {totalProposals:N0} proposals (P7.2 gate wants ≥ 0.5%)");

        Say("H3  readout dominated by hop 0",
            grandTotal > 0 && hopTotals[0] / grandTotal > 0.5,
            $"hop 0 carries {(grandTotal > 0 ? hopTotals[0] / grandTotal : 0):P1}, multi-hop {(grandTotal > 0 ? hopTotals[2] / grandTotal : 0):P1}");

        Console.WriteLine($"\nrun: {stats.Sentences:N0} sentences, {stats.Tokens:N0} tokens, " +
                          $"{stats.Seconds:F0}s, working set {stats.WorkingSetHighWater:N0}, " +
                          $"truncations {stats.Truncations:N0}");
        Console.WriteLine($"\nCOMMAND: {args.CommandLine}");
    }

    private static void Say(string hypothesis, bool confirmed, string evidence)
    {
        Console.WriteLine($"{(confirmed ? "CONFIRMED" : "KILLED   ")}  {hypothesis}");
        Console.WriteLine($"            {evidence}");
    }
}
