using GreyMatter.Poc.Eval;
using Xunit;

namespace GreyMatter.Poc.Tests;

public class HarnessTests
{
    [Fact]
    public void Spearman_IsOneForMonotoneAgreement()
    {
        var a = new List<double> { 1, 2, 3, 4, 5 };
        var b = new List<double> { 10, 20, 30, 40, 50 };
        Assert.Equal(1.0, Harness.Spearman(a, b), 9);
    }

    [Fact]
    public void Spearman_IsMinusOneForReversal()
    {
        var a = new List<double> { 1, 2, 3, 4, 5 };
        var b = new List<double> { 50, 40, 30, 20, 10 };
        Assert.Equal(-1.0, Harness.Spearman(a, b), 9);
    }

    [Fact]
    public void Spearman_RefusesFewerThanThreePoints()
    {
        Assert.Equal(0.0, Harness.Spearman(new List<double> { 1, 2 }, new List<double> { 2, 1 }));
    }

    [Fact]
    public void RankOf_AveragesTies()
    {
        // Most corpus bigrams occur exactly once, so tie handling is not a corner case.
        var r = Harness.RankOf(new List<double> { 5, 5, 9 });
        Assert.Equal(1.5, r[0]);
        Assert.Equal(1.5, r[1]);
        Assert.Equal(3.0, r[2]);
    }

    [Fact]
    public void Auc_IsOneWhenFullySeparated()
    {
        Assert.Equal(1.0, Harness.Auc(new[] { 0.9, 0.8 }, new[] { 0.1, 0.2 }));
    }

    [Fact]
    public void Auc_IsHalfWhenAllTied()
    {
        Assert.Equal(0.5, Harness.Auc(new[] { 0.5, 0.5 }, new[] { 0.5, 0.5 }));
    }

    [Fact]
    public void DPrime_IsZeroForIdenticalDistributions()
    {
        var xs = new[] { 0.1, 0.2, 0.3 };
        Assert.Equal(0.0, Harness.DPrime(xs, xs), 9);
    }

    [Fact]
    public void Verdicts_RefuseUnderFiveRepeats()
    {
        Assert.NotNull(Verdicts.RefuseForRepeats(4));
        Assert.Null(Verdicts.RefuseForRepeats(5));
    }

    [Fact]
    public void Verdicts_RefuseUnderTwentyPercentSupport()
    {
        Assert.NotNull(Verdicts.RefuseForSupport(0.19));
        Assert.Null(Verdicts.RefuseForSupport(0.20));
    }

    [Fact]
    public void Verdicts_SeparationRequiresNonOverlappingRanges()
    {
        Assert.True(Verdicts.RangesSeparated((0.5, 0.4, 0.6), (0.1, 0.0, 0.3)));
        Assert.False(Verdicts.RangesSeparated((0.5, 0.2, 0.6), (0.1, 0.0, 0.3)));
    }
}
