using Microsoft.Win32.SafeHandles;

namespace GreyMatter.Poc.Storage;

/// <summary>
/// Direct virtual-ID offsets, on-disk presence index, fixed arrays, synchronous writeback.
/// Working files are disposable; only RelayCheckpoint generations are restartable.
/// </summary>
public sealed class DiskRelayRecords : IRelayRecords
{
    // 328 payload/checksum bytes + 16 actual parallel-array bytes, rounded up.
    // Array/object headers are separately reserved; no dictionary or full index in RAM.
    public const int SlotCharge = RelayRecord.Bytes + 32, FixedCharge = 4096;
    private readonly SafeFileHandle _data, _index;
    private readonly byte[] _cache;
    private readonly uint[] _ids;
    private readonly long[] _ages;
    private readonly bool[] _used, _dirty;
    private long _clock;
    private bool _disposed;
    private readonly bool _readOnly;
    public uint IdLimit { get; }
    public string DirectoryPath { get; }
    public int Slots => _ids.Length;
    public long BudgetBytes { get; }
    public long ReservedBytes => FixedCharge + (long)Slots * SlotCharge;
    public long Hits { get; private set; }
    public long Misses { get; private set; }
    public long Evictions { get; private set; }
    public long DirtyWrites { get; private set; }
    public long BytesRead { get; private set; }
    public long BytesWritten { get; private set; }

    public DiskRelayRecords(string directory, uint idLimit, long budgetBytes) : this(directory, idLimit, budgetBytes, false) { }
    internal DiskRelayRecords(string directory, uint idLimit, long budgetBytes, bool existing)
    {
        _readOnly = existing;
        if (idLimit == 0) throw new ArgumentOutOfRangeException(nameof(idLimit));
        long slots = (budgetBytes - FixedCharge) / SlotCharge;
        if (budgetBytes < FixedCharge + SlotCharge || slots > int.MaxValue / RelayRecord.Bytes)
            throw new ArgumentOutOfRangeException(nameof(budgetBytes), "Insufficient or unsupported record cache budget");
        IdLimit = idLimit; BudgetBytes = budgetBytes; DirectoryPath = directory;
        if (!existing && Directory.Exists(directory)) throw new IOException("Working directory already exists");
        if (existing && !Directory.Exists(directory)) throw new DirectoryNotFoundException(directory);
        Directory.CreateDirectory(directory);
        _cache = new byte[(int)slots * RelayRecord.Bytes]; _ids = new uint[(int)slots];
        _ages = new long[(int)slots]; _used = new bool[(int)slots]; _dirty = new bool[(int)slots];
        _data = File.OpenHandle(Path.Combine(directory, "records.bin"), existing ? FileMode.Open : FileMode.CreateNew,
            existing ? FileAccess.Read : FileAccess.ReadWrite, FileShare.Read, FileOptions.RandomAccess);
        try
        {
            _index = File.OpenHandle(Path.Combine(directory, "presence.bin"), existing ? FileMode.Open : FileMode.CreateNew,
                existing ? FileAccess.Read : FileAccess.ReadWrite, FileShare.Read, FileOptions.RandomAccess);
            if (!existing)
            {
                RandomAccess.SetLength(_data, checked((long)idLimit * RelayRecord.Bytes));
                RandomAccess.SetLength(_index, idLimit);
            }
            else if (RandomAccess.GetLength(_data) != (long)idLimit * RelayRecord.Bytes || RandomAccess.GetLength(_index) != idLimit)
                throw new InvalidDataException("Missing/truncated working store");
        }
        catch { _data.Dispose(); _index?.Dispose(); throw; }
    }
    private Span<byte> At(int slot) => _cache.AsSpan(slot * RelayRecord.Bytes, RelayRecord.Bytes);
    private int Find(uint id)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (id >= IdLimit) throw new ArgumentOutOfRangeException(nameof(id));
        for (int i = 0; i < Slots; i++) if (_used[i] && _ids[i] == id)
        { _ages[i] = ++_clock; Hits++; return i; }
        Misses++; return -1;
    }
    private int Allocate(uint id)
    {
        int slot = 0;
        for (int i = 0; i < Slots; i++)
        {
            if (!_used[i]) { slot = i; break; }
            if (_ages[i] < _ages[slot]) slot = i;
        }
        if (_used[slot]) { Writeback(slot); Evictions++; }
        _used[slot] = true; _ids[slot] = id; _ages[slot] = ++_clock;
        return slot;
    }
    private void Writeback(int slot)
    {
        if (!_dirty[slot]) return;
        RandomAccess.Write(_data, At(slot), (long)_ids[slot] * RelayRecord.Bytes);
        Span<byte> present = stackalloc byte[1]; present[0] = 1;
        RandomAccess.Write(_index, present, _ids[slot]);
        _dirty[slot] = false; DirtyWrites++; BytesWritten += RelayRecord.Bytes + 1;
    }
    internal static void ReadExactly(SafeFileHandle file, Span<byte> bytes, long offset)
    {
        while (!bytes.IsEmpty)
        {
            int n = RandomAccess.Read(file, bytes, offset);
            if (n == 0) throw new InvalidDataException("Truncated learned store");
            offset += n; bytes = bytes[n..];
        }
    }
    public bool Read(uint id, Span<byte> record)
    {
        if (record.Length != RelayRecord.Bytes) throw new ArgumentException("Record buffer length");
        int slot = Find(id);
        if (slot >= 0) { At(slot).CopyTo(record); return true; }
        Span<byte> marker = stackalloc byte[1]; ReadExactly(_index, marker, id); BytesRead++;
        if (marker[0] == 0) { record.Clear(); return false; }
        if (marker[0] != 1) throw new InvalidDataException("Corrupt presence index");
        ReadExactly(_data, record, (long)id * RelayRecord.Bytes); BytesRead += RelayRecord.Bytes;
        RelayRecord.Validate(id, record);
        slot = Allocate(id); record.CopyTo(At(slot)); return true;
    }
    public void Write(uint id, ReadOnlySpan<byte> record)
    {
        if (_readOnly) throw new InvalidOperationException("Committed generations are immutable");
        RelayRecord.Validate(id, record);
        int slot = Find(id); if (slot < 0) slot = Allocate(id);
        record.CopyTo(At(slot)); _dirty[slot] = true;
    }
    public void Flush()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_readOnly) return;
        for (int i = 0; i < Slots; i++) Writeback(i);
        RandomAccess.FlushToDisk(_data); RandomAccess.FlushToDisk(_index);
    }
    public void Dispose()
    {
        if (_disposed) return;
        // Do not commit dirty working state on teardown. Explicit checkpoints publish it.
        _data.Dispose(); _index.Dispose(); _disposed = true;
    }
}
