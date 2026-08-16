namespace GreyMatter.Poc.Eval;

/// <summary>
/// plan.md §6.2 — shared statistics, ported from legacy <c>Program.cs</c>.
/// The rules that surround these numbers (§6.1) matter more than the numbers;
/// they live in <see cref="Verdicts"/>.
/// </summary>
public static class Harness
{
    public static double Cosine(double[] a, double[] b)
    {
        double dot = 0, na = 0, nb = 0;
        for (int i = 0; i < a.Length; i++) { dot += a[i] * b[i]; na += a[i] * a[i]; nb += b[i] * b[i]; }
        return (na <= 0 || nb <= 0) ? 0 : dot / (Math.Sqrt(na) * Math.Sqrt(nb));
    }

    /// <summary>
    /// Rank-based, not Pearson: the quantities compared (activation mass vs corpus
    /// counts) are on wildly different scales and neither is normally distributed.
    /// </summary>
    public static double Spearman(IReadOnlyList<double> a, IReadOnlyList<double> b)
    {
        if (a.Count < 3) return 0;
        var ra = RankOf(a);
        var rb = RankOf(b);
        double ma = Mean(ra), mb = Mean(rb);
        double num = 0, da = 0, db = 0;
        for (int i = 0; i < ra.Count; i++)
        {
            num += (ra[i] - ma) * (rb[i] - mb);
            da += (ra[i] - ma) * (ra[i] - ma);
            db += (rb[i] - mb) * (rb[i] - mb);
        }
        return (da <= 0 || db <= 0) ? 0 : num / Math.Sqrt(da * db);
    }

    /// <summary>Average ranks for ties — corpus counts have many (most bigrams occur once).</summary>
    public static List<double> RankOf(IReadOnlyList<double> v)
    {
        var idx = Enumerable.Range(0, v.Count).OrderBy(i => v[i]).ToList();
        var r = new double[v.Count];
        int p = 0;
        while (p < idx.Count)
        {
            int q = p;
            while (q + 1 < idx.Count && v[idx[q + 1]] == v[idx[p]]) q++;
            double avg = (p + q) / 2.0 + 1;
            for (int k = p; k <= q; k++) r[idx[k]] = avg;
            p = q + 1;
        }
        return r.ToList();
    }

    /// <summary>
    /// Mann–Whitney AUC by exhaustive pair counting, ties at half weight — the
    /// same statistic the legacy fidelity harness reported, so the two are
    /// directly comparable (§1.3).
    /// </summary>
    public static double Auc(IReadOnlyList<double> positive, IReadOnlyList<double> negative)
    {
        long wins = 0, ties = 0, pairs = 0;
        foreach (var t in positive)
            foreach (var c in negative)
            {
                pairs++;
                if (t > c) wins++;
                else if (Math.Abs(t - c) < 1e-9) ties++;
            }
        return pairs > 0 ? (wins + 0.5 * ties) / pairs : 0;
    }

    public static double DPrime(IReadOnlyList<double> positive, IReadOnlyList<double> negative)
    {
        var sd = Math.Sqrt((Variance(positive) + Variance(negative)) / 2);
        return sd > 1e-9 ? (Mean(positive) - Mean(negative)) / sd : 0;
    }

    public static double Mean(IReadOnlyList<double> xs)
    {
        if (xs.Count == 0) return 0;
        double s = 0;
        foreach (var x in xs) s += x;
        return s / xs.Count;
    }

    public static double Variance(IReadOnlyList<double> xs)
    {
        if (xs.Count <= 1) return 0;
        var m = Mean(xs);
        double s = 0;
        foreach (var x in xs) s += (x - m) * (x - m);
        return s / (xs.Count - 1);
    }

    /// <summary>Mean and [min..max] across repeats — the form every reported metric takes (rule 1).</summary>
    public static (double mean, double lo, double hi) Aggregate(IReadOnlyList<double> repeats) =>
        repeats.Count == 0 ? (0, 0, 0) : (Mean(repeats), repeats.Min(), repeats.Max());

    /// <summary>Percentile of an already-sorted list, by nearest-rank.</summary>
    public static double Percentile(IReadOnlyList<double> sorted, double p)
    {
        if (sorted.Count == 0) return 0;
        int i = (int)(sorted.Count * p);
        if (i >= sorted.Count) i = sorted.Count - 1;
        return sorted[i];
    }
}

/// <summary>
/// plan.md §6.1 — the ground rules as executable checks. The legacy harness's
/// greatest strength was refusing verdicts it could not support; these are that
/// refusal, factored out so no experiment can quietly skip one.
/// </summary>
public static class Verdicts
{
    public const int MinRepeatsForVerdict = 5;
    public const double MinOrderSupport = 0.20;

    /// <summary>Rule 1: no verdict from n&lt;5 on any correlation- or AUC-valued metric.</summary>
    public static string? RefuseForRepeats(int repeats) =>
        repeats < MinRepeatsForVerdict
            ? $"INSUFFICIENT REPEATS — {repeats} run(s). Correlation- and AUC-valued metrics need " +
              $"--repeats {MinRepeatsForVerdict} or more before any verdict is meaningful. " +
              "Numbers above are diagnostic only."
            : null;

    /// <summary>Rule 4: refuse order verdicts when too few bigrams were seen more than once.</summary>
    public static string? RefuseForSupport(double supportFraction) =>
        supportFraction < MinOrderSupport
            ? $"INSUFFICIENT SUPPORT — only {supportFraction:P1} of scored bigrams occur more than once " +
              $"(rule 4 floor is {MinOrderSupport:P0}). Correlations against single-observation counts are noise."
            : null;

    /// <summary>Rule 1: separation is claimed only when repeat ranges do not overlap.</summary>
    public static bool RangesSeparated((double mean, double lo, double hi) real,
                                       (double mean, double lo, double hi) nullArm)
        => real.lo > nullArm.hi;
}
