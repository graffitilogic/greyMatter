using GreyMatter.Poc.Encoding;
using GreyMatter.Poc.Pipeline;
using GreyMatter.Poc.Runtime;
using GreyMatter.Poc.Substrate;

namespace GreyMatter.Poc.Eval;

/// <summary>
/// plan.md Addendum A P7.3 / Addendum B P9.0 — the association gate.
///
/// Registered in RESULTS § P9.0. This is the instrument P7.3 named as an
/// equal-alternative gate and that was never built, which is why every verdict
/// since P5 came from `gm eval order` — the *hardest* association question
/// (rank a cue's successors against each other by base-rate-corrected sequence
/// statistics) rather than the one Prompt.md actually asks (does activating a
/// concept light up related material at all?).
///
/// For each cue: rank frequency-matched in-vocabulary words that DID co-occur with
/// it against those that never did, by cascade mass. AUC over the cue set.
///
/// The null redistributes tokens GLOBALLY across the corpus, preserving every
/// unigram count and every sentence length while destroying which words share a
/// sentence (rule 2, same pairs scored).
///
/// The obvious null — shuffling words *within* each sentence, as `gm eval order`
/// uses — is wrong for this test and was tried first. It destroys order but
/// preserves sentence co-membership almost perfectly, and "related" here means
/// "co-occurred in a sentence". So a correctly-associating system scores identically
/// in both arms: measured 0.574 real against 0.569 shuffled, which reads as
/// CONFOUNDED and is really the null preserving the signal it exists to remove. A-R2
/// reasoned that shuffling preserves unigram frequency (true) and therefore isolates
/// association (false — it preserves that too). Within-sentence shuffling is the
/// right null for ORDER, which is where it came from.
/// </summary>
public static class AssocEval
{
    public sealed record ArmScore(double Auc, double DPrime, double RelatedMean, double UnrelatedMean,
                                  int CuesScored, int PairsScored);

    /// <summary>P9.2R — where the association is lost between edges and readout.</summary>
    public enum Readout { Winners, Drive, Edge }

    public static (double mean, double lo, double hi) Run(Config cfg, Args args)
    {
        int repeats = args.Int("--repeats", 5);
        int train = args.Int("--train", 2000);
        int cueCount = args.Int("--cues", 20);
        int perCue = args.Int("--per-cue", 8);

        var corpus = new Corpus(cfg.TrainingDataRoot, args.Has("--local-sample"));

        Console.WriteLine("🔬 P9.0 ASSOC — does a cue activate related material at all?");
        Console.WriteLine("============================================================\n");
        Console.WriteLine($"source: {corpus.Describe(cfg.Dataset)}");
        Console.WriteLine($"train: {train:N0}   repeats: {repeats}   cues: {cueCount}   " +
                          $"pairs/cue: {perCue} related + {perCue} matched unrelated");
        Console.WriteLine($"λ={cfg.BaseRateDepression}  erosion={cfg.ContestErosion}  " +
                          $"quota={cfg.PropagatedWinnerQuota}  within-cap={cfg.WithinAssemblyCap}\n");

        var sentences = corpus.Sentences(cfg.Dataset, train).ToList();
        var pairMode = args.Value("--pairs-from", "window");
        var (cues, related, unrelated, unigram) = pairMode == "bigram"
            ? BuildBigramPairs(sentences, cueCount, perCue)
            : BuildPairs(sentences, cueCount, perCue, cfg.Seed);
        Console.WriteLine($"pair selection: {pairMode}   " +
                          (pairMode == "bigram"
                            ? "(P8a definition — top ADJACENT bigrams per cue)"
                            : "(uniform sample of ±2 co-occurrence)"));

        if (cues.Count < 5)
        {
            Console.WriteLine($"⚠️  only {cues.Count} cues had enough matched pairs. Raise --train.");
            Console.WriteLine("VERDICT: INCONCLUSIVE — insufficient frequency-matched pairs to score.");
            return (0, 0, 0);
        }

        Console.WriteLine($"── cue set ({cues.Count} cues) ──");
        for (int i = 0; i < Math.Min(4, cues.Count); i++)
            Console.WriteLine($"   {cues[i],-10} related: {string.Join(", ", related[cues[i]].Take(4))}" +
                              $"   unrelated: {string.Join(", ", unrelated[cues[i]].Take(4))}");

        var modes = args.Value("--readout", "winners") switch
        {
            "all" => new[] { Readout.Winners, Readout.Drive, Readout.Edge },
            "drive" => new[] { Readout.Drive },
            "edge" => new[] { Readout.Edge },
            _ => new[] { Readout.Winners }
        };

        if (args.Has("--diagnose"))
        {
            Diagnose(cfg, sentences, cues, related, unrelated, args);
            return (0, 0, 0);
        }

        var realAucs = new List<double>();
        var nullAucs = new List<double>();
        ArmScore lastReal = new(0, 0, 0, 0, 0, 0), lastNull = new(0, 0, 0, 0, 0, 0);

        if (modes.Length > 1)
        {
            Console.WriteLine("\n── P9.2R: three readouts, same brains, same pairs, same null ──");
            var byMode = new Dictionary<Readout, (List<double> real, List<double> nul)>();
            foreach (var m in modes) byMode[m] = (new List<double>(), new List<double>());

            for (int r = 0; r < repeats; r++)
            {
                var rc = cfg.Clone(); rc.Seed = cfg.Seed + r;
                foreach (var m in modes)
                {
                    byMode[m].real.Add(ScoreArm(rc, sentences, cues, related, unrelated, false, m).Auc);
                    byMode[m].nul.Add(ScoreArm(rc, sentences, cues, related, unrelated, true, m).Auc);
                }
                Console.WriteLine($"   repeat {r + 1}/{repeats}: " +
                    string.Join("   ", modes.Select(m => $"{m} {byMode[m].real[^1]:F3}/{byMode[m].nul[^1]:F3}")));
            }

            Console.WriteLine("\n| readout | ASSOC_AUC | shuffled | gap | separated |");
            Console.WriteLine("|---|---|---|---|---|");
            foreach (var m in modes)
            {
                var re = Harness.Aggregate(byMode[m].real);
                var nu = Harness.Aggregate(byMode[m].nul);
                Console.WriteLine($"| **{m}** | {re.mean:F3} [{re.lo:F3}..{re.hi:F3}] | " +
                                  $"{nu.mean:F3} [{nu.lo:F3}..{nu.hi:F3}] | {re.mean - nu.mean:+0.000;-0.000} | " +
                                  $"{Verdicts.RangesSeparated(re, nu)} |");
            }
            Console.WriteLine($"\nCOMMAND: {args.CommandLine}");
            return Harness.Aggregate(byMode[modes[0]].real);
        }

        for (int r = 0; r < repeats; r++)
        {
            var runCfg = cfg.Clone();
            runCfg.Seed = cfg.Seed + r;

            var real = ScoreArm(runCfg, sentences, cues, related, unrelated, false, modes[0]);
            var shuf = ScoreArm(runCfg, sentences, cues, related, unrelated, true, modes[0]);

            realAucs.Add(real.Auc);
            nullAucs.Add(shuf.Auc);
            lastReal = real; lastNull = shuf;

            Console.WriteLine($"   repeat {r + 1}/{repeats}: ASSOC_AUC {real.Auc:F3} (d′ {real.DPrime:F2})   " +
                              $"shuffled {shuf.Auc:F3}   related {real.RelatedMean:F1} vs unrelated {real.UnrelatedMean:F1}   " +
                              $"cues {real.CuesScored}");
        }

        var real5 = Harness.Aggregate(realAucs);
        var null5 = Harness.Aggregate(nullAucs);
        bool separated = Verdicts.RangesSeparated(real5, null5);

        Console.WriteLine($"\n── Result (mean of {repeats} repeats, [min..max]) ──");
        Console.WriteLine($"ASSOC_AUC:    {real5.mean:F3} [{real5.lo:F3}..{real5.hi:F3}]");
        Console.WriteLine($"SHUFFLED_AUC: {null5.mean:F3} [{null5.lo:F3}..{null5.hi:F3}]   " +
                          "(order destroyed, unigram frequency preserved, same pairs)");
        Console.WriteLine($"ASSOC_GAP:    {real5.mean - null5.mean:+0.000;-0.000}");
        Console.WriteLine($"SEPARATED:    {separated}");
        SampleCheck.Report(
            new ArmSample("real", lastReal.PairsScored, lastReal.PairsScored, lastReal.CuesScored),
            new ArmSample("shuffled", lastNull.PairsScored, lastNull.PairsScored, lastNull.CuesScored));

        var refusal = Verdicts.RefuseForRepeats(repeats);
        Console.WriteLine();
        if (refusal is not null)
        {
            Console.WriteLine($"VERDICT: {refusal}");
            return real5;
        }

        // The unchanged P7.3 bar (B-R2).
        bool pass = real5.mean >= 0.70 && null5.mean <= 0.55 && separated;
        if (pass)
            Console.WriteLine("VERDICT: ASSOCIATION — the cue's activated graph distinguishes related from " +
                              "frequency-matched unrelated material, above a shuffled null, ranges non-overlapping.");
        else if (real5.mean < 0.55)
            Console.WriteLine($"VERDICT: NO ASSOCIATION — ASSOC_AUC {real5.mean:F3} is at or near chance; " +
                              "the activated graph does not distinguish related material.");
        else if (null5.mean > 0.55)
            Console.WriteLine($"VERDICT: CONFOUNDED — the shuffled null itself scores {null5.mean:F3}. " +
                              "Whatever is being measured survives destroying word order, so it is not association.");
        else
            Console.WriteLine($"VERDICT: SIGNAL BUT SHORT — ASSOC_AUC {real5.mean:F3} against a {0.70:F2} bar" +
                              (separated ? "" : ", and repeat ranges overlap") + ".");

        Console.WriteLine($"P7.3_ASSOC_GATE: {(pass ? "PASS" : "FAIL")}");
        Console.WriteLine($"\nCOMMAND: {args.CommandLine}");
        return real5;
    }

    /// <summary>
    /// Related = co-occurred within ±2 in some sentence. Unrelated = never co-occurred
    /// with this cue anywhere, matched to a related word on unigram frequency (A-R1),
    /// so the contrast cannot be won by preferring common or rare words.
    /// </summary>
    private static (List<string>, Dictionary<string, List<string>>, Dictionary<string, List<string>>,
                    Dictionary<string, int>)
        BuildPairs(List<string> sentences, int cueCount, int perCue, int seed)
    {
        var unigram = new Dictionary<string, int>(StringComparer.Ordinal);
        var cooc = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var s in sentences)
        {
            var w = Corpus.Tokenize(s);
            for (int i = 0; i < w.Count; i++)
            {
                unigram[w[i]] = unigram.GetValueOrDefault(w[i]) + 1;
                if (!cooc.TryGetValue(w[i], out var set)) cooc[w[i]] = set = new HashSet<string>(StringComparer.Ordinal);
                for (int j = Math.Max(0, i - ContextEncoder.Window); j <= Math.Min(w.Count - 1, i + ContextEncoder.Window); j++)
                    if (j != i) set.Add(w[j]);
            }
        }

        var vocab = unigram.Keys.OrderBy(x => x, StringComparer.Ordinal).ToList();

        var cues = unigram.Where(kv => cooc.TryGetValue(kv.Key, out var c) && c.Count >= perCue * 2)
                          .OrderByDescending(kv => kv.Value)
                          .ThenBy(kv => kv.Key, StringComparer.Ordinal)
                          .Take(cueCount).Select(kv => kv.Key).ToList();

        var related = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var unrelated = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var chosen = new List<string>();

        foreach (var cue in cues)
        {
            var partners = cooc[cue];

            // Related partners are sampled UNIFORMLY from the cue's co-occurrence set,
            // not taken by descending frequency. Frequency-ordering made "related" the
            // same handful of globally-common words for every cue ("to, the, is, have"
            // for all of you/to/the/is) — not a cue-specific set, and trivially
            // high-mass for any cue because cascade mass tracks frequency (ρ≈0.73).
            var pool = partners.Where(p => p != cue)
                               .OrderBy(p => p, StringComparer.Ordinal).ToList();
            Rng.Shuffle(pool, seed, Rng.Purpose.Benchmark);
            var rel = pool.Take(perCue).ToList();
            if (rel.Count < perCue) continue;


            // For each related word, the nearest-frequency word that never co-occurred.
            var unrel = new List<string>();
            var used = new HashSet<string>(StringComparer.Ordinal);
            foreach (var target in rel)
            {
                int want = unigram[target];
                string? best = null;
                int bestGap = int.MaxValue;
                foreach (var cand in vocab)
                {
                    if (cand == cue || partners.Contains(cand) || used.Contains(cand)) continue;
                    int gap = Math.Abs(unigram[cand] - want);
                    if (gap < bestGap) { bestGap = gap; best = cand; }
                    if (gap == 0) break;
                }
                if (best is null) break;
                used.Add(best);
                unrel.Add(best);
            }
            if (unrel.Count < perCue) continue;

            related[cue] = rel;
            unrelated[cue] = unrel;
            chosen.Add(cue);
        }

        return (chosen, related, unrelated, unigram);
    }

    /// <summary>
    /// The association null: every token in the corpus pooled and dealt back out into
    /// sentences of the original lengths. Unigram counts and sentence-length
    /// distribution are preserved exactly; which words share a sentence is destroyed.
    /// </summary>
    private static List<string> RedistributeGlobally(List<string> sentences, int seed)
    {
        var lengths = new List<int>(sentences.Count);
        var pool = new List<string>();
        foreach (var s in sentences)
        {
            var w = Corpus.Tokenize(s);
            lengths.Add(w.Count);
            pool.AddRange(w);
        }

        Rng.Shuffle(pool, seed, Rng.Purpose.Projection);

        var outp = new List<string>(sentences.Count);
        int at = 0;
        foreach (var len in lengths)
        {
            if (at + len > pool.Count) break;
            outp.Add(string.Join(' ', pool.GetRange(at, len)));
            at += len;
        }
        return outp;
    }

    /// <summary>
    /// P9.2R follow-up — pairs as P8a defined them: a cue's most frequent ADJACENT
    /// successors, against frequency-matched words that never co-occur with it.
    ///
    /// P8a measured a large edge-mass signal on this definition (60% vs 15%
    /// connectivity, 418×) while `gm eval assoc` measures chance on a uniform sample
    /// of the ±2 window. The two cannot both describe "the edges", so which pairs are
    /// scored is the variable to isolate.
    /// </summary>
    private static (List<string>, Dictionary<string, List<string>>, Dictionary<string, List<string>>,
                    Dictionary<string, int>)
        BuildBigramPairs(List<string> sentences, int cueCount, int perCue)
    {
        var unigram = new Dictionary<string, int>(StringComparer.Ordinal);
        var bigram = new Dictionary<(string, string), int>();
        var anyCooc = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var s in sentences)
        {
            var w = Corpus.Tokenize(s);
            for (int i = 0; i < w.Count; i++)
            {
                unigram[w[i]] = unigram.GetValueOrDefault(w[i]) + 1;
                if (!anyCooc.TryGetValue(w[i], out var set))
                    anyCooc[w[i]] = set = new HashSet<string>(StringComparer.Ordinal);
                for (int j = Math.Max(0, i - ContextEncoder.Window); j <= Math.Min(w.Count - 1, i + ContextEncoder.Window); j++)
                    if (j != i) set.Add(w[j]);
                if (i + 1 < w.Count)
                {
                    var k = (w[i], w[i + 1]);
                    bigram[k] = bigram.GetValueOrDefault(k) + 1;
                }
            }
        }

        var successors = new Dictionary<string, List<(string t, int c)>>(StringComparer.Ordinal);
        foreach (var (k, c) in bigram)
        {
            if (!successors.TryGetValue(k.Item1, out var l)) successors[k.Item1] = l = new List<(string, int)>();
            l.Add((k.Item2, c));
        }

        var vocab = unigram.Keys.OrderBy(x => x, StringComparer.Ordinal).ToList();
        var cues = successors.Where(kv => kv.Value.Count >= perCue)
                             .OrderByDescending(kv => unigram[kv.Key])
                             .ThenBy(kv => kv.Key, StringComparer.Ordinal)
                             .Take(cueCount).Select(kv => kv.Key).ToList();

        var related = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var unrelated = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var chosen = new List<string>();

        foreach (var cue in cues)
        {
            var rel = successors[cue].OrderByDescending(x => x.c).ThenBy(x => x.t, StringComparer.Ordinal)
                                     .Take(perCue).Select(x => x.t).ToList();
            if (rel.Count < perCue) continue;

            var partners = anyCooc[cue];
            var unrel = new List<string>();
            var used = new HashSet<string>(StringComparer.Ordinal);
            foreach (var target in rel)
            {
                int want = unigram[target];
                string? best = null; int bestGap = int.MaxValue;
                foreach (var cand in vocab)
                {
                    if (cand == cue || partners.Contains(cand) || used.Contains(cand)) continue;
                    int gap = Math.Abs(unigram[cand] - want);
                    if (gap < bestGap) { bestGap = gap; best = cand; }
                    if (gap == 0) break;
                }
                if (best is null) break;
                used.Add(best); unrel.Add(best);
            }
            if (unrel.Count < perCue) continue;

            related[cue] = rel; unrelated[cue] = unrel; chosen.Add(cue);
        }
        return (chosen, related, unrelated, unigram);
    }

    /// <summary>
    /// plan.md Addendum C, P10.1 — the last diagnostic. One brain, one run, no
    /// repeats, claims nothing (C-R3). Prints the four things P9.3D said would close
    /// the remaining question about why `edge` reads chance while the population-level
    /// mass ratio is 12×.
    /// </summary>
    private static void Diagnose(Config cfg, List<string> sentences, List<string> cues,
                                 Dictionary<string, List<string>> related,
                                 Dictionary<string, List<string>> unrelated, Args args)
    {
        Console.WriteLine("\n🔬 P10.1 — tie structure of the association AUC (diagnostic, n=1, claims nothing)\n");

        var (relReal, unrelReal) = MassesFor(cfg, sentences, cues, related, unrelated, shuffle: false);
        var (relNull, unrelNull) = MassesFor(cfg, sentences, cues, related, unrelated, shuffle: true);

        // ── 1. tie fraction, both arms ──────────────────────────────────────
        var real = Harness.AucWithTies(relReal, unrelReal);
        var nul  = Harness.AucWithTies(relNull, unrelNull);

        Console.WriteLine("── 1. tie fraction ──");
        Console.WriteLine("| arm | AUC | comparisons | ties | tie fraction |");
        Console.WriteLine("|---|---|---|---|---|");
        Console.WriteLine($"| real | {real.auc:F3} | {real.pairs:N0} | {real.ties:N0} | **{real.tieFraction:P1}** |");
        Console.WriteLine($"| null | {nul.auc:F3} | {nul.pairs:N0} | {nul.ties:N0} | {nul.tieFraction:P1} |");

        // ── 2. AUC restricted to pairs with mass somewhere ──────────────────
        //
        // DIAGNOSTIC ONLY. The gate number stays the full-sample AUC: an edgeless
        // related pair is a real recall failure, and coverage may not be assumed
        // away (the P5.6 lesson).
        var relNz = relReal.Where(x => x > 1e-9).ToList();
        var unrelNz = unrelReal.Where(x => x > 1e-9).ToList();
        Console.WriteLine("\n── 2. AUC restricted to non-zero pairs (diagnostic only) ──");
        if (relNz.Count >= 3 && unrelNz.Count >= 3)
        {
            var restricted = Harness.AucWithTies(relNz, unrelNz);
            Console.WriteLine($"RESTRICTED_AUC: {restricted.auc:F3}   " +
                              $"(related {relNz.Count}/{relReal.Count} non-zero, " +
                              $"unrelated {unrelNz.Count}/{unrelReal.Count})");
            SampleCheck.Report(new ArmSample("related-nz", relNz.Count, relReal.Count, cues.Count),
                               new ArmSample("unrelated-nz", unrelNz.Count, unrelReal.Count, cues.Count));
        }
        else Console.WriteLine($"   too few non-zero pairs to restrict ({relNz.Count} / {unrelNz.Count})");

        // ── 3. per-pair mass distribution ───────────────────────────────────
        Console.WriteLine("\n── 3. per-pair mass distribution (the numbers P9.2R aggregated) ──");
        void Dist(string name, List<double> xs)
        {
            var s = xs.OrderBy(x => x).ToList();
            Console.WriteLine($"   {name,-10} n={s.Count,4}  zero={s.Count(x => x <= 1e-9),4}  " +
                              $"median={s[s.Count / 2]:F2}  p90={s[(int)(s.Count * 0.9)]:F2}  max={s[^1]:F2}  " +
                              $"mean={Harness.Mean(s):F2}");
        }
        Dist("related", relReal); Dist("unrelated", unrelReal);
        Dist("rel-null", relNull); Dist("unrel-null", unrelNull);

        // ── 4. does the redistribution null actually destroy co-occurrence? ──
        var shuffled = RedistributeGlobally(sentences, cfg.Seed);
        var residual = ResidualCooccurrence(shuffled, cues, related);
        var original = ResidualCooccurrence(sentences, cues, related);
        Console.WriteLine("\n── 4. residual ±2 co-occurrence in the null corpus ──");
        Console.WriteLine($"   related pairs still co-occurring: null {residual:P1}  vs  original {original:P1}");
        Console.WriteLine(residual < 0.10
            ? "   ✅ the null destroys co-occurrence as intended."
            : "   ⚠️  the null RETAINS co-occurrence — it cannot isolate association.");

        Console.WriteLine($"\nCOMMAND: {args.CommandLine}");
    }

    private static double ResidualCooccurrence(List<string> corpus, List<string> cues,
                                               Dictionary<string, List<string>> related)
    {
        var cooc = new HashSet<(string, string)>();
        foreach (var s in corpus)
        {
            var w = Corpus.Tokenize(s);
            for (int i = 0; i < w.Count; i++)
                for (int j = Math.Max(0, i - ContextEncoder.Window); j <= Math.Min(w.Count - 1, i + ContextEncoder.Window); j++)
                    if (j != i) cooc.Add((w[i], w[j]));
        }
        int hit = 0, total = 0;
        foreach (var c in cues)
            foreach (var t in related[c]) { total++; if (cooc.Contains((c, t))) hit++; }
        return total > 0 ? (double)hit / total : 0;
    }

    private static (List<double> rel, List<double> unrel) MassesFor(
        Config cfg, List<string> sentences, List<string> cues,
        Dictionary<string, List<string>> related, Dictionary<string, List<string>> unrelated, bool shuffle)
    {
        var text = shuffle ? RedistributeGlobally(sentences, cfg.Seed) : sentences;
        var encoder = new ContextEncoder(cfg);
        Trainer.AccumulateContext(encoder, text);
        using var scope = new ActivationScope(cfg);
        new Trainer(cfg, scope, encoder).Run(text, quiet: true);
        scope.ConsolidateAll();

        var rel = new List<double>(); var unrel = new List<double>();
        foreach (var cue in cues)
        {
            var from = Assembly.Members(encoder.Encode(cue), cfg.BaselineNeuronCount, cfg.AssemblyOverlap);
            double EdgeMass(string word)
            {
                var to = Assembly.Members(encoder.Encode(word), cfg.BaselineNeuronCount, cfg.AssemblyOverlap).ToHashSet();
                double m = 0;
                foreach (var vid in from)
                {
                    if (!scope.Recipes.TryGetValue(vid, out var r)) continue;
                    for (int i = 0; i < r.SynapseTargets.Length; i++)
                        if (to.Contains(r.SynapseTargets[i])) m += r.SynapseWeights[i];
                }
                return m;
            }
            foreach (var w in related[cue]) rel.Add(EdgeMass(w));
            foreach (var w in unrelated[cue]) unrel.Add(EdgeMass(w));
        }
        return (rel, unrel);
    }

    private static ArmScore ScoreArm(Config cfg, List<string> sentences, List<string> cues,
                                     Dictionary<string, List<string>> related,
                                     Dictionary<string, List<string>> unrelated,
                                     bool shuffle, Readout readout = Readout.Winners)
    {
        var text = shuffle ? RedistributeGlobally(sentences, cfg.Seed) : sentences;

        var encoder = new ContextEncoder(cfg);
        Trainer.AccumulateContext(encoder, text);

        using var scope = new ActivationScope(cfg);
        new Trainer(cfg, scope, encoder).Run(text, quiet: true);

        var cascade = new Cascade(cfg, scope);
        var relScores = new List<double>();
        var unrelScores = new List<double>();
        int scored = 0;

        scope.ConsolidateAll();   // recipes authoritative, for the Edge readout

        foreach (var cue in cues)
        {
            var result = cascade.Run(encoder.Encode(cue), learningMode: false);
            var winners = cascade.Winners(result.WinnerCount);
            var scores = cascade.WinnerScores(result.WinnerCount);

            // Post-k-WTA: only ActivationWidth neurons survive selection.
            var active = new Dictionary<uint, float>();
            for (int i = 0; i < winners.Length; i++) active[scope.Pool.VirtualId[winners[i]]] = scores[i];

            // Pre-k-WTA: everything the edges actually delivered.
            var deliveredSpan = cascade.DeliveredDrive;
            var delivered = new float[scope.Pool.Count];
            for (int i = 0; i < delivered.Length && i < deliveredSpan.Length; i++) delivered[i] = deliveredSpan[i];
            var cueMembers = Assembly.Members(encoder.Encode(cue), cfg.BaselineNeuronCount, cfg.AssemblyOverlap);

            double MassOn(string word)
            {
                var members = Assembly.Members(encoder.Encode(word), cfg.BaselineNeuronCount, cfg.AssemblyOverlap);
                double m = 0;

                if (readout == Readout.Edge)
                {
                    var to = members.ToHashSet();
                    foreach (var vid in cueMembers)
                    {
                        if (!scope.Recipes.TryGetValue(vid, out var rec)) continue;
                        for (int i = 0; i < rec.SynapseTargets.Length; i++)
                            if (to.Contains(rec.SynapseTargets[i])) m += rec.SynapseWeights[i];
                    }
                    return m;
                }

                foreach (var vid in members)
                {
                    if (readout == Readout.Winners)
                    {
                        if (active.TryGetValue(vid, out var v)) m += v;
                    }
                    else
                    {
                        int slot = scope.Pool.Find(vid);
                        if (slot >= 0 && slot < delivered.Length) m += delivered[slot];
                    }
                }
                return m;
            }

            foreach (var w in related[cue]) relScores.Add(MassOn(w));
            foreach (var w in unrelated[cue]) unrelScores.Add(MassOn(w));
            scored++;
        }

        return new ArmScore(Harness.Auc(relScores, unrelScores), Harness.DPrime(relScores, unrelScores),
                            Harness.Mean(relScores), Harness.Mean(unrelScores), scored,
                            relScores.Count + unrelScores.Count);
    }
}
