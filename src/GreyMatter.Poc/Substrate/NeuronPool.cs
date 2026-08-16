namespace GreyMatter.Poc.Substrate;

/// <summary>
/// plan.md §4.1 — structure-of-arrays over the MATERIALIZED neuron set.
///
/// The virtual space is <c>BaselineNeuronCount</c> neurons wide and exists only
/// as an address range; nothing is allocated for it. This pool holds the
/// <c>WorkingSetMax</c> neurons currently resident, and every array below is
/// allocated exactly once at construction. There is no per-neuron heap object,
/// no <c>Guid</c>, and no allocation on any path after the constructor — that is
/// the P1 gate's zero-gen2 requirement and the §7 CUDA translation surface.
///
/// Eviction is LRU by last-active tick, done in BATCHES via a threshold scan
/// rather than by maintaining an intrusive linked list. A linked list would make
/// "touch" a pointer-chasing read-modify-write on the hottest path in the system;
/// a tick store is a single array write that any number of threads can do at
/// once, and the eviction scan is a reduction. §7 asks for exactly that trade.
/// </summary>
public sealed class NeuronPool
{
    public const uint Empty = uint.MaxValue;

    // ── Per-slot state (SoA) ────────────────────────────────────────────────
    public readonly float[] Potential;
    public readonly float[] Threshold;
    public readonly float[] Fatigue;
    public readonly float[] Familiarity;
    public readonly uint[] VirtualId;
    public readonly uint[] LastActiveTick;

    /// <summary>Slots currently holding a neuron, always a prefix [0, Count).</summary>
    public int Count { get; private set; }

    public int Capacity { get; }

    /// <summary>Monotonic logical clock. Advanced by the caller, once per cycle.</summary>
    public uint Tick { get; private set; }

    // ── virtualId → slot, open addressing over flat arrays ──────────────────
    // A Dictionary<uint,int> would work in .NET and translate to nothing on a
    // GPU. Linear-probed arrays translate directly and never allocate.
    private readonly uint[] _hashKey;
    private readonly int[] _hashSlot;
    private readonly int _hashMask;

    // Scratch for batch eviction, allocated once (never in the hot path).
    private readonly uint[] _tickSample;
    private readonly int _evictBatch;

    public long TotalMaterialized { get; private set; }
    public long TotalEvicted { get; private set; }
    public int HighWaterMark { get; private set; }

    public NeuronPool(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        Capacity = capacity;

        Potential = new float[capacity];
        Threshold = new float[capacity];
        Fatigue = new float[capacity];
        Familiarity = new float[capacity];
        VirtualId = new uint[capacity];
        LastActiveTick = new uint[capacity];

        // Load factor ≤ 0.5 keeps linear probing short.
        int hashSize = 1;
        while (hashSize < capacity * 2) hashSize <<= 1;
        _hashKey = new uint[hashSize];
        _hashSlot = new int[hashSize];
        _hashMask = hashSize - 1;
        Array.Fill(_hashKey, Empty);

        // Evict 1/16th of the pool at a time so the O(n) threshold scan amortises
        // to O(16) per evicted neuron instead of running on every single miss.
        _evictBatch = Math.Max(1, capacity / 16);
        _tickSample = new uint[capacity];
    }

    public void AdvanceTick() => Tick++;

    /// <summary>Slot holding this virtual neuron, or −1 if not resident.</summary>
    public int Find(uint virtualId)
    {
        int i = (int)(Rng.Mix(virtualId) & (ulong)_hashMask);
        while (true)
        {
            var k = _hashKey[i];
            if (k == Empty) return -1;
            if (k == virtualId) return _hashSlot[i];
            i = (i + 1) & _hashMask;
        }
    }

    public bool IsResident(uint virtualId) => Find(virtualId) >= 0;

    /// <summary>
    /// Make a virtual neuron resident, returning its slot. Already-resident
    /// neurons are touched and returned unchanged (regeneration is skipped by the
    /// caller on a hit — §4.4 step 3). When the pool is full this evicts a batch
    /// of least-recently-active neurons first, invoking <paramref name="onEvict"/>
    /// for each so the caller can consolidate deviations (§4.4 step 6).
    /// </summary>
    public int Materialize(uint virtualId, Action<int>? onEvict = null)
    {
        int slot = TryMaterialize(virtualId, onEvict);
        if (slot < 0)
            throw new InvalidOperationException(
                $"Cannot materialize: all {Capacity:N0} slots hold neurons active on the current tick. " +
                $"The activation scope exceeds WorkingSetMax ({Capacity:N0}). Raise WorkingSetMax or " +
                "lower ActivationWidth/scope size.");
        return slot;
    }

    /// <summary>
    /// Materialize, or return −1 when the pool is full of neurons that are all
    /// active on the current tick and therefore cannot be evicted.
    ///
    /// Callers that can degrade gracefully (the cascade truncates) should use this
    /// and check for −1, NOT pre-test <c>Count == Capacity</c>. Pre-testing was a
    /// real defect: it refused every materialization once the pool first filled, so
    /// the working set froze at exactly <c>WorkingSetMax</c> neurons for the rest of
    /// the run and the evict/regenerate cycle never ran at all.
    /// </summary>
    public int TryMaterialize(uint virtualId, Action<int>? onEvict = null)
    {
        int existing = Find(virtualId);
        if (existing >= 0) { LastActiveTick[existing] = Tick; return existing; }

        if (Count == Capacity && !EvictBatch(onEvict)) return -1;

        int slot = Count++;
        VirtualId[slot] = virtualId;
        Potential[slot] = 0f;
        Threshold[slot] = 1f;
        Fatigue[slot] = 0f;
        Familiarity[slot] = 0f;
        LastActiveTick[slot] = Tick;
        HashInsert(virtualId, slot);

        TotalMaterialized++;
        if (Count > HighWaterMark) HighWaterMark = Count;
        return slot;
    }

    /// <summary>Record that a slot was active this cycle. One array write.</summary>
    public void Touch(int slot) => LastActiveTick[slot] = Tick;

    /// <summary>
    /// Evict the least-recently-active <c>_evictBatch</c> neurons. Two passes:
    /// find the tick cut-off, then compact. Both are flat scans.
    /// </summary>
    /// <returns>true when at least one slot was freed.</returns>
    private bool EvictBatch(Action<int>? onEvict)
    {
        Array.Copy(LastActiveTick, _tickSample, Count);
        Array.Sort(_tickSample, 0, Count);
        uint cutoff = _tickSample[Math.Min(Count - 1, _evictBatch)];

        // Eviction is `tick <= cutoff` under a budget, not `tick < cutoff`.
        //
        // The strict form looks right and silently evicts NOTHING whenever ticks
        // tie at the cut-off — which is the normal case, since a cycle stamps its
        // whole scope with one tick. A pool holding 1,500 neurons at tick T−1 and
        // 500 at tick T has cutoff = T−1 and no neuron strictly below it.
        //
        // Neurons touched on the CURRENT tick are never evicted. They are the scope
        // being assembled right now; evicting one would hand the caller a slot index
        // that is invalid the moment it is returned.
        int budget = _evictBatch;
        int write = 0;
        for (int read = 0; read < Count; read++)
        {
            bool evict = budget > 0
                      && LastActiveTick[read] <= cutoff
                      && LastActiveTick[read] != Tick;

            if (evict)
            {
                budget--;
                onEvict?.Invoke(read);
                HashRemove(VirtualId[read]);
                TotalEvicted++;
                continue;
            }

            if (write != read)
            {
                Potential[write] = Potential[read];
                Threshold[write] = Threshold[read];
                Fatigue[write] = Fatigue[read];
                Familiarity[write] = Familiarity[read];
                VirtualId[write] = VirtualId[read];
                LastActiveTick[write] = LastActiveTick[read];
                HashUpdateSlot(VirtualId[read], write);
                OnSlotMoved?.Invoke(read, write);
            }
            write++;
        }
        Count = write;

        // Nothing was evictable: every resident neuron belongs to the scope being
        // built this cycle. The caller decides whether that is fatal (Materialize
        // throws) or a graceful truncation (TryMaterialize returns −1).
        return Count < Capacity;
    }

    /// <summary>
    /// Raised when compaction moves a neuron from one slot to another, so
    /// slot-indexed side tables (SynapseStore segments) can follow. Set once at
    /// wiring time; not a hot path.
    /// </summary>
    public Action<int, int>? OnSlotMoved { get; set; }

    public void Clear()
    {
        Count = 0;
        Tick = 0;
        Array.Fill(_hashKey, Empty);
    }

    private void HashInsert(uint key, int slot)
    {
        int i = (int)(Rng.Mix(key) & (ulong)_hashMask);
        while (_hashKey[i] != Empty && _hashKey[i] != key) i = (i + 1) & _hashMask;
        _hashKey[i] = key;
        _hashSlot[i] = slot;
    }

    private void HashUpdateSlot(uint key, int slot)
    {
        int i = (int)(Rng.Mix(key) & (ulong)_hashMask);
        while (_hashKey[i] != key)
        {
            if (_hashKey[i] == Empty) return;
            i = (i + 1) & _hashMask;
        }
        _hashSlot[i] = slot;
    }

    /// <summary>
    /// Backward-shift deletion. Tombstones would be simpler but degrade the table
    /// permanently under the materialize/evict churn this pool is built for.
    /// </summary>
    private void HashRemove(uint key)
    {
        int i = (int)(Rng.Mix(key) & (ulong)_hashMask);
        while (_hashKey[i] != key)
        {
            if (_hashKey[i] == Empty) return;
            i = (i + 1) & _hashMask;
        }

        int hole = i;
        i = (i + 1) & _hashMask;
        while (_hashKey[i] != Empty)
        {
            int home = (int)(Rng.Mix(_hashKey[i]) & (ulong)_hashMask);
            // Does the entry at i belong at or before the hole in probe order?
            bool canMove = hole <= i
                ? (home <= hole || home > i)
                : (home <= hole && home > i);
            if (canMove)
            {
                _hashKey[hole] = _hashKey[i];
                _hashSlot[hole] = _hashSlot[i];
                hole = i;
            }
            i = (i + 1) & _hashMask;
        }
        _hashKey[hole] = Empty;
    }
}
