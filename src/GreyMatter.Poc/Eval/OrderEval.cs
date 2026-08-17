using GreyMatter.Poc.Encoding;
using GreyMatter.Poc.Pipeline;
using GreyMatter.Poc.Runtime;
using GreyMatter.Poc.Substrate;

namespace GreyMatter.Poc.Eval;

/// <summary>
/// plan.md P5 / §6.1 — does synapse strength track corpus STATISTICS, or just topology?
///
/// Ported from legacy <c>RunCascadeStats</c>/<c>ScoreArm</c>, including the parts
/// that exist to stop it lying:
///
///   • **PMI is primary** (rule 3). Raw bigram and unigram counts are collinear, so
///     comparing r_bigram against r_unigram cannot separate "learned this sequence"
///     from "this word is common". Dividing out the target base rate can.
///   • **The null is scored on the SAME pairs as the real arm** (rule 2). The legacy
///     P5.6 lesson: filtering to reached successors silently changed the experiment.
///   • **Real correlation must itself be positive** (rule 3). The legacy P5.4 bug
///     fired "LEARNED ORDER" on an arm whose real R_PMI was −0.0263, because the
///     shuffled arm was *more* negative. Being less anti-correlated than noise is
///     not learning.
///   • **Support floor** (rule 4): refuse a verdict when under 20% of scored bigrams
///     occur more than once.
///   • **≥5 repeats** (rule 1): legacy P5.3 reported a verdict from n=1 and had to
///     retract it.
///
/// The P5 gate is that this instrument WORKS and emits a rule-compliant verdict.
/// NULL is a passing verdict.
/// </summary>
public static class OrderEval
{
    public sealed record ArmScore(double RBigram, double RUnigram, double RPmi, int CuesScored, double Support);

    public sealed record Result(
        (double mean, double lo, double hi) RealPmi,
        (double mean, double lo, double hi) ShuffledPmi,
        (double mean, double lo, double hi) RealBigram,
        (double mean, double lo, double hi) RealUnigram,
        double PmiGap, int CuesScored, double Support, string Verdict);

    public static Result Run(Config cfg, Args args)
    {
        int repeats = args.Int("--repeats", 5);
        int trainSentences = args.Int("--train", 2000);
        int topK = args.Int("--topk", 16);
        int minSuccessors = args.Int("--min-successors", 5);

        var corpus = new Corpus(cfg.TrainingDataRoot, args.Has("--local-sample"));

        Console.WriteLine("🔬 ORDER — does cascade mass rank successors the way the corpus does?");
        Console.WriteLine("=====================================================================\n");
        Console.WriteLine($"source: {corpus.Describe(cfg.Dataset)}");
        Console.WriteLine($"train: {trainSentences}   repeats: {repeats}   top-k: {topK}\n");

        var sentences = corpus.Sentences(cfg.Dataset, trainSentences).ToList();

        // Ground truth from the REAL corpus, used to score EVERY arm including the
        // shuffled one. That is what makes the shuffled arm a null rather than a
        // different experiment (rule 2).
        var bigram = new Dictionary<(string, string), int>();
        var unigram = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var s in sentences)
        {
            var w = Corpus.Tokenize(s);
            for (int i = 0; i < w.Count; i++)
            {
                unigram[w[i]] = unigram.GetValueOrDefault(w[i]) + 1;
                if (i + 1 < w.Count)
                {
                    var key = (w[i], w[i + 1]);
                    bigram[key] = bigram.GetValueOrDefault(key) + 1;
                }
            }
        }

        var successors = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var (a, b) in bigram.Keys)
        {
            if (!successors.TryGetValue(a, out var list)) successors[a] = list = new List<string>();
            list.Add(b);
        }

        var cues = unigram
            .Where(kv => successors.TryGetValue(kv.Key, out var s) && s.Count >= minSuccessors)
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.Ordinal)
            .Take(20)
            .Select(kv => kv.Key)
            .ToList();

        if (cues.Count == 0)
        {
            Console.WriteLine($"⚠️  no cue had ≥{minSuccessors} distinct successors. Train more sentences.");
            return new Result((0, 0, 0), (0, 0, 0), (0, 0, 0), (0, 0, 0), 0, 0, 0,
                              "INCONCLUSIVE — no cue had enough successors to rank.");
        }

        var reals = new List<ArmScore>();
        var shufs = new List<ArmScore>();

        for (int r = 0; r < repeats; r++)
        {
            Console.WriteLine($"═══ repeat {r + 1}/{repeats} ═══");
            var runCfg = Clone(cfg);
            runCfg.Seed = cfg.Seed + r;

            reals.Add(ScoreArm(runCfg, sentences, cues, successors, bigram, unigram, topK, shuffle: false));
            shufs.Add(ScoreArm(runCfg, sentences, cues, successors, bigram, unigram, topK, shuffle: true));

            Console.WriteLine($"   real  R_PMI {reals[^1].RPmi:+0.0000;-0.0000}   " +
                              $"shuffled R_PMI {shufs[^1].RPmi:+0.0000;-0.0000}   " +
                              $"cues {reals[^1].CuesScored}");
        }

        var realPmi = Harness.Aggregate(reals.Select(x => x.RPmi).ToList());
        var shufPmi = Harness.Aggregate(shufs.Select(x => x.RPmi).ToList());
        var realBig = Harness.Aggregate(reals.Select(x => x.RBigram).ToList());
        var realUni = Harness.Aggregate(reals.Select(x => x.RUnigram).ToList());
        var gap = realPmi.mean - shufPmi.mean;
        int scored = reals[0].CuesScored;
        double support = Harness.Mean(reals.Select(x => x.Support).ToList());

        Console.WriteLine($"\n── Result (mean of {repeats} repeats, [min..max]) ──");
        Console.WriteLine($"R_BIGRAM:   {realBig.mean:+0.0000;-0.0000} [{realBig.lo:+0.0000;-0.0000}..{realBig.hi:+0.0000;-0.0000}]   (raw count)");
        Console.WriteLine($"R_UNIGRAM:  {realUni.mean:+0.0000;-0.0000} [{realUni.lo:+0.0000;-0.0000}..{realUni.hi:+0.0000;-0.0000}]   (frequency confound diagnostic)");
        Console.WriteLine($"R_PMI:      {realPmi.mean:+0.0000;-0.0000} [{realPmi.lo:+0.0000;-0.0000}..{realPmi.hi:+0.0000;-0.0000}]   vs shuffled {shufPmi.mean:+0.0000;-0.0000} [{shufPmi.lo:+0.0000;-0.0000}..{shufPmi.hi:+0.0000;-0.0000}]");
        Console.WriteLine($"PMI_GAP:    {gap:+0.0000;-0.0000}   (real − shuffled)");
        Console.WriteLine($"CUES_SCORED: {scored}");
        Console.WriteLine($"SUPPORT:    {support:P1} of scored bigrams occur more than once");

        var verdict = Decide(repeats, scored, support, realPmi, shufPmi, gap);
        Console.WriteLine($"\nVERDICT: {verdict}");

        return new Result(realPmi, shufPmi, realBig, realUni, gap, scored, support, verdict);
    }

    private static string Decide(int repeats, int scored, double support,
                                 (double mean, double lo, double hi) realPmi,
                                 (double mean, double lo, double hi) shufPmi,
                                 double gap)
    {
        var refusal = Verdicts.RefuseForRepeats(repeats);
        if (refusal is not null) return refusal;

        if (scored < 5)
            return "INCONCLUSIVE — too few cues had enough reachable successors to rank.";

        var supportRefusal = Verdicts.RefuseForSupport(support);
        if (supportRefusal is not null) return supportRefusal;

        // Rule 3: the real correlation must itself be positive. A positive gap over
        // a more-negative null is not learning.
        if (realPmi.mean < 0.10)
            return $"NO SIGNAL — real R_PMI is {realPmi.mean:+0.0000;-0.0000}; the graph does not rank successors " +
                   "by association at all. A positive gap here would only mean the shuffled arm is worse.";

        bool separated = realPmi.lo > shufPmi.hi;
        if (gap > 0.15 && separated)
            return "LEARNED ORDER — real R_PMI positive, beats shuffle, and repeat ranges do not overlap.";
        if (gap > 0.15)
            return "PROMISING BUT NOISY — gap is large but repeat ranges overlap. Raise --repeats or --train.";
        if (gap > 0.05)
            return "WEAK ORDER SIGNAL — present but small.";
        return "NULL NOT REJECTED — destroying word order costs almost nothing.";
    }

    /// <summary>
    /// Train one arm and return mean within-cue rank correlations across cues.
    /// The shuffled arm trains on word-order-shuffled sentences and is scored
    /// against the REAL bigram counts: vocabulary and frequency preserved, order
    /// destroyed.
    /// </summary>
    private static ArmScore ScoreArm(Config cfg, List<string> sentences, List<string> cues,
                                     Dictionary<string, List<string>> successors,
                                     Dictionary<(string, string), int> bigram,
                                     Dictionary<string, int> unigram,
                                     int topK, bool shuffle)
    {
        var trainingText = sentences;
        if (shuffle)
        {
            trainingText = new List<string>(sentences.Count);
            for (int i = 0; i < sentences.Count; i++)
            {
                var w = Corpus.Tokenize(sentences[i]);
                Rng.Shuffle(w, cfg.Seed + i, Rng.Purpose.Benchmark);
                trainingText.Add(string.Join(' ', w));
            }
        }

        var encoder = new ContextEncoder(cfg);
        Trainer.AccumulateContext(encoder, trainingText);

        using var scope = new ActivationScope(cfg);
        new Trainer(cfg, scope, encoder).Run(trainingText, quiet: true);

        var cascade = new Cascade(cfg, scope);
        long totalTokens = unigram.Values.Sum();

        var rBigrams = new List<double>();
        var rUnigrams = new List<double>();
        var rPmis = new List<double>();
        int supported = 0, totalPairs = 0;

        foreach (var cue in cues)
        {
            var targets = successors[cue].Distinct().OrderBy(x => x, StringComparer.Ordinal).ToList();
            if (targets.Count < 3) continue;

            // Mass that cueing `cue` puts on each successor's assembly. Scored for
            // EVERY successor, reached or not — filtering to reached ones is the
            // P5.6 mistake that silently changed the experiment.
            var cueReadout = cascade.Run(encoder.Encode(cue), learningMode: false);
            var winners = cascade.Winners(cueReadout.WinnerCount);
            var scores = cascade.WinnerScores(cueReadout.WinnerCount);

            var active = new Dictionary<uint, float>();
            for (int i = 0; i < winners.Length; i++)
                active[scope.Pool.VirtualId[winners[i]]] = scores[i];

            var mass = new List<double>();
            var bigramCounts = new List<double>();
            var unigramCounts = new List<double>();
            var pmis = new List<double>();

            foreach (var target in targets)
            {
                var members = Assembly.Members(encoder.Encode(target), cfg.BaselineNeuronCount);
                double m = 0;
                foreach (var vid in members) if (active.TryGetValue(vid, out var v)) m += v;
                mass.Add(m);

                int bc = bigram.GetValueOrDefault((cue, target));
                int uc = unigram.GetValueOrDefault(target);
                bigramCounts.Add(bc);
                unigramCounts.Add(uc);

                totalPairs++;
                if (bc > 1) supported++;

                // PMI ≈ log( p(a,b) / (p(a)·p(b)) ), base-rate corrected so a merely
                // common successor does not outrank an actually associated one.
                double pAB = (double)bc / Math.Max(1, totalTokens);
                double pA = (double)unigram.GetValueOrDefault(cue) / Math.Max(1, totalTokens);
                double pB = (double)uc / Math.Max(1, totalTokens);
                pmis.Add(pAB <= 0 || pA <= 0 || pB <= 0 ? 0 : Math.Log(pAB / (pA * pB)));
            }

            rBigrams.Add(Harness.Spearman(mass, bigramCounts));
            rUnigrams.Add(Harness.Spearman(mass, unigramCounts));
            rPmis.Add(Harness.Spearman(mass, pmis));
        }

        return new ArmScore(Harness.Mean(rBigrams), Harness.Mean(rUnigrams), Harness.Mean(rPmis),
                            rPmis.Count, totalPairs == 0 ? 0 : (double)supported / totalPairs);
    }

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
