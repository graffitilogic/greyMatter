using GreyMatter.Poc.Encoding;
using Xunit;

namespace GreyMatter.Poc.Tests;

public class SparseCodeTests
{
    [Fact]
    public void TopK_ReturnsSortedDistinctDimensions()
    {
        var code = SparseCode.TopK(new[] { 0.1f, -0.9f, 0.5f, 0.05f, 0.7f }, 3);
        Assert.Equal(new[] { 1, 2, 4 }, code.Dims);
    }

    [Fact]
    public void TopK_BreaksTiesTowardTheLowerIndex()
    {
        // Matches the legacy OrderByDescending(|v|).ThenBy(i) rule the P0 baseline used.
        var code = SparseCode.TopK(new[] { 0.5f, 0.5f, 0.5f, 0.5f }, 2);
        Assert.Equal(new[] { 0, 1 }, code.Dims);
    }

    [Fact]
    public void TopK_RanksByMagnitudeIncludingNegatives()
    {
        var code = SparseCode.TopK(new[] { 0.2f, -0.8f, 0.3f }, 1);
        Assert.Equal(new[] { 1 }, code.Dims);
    }

    [Fact]
    public void TopK_HandlesKLargerThanTheVector()
    {
        Assert.Equal(3, SparseCode.TopK(new[] { 1f, 2f, 3f }, 10).K);
    }

    [Fact]
    public void TopK_AllocatesOnTheHeapForLargeK()
    {
        // k > 128 takes the non-stackalloc path; exercise it so the branch is covered.
        var dense = new float[4096];
        for (int i = 0; i < dense.Length; i++) dense[i] = i;
        var code = SparseCode.TopK(dense, 256);
        Assert.Equal(256, code.K);
        Assert.Equal(4095, code.Dims[^1]);
    }

    [Fact]
    public void Overlap_CountsSharedDimensions()
    {
        var a = new SparseCode(new[] { 1, 3, 5, 7 });
        var b = new SparseCode(new[] { 3, 4, 5, 9 });
        Assert.Equal(2, a.Overlap(b));
        Assert.Equal(2, b.Overlap(a));
    }

    [Fact]
    public void Similarity_IsOneForIdenticalCodesAndZeroForDisjoint()
    {
        var a = new SparseCode(new[] { 1, 2, 3 });
        Assert.Equal(1f, a.Similarity(a));
        Assert.Equal(0f, a.Similarity(new SparseCode(new[] { 4, 5, 6 })));
    }

    [Fact]
    public void Hash_IsStableAndDependsOnTheWholeActiveSet()
    {
        var a = new SparseCode(new[] { 1, 2, 3 });
        Assert.Equal(a.Hash(), new SparseCode(new[] { 1, 2, 3 }).Hash());
        Assert.NotEqual(a.Hash(), new SparseCode(new[] { 1, 2, 4 }).Hash());
    }

    [Fact]
    public void Hash_DoesNotCollideAcrossAThousandDistinctCodes()
    {
        // The hash is the only identifier that reaches disk (§4.3), so collisions
        // are silent data corruption rather than a performance concern.
        var hashes = new HashSet<ulong>();
        for (int i = 0; i < 1000; i++)
            hashes.Add(new SparseCode(new[] { i, i + 1000, i + 2000 }).Hash());
        Assert.Equal(1000, hashes.Count);
    }

    [Fact]
    public void RarityWeighting_DiscountsGenericDimensions()
    {
        // Two pairs with identical raw overlap, but one shares a dimension present
        // in every word and the other shares a rare one. Plain overlap cannot tell
        // them apart; that is the defect §4.2 exists to fix.
        var table = new RarityTable(8);
        for (int i = 0; i < 100; i++) table.Observe(new SparseCode(new[] { 0, 1 })); // dim 0,1 generic
        table.Observe(new SparseCode(new[] { 0, 5 }));                               // dim 5 rare
        table.Observe(new SparseCode(new[] { 0, 6 }));                               // dim 6 rare
        var idf = table.Idf();

        var sharesGeneric = new SparseCode(new[] { 0, 2 }).WeightedSimilarity(new SparseCode(new[] { 0, 3 }), idf);
        var sharesRare = new SparseCode(new[] { 5, 2 }).WeightedSimilarity(new SparseCode(new[] { 5, 3 }), idf);

        Assert.Equal(1, new SparseCode(new[] { 0, 2 }).Overlap(new SparseCode(new[] { 0, 3 })));
        Assert.Equal(1, new SparseCode(new[] { 5, 2 }).Overlap(new SparseCode(new[] { 5, 3 })));
        Assert.True(sharesRare > sharesGeneric,
            $"sharing a rare dim ({sharesRare:F3}) should count for more than a generic one ({sharesGeneric:F3})");
    }

    [Fact]
    public void RarityTable_GivesUnseenDimensionsTheHighestWeight()
    {
        var table = new RarityTable(4);
        for (int i = 0; i < 50; i++) table.Observe(new SparseCode(new[] { 0 }));
        var idf = table.Idf();
        Assert.True(idf[3] > idf[0]);
    }
}
