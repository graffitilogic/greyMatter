using GreyMatter.Poc.Eval;
using Xunit;

namespace GreyMatter.Poc.Tests;

/// <summary>
/// The sample-composition check exists because four nulls in P7–P9 were built on a
/// different sample than their real arm, and three of them produced a published
/// number before anyone noticed. A check that cannot fail would be the fifth.
/// </summary>
public class SampleCheckTests
{
    [Fact]
    public void ComparableArmsPass()
    {
        Assert.True(SampleCheck.Report(
            new ArmSample("real", 40, 40, 21),
            new ArmSample("null", 38, 40, 21)));
    }

    /// <summary>P8a exactly: 40 pairs over 21 cues against a null of 40 from one cue.</summary>
    [Fact]
    public void P8aUnbalancedNullIsCaught()
    {
        Assert.False(SampleCheck.Report(
            new ArmSample("co-occurring", 40, 40, 21),
            new ArmSample("null", 40, 40, 1)));
    }

    /// <summary>P7.2.5 exactly: same pairs offered, wildly different retention.</summary>
    [Fact]
    public void UnequalRetentionIsCaught()
    {
        Assert.False(SampleCheck.Report(
            new ArmSample("suppressed", 15, 24, 8),
            new ArmSample("unaffected", 5, 24, 8)));
    }

    [Fact]
    public void SingleArmIsAlwaysFine()
    {
        Assert.True(SampleCheck.Report(new ArmSample("only", 10, 10, 5)));
    }

    [Fact]
    public void RetentionIsReportedOnlyWhenItIsBelowOne()
    {
        Assert.Equal("a: 10 from 5 cues", new ArmSample("a", 10, 10, 5).ToString());
        Assert.Contains("50%", new ArmSample("a", 5, 10, 5).ToString());
    }
}
