using System.Buffers.Binary;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace GreyMatter.Poc.Storage;

/// <summary>Dense unchanged records; growing disk hash index, bounded record/index caches.</summary>
public sealed class PackedRelayRecords : IRelayRecords
{
    public const int FixedCharge = 65536, SlotCharge = RelayRecord.Bytes + 72;
    public const int PageBytes = 4096, IndexPages = 8, EntryBytes = 16, FormatBytes = 64;
    private const uint Magic = 0x34504D47;
    private readonly SafeFileHandle _data;
    private SafeFileHandle _index;
    private readonly byte[] _cache, _pages = new byte[PageBytes * IndexPages];
    private readonly uint[] _ids;
    private readonly ulong[] _ordinals;
    private readonly int[] _hash, _next, _previous;
    private readonly bool[] _dirty, _pageDirty = new bool[IndexPages];
    private readonly long[] _pageIds = Enumerable.Repeat(-1L, IndexPages).ToArray();
    private int _occupied, _head = -1, _tail = -1;
    private ulong _capacity = 64, _count;
    private bool _disposed;
    private readonly bool _readOnly;
    public uint IdLimit { get; }
    public string DirectoryPath { get; }
    public int Slots => _ids.Length;
    public long ReservedBytes => FixedCharge + (long)Slots * SlotCharge;
    public long BudgetBytes { get; }
    public long Count => checked((long)_count);
    public long IndexCapacity => checked((long)_capacity);
    public long Hits { get; private set; }
    public long Misses { get; private set; }
    public long Evictions { get; private set; }
    public long DirtyWrites { get; private set; }
    public long BytesRead { get; private set; }
    public long BytesWritten { get; private set; }
    public long IndexBytesRead { get; private set; }
    public long IndexBytesWritten { get; private set; }
    public long IndexPageHits { get; private set; }
    public long IndexPageMisses { get; private set; }
    public long Rehashes { get; private set; }

    public PackedRelayRecords(string directory, uint limit, long budget) : this(directory, limit, budget, false) { }
    internal PackedRelayRecords(string directory, uint limit, long budget, bool existing)
    {
        if (limit == 0 || budget < FixedCharge + SlotCharge) throw new ArgumentOutOfRangeException(nameof(budget));
        long slots = (budget - FixedCharge) / SlotCharge;
        if (slots > int.MaxValue / RelayRecord.Bytes) throw new ArgumentOutOfRangeException(nameof(budget));
        IdLimit = limit; BudgetBytes = budget; DirectoryPath = directory; _readOnly = existing;
        if (!existing && Directory.Exists(directory)) throw new IOException("Packed working directory already exists");
        if (existing)
        {
            byte[] header = RelayCheckpoint.SmallFile(Path.Combine(directory, "format.bin"), FormatBytes);
            if (!SHA256.HashData(header.AsSpan(0, 32)).AsSpan().SequenceEqual(header.AsSpan(32)) ||
                BinaryPrimitives.ReadUInt32LittleEndian(header) != Magic || BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(4)) != 4 ||
                BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(8)) != limit)
                throw new InvalidDataException("Packed format checksum/version/limit");
            _capacity = BinaryPrimitives.ReadUInt64LittleEndian(header.AsSpan(16));
            _count = BinaryPrimitives.ReadUInt64LittleEndian(header.AsSpan(24));
            if (_capacity < 64 || (_capacity & (_capacity - 1)) != 0 || _capacity > (1UL << 33) || _count > limit || _count * 10 > _capacity * 7)
                throw new InvalidDataException("Packed count/capacity");
        }
        else Directory.CreateDirectory(directory);
        _cache = new byte[checked((int)slots * RelayRecord.Bytes)]; _ids = new uint[(int)slots]; _ordinals = new ulong[(int)slots];
        _next = new int[(int)slots]; _previous = new int[(int)slots]; _dirty = new bool[(int)slots];
        int buckets = 1; while (buckets < slots * 2) buckets = checked(buckets * 2); _hash = new int[buckets];
        _data = File.OpenHandle(Path.Combine(directory, "records.bin"), existing ? FileMode.Open : FileMode.CreateNew,
            existing ? FileAccess.Read : FileAccess.ReadWrite, FileShare.Read, FileOptions.RandomAccess);
        try
        {
            _index = File.OpenHandle(Path.Combine(directory, "index.bin"), existing ? FileMode.Open : FileMode.CreateNew,
                existing ? FileAccess.Read : FileAccess.ReadWrite, FileShare.Read, FileOptions.RandomAccess);
            if (!existing) { RandomAccess.SetLength(_index, checked((long)_capacity * EntryBytes)); WriteFormat(); }
            else if (RandomAccess.GetLength(_index) != checked((long)_capacity * EntryBytes) || RandomAccess.GetLength(_data) != checked((long)_count * RelayRecord.Bytes))
                throw new InvalidDataException("Packed file length");
        }
        catch { _data.Dispose(); _index?.Dispose(); throw; }
    }
    private void WriteFormat()
    {
        Span<byte> header = stackalloc byte[FormatBytes]; header.Clear();
        BinaryPrimitives.WriteUInt32LittleEndian(header, Magic); BinaryPrimitives.WriteInt32LittleEndian(header[4..], 4);
        BinaryPrimitives.WriteUInt32LittleEndian(header[8..], IdLimit);
        BinaryPrimitives.WriteUInt64LittleEndian(header[16..], _capacity); BinaryPrimitives.WriteUInt64LittleEndian(header[24..], _count);
        SHA256.HashData(header[..32], header[32..]);
        using var file = new FileStream(Path.Combine(DirectoryPath, "format.bin"), FileMode.Create, FileAccess.Write, FileShare.Read);
        file.Write(header); file.Flush(true); BytesWritten += FormatBytes;
    }
    private void FlushPage(int slot)
    {
        if (!_pageDirty[slot]) return;
        long offset = _pageIds[slot] * PageBytes;
        int n = (int)Math.Min(PageBytes, checked((long)_capacity * EntryBytes) - offset);
        GreyMatter.Poc.Eval.CostProfile.Write(_index, _pages.AsSpan(slot * PageBytes, n), offset);
        IndexBytesWritten += n; _pageDirty[slot] = false;
    }
    private Span<byte> Entry(ulong bucket)
    {
        long offset = checked((long)bucket * EntryBytes), page = offset / PageBytes;
        int slot = (int)(page % IndexPages);
        if (_pageIds[slot] != page)
        {
            IndexPageMisses++; FlushPage(slot); _pageIds[slot] = page;
            var buffer = _pages.AsSpan(slot * PageBytes, PageBytes); buffer.Clear();
            int n = (int)Math.Min(PageBytes, checked((long)_capacity * EntryBytes) - page * PageBytes);
            DiskRelayRecords.ReadExactly(_index, buffer[..n], page * PageBytes); IndexBytesRead += n;
        }
        else IndexPageHits++;
        return _pages.AsSpan(slot * PageBytes + (int)(offset % PageBytes), EntryBytes);
    }
    private static ulong Home(uint id, ulong capacity) => unchecked((ulong)id * 11400714819323198485UL) & (capacity - 1);
    private (ulong Bucket, ulong OrdinalPlusOne) Locate(uint id)
    {
        ulong at = Home(id, _capacity);
        for (ulong probes = 0; probes < _capacity; probes++, at = (at + 1) & (_capacity - 1))
        {
            var entry = Entry(at); ulong position = BinaryPrimitives.ReadUInt64LittleEndian(entry[8..]);
            if (position == 0) return (at, 0);
            uint key = BinaryPrimitives.ReadUInt32LittleEndian(entry);
            if (key >= IdLimit || position > _count || BinaryPrimitives.ReadUInt32LittleEndian(entry[4..]) != 0)
                throw new InvalidDataException("Packed index pointer/key");
            if (key == id) return (at, position);
        }
        throw new InvalidDataException("Packed index has no empty slot");
    }
    private ulong Address(uint id, bool create)
    {
        var found = Locate(id);
        if (found.OrdinalPlusOne != 0 || !create) return found.OrdinalPlusOne;
        if (_count == IdLimit) throw new InvalidDataException("Record count exceeds ID space");
        if ((_count + 1) * 10 > _capacity * 7) { Grow(); found = Locate(id); }
        var entry = Entry(found.Bucket); entry.Clear();
        BinaryPrimitives.WriteUInt32LittleEndian(entry, id); BinaryPrimitives.WriteUInt64LittleEndian(entry[8..], _count + 1);
        _pageDirty[(int)((found.Bucket * EntryBytes / PageBytes) % IndexPages)] = true;
        return ++_count;
    }
    private void Grow()
    {
        for (int i = 0; i < IndexPages; i++) FlushPage(i);
        ulong capacity = checked(_capacity * 2); string pending = Path.Combine(DirectoryPath, "index.growing");
        using (var target = File.OpenHandle(pending, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, FileOptions.RandomAccess))
        {
            RandomAccess.SetLength(target, checked((long)capacity * EntryBytes));
            Span<byte> block = stackalloc byte[4096], probe = stackalloc byte[EntryBytes];
            for (long first = 0; first < checked((long)_capacity * EntryBytes); first += block.Length)
            {
                int n = (int)Math.Min(block.Length, checked((long)_capacity * EntryBytes) - first);
                DiskRelayRecords.ReadExactly(_index, block[..n], first); IndexBytesRead += n;
                for (int at = 0; at < n; at += EntryBytes)
                {
                    var entry = block.Slice(at, EntryBytes);
                    if (BinaryPrimitives.ReadUInt64LittleEndian(entry[8..]) == 0) continue;
                    ulong slot = Home(BinaryPrimitives.ReadUInt32LittleEndian(entry), capacity);
                    while (true)
                    {
                        DiskRelayRecords.ReadExactly(target, probe, checked((long)slot * EntryBytes)); IndexBytesRead += EntryBytes;
                        if (BinaryPrimitives.ReadUInt64LittleEndian(probe[8..]) == 0) break;
                        slot = (slot + 1) & (capacity - 1);
                    }
                    GreyMatter.Poc.Eval.CostProfile.Write(target, entry, checked((long)slot * EntryBytes)); IndexBytesWritten += EntryBytes;
                }
            }
            GreyMatter.Poc.Eval.CostProfile.Flush(target);
        }
        _index.Dispose(); File.Move(pending, Path.Combine(DirectoryPath, "index.bin"), true);
        _index = File.OpenHandle(Path.Combine(DirectoryPath, "index.bin"), FileMode.Open, FileAccess.ReadWrite, FileShare.Read, FileOptions.RandomAccess);
        _capacity = capacity; Array.Fill(_pageIds, -1); Array.Clear(_pageDirty); Rehashes++;
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
        GreyMatter.Poc.Eval.CostProfile.Write(_data, At(slot), checked((long)_ordinals[slot] * RelayRecord.Bytes));
        _dirty[slot] = false; DirtyWrites++; BytesWritten += RelayRecord.Bytes;
    }
    public bool Read(uint id, Span<byte> record)
    {
        using var attribution = GreyMatter.Poc.Eval.CostProfile.Enter(GreyMatter.Poc.Eval.CostProfile.Kind.CacheIndex);
        if (record.Length != RelayRecord.Bytes) throw new ArgumentException("Record buffer length");
        int slot = Find(id);
        if (slot >= 0) { At(slot).CopyTo(record); return true; }
        ulong address = Address(id, false);
        if (address == 0) { record.Clear(); return false; }
        DiskRelayRecords.ReadExactly(_data, record, checked((long)(address - 1) * RelayRecord.Bytes)); BytesRead += RelayRecord.Bytes;
        RelayRecord.Validate(id, record);
        slot = Allocate(id, out _); _ordinals[slot] = address - 1; record.CopyTo(At(slot)); return true;
    }
    public void Write(uint id, ReadOnlySpan<byte> record)
    {
        using var attribution = GreyMatter.Poc.Eval.CostProfile.Enter(GreyMatter.Poc.Eval.CostProfile.Kind.CacheIndex);
        if (_readOnly) throw new InvalidOperationException("Committed packed generations are immutable");
        RelayRecord.Validate(id, record); int slot = Find(id);
        if (slot < 0) { ulong address = Address(id, true); slot = Allocate(id, out _); _ordinals[slot] = address - 1; }
        record.CopyTo(At(slot)); _dirty[slot] = true;
    }
    /// <summary>Read-only experiment: request per-descriptor no-cache I/O; not a cold-disk guarantee.</summary>
    public void SetReadNoCache(bool enabled)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_readOnly) throw new InvalidOperationException("Cache-policy experiment requires a frozen snapshot");
        MacFileIo.SetNoCache(_data, enabled); MacFileIo.SetNoCache(_index, enabled);
    }
    public void ClearCache()
    {
        Flush(); Array.Clear(_hash); _occupied = 0; _head = _tail = -1;
        Array.Fill(_pageIds, -1); Array.Clear(_pageDirty);
    }
    public void Flush()
    {
        using var attribution = GreyMatter.Poc.Eval.CostProfile.Enter(GreyMatter.Poc.Eval.CostProfile.Kind.CacheIndex);
        ObjectDisposedException.ThrowIf(_disposed, this); if (_readOnly) return;
        for (int i = 0; i < _occupied; i++) Writeback(i);
        for (int i = 0; i < IndexPages; i++) FlushPage(i);
        GreyMatter.Poc.Eval.CostProfile.Flush(_data); GreyMatter.Poc.Eval.CostProfile.Flush(_index); WriteFormat();
    }
    public void VisitPresent(Action<uint> visit)
    {
        // Packed birth order, deliberately not sorted ID order. Never collect all IDs in RAM.
        Flush(); ulong count = _count; var record = new byte[RelayRecord.Bytes];
        for (ulong ordinal = 0; ordinal < count; ordinal++)
        {
            DiskRelayRecords.ReadExactly(_data, record, checked((long)ordinal * RelayRecord.Bytes)); BytesRead += RelayRecord.Bytes;
            uint id = BinaryPrimitives.ReadUInt32LittleEndian(record); RelayRecord.Validate(id, record);
            if (id >= IdLimit || Address(id, false) != ordinal + 1) throw new InvalidDataException("Packed enumeration index mismatch");
            visit(id);
        }
        if (_count != count) throw new InvalidOperationException("Enumeration callback added records");
    }
    public void Dispose()
    {
        if (_disposed) return; _data.Dispose(); _index.Dispose(); _disposed = true;
    }
}
