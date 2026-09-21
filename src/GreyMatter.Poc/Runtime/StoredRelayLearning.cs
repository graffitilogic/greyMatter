using GreyMatter.Poc.Encoding;
using GreyMatter.Poc.Storage;
using GreyMatter.Poc.Substrate;

namespace GreyMatter.Poc.Runtime;

/// <summary>
/// A1 learning at the record boundary. Frozen numeric codes supplied by the caller;
/// no encoder, full recipe dictionary, resident pool or answer vocabulary retained.
/// R3 will provide bounded traversal separately. Default A1 rules only, version 1.
/// </summary>
public sealed class StoredRelayLearning
{
    private readonly IRelayRecords _records;
    private readonly SynapseStore _scratch = new(1, RelayRecord.Cap);
    private readonly byte[] _buffer = new byte[RelayRecord.Bytes];
    private readonly uint[] _sparse = new uint[SparseFanIn];   // fixed scratch for D1 target subsets
    private uint[] _previous;
    public long Updates { get; private set; }
    public long DecayVisits { get; private set; }
    public long DecayElapsedTicks { get; private set; }
    public long DecayRecordTicks { get; private set; }
    private RelayLearningPolicy _policy;
    private readonly bool _policyLocked;
    public RelayLearningPolicy Policy => _policy;
    public bool DisableDecayForDiagnostic { get => _policy == RelayLearningPolicy.NoDecayDiagnostic; init { SelectFlag(RelayLearningPolicy.NoDecayDiagnostic, value); } }
    public bool SourceLocalForgetting { get => _policy == RelayLearningPolicy.SourceLocal; init { SelectFlag(RelayLearningPolicy.SourceLocal, value); } }
    /// <summary>D1: source-local forgetting plus sparse 2-of-8 target connectivity. Explicit policy identity 3.</summary>
    public bool SparseRelayConnectivity { get => _policy == RelayLearningPolicy.SparseRelay; init { SelectFlag(RelayLearningPolicy.SparseRelay, value); } }
    /// <summary>CB: directed counts, least-count displacement, no decay. Explicit policy identity 4.</summary>
    public bool CountBaseline { get => _policy == RelayLearningPolicy.CountBaseline; init { SelectFlag(RelayLearningPolicy.CountBaseline, value); } }
    /// <summary>Policies whose forgetting clock is the source's own observations, never a global schedule.</summary>
    private bool LocalForgetting => _policy is RelayLearningPolicy.SourceLocal or RelayLearningPolicy.SparseRelay;
    /// <summary>Policies that never run the scheduled global decay.</summary>
    private bool NoGlobalDecay => LocalForgetting || CountBaseline;

    /// <summary>Target-cohort members one source member connects to under SparseRelay.</summary>
    public const int SparseFanIn = 2;

    /// <summary>
    /// D1 sparse connectivity: choose <see cref="SparseFanIn"/> distinct members of the
    /// target cohort for this source member, deterministically from (source member ID,
    /// target cohort's first member ID). Every target cohort therefore costs 2 record
    /// slots per source member instead of 8, raising the per-token successor ceiling from
    /// 4 to 16 at unchanged degree cap and record bytes. Nothing in this choice consults
    /// labels, candidates or learned state. Cohorts smaller than the fan-in connect fully.
    /// </summary>
    public static int SparseTargets(uint source, ReadOnlySpan<uint> cohort, Span<uint> chosen)
    {
        if (cohort.IsEmpty) return 0;
        if (cohort.Length <= SparseFanIn) { cohort.CopyTo(chosen); return cohort.Length; }
        ulong h = Rng.Mix(((ulong)source << 32) | cohort[0]);
        int first = (int)(h % (ulong)cohort.Length);
        int second = (int)((h >> 32) % (ulong)(cohort.Length - 1)); if (second >= first) second++;
        chosen[0] = cohort[first]; chosen[1] = cohort[second];
        return SparseFanIn;
    }
    private void SelectFlag(RelayLearningPolicy policy, bool enabled)
    {
        if (enabled) Select(policy);
        else if (_policy == policy) throw new InvalidOperationException("Cannot disable the saved learning policy");
    }
    private void Select(RelayLearningPolicy policy)
    {
        if ((_policyLocked || _policy != RelayLearningPolicy.Global) && policy != _policy)
            throw new InvalidOperationException("Cannot override the saved learning policy");
        _policy = policy;
    }
    public long LocallyPruned { get; private set; }
    public long Episodes { get; private set; }
    public long Observations { get; private set; }
    public StoredRelayLearning(IRelayRecords records, RelayTrainingState? state = null)
    {
        _records = records; _policyLocked = state != null; state ??= RelayTrainingState.Empty;
        if (!Enum.IsDefined(state.Policy)) throw new ArgumentException("Unknown learning policy");
        _policy = state.Policy;
        if (state.Previous.Length > 8 || state.Previous.Any(id => id >= records.IdLimit)) throw new ArgumentException("Invalid cohort");
        _previous = state.Previous.ToArray(); Updates = state.Updates; Episodes = state.Episodes; Observations = state.Observations;
    }
    public RelayTrainingState Capture() => new(Updates, Episodes, Observations, _previous.ToArray(), _policy);
    private void Load(uint id)
    {
        if (_records.Read(id, _buffer)) RelayRecord.Decode(id, _buffer, _scratch);
        else _scratch.Degree[0] = 0;
    }
    private void Save(uint id) { RelayRecord.Encode(id, _scratch, _buffer); _records.Write(id, _buffer); }
    public void Observe(in SparseCode code) => ObserveMembers(AssemblyRelay.Members(code, checked((int)_records.IdLimit)));
    public void ObserveMembers(ReadOnlySpan<uint> current)
    {
        using var attribution = GreyMatter.Poc.Eval.CostProfile.Enter(GreyMatter.Poc.Eval.CostProfile.Kind.Learning);
        if (NoGlobalDecay && (DisableDecayForDiagnostic || (_records is DeferredRelayRecords d && d.Epoch != 0)))
            throw new InvalidOperationException("Source-local policy requires a separate model with no global decay history");
        if (current.Length > 8 || current.IsEmpty) throw new ArgumentException("Cohort size");
        foreach (uint id in current) if (id >= _records.IdLimit) throw new ArgumentOutOfRangeException(nameof(current));
        // Preparing all targets, including degree-zero terminals, is persistent numeric state.
        foreach (uint id in current) { Load(id); Save(id); }
        foreach (uint source in _previous)
        {
            Load(source);
            // Unobserved-target decay compares against the whole observed cohort; under
            // SparseRelay the members this source does not connect to have no edge from
            // it, so the comparison is unchanged by the sparser wiring.
            if (LocalForgetting) LocallyPruned += _scratch.DecayUnobserved(0, current);
            if (CountBaseline) { foreach (uint target in current) { Count(source, target); Updates++; } Save(source); continue; }
            ReadOnlySpan<uint> targets = current;
            if (SparseRelayConnectivity) targets = _sparse.AsSpan(0, SparseTargets(source, current, _sparse));
            foreach (uint target in targets)
            { _scratch.RecordCoactivation(0, source, target, .5f, 1f, SynapsePopulation.CrossCue); Updates++; }
            Save(source);
        }
        _previous = current.ToArray(); Observations++;
    }
    /// <summary>
    /// CB update on the loaded scratch record: existing edge weight += 1 (a count);
    /// otherwise append at 1 while the cap allows; otherwise displace the lowest-count
    /// incumbent only if that count is 1 (lowest index on ties); otherwise decline.
    /// Weights are counts, not learned strengths; recall's w/Σw is then a transition
    /// probability. Registered in RESULTS as a baseline, never as learning.
    /// </summary>
    private void Count(uint source, uint target)
    {
        if (source == target) return;
        int start = _scratch.SegmentStart(0), degree = _scratch.Degree[0];
        for (int i = start; i < start + degree; i++)
            if (_scratch.Target[i] == target) { if (_scratch.Weight[i] < RelayRecord.MaxCount) _scratch.Weight[i] += 1f; return; }
        if (degree < RelayRecord.Cap)
        {
            _scratch.Target[start + degree] = target; _scratch.Weight[start + degree] = 1f;
            _scratch.Population[start + degree] = RelayRecord.CountPopulation; _scratch.Degree[0] = degree + 1; return;
        }
        int weakest = start; for (int i = start + 1; i < start + degree; i++) if (_scratch.Weight[i] < _scratch.Weight[weakest]) weakest = i;
        if (_scratch.Weight[weakest] > 1f) { CountDeclined++; return; }
        _scratch.Target[weakest] = target; _scratch.Weight[weakest] = 1f; _scratch.Population[weakest] = RelayRecord.CountPopulation; CountDisplaced++;
    }
    public long CountDeclined { get; private set; }
    public long CountDisplaced { get; private set; }
    public void EndSequence()
    {
        _previous = Array.Empty<uint>(); Episodes++;
        if (!NoGlobalDecay && !DisableDecayForDiagnostic && Episodes % 500 == 0) Decay();
    }
    public void Decay()
    {
        if (NoGlobalDecay) throw new InvalidOperationException("Global decay is not part of this learning policy");
        long start = System.Diagnostics.Stopwatch.GetTimestamp();
        if (_records is DeferredRelayRecords deferred)
        {
            deferred.AdvanceDecay();
            DecayElapsedTicks += System.Diagnostics.Stopwatch.GetTimestamp() - start;
            return;
        }
        _records.VisitPresent(id =>
        {
            long recordStart = System.Diagnostics.Stopwatch.GetTimestamp();
            if (!_records.Read(id, _buffer)) throw new InvalidDataException("Enumerated record missing");
            RelayRecord.Decode(id, _buffer, _scratch);
            _scratch.ApplyDecay(1, .99f); Save(id); DecayVisits++;
            DecayRecordTicks += System.Diagnostics.Stopwatch.GetTimestamp() - recordStart;
        });
        DecayElapsedTicks += System.Diagnostics.Stopwatch.GetTimestamp() - start;
    }
}
