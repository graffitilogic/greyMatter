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
        if (SourceLocalForgetting && (DisableDecayForDiagnostic || (_records is DeferredRelayRecords d && d.Epoch != 0)))
            throw new InvalidOperationException("Source-local policy requires a separate model with no global decay history");
        if (current.Length > 8 || current.IsEmpty) throw new ArgumentException("Cohort size");
        foreach (uint id in current) if (id >= _records.IdLimit) throw new ArgumentOutOfRangeException(nameof(current));
        // Preparing all targets, including degree-zero terminals, is persistent numeric state.
        foreach (uint id in current) { Load(id); Save(id); }
        foreach (uint source in _previous)
        {
            Load(source);
            if (SourceLocalForgetting) LocallyPruned += _scratch.DecayUnobserved(0, current);
            foreach (uint target in current)
            { _scratch.RecordCoactivation(0, source, target, .5f, 1f, SynapsePopulation.CrossCue); Updates++; }
            Save(source);
        }
        _previous = current.ToArray(); Observations++;
    }
    public void EndSequence()
    {
        _previous = Array.Empty<uint>(); Episodes++;
        if (!SourceLocalForgetting && !DisableDecayForDiagnostic && Episodes % 500 == 0) Decay();
    }
    public void Decay()
    {
        if (SourceLocalForgetting) throw new InvalidOperationException("Global decay is not part of source-local learning");
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
