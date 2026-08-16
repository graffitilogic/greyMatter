using GreyMatter.Poc.Encoding;
using GreyMatter.Poc.Eval;
using Xunit;

namespace GreyMatter.Poc.Tests;

public class SurfaceEncoderTests
{
    private readonly SurfaceEncoder _enc = new();

    [Fact]
    public void Encode_IsDeterministicAcrossInstances()
    {
        var a = new SurfaceEncoder().Encode("sleep");
        var b = new SurfaceEncoder().Encode("sleep");
        Assert.Equal(a, b);
    }

    [Fact]
    public void Encode_IsCaseInsensitive()
    {
        Assert.Equal(_enc.Encode("Water"), _enc.Encode("water"));
    }

    [Fact]
    public void Encode_ReturnsUnitVector()
    {
        var v = _enc.Encode("people");
        var mag = Math.Sqrt(v.Sum(x => x * x));
        Assert.Equal(1.0, mag, 9);
    }

    [Fact]
    public void Encode_EmptyInputIsAllZeroNotACrash()
    {
        Assert.All(_enc.Encode("   "), x => Assert.Equal(0.0, x));
    }

    [Fact]
    public void StableHash_MatchesLegacyConstants()
    {
        // hash = 17; hash = hash*31 + c  ⇒  "a" = 17*31 + 97 = 624
        Assert.Equal(624, SurfaceEncoder.StableHash("a"));
        Assert.Equal(624 * 31 + 98, SurfaceEncoder.StableHash("ab"));
    }

    [Fact]
    public void TopKDims_IsSortedAndCorrectlySized()
    {
        var v = _enc.Encode("think");
        var dims = EncoderCeiling.TopKDims(v, 32);
        Assert.Equal(32, dims.Length);
        Assert.Equal(dims.OrderBy(d => d), dims);
        Assert.Equal(dims.Distinct().Count(), dims.Length);
    }

    [Fact]
    public void TopKDims_PicksLargestMagnitudesIncludingNegatives()
    {
        var v = new double[] { 0.1, -0.9, 0.2, 0.05 };
        Assert.Equal(new[] { 1, 2 }, EncoderCeiling.TopKDims(v, 2));
    }

    /// <summary>
    /// Pins the §1.3 defect as MEASURED, and it is not the defect the plan assumed.
    ///
    /// The natural reading of "surface-form encoder" is that morphological relatives
    /// collide. They do not. What actually governs similarity here is hash noise:
    /// three quarters of every 32-dim section is per-word hash spread over
    /// [-0.5, 0.5], which contributes zero-mean noise of large variance to every dot
    /// product, while a handful of structural dims (length, vowel ratio, syllables)
    /// contribute a shared positive offset.
    ///
    /// The consequence is that cosine similarity is close to ARBITRARY with respect
    /// to meaning or morphology — the corpus's most-confused pair is `if`~`so`
    /// (0.954), two unrelated function words, while `sleep`~`sleeps` sits at 0.143
    /// and `the`~`teh`, an exact anagram, at −0.024.
    ///
    /// These constants ARE the P0 baseline. If any of them moves, the surface stage
    /// has changed and every lift measured against the recorded ceiling is void.
    /// </summary>
    [Theory]
    [InlineData("sleep", "sleeps", 0.143)]   // morphological relatives: well separated
    [InlineData("sleep", "sleeping", 0.436)]
    [InlineData("want", "wants", 0.938)]     // …but this relative pair is badly confused
    [InlineData("if", "so", 0.954)]          // unrelated, and the most confused pair in corpus
    [InlineData("had", "look", 0.949)]       // unrelated
    [InlineData("the", "teh", -0.024)]       // exact anagram: no shared representation at all
    public void SurfaceSimilarityIsGovernedByHashNoiseNotByForm(string a, string b, double expected)
    {
        Assert.Equal(expected, Harness.Cosine(_enc.Encode(a), _enc.Encode(b)), 3);
    }

    /// <summary>
    /// The top-k code inherits the same arbitrariness: `if`~`so` are the nearest pair
    /// in dense space yet share only 13 of 32 dims, while `want`~`wants` share 29.
    /// Overlap and cosine rank pairs differently, so the sparse code is not merely a
    /// compression of the dense vector — which is why §4.2 must weight dims by rarity
    /// rather than by magnitude.
    /// </summary>
    [Theory]
    [InlineData("if", "so", 13)]
    [InlineData("want", "wants", 29)]
    [InlineData("sleep", "sleeps", 16)]
    [InlineData("the", "teh", 2)]
    public void TopKOverlapRanksPairsDifferentlyFromCosine(string a, string b, int expectedOverlap)
    {
        var overlap = EncoderCeiling.TopKDims(_enc.Encode(a), 32)
            .Intersect(EncoderCeiling.TopKDims(_enc.Encode(b), 32)).Count();
        Assert.Equal(expectedOverlap, overlap);
    }
}
