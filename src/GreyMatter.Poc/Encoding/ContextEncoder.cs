using GreyMatter.Poc.Substrate;

namespace GreyMatter.Poc.Encoding;

/// <summary>
/// plan.md §4.2 stage 2 — the distributional stage the encoder-ceiling finding demands.
///
/// P0 measured what the surface stage actually is: ~79 of its 128 dimensions are
/// per-word hash spread, so its similarity structure is close to arbitrary with
/// respect to meaning (`if`~`so` at 0.954, `sleep`~`sleeps` at 0.143). This stage
/// is therefore not *refining* a weak-but-real signal; it is supplying the first
/// non-arbitrary signal in the pipeline.
///
/// ── Layout, and why β=0 is exact ────────────────────────────────────────────
/// The blended space is <c>PatternSize</c> (n=2048) dimensions, partitioned:
///
///   dims [0, 128)          (1−β) · surface features
///   dims [128, PatternSize) β     · context accumulation
///
/// A partition rather than a projection, because §4.2 requires that β=0 *exactly*
/// reproduce the null model. Projecting 128 surface dims across all 2048 would
/// change which dimensions win the top-k even at β=0, and the P0 baseline would
/// no longer describe the β=0 arm. With this layout the context half is
/// identically zero at β=0, the surface half is the only thing with magnitude, and
/// the resulting top-32 is bit-identical to what P0 recorded.
///
/// ── Guardrail (§4.3) ────────────────────────────────────────────────────────
/// No vocabulary table. Accumulators are keyed by the 64-bit hash of a word's
/// surface code, never by the word. Nothing here can be serialized into readable
/// text, and the store is bounded by <c>ContextSlots</c> with decay-and-evict.
/// </summary>
public sealed class ContextEncoder
{
    private readonly SurfaceEncoder _surface;
    private readonly int _patternSize;
    private readonly int _surfaceDims;
    private readonly int _contextDims;
    private readonly int _k;
    private readonly float _beta;
    private readonly int _seed;

    /// <summary>Co-occurrence window, ±<c>Window</c> tokens (§4.2: ±2).</summary>
    public const int Window = 2;

    /// <summary>Projection fan-out: how many context dims one neighbour excites.</summary>
    public const int ProjectionFanout = 8;

    private readonly Dictionary<ulong, float[]> _accumulators;
    private readonly Dictionary<ulong, int> _lastSeen;
    private readonly int _contextSlots;
    private int _observations;

    public ContextEncoder(Config cfg, SurfaceEncoder? surface = null, int contextSlots = 50_000)
    {
        _surface = surface ?? new SurfaceEncoder(cfg.SurfaceDimensions);
        _patternSize = cfg.PatternSize;
        _surfaceDims = cfg.SurfaceDimensions;
        _contextDims = cfg.PatternSize - cfg.SurfaceDimensions;
        _k = cfg.Sparsity;
        _beta = (float)cfg.ContextBlend;
        _seed = cfg.Seed;
        _contextSlots = contextSlots;

        if (_contextDims <= 0)
            throw new ArgumentException($"PatternSize ({cfg.PatternSize}) must exceed SurfaceDimensions ({cfg.SurfaceDimensions}).");

        _accumulators = new Dictionary<ulong, float[]>(contextSlots);
        _lastSeen = new Dictionary<ulong, int>(contextSlots);
    }

    public int VocabularyObserved => _accumulators.Count;
    public int Observations => _observations;
    public float Beta => _beta;

    /// <summary>Identity of a word in the store: the hash of its surface code, never the word.</summary>
    public ulong KeyOf(string word) => SurfaceCodeOf(word).Hash();

    private SparseCode SurfaceCodeOf(string word)
    {
        var dense = _surface.Encode(word);
        var f = new float[dense.Length];
        for (int i = 0; i < dense.Length; i++) f[i] = (float)dense[i];
        return SparseCode.TopK(f, _k);
    }

    /// <summary>
    /// Accumulate co-occurrence statistics from one sentence. Called during the
    /// training pass; encoding itself never mutates state.
    /// </summary>
    public void Observe(IReadOnlyList<string> tokens)
    {
        if (tokens.Count < 2) return;

        Span<ulong> keys = tokens.Count <= 64 ? stackalloc ulong[tokens.Count] : new ulong[tokens.Count];
        for (int i = 0; i < tokens.Count; i++) keys[i] = KeyOf(tokens[i]);

        for (int i = 0; i < tokens.Count; i++)
        {
            var acc = AccumulatorFor(keys[i]);

            int lo = Math.Max(0, i - Window);
            int hi = Math.Min(tokens.Count - 1, i + Window);
            for (int j = lo; j <= hi; j++)
            {
                if (j == i) continue;

                // Nearer neighbours carry more weight, and the two directions are
                // kept distinct: "the cat" and "cat the" are different evidence,
                // and collapsing them is what made the legacy graph time-blind.
                int distance = j - i;
                float weight = 1f / Math.Abs(distance);
                bool after = distance > 0;

                Project(acc, keys[j], after, weight);
            }
        }

        _observations++;
        if (_accumulators.Count > _contextSlots) EvictColdest();
    }

    /// <summary>
    /// Scatter a neighbour's identity into the context half. The target dimensions
    /// are a pure function of (neighbour key, direction, tap), so two words with
    /// the same neighbours accumulate into the same dimensions and become similar
    /// — which is the whole mechanism.
    /// </summary>
    private void Project(float[] acc, ulong neighbourKey, bool after, float weight)
    {
        uint id = (uint)(neighbourKey ^ (neighbourKey >> 32));
        var purpose = after ? Rng.Purpose.Projection : Rng.Purpose.Synapse;

        for (uint tap = 0; tap < ProjectionFanout; tap++)
        {
            int dim = _surfaceDims + (int)Rng.NextUInt(_seed, purpose, id, (uint)_contextDims, tap);
            // Signed taps: without them every accumulator drifts positive together
            // and all words converge on the same dense direction.
            float sign = (Rng.Bits(_seed, purpose, id, tap + 1000) & 1) == 0 ? 1f : -1f;
            acc[dim] += sign * weight;
        }
    }

    private float[] AccumulatorFor(ulong key)
    {
        if (!_accumulators.TryGetValue(key, out var acc))
        {
            acc = new float[_patternSize];
            _accumulators[key] = acc;
        }
        _lastSeen[key] = _observations;
        return acc;
    }

    /// <summary>Bounded store: drop the least-recently-observed half of the slots.</summary>
    private void EvictColdest()
    {
        var cutoff = _lastSeen.Values.OrderBy(v => v).ElementAt(_lastSeen.Count / 2);
        foreach (var key in _lastSeen.Where(kv => kv.Value <= cutoff).Select(kv => kv.Key).ToList())
        {
            _accumulators.Remove(key);
            _lastSeen.Remove(key);
        }
    }

    /// <summary>
    /// The blended dense vector for a word. Surface half is always present; the
    /// context half is zero for a word never observed, so rare and unseen words
    /// degrade gracefully to surface-only rather than failing.
    /// </summary>
    public float[] EncodeDense(string word)
    {
        var blended = new float[_patternSize];
        var dense = _surface.Encode(word);

        float[]? acc = null;
        bool hasContext = _beta > 0f
                       && _accumulators.TryGetValue(SurfaceCodeOf(word).Hash(), out acc)
                       && HasMass(acc);

        // §4.2: rare and unseen words "degrade gracefully to surface-only". That
        // means surface at FULL strength, not surface scaled by (1−β).
        //
        // Scaling it was a real defect: at β=1 every context-less word became an
        // all-zero vector, so its top-k fell back to dims 0..31 by index order and
        // every such word produced the SAME code. Measured cost: 1.6% collisions at
        // k=32 and β=1, against a gate requiring 0.0%.
        float surfaceScale = hasContext ? 1f - _beta : 1f;
        for (int i = 0; i < _surfaceDims && i < dense.Length; i++)
            blended[i] = surfaceScale * (float)dense[i];

        if (!hasContext) return blended;

        // L2-normalise the context half before blending, so a word seen 10,000
        // times does not simply outweigh the surface signal by its raw count.
        float inv = (float)(1.0 / Math.Sqrt(Mass(acc!)));
        for (int d = _surfaceDims; d < _patternSize; d++)
            blended[d] = _beta * acc![d] * inv;

        return blended;
    }

    private double Mass(float[] acc)
    {
        double sum = 0;
        for (int d = _surfaceDims; d < _patternSize; d++) sum += (double)acc[d] * acc[d];
        return sum;
    }

    private bool HasMass(float[] acc) => Mass(acc) > 0;

    /// <summary>The k-of-n code for a word. At β=0 this is exactly the surface null model.</summary>
    public SparseCode Encode(string word) => SparseCode.TopK(EncodeDense(word), _k);

    public bool HasContext(string word) => _accumulators.ContainsKey(SurfaceCodeOf(word).Hash());
}
