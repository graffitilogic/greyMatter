using GreyMatter.Poc;
using GreyMatter.Poc.Encoding;
using GreyMatter.Poc.Eval;
using GreyMatter.Poc.Pipeline;
using Xunit;

namespace GreyMatter.Poc.Tests;

public class ContextEncoderTests
{
    private static Config Cfg(double beta) => new() { ContextBlend = beta, Seed = 12345 };

    private static void Train(ContextEncoder enc, int passes = 3)
    {
        for (int p = 0; p < passes; p++)
            foreach (var s in LocalSample.Sentences)
                enc.Observe(Corpus.Tokenize(s));
    }

    /// <summary>
    /// §4.2's hard requirement: β=0 must EXACTLY reproduce the null model.
    /// Without this the P0 baseline no longer describes the β=0 arm and every
    /// lift measured against it is void.
    /// </summary>
    [Fact]
    public void BetaZeroExactlyReproducesTheSurfaceNullModel()
    {
        var surface = new SurfaceEncoder();
        var enc = new ContextEncoder(Cfg(0.0), surface);
        Train(enc);   // accumulate context that β=0 must completely ignore

        foreach (var word in new[] { "the", "water", "sleep", "people", "qwertyuiop", "blorp" })
        {
            var viaContext = enc.Encode(word).Dims;
            var viaSurface = EncoderCeiling.TopKDims(surface.Encode(word), 32);
            Assert.Equal(viaSurface, viaContext);
        }
    }

    [Fact]
    public void BetaZeroLeavesTheContextHalfEmpty()
    {
        var enc = new ContextEncoder(Cfg(0.0));
        Train(enc);
        var dense = enc.EncodeDense("water");
        for (int d = 128; d < dense.Length; d++) Assert.Equal(0f, dense[d]);
    }

    [Fact]
    public void UnseenWordsDegradeToSurfaceOnlyRatherThanFailing()
    {
        var enc = new ContextEncoder(Cfg(0.5));
        Train(enc);

        Assert.False(enc.HasContext("zzzqqqxxx"));
        var code = enc.Encode("zzzqqqxxx");
        Assert.Equal(32, code.K);

        // With no context accumulated, the code is the surface code.
        var nullEnc = new ContextEncoder(Cfg(0.0));
        Assert.Equal(nullEnc.Encode("zzzqqqxxx").Dims, code.Dims);
    }

    [Fact]
    public void EmptyAndSingleTokenInputAreHandled()
    {
        var enc = new ContextEncoder(Cfg(0.5));
        enc.Observe(Array.Empty<string>());
        enc.Observe(new[] { "lonely" });
        Assert.Equal(0, enc.VocabularyObserved);
    }

    [Fact]
    public void EncodingIsDeterministicAcrossIdenticallyTrainedEncoders()
    {
        var a = new ContextEncoder(Cfg(0.5));
        var b = new ContextEncoder(Cfg(0.5));
        Train(a);
        Train(b);
        Assert.Equal(a.Encode("water").Dims, b.Encode("water").Dims);
    }

    [Fact]
    public void EncodingDoesNotMutateAccumulatedState()
    {
        var enc = new ContextEncoder(Cfg(0.5));
        Train(enc);
        var before = enc.Encode("water").Dims;
        for (int i = 0; i < 50; i++) _ = enc.Encode("water");
        Assert.Equal(before, enc.Encode("water").Dims);
    }

    [Fact]
    public void ContextAccumulationChangesTheCode()
    {
        // If β>0 produced the same code as β=0, the stage would be inert.
        var trained = new ContextEncoder(Cfg(0.5));
        Train(trained);
        var nullEnc = new ContextEncoder(Cfg(0.0));

        Assert.NotEqual(nullEnc.Encode("water").Dims, trained.Encode("water").Dims);
    }

    [Fact]
    public void WordsSharingContextsBecomeMoreSimilarThanUnrelatedOnes()
    {
        // The mechanism, stated as a test: `water` and `time` both appear as the
        // object of similar frames in the sample; `water` and `qwertyuiop` share
        // nothing because the latter never occurs at all.
        var enc = new ContextEncoder(Cfg(1.0));
        Train(enc, passes: 5);

        var water = enc.Encode("water");
        var related = water.Similarity(enc.Encode("time"));
        var unrelated = water.Similarity(enc.Encode("qwertyuiop"));
        Assert.True(related > unrelated,
            $"context-sharing words ({related:F3}) should beat an unseen control ({unrelated:F3})");
    }

    [Fact]
    public void KeysAreCodeHashesSoNoWordEverReachesTheStore()
    {
        // §4.3 guardrail: identity is a hash, not a string.
        var enc = new ContextEncoder(Cfg(0.5));
        var key = enc.KeyOf("water");
        Assert.Equal(key, enc.KeyOf("Water"));      // normalised by the surface stage
        Assert.NotEqual(key, enc.KeyOf("time"));
    }

    [Fact]
    public void ContextStoreIsBounded()
    {
        var enc = new ContextEncoder(Cfg(0.5), contextSlots: 40);
        for (int i = 0; i < 400; i++)
            enc.Observe(new[] { $"w{i}a", $"w{i}b", $"w{i}c" });
        Assert.True(enc.VocabularyObserved <= 80,
            $"store should stay bounded, held {enc.VocabularyObserved}");
    }

    [Fact]
    public void PatternSizeMustExceedSurfaceDimensions()
    {
        var cfg = new Config { PatternSize = 64, SurfaceDimensions = 128 };
        Assert.Throws<ArgumentException>(() => new ContextEncoder(cfg));
    }

    [Fact]
    public void CodeSizeIsAlwaysTheConfiguredSparsity()
    {
        var enc = new ContextEncoder(Cfg(0.5));
        Train(enc);
        foreach (var w in new[] { "the", "water", "unseen-token", "a" })
            Assert.Equal(32, enc.Encode(w).K);
    }
}
