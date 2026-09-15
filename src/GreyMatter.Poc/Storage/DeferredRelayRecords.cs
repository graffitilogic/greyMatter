using System.Buffers.Binary;
using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;
using GreyMatter.Poc.Substrate;

namespace GreyMatter.Poc.Storage;

/// <summary>
/// Version 2: base v1 adjacency plus authenticated per-record epochs. Reads return the
/// exact aged v1 view in caller scratch; physical records never change during recall.
/// Working files require explicit Flush; crash recovery uses DeferredRelayCheckpoint.
/// </summary>
public sealed class DeferredRelayRecords : IRelayRecords
{
    public const int ExtraReservedBytes = 8192, EpochRecordBytes = 48, FormatBytes = 48;
    internal DiskRelayRecords Cache { get; }
    private readonly SafeFileHandle _epochs, _format;
    private readonly bool _readOnly;
    private readonly SynapseStore _scratch = new(1, RelayRecord.Cap);
    private bool _disposed;
    public uint IdLimit => Cache.IdLimit;
    public string DirectoryPath { get; }
    public ulong Epoch { get; private set; }
    public long ReservedBytes => Cache.ReservedBytes + ExtraReservedBytes;
    public long AgedReads { get; private set; }
    public long ReplaySteps { get; private set; }
    public long AgingTicks { get; private set; }
    public long EpochBytesRead { get; private set; }
    public long EpochBytesWritten { get; private set; }

    public DeferredRelayRecords(string directory, uint limit, long budgetBytes)
        : this(directory, limit, budgetBytes, false, 0) { }
    internal DeferredRelayRecords(string directory, uint limit, long budgetBytes, bool existing, ulong initialEpoch = 0)
    {
        if (budgetBytes < ExtraReservedBytes + DiskRelayRecords.FixedCharge + DiskRelayRecords.SlotCharge)
            throw new ArgumentOutOfRangeException(nameof(budgetBytes));
        if (!existing && Directory.Exists(directory)) throw new IOException("Version-2 working directory already exists");
        DirectoryPath = directory; _readOnly = existing; Epoch = initialEpoch;
        if (existing)
        {
            byte[] header = RelayCheckpoint.SmallFile(Path.Combine(directory, "format.bin"), FormatBytes);
            if (!SHA256.HashData(header.AsSpan(0, 16)).AsSpan().SequenceEqual(header.AsSpan(16)) ||
                BinaryPrimitives.ReadInt32LittleEndian(header) != 2 || BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(4)) != limit)
                throw new InvalidDataException("Version-2 format/epoch checksum");
            Epoch = BinaryPrimitives.ReadUInt64LittleEndian(header.AsSpan(8));
        }
        else Directory.CreateDirectory(directory);
        Cache = new DiskRelayRecords(Path.Combine(directory, "base"), limit, budgetBytes - ExtraReservedBytes, existing);
        try
        {
            _epochs = File.OpenHandle(Path.Combine(directory, "epochs.bin"), existing ? FileMode.Open : FileMode.CreateNew,
                existing ? FileAccess.Read : FileAccess.ReadWrite, FileShare.Read, FileOptions.RandomAccess);
            _format = File.OpenHandle(Path.Combine(directory, "format.bin"), existing ? FileMode.Open : FileMode.CreateNew,
                existing ? FileAccess.Read : FileAccess.ReadWrite, FileShare.Read, FileOptions.RandomAccess);
            if (!existing) { RandomAccess.SetLength(_epochs, (long)limit * EpochRecordBytes); WriteFormat(); }
            else if (RandomAccess.GetLength(_epochs) != (long)limit * EpochRecordBytes)
                throw new InvalidDataException("Missing/truncated epoch store");
        }
        catch { Cache.Dispose(); _epochs?.Dispose(); _format?.Dispose(); throw; }
    }
    private static void Bind(ReadOnlySpan<byte> record, ReadOnlySpan<byte> epochHeader, Span<byte> hash)
    {
        Span<byte> bound = stackalloc byte[RelayRecord.Bytes + 16];
        record.CopyTo(bound); epochHeader.CopyTo(bound[RelayRecord.Bytes..]); SHA256.HashData(bound, hash);
    }
    public bool Read(uint id, Span<byte> record)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!Cache.Read(id, record)) return false;
        Span<byte> meta = stackalloc byte[EpochRecordBytes], hash = stackalloc byte[32];
        DiskRelayRecords.ReadExactly(_epochs, meta, (long)id * EpochRecordBytes); EpochBytesRead += EpochRecordBytes;
        Bind(record, meta[..16], hash);
        ulong applied = BinaryPrimitives.ReadUInt64LittleEndian(meta);
        if (BinaryPrimitives.ReadUInt32LittleEndian(meta[8..]) != id || applied > Epoch || !hash.SequenceEqual(meta[16..]))
            throw new InvalidDataException("Missing/corrupt/future record epoch");
        if (applied == Epoch) return true;
        long started = Stopwatch.GetTimestamp(); AgedReads++;
        RelayRecord.Decode(id, record, _scratch);
        while (applied < Epoch && _scratch.Degree[0] != 0)
        { _scratch.ApplyDecay(1, .99f); applied++; ReplaySteps++; }
        RelayRecord.Encode(id, _scratch, record);
        AgingTicks += Stopwatch.GetTimestamp() - started;
        return true;
    }
    public void Write(uint id, ReadOnlySpan<byte> record)
    {
        if (_readOnly) throw new InvalidOperationException("Committed version-2 records are immutable");
        RelayRecord.Validate(id, record);
        Span<byte> meta = stackalloc byte[EpochRecordBytes]; meta.Clear();
        BinaryPrimitives.WriteUInt64LittleEndian(meta, Epoch); BinaryPrimitives.WriteUInt32LittleEndian(meta[8..], id);
        Bind(record, meta[..16], meta[16..]);
        Cache.Write(id, record);
        RandomAccess.Write(_epochs, meta, (long)id * EpochRecordBytes); EpochBytesWritten += EpochRecordBytes;
    }
    public void AdvanceDecay()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_readOnly) throw new InvalidOperationException("Recall cannot advance decay");
        Epoch = checked(Epoch + 1);
    }
    private void WriteFormat()
    {
        Span<byte> header = stackalloc byte[FormatBytes]; header.Clear();
        BinaryPrimitives.WriteInt32LittleEndian(header, 2); BinaryPrimitives.WriteUInt32LittleEndian(header[4..], IdLimit);
        BinaryPrimitives.WriteUInt64LittleEndian(header[8..], Epoch); SHA256.HashData(header[..16], header[16..]);
        RandomAccess.Write(_format, header, 0);
    }
    public void Flush()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_readOnly) return;
        Cache.Flush(); RandomAccess.FlushToDisk(_epochs); WriteFormat(); RandomAccess.FlushToDisk(_format);
    }
    public void VisitPresent(Action<uint> visit) => Cache.VisitPresent(visit);
    public void Dispose()
    {
        if (_disposed) return;
        Cache.Dispose(); _epochs.Dispose(); _format.Dispose(); _disposed = true;
    }
}
