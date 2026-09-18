using Microsoft.Win32.SafeHandles;

namespace GreyMatter.Poc.Storage;

/// <summary>
/// Direct virtual-ID offsets, on-disk presence index, fixed arrays, synchronous writeback.
/// Working files are disposable; only RelayCheckpoint generations are restartable.
/// </summary>
public sealed class DiskRelayRecords : IRelayRecords
{
    // 328 record bytes + conservatively charged hash/LRU/ID/dirty metadata.
    // Array/object headers are separately reserved; no dictionary or full index in RAM.
    public const int SlotCharge = RelayRecord.Bytes + 64, FixedCharge = 4096;
    private readonly SafeFileHandle _data, _index;
    private readonly byte[] _cache;
    private readonly uint[] _ids;
    private readonly int[] _hash, _next, _previous;
    private readonly bool[] _dirty;
    private int _occupied, _head = -1, _tail = -1;
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
    public readonly record struct Access(uint Id, bool Hit, bool Loaded, bool Unseen, uint? EvictedId);
    // Optional observer; the caller owns and bounds its trace buffer.
    public Action<Access>? ObserveRead { get; set; }
    public void ClearCache()
    {
        Flush(); Array.Clear(_hash); _occupied = 0; _head = _tail = -1;
    }

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
        _next = new int[(int)slots]; _previous = new int[(int)slots]; _dirty = new bool[(int)slots];
        int buckets = 1; while (buckets < slots * 2) buckets = checked(buckets * 2);
        _hash = new int[buckets];
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
    private int Bucket(uint id) => (int)(unchecked(id * 2654435761u) & (uint)(_hash.Length - 1));
    private int HashSlot(uint id)
    {
        int at = Bucket(id);
        while (_hash[at] != 0)
        {
            int slot = _hash[at] - 1;
            if (_ids[slot] == id) return slot;
            at = (at + 1) & (_hash.Length - 1);
        }
        return -1;
    }
    private void InsertHash(int slot)
    {
        int at = Bucket(_ids[slot]);
        while (_hash[at] != 0) at = (at + 1) & (_hash.Length - 1);
        _hash[at] = slot + 1;
    }
    private void DeleteHash(uint id)
    {
        int mask = _hash.Length - 1, hole = Bucket(id);
        while (_hash[hole] != 0 && _ids[_hash[hole] - 1] != id) hole = (hole + 1) & mask;
        if (_hash[hole] == 0) throw new InvalidOperationException("Cache index mismatch");
        int scan = (hole + 1) & mask;
        while (_hash[scan] != 0)
        {
            int home = Bucket(_ids[_hash[scan] - 1]);
            if (((scan - home) & mask) >= ((scan - hole) & mask))
            { _hash[hole] = _hash[scan]; hole = scan; }
            scan = (scan + 1) & mask;
        }
        _hash[hole] = 0;
    }
    private void Unlink(int slot)
    {
        int before = _previous[slot], after = _next[slot];
        if (before < 0) _head = after; else _next[before] = after;
        if (after < 0) _tail = before; else _previous[after] = before;
    }
    private void Prepend(int slot)
    {
        _previous[slot] = -1; _next[slot] = _head;
        if (_head >= 0) _previous[_head] = slot; else _tail = slot;
        _head = slot;
    }
    private int Find(uint id)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (id >= IdLimit) throw new ArgumentOutOfRangeException(nameof(id));
        int slot = HashSlot(id);
        if (slot >= 0)
        {
            if (slot != _head) { Unlink(slot); Prepend(slot); }
            Hits++; return slot;
        }
        Misses++; return -1;
    }
    private int Allocate(uint id, out uint? victim)
    {
        int slot; victim = null;
        if (_occupied < Slots) slot = _occupied++;
        else
        {
            slot = _tail; victim = _ids[slot]; Writeback(slot);
            DeleteHash(_ids[slot]); Unlink(slot); Evictions++;
        }
        _ids[slot] = id; InsertHash(slot); Prepend(slot); return slot;
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
        using var attribution = GreyMatter.Poc.Eval.CostProfile.Enter(GreyMatter.Poc.Eval.CostProfile.Kind.FileApi);
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
        if (slot >= 0) { At(slot).CopyTo(record); ObserveRead?.Invoke(new(id, true, false, false, null)); return true; }
        Span<byte> marker = stackalloc byte[1]; ReadExactly(_index, marker, id); BytesRead++;
        if (marker[0] == 0) { record.Clear(); ObserveRead?.Invoke(new(id, false, false, true, null)); return false; }
        if (marker[0] != 1) throw new InvalidDataException("Corrupt presence index");
        ReadExactly(_data, record, (long)id * RelayRecord.Bytes); BytesRead += RelayRecord.Bytes;
        RelayRecord.Validate(id, record);
        slot = Allocate(id, out uint? victim); record.CopyTo(At(slot));
        ObserveRead?.Invoke(new(id, false, true, false, victim)); return true;
    }
    public void Write(uint id, ReadOnlySpan<byte> record)
    {
        if (_readOnly) throw new InvalidOperationException("Committed generations are immutable");
        RelayRecord.Validate(id, record);
        int slot = Find(id); if (slot < 0) slot = Allocate(id, out _);
        record.CopyTo(At(slot)); _dirty[slot] = true;
    }
    public void Flush()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_readOnly) return;
        for (int i = 0; i < _occupied; i++) Writeback(i);
        RandomAccess.FlushToDisk(_data); RandomAccess.FlushToDisk(_index);
    }
    public void VisitPresent(Action<uint> visit)
    {
        // Publish pending presence bits before taking this streaming enumeration.
        // The callback may update existing records, but must not add new IDs.
        Flush();
        using var index = new FileStream(Path.Combine(DirectoryPath, "presence.bin"), FileMode.Open,
            FileAccess.Read, FileShare.ReadWrite, 1);
        Span<byte> markers = stackalloc byte[4096]; uint first = 0;
        while (first < IdLimit)
        {
            int n = (int)Math.Min((uint)markers.Length, IdLimit - first); index.ReadExactly(markers[..n]);
            for (int i = 0; i < n; i++)
            {
                if (markers[i] == 1) visit(first + (uint)i);
                else if (markers[i] != 0) throw new InvalidDataException("Presence index corruption");
            }
            first += (uint)n;
        }
    }
    public void Dispose()
    {
        if (_disposed) return;
        // Do not commit dirty working state on teardown. Explicit checkpoints publish it.
        _data.Dispose(); _index.Dispose(); _disposed = true;
    }
}
