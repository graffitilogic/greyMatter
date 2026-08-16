using GreyMatter.Poc.Encoding;
using GreyMatter.Poc.Engrams;
using GreyMatter.Poc.Substrate;

namespace GreyMatter.Poc.Runtime;

/// <summary>
/// plan.md §4.4 — the materialize/evict lifecycle, and the object that owns the
/// substrate for a run.
///
/// The whole point of the project lives here: a virtual space of
/// <c>BaselineNeuronCount</c> neurons, of which only <c>WorkingSetMax</c> are ever
/// real. A cue regenerates its scope from recipes, the scope runs, and eviction
/// consolidates whatever learning moved back into deviations. Neurons that did not
/// change cost zero bytes and zero writes.
/// </summary>
public sealed class ActivationScope : IDisposable
{
    private readonly Config _cfg;
    private readonly NeuronPool _pool;
    private readonly SynapseStore _synapses;
    private readonly VqCodebook _codebook;

    /// <summary>
    /// Short-term weight state for resident neurons: [slot * dim + d]. Allocated
    /// once. This is the STM half of §4.4 step 5 — deltas accumulate here and
    /// consolidate into recipe deviations on eviction, so the store is written
    /// once per eviction rather than once per learning event.
    /// </summary>
    private readonly float[] _stm;

    private readonly bool[] _dirty;
    private readonly Dictionary<uint, NeuronRecipe> _recipes = new();
    private readonly float[] _regenScratch;
    private readonly Action<int> _onEvict;

    public NeuronPool Pool => _pool;
    public SynapseStore Synapses => _synapses;
    public VqCodebook Codebook => _codebook;
    public int Dim { get; }

    public long Consolidations { get; private set; }
    public long DeviationsWritten { get; private set; }

    public ActivationScope(Config cfg, VqCodebook? codebook = null)
    {
        _cfg = cfg;
        Dim = cfg.SurfaceDimensions;
        _pool = new NeuronPool(cfg.WorkingSetMax);
        _synapses = new SynapseStore(cfg.WorkingSetMax, cfg.SynapseCapPerNeuron);
        _codebook = codebook ?? new VqCodebook(cfg.VqCodebookSize, cfg.SurfaceDimensions, cfg.Seed);

        _stm = new float[(long)cfg.WorkingSetMax * Dim <= int.MaxValue
            ? cfg.WorkingSetMax * Dim
            : throw new ArgumentOutOfRangeException(nameof(cfg), "WorkingSetMax × SurfaceDimensions overflows")];
        _dirty = new bool[cfg.WorkingSetMax];
        _regenScratch = new float[Dim];

        // Bound once — a lambda at the call site would allocate per materialization.
        _onEvict = EvictSlot;

        // Slot moves must carry BOTH the synapse segment and the STM row, or a
        // compaction silently reassigns one neuron's learned weights to another.
        _pool.OnSlotMoved = MoveSlot;
    }

    private void MoveSlot(int from, int to)
    {
        _synapses.MoveSlot(from, to);
        Array.Copy(_stm, (long)from * Dim, _stm, (long)to * Dim, Dim);
        _dirty[to] = _dirty[from];
        _dirty[from] = false;
    }

    public void AdvanceTick() => _pool.AdvanceTick();

    /// <summary>
    /// Make a virtual neuron resident, regenerating its receptive field from its
    /// recipe. Already-resident neurons are touched, not regenerated (§4.4 step 3).
    /// </summary>
    public int Materialize(uint virtualId)
    {
        int existing = _pool.Find(virtualId);
        if (existing >= 0) { _pool.Touch(existing); return existing; }

        int slot = _pool.Materialize(virtualId, _onEvict);

        var recipe = RecipeFor(virtualId);
        Regeneration.Regenerate(recipe, _codebook, _regenScratch);
        Array.Copy(_regenScratch, 0, _stm, (long)slot * Dim, Dim);
        _dirty[slot] = false;

        _pool.Familiarity[slot] = recipe.Familiarity;
        _pool.Threshold[slot] = 1f;
        _pool.Potential[slot] = 0f;
        _pool.Fatigue[slot] = 0f;
        return slot;
    }

    /// <summary>
    /// A neuron's recipe. Absent ones are BORN, not an error: a virtual space
    /// larger than RAM means most neurons have never been seen, and their
    /// prototype is a deterministic function of their id.
    /// </summary>
    public NeuronRecipe RecipeFor(uint virtualId)
    {
        if (_recipes.TryGetValue(virtualId, out var r)) return r;

        // Code from identity so an unvisited neuron still has a prototype, and the
        // same neuron always gets the same one.
        var code = (ushort)Rng.NextUInt(_cfg.Seed, Rng.Purpose.NeuronSeed, virtualId, (uint)_cfg.VqCodebookSize);
        r = new NeuronRecipe(virtualId, code, (uint)Rng.Bits(_cfg.Seed, Rng.Purpose.NeuronSeed, virtualId));
        _recipes[virtualId] = r;
        return r;
    }

    public Span<float> Weights(int slot) => _stm.AsSpan(slot * Dim, Dim);

    public void MarkDirty(int slot) => _dirty[slot] = true;

    /// <summary>
    /// §4.4 step 6 — diff a slot against what regeneration would produce and write
    /// only above-threshold deviations back to its recipe. Clean neurons cost
    /// nothing.
    /// </summary>
    private void EvictSlot(int slot)
    {
        ConsolidateSlot(slot);

        // Releasing the synapse segment belongs to EVICTION, not to consolidation.
        // Doing it inside ConsolidateSlot meant ConsolidateAll — which runs at every
        // checkpoint and at shutdown, on RESIDENT neurons — wiped the entire graph:
        // 241M Hebbian updates produced a store reporting 0 synapses.
        _synapses.ClearSlot(slot);
    }

    private void ConsolidateSlot(int slot)
    {
        if (!_dirty[slot]) return;

        var recipe = RecipeFor(_pool.VirtualId[slot]);
        recipe.Familiarity = _pool.Familiarity[slot];

        var learned = new float[Dim];
        _stm.AsSpan(slot * Dim, Dim).CopyTo(learned);
        Regeneration.Consolidate(recipe, _codebook, learned, (float)_cfg.DeviationThreshold);

        Consolidations++;
        DeviationsWritten += recipe.DeviationCount;
        _dirty[slot] = false;
    }

    /// <summary>Consolidate every resident neuron — called at checkpoint and shutdown.</summary>
    public void ConsolidateAll()
    {
        for (int slot = 0; slot < _pool.Count; slot++) ConsolidateSlot(slot);
    }

    public IReadOnlyDictionary<uint, NeuronRecipe> Recipes => _recipes;

    /// <summary>Recipes carrying at least one deviation — the only ones worth persisting.</summary>
    public IEnumerable<NeuronRecipe> DirtyRecipes() => _recipes.Values.Where(r => r.DeviationCount > 0);

    public void Dispose() => ConsolidateAll();
}
