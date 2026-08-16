using GreyMatter.Poc;
using GreyMatter.Poc.Encoding;
using GreyMatter.Poc.Engrams;
using Xunit;

namespace GreyMatter.Poc.Tests;

public class EngramTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "gm_p3_" + Path.GetRandomFileName());

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    private static VqCodebook Codebook(int seed = 7) => new(size: 32, dim: 16, seed);

    // ── VqCodebook ──────────────────────────────────────────────────────────

    [Fact]
    public void Codebook_IsSeededSoRegenerationIsReproducible()
    {
        // Legacy used Random.Shared here, which made every regenerated weight
        // differ run to run — rule 8's exact failure mode.
        Assert.Equal(Codebook().Export(), Codebook().Export());
        Assert.NotEqual(Codebook(1).Export(), Codebook(2).Export());
    }

    [Fact]
    public void Quantize_IsDeterministicAndDoesNotMutate()
    {
        var cb = Codebook();
        var v = new float[16];
        for (int i = 0; i < 16; i++) v[i] = i * 0.01f;

        var before = cb.Export();
        var a = cb.Quantize(v);
        var b = cb.Quantize(v);

        Assert.Equal(a, b);
        Assert.Equal(before, cb.Export());
    }

    [Fact]
    public void QuantizeAndLearn_MovesTheCodebook()
    {
        var cb = Codebook();
        var v = new float[16];
        for (int i = 0; i < 16; i++) v[i] = 0.5f;

        var before = cb.Export();
        for (int i = 0; i < 50; i++) cb.QuantizeAndLearn(v);
        Assert.NotEqual(before, cb.Export());
    }

    [Fact]
    public void Quantize_RejectsWrongDimensionality()
    {
        Assert.Throws<ArgumentException>(() => Codebook().Quantize(new float[3]));
    }

    // ── Regeneration ────────────────────────────────────────────────────────

    [Fact]
    public void Regeneration_IsDeterministic()
    {
        var cb = Codebook();
        var recipe = new NeuronRecipe(12345, 7, 99);
        var a = new float[16];
        var b = new float[16];
        Regeneration.Regenerate(recipe, cb, a);
        Regeneration.Regenerate(recipe, cb, b);
        Assert.Equal(a, b);
    }

    [Fact]
    public void NeuronsSharingAVqCodeAreNotClones()
    {
        // The legacy P1.6n failure mode: identical prototypes make every neuron
        // with a given code indistinguishable.
        var cb = Codebook();
        var a = new float[16];
        var b = new float[16];
        Regeneration.Regenerate(new NeuronRecipe(1, 5, 0), cb, a);
        Regeneration.Regenerate(new NeuronRecipe(2, 5, 0), cb, b);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void ConsolidateThenRegenerate_ReproducesWeightsWithinThreshold()
    {
        var cb = Codebook();
        var recipe = new NeuronRecipe(42, 3, 0);
        var learned = new float[16];
        Regeneration.Regenerate(recipe, cb, learned);

        for (int d = 0; d < learned.Length; d++)
            if (learned[d] != 0f) learned[d] += (d % 5) - 2;   // −2..+2 drift

        Regeneration.Consolidate(recipe, cb, learned, threshold: 1.0f);

        var regenerated = new float[16];
        Regeneration.Regenerate(recipe, cb, regenerated);
        for (int d = 0; d < learned.Length; d++)
            Assert.True(Math.Abs(learned[d] - regenerated[d]) <= 1.0f + 1e-5f,
                $"dim {d}: learned {learned[d]}, regenerated {regenerated[d]}");
    }

    [Fact]
    public void AnUnchangedNeuronCostsZeroDeviations()
    {
        // The thesis: persisted size is a function of how much a neuron learned.
        var cb = Codebook();
        var recipe = new NeuronRecipe(7, 2, 0);
        var weights = new float[16];
        Regeneration.Regenerate(recipe, cb, weights);
        Regeneration.Consolidate(recipe, cb, weights, threshold: 1.0f);
        Assert.Equal(0, recipe.DeviationCount);
    }

    /// <summary>
    /// The defect the P3 gate's roundtrip check caught: a recipe is only valid
    /// relative to the codebook version it was consolidated against. Letting the
    /// codebook keep learning afterwards silently invalidates stored deviations —
    /// measured at 2,555,679 violations out of 12.8M weights, max error 49.7
    /// against a threshold of 1.0.
    /// </summary>
    [Fact]
    public void AMovingCodebookInvalidatesAlreadyConsolidatedRecipes()
    {
        var cb = Codebook();
        var recipe = new NeuronRecipe(42, 3, 0);
        var learned = new float[16];
        Regeneration.Regenerate(recipe, cb, learned);
        for (int d = 0; d < learned.Length; d++) if (learned[d] != 0f) learned[d] += 2f;
        Regeneration.Consolidate(recipe, cb, learned, threshold: 1.0f);

        // Move THIS recipe's prototype after consolidation, as online training
        // would once some other neuron quantises to the same code.
        var moved = cb.Export();
        for (int d = 0; d < 16; d++) moved[recipe.VqCode * 16 + d] += 0.5f;
        cb.Import(moved);

        var regenerated = new float[16];
        Regeneration.Regenerate(recipe, cb, regenerated);

        double maxErr = 0;
        for (int d = 0; d < learned.Length; d++)
            maxErr = Math.Max(maxErr, Math.Abs(learned[d] - regenerated[d]));

        Assert.True(maxErr > 1.0f,
            "expected a moving codebook to break fidelity; if this stops failing, the " +
            "freeze-per-checkpoint requirement is no longer load-bearing and P3's finding is stale");
    }

    // ── Partition roundtrip ─────────────────────────────────────────────────

    [Fact]
    public void PartitionRoundtrip_PreservesEveryField()
    {
        var recipes = new List<NeuronRecipe>
        {
            new(1, 10, 100) { Familiarity = 0.5f, ActivationCount = 7,
                              DeviationDims = new ushort[] { 2, 5 }, DeviationDeltas = new[] { 1.5f, -2.5f } },
            new(2, 20, 200) { Familiarity = 0.25f, ActivationCount = 3 },
            new(3, 30, 300) { Familiarity = 0f, ActivationCount = 0,
                              DeviationDims = new ushort[] { 0 }, DeviationDeltas = new[] { 9.5f } }
        };

        var store = new EngramStore(_dir);
        store.Save(EngramPartition.FromRecipes(77, recipes));
        var loaded = store.Load(77)!;

        Assert.Equal(3, loaded.RecipeCount);
        var back = loaded.Recipes().ToList();
        for (int i = 0; i < 3; i++)
        {
            Assert.Equal(recipes[i].Id, back[i].Id);
            Assert.Equal(recipes[i].VqCode, back[i].VqCode);
            Assert.Equal(recipes[i].Seed, back[i].Seed);
            Assert.Equal(recipes[i].Familiarity, back[i].Familiarity);
            Assert.Equal(recipes[i].ActivationCount, back[i].ActivationCount);
            Assert.Equal(recipes[i].DeviationDims, back[i].DeviationDims);
            Assert.Equal(recipes[i].DeviationDeltas, back[i].DeviationDeltas);
        }
    }

    [Fact]
    public void Load_ReturnsNullForAnAbsentPartition()
    {
        Assert.Null(new EngramStore(_dir).Load(12345));
    }

    [Fact]
    public void Save_LeavesNoTempFileBehind()
    {
        var store = new EngramStore(_dir);
        store.Save(EngramPartition.FromRecipes(1, new List<NeuronRecipe> { new(1, 1, 1) }));
        Assert.Empty(Directory.GetFiles(_dir, "*.tmp"));
    }

    [Fact]
    public void Append_ReplacesExistingRecipesByIdAndKeepsTheRest()
    {
        var store = new EngramStore(_dir);
        store.Append(5, new List<NeuronRecipe> { new(1, 1, 1), new(2, 2, 2) });
        store.Append(5, new List<NeuronRecipe> { new(2, 99, 99), new(3, 3, 3) });

        var loaded = store.Load(5)!.Recipes().ToList();
        Assert.Equal(3, loaded.Count);
        Assert.Equal(99, loaded.Single(r => r.Id == 2).VqCode);
        Assert.Equal(1, loaded.Single(r => r.Id == 1).VqCode);
    }

    [Fact]
    public void AssemblyMembers_SurviveDeltaEncoding()
    {
        var p = new EngramPartition { Bucket = 1 };
        var members = new uint[] { 5, 900_000, 12, 3, 700 };
        p.SetAssemblies(new List<(ulong, uint[])> { (0xABCDEF, members) });

        Assert.Equal(new uint[] { 3, 5, 12, 700, 900_000 }, p.AssemblyMembersAt(0));
    }

    [Fact]
    public void AssemblyMembers_SurviveASaveLoadRoundtrip()
    {
        var p = EngramPartition.FromRecipes(9, new List<NeuronRecipe> { new(1, 1, 1) });
        p.SetAssemblies(new List<(ulong, uint[])> { (11UL, new uint[] { 4, 1, 9 }), (22UL, new uint[] { 7 }) });

        var store = new EngramStore(_dir);
        store.Save(p);
        var loaded = store.Load(9)!;

        Assert.Equal(new uint[] { 1, 4, 9 }, loaded.AssemblyMembersAt(0));
        Assert.Equal(new uint[] { 7 }, loaded.AssemblyMembersAt(1));
    }

    // ── LSH ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Lsh_IsDeterministicForTheSameCode()
    {
        var lsh = new LshIndex(seed: 3);
        var code = new SparseCode(new[] { 1, 5, 9, 20 });
        Assert.Equal(lsh.BucketsFor(code), lsh.BucketsFor(code));
    }

    [Fact]
    public void Lsh_RecallsPlantedNeighbours()
    {
        // The property that makes it a lookup scheme rather than a hash table:
        // a code sharing most dims with a stored one must retrieve it.
        var lsh = new LshIndex(seed: 3, bands: 24, rowsPerBand: 2);
        var stored = new SparseCode(Enumerable.Range(0, 32).ToArray());
        lsh.Add(stored, assemblyHash: 4242);

        var neighbour = new SparseCode(Enumerable.Range(0, 32).Select(i => i == 31 ? 100 : i).ToArray());
        Assert.Contains(4242UL, lsh.Candidates(neighbour));
    }

    [Fact]
    public void Lsh_LargelyMissesUnrelatedCodes()
    {
        var lsh = new LshIndex(seed: 3, bands: 8, rowsPerBand: 4);
        lsh.Add(new SparseCode(Enumerable.Range(0, 32).ToArray()), 4242);

        var unrelated = new SparseCode(Enumerable.Range(1000, 32).ToArray());
        Assert.DoesNotContain(4242UL, lsh.Candidates(unrelated));
    }

    [Fact]
    public void Lsh_EntriesRoundtripThroughLoad()
    {
        var a = new LshIndex(seed: 3);
        var code = new SparseCode(new[] { 2, 4, 8 });
        a.Add(code, 555);

        var b = new LshIndex(seed: 3);
        b.Load(a.Entries());
        Assert.Contains(555UL, b.Candidates(code));
    }

    // ── Guardrail ───────────────────────────────────────────────────────────

    [Fact]
    public void Audit_FindsNoStringsInAWrittenPartition()
    {
        var store = new EngramStore(_dir);
        store.Save(EngramPartition.FromRecipes(1, new List<NeuronRecipe>
        {
            new(1, 1, 1) { DeviationDims = new ushort[] { 70, 73, 76, 77 },   // spells "FILM" as bytes
                           DeviationDeltas = new[] { 1f, 2f, 3f, 4f } }
        }));

        var report = StoreAudit.Scan(store, new HashSet<string> { "film", "lost", "know" });
        Assert.Equal(0, report.StringTokens);
        Assert.Equal(0, report.CorpusWordHits);
        Assert.True(report.Clean);
        Assert.True(report.AscendingExcluded >= 1, "the sorted-dim alias should have been recognised");
    }

    /// <summary>
    /// The audit must be able to FAIL, or it certifies nothing. Legacy wrote
    /// concept strings into partitions (§1.5); this is that scenario.
    /// </summary>
    /// <summary>
    /// The semantic check must catch text hand-packed through a NON-string encoding
    /// — which is exactly what the legacy tree's ConceptTag and string concept index
    /// would look like if someone "fixed" the guardrail by byte-packing them.
    /// Without this test, raising MinSemanticLength to suppress chance matches could
    /// silently disable the check.
    /// </summary>
    [Fact]
    public void Audit_CatchesAWordListPackedAsRawBytesRatherThanStrings()
    {
        var store = new EngramStore(_dir);
        var words = new[] { "elephant", "kitchen", "morning", "brother", "picture" };

        // Packed as byte arrays inside a MessagePack bin, so no str token exists.
        var packed = System.Text.Encoding.ASCII.GetBytes(string.Join('\0', words));
        var payload = MessagePack.MessagePackSerializer.Serialize(new object[] { 1, packed, 2.5 });

        using (var fs = new FileStream(store.PathFor(3), FileMode.Create))
        using (var gz = new System.IO.Compression.GZipStream(fs, System.IO.Compression.CompressionLevel.Optimal))
            gz.Write(payload, 0, payload.Length);

        var report = StoreAudit.Scan(store, words.ToHashSet());

        Assert.Equal(0, report.StringTokens);          // genuinely no strings…
        Assert.True(report.CorpusWordHits >= 3,        // …but the words are still there
            $"semantic check missed a packed word list: {report.CorpusWordHits} hits");
        Assert.False(report.Clean);
    }

    [Fact]
    public void Audit_CatchesAStringSmuggledIntoAPartitionFile()
    {
        var store = new EngramStore(_dir);
        var path = store.PathFor(2);
        var payload = MessagePack.MessagePackSerializer.Serialize(
            new object[] { 1, "elephant", 3.5 });

        using (var fs = new FileStream(path, FileMode.Create))
        using (var gz = new System.IO.Compression.GZipStream(fs, System.IO.Compression.CompressionLevel.Optimal))
            gz.Write(payload, 0, payload.Length);

        var report = StoreAudit.Scan(store);
        Assert.False(report.Clean);
        Assert.Contains(report.Findings, f => f.Text == "elephant");
    }
}
