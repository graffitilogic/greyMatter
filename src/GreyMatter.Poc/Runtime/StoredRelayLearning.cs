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
    public long Episodes { get; private set; }
    public long Observations { get; private set; }
    public StoredRelayLearning(IRelayRecords records, RelayTrainingState? state = null)
    {
        _records = records; state ??= RelayTrainingState.Empty;
        if (state.Previous.Length > 8 || state.Previous.Any(id => id >= records.IdLimit)) throw new ArgumentException("Invalid cohort");
        _previous = state.Previous.ToArray(); Updates = state.Updates; Episodes = state.Episodes; Observations = state.Observations;
    }
    public RelayTrainingState Capture() => new(Updates, Episodes, Observations, _previous.ToArray());
    private void Load(uint id)
    {
        if (_records.Read(id, _buffer)) RelayRecord.Decode(id, _buffer, _scratch);
        else _scratch.Degree[0] = 0;
    }
    private void Save(uint id) { RelayRecord.Encode(id, _scratch, _buffer); _records.Write(id, _buffer); }
    public void Observe(in SparseCode code) => ObserveMembers(AssemblyRelay.Members(code, checked((int)_records.IdLimit)));
    public void ObserveMembers(ReadOnlySpan<uint> current)
    {
        if (current.Length > 8 || current.IsEmpty) throw new ArgumentException("Cohort size");
        foreach (uint id in current) if (id >= _records.IdLimit) throw new ArgumentOutOfRangeException(nameof(current));
        // Preparing all targets, including degree-zero terminals, is persistent numeric state.
        foreach (uint id in current) { Load(id); Save(id); }
        foreach (uint source in _previous)
        {
            Load(source);
            foreach (uint target in current)
            { _scratch.RecordCoactivation(0, source, target, .5f, 1f, SynapsePopulation.CrossCue); Updates++; }
            Save(source);
        }
        _previous = current.ToArray(); Observations++;
    }
    public void EndSequence()
    {
        _previous = Array.Empty<uint>(); Episodes++;
        if (Episodes % 500 == 0) Decay();
    }
    public void Decay()
    {
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
