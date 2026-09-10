using GreyMatter.Poc.Storage;
using GreyMatter.Poc.Substrate;

namespace GreyMatter.Poc.Runtime;

/// <summary>
/// Exact A1 synchronous traversal over stable IDs. All arrays have query-contract bounds.
/// Results remain available until the next Run; callers must not retain mutable buffers.
/// No candidate labels, learning, resident slots or silently truncated work.
/// </summary>
public sealed class StoredRelayRecall
{
    private struct Activation { public uint Id; public float Drive; }
    private readonly IRelayRecords _records;
    private readonly int _width, _maxTicks;
    private readonly Activation[] _active, _incoming, _selection;
    private readonly uint[] _deliveredIds;
    private readonly double[] _delivered;
    private readonly byte[] _source = new byte[RelayRecord.Bytes], _target = new byte[RelayRecord.Bytes];
    private readonly SynapseStore _synapses = new(1, RelayRecord.Cap);
    private int _activeCount, _incomingCount, _deliveredCount;
    private readonly AssemblyRelay.Step[] _steps;
    public long ReservedBytes { get; }
    public int StepCount { get; private set; }
    public int DeliveredCount => _deliveredCount;
    public int Truncations => 0; // This mode refuses insufficient budgets; it never drops work.
    public AssemblyRelay.Step Step(int i) => i >= 0 && i < StepCount ? _steps[i] : throw new ArgumentOutOfRangeException(nameof(i));

    public StoredRelayRecall(IRelayRecords records, int width, int maxTicks, long scratchBudgetBytes)
    {
        if (width < 1 || maxTicks < 1) throw new ArgumentOutOfRangeException(nameof(width));
        long active = Math.Max(8L, width), incoming = checked(active * RelayRecord.Cap);
        long delivered = checked(incoming * maxTicks);
        long bytes = checked(4096 + active * 8 + incoming * 16 + delivered * 12 + maxTicks * 128L);
        if (bytes > scratchBudgetBytes || incoming > int.MaxValue || delivered > int.MaxValue)
            throw new ArgumentException($"Exact traversal requires {bytes} reserved scratch bytes; budget is {scratchBudgetBytes}");
        _records = records; _width = width; _maxTicks = maxTicks; ReservedBytes = bytes;
        _active = new Activation[(int)active]; _incoming = new Activation[(int)incoming];
        _selection = new Activation[(int)incoming]; _deliveredIds = new uint[(int)delivered];
        _delivered = new double[(int)delivered]; _steps = new AssemblyRelay.Step[maxTicks];
    }
    public double Value(uint id)
    {
        for (int i = 0; i < _deliveredCount; i++) if (_deliveredIds[i] == id) return _delivered[i];
        return 0;
    }
    private void Add(uint id, float value)
    {
        int i;
        for (i = 0; i < _incomingCount; i++) if (_incoming[i].Id == id) break;
        if (i == _incomingCount)
        {
            if (i == _incoming.Length) throw new InvalidOperationException("Step bound violated");
            _incoming[i] = new() { Id = id }; _incomingCount++;
        }
        _incoming[i].Drive += value; // Same float addition order as A1.
        for (i = 0; i < _deliveredCount; i++) if (_deliveredIds[i] == id) break;
        if (i == _deliveredCount)
        {
            if (i == _delivered.Length) throw new InvalidOperationException("Readout bound violated");
            _deliveredIds[i] = id; _delivered[i] = 0; _deliveredCount++;
        }
        _delivered[i] += value; // A1 accumulates each float contribution into double.
    }
    private static readonly IComparer<Activation> ById = Comparer<Activation>.Create((a,b) => a.Id.CompareTo(b.Id));
    private static readonly IComparer<Activation> ByDrive = Comparer<Activation>.Create((a,b) =>
    { int c = b.Drive.CompareTo(a.Drive); return c != 0 ? c : a.Id.CompareTo(b.Id); });

    public void Run(ReadOnlySpan<uint> roots, int ticks)
    {
        if (ticks < 1 || ticks > _maxTicks || roots.IsEmpty || roots.Length > 8)
            throw new ArgumentOutOfRangeException(nameof(ticks));
        // Validate before touching prior result; actual failed traversal invalidates its result.
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] >= _records.IdLimit) throw new ArgumentOutOfRangeException(nameof(roots));
            for (int j = 0; j < i; j++) if (roots[i] == roots[j]) throw new ArgumentException("Duplicate root");
        }
        StepCount = 0; _deliveredCount = 0; _activeCount = roots.Length;
        for (int i = 0; i < roots.Length; i++) _active[i] = new() { Id = roots[i], Drive = 1 };
        for (int tick = 0; tick < ticks; tick++)
        {
            double inputMass = 0; for (int i = 0; i < _activeCount; i++) inputMass += _active[i].Drive;
            Array.Sort(_active, 0, _activeCount, ById);
            _incomingCount = 0; int emitting = 0;
            for (int i = 0; i < _activeCount; i++)
            {
                var source = _active[i]; if (source.Drive < .5f) continue;
                // Truly unseen cue IDs are the empty baseline, never fabricated edges.
                if (!_records.Read(source.Id, _source)) continue;
                RelayRecord.Decode(source.Id, _source, _synapses);
                int degree = _synapses.Degree[0]; double sum = 0;
                for (int e = 0; e < degree; e++) sum += Math.Max(0, _synapses.Weight[e]);
                if (sum <= 0) continue;
                emitting++;
                for (int e = 0; e < degree; e++)
                {
                    float weight = _synapses.Weight[e]; if (weight <= 0) continue;
                    uint target = _synapses.Target[e];
                    // Load now, even at the last tick. Source adjacency has already been copied,
                    // so a one-record cache can evict that source without corrupting this step.
                    if (target >= _records.IdLimit || !_records.Read(target, _target))
                        throw new InvalidDataException("Learned edge points to missing target record");
                    Add(target, (float)(source.Drive * weight / sum));
                }
            }
            double mass = 0; for (int i = 0; i < _incomingCount; i++) mass += _incoming[i].Drive;
            Array.Copy(_incoming, _selection, _incomingCount);
            Array.Sort(_selection, 0, _incomingCount, ByDrive);
            int winners = Math.Min(_width, _incomingCount);
            _steps[tick] = new(tick + 1, _activeCount, emitting, _incomingCount, winners, inputMass, mass);
            StepCount++;
            Array.Copy(_selection, _active, winners); _activeCount = winners;
        }
    }
}
