using System.Buffers.Binary;
using System.Security.Cryptography;
using GreyMatter.Poc.Substrate;

namespace GreyMatter.Poc.Storage;

/// <summary>R2 version-1 ordered adjacency. Exactly the A1 default degree-32 channel.</summary>
public static class RelayRecord
{
    public const int Cap = 32, PayloadBytes = 8 + 9 * Cap, Bytes = PayloadBytes + 32;
    /// <summary>Item 5: <paramref name="seal"/> false leaves the hash slot zero for a store that seals at its disk boundary.</summary>
    public static void Encode(uint id, SynapseStore synapses, Span<byte> record, bool seal = true)
    {
        using var attribution = GreyMatter.Poc.Eval.CostProfile.Enter(GreyMatter.Poc.Eval.CostProfile.Kind.Serialization);
        record.Clear();
        BinaryPrimitives.WriteUInt32LittleEndian(record, id);
        BinaryPrimitives.WriteInt32LittleEndian(record[4..], synapses.Degree[0]);
        for (int e = 0; e < synapses.Degree[0]; e++)
        {
            int at = 8 + 9 * e;
            BinaryPrimitives.WriteUInt32LittleEndian(record[at..], synapses.Target[e]);
            BinaryPrimitives.WriteSingleLittleEndian(record[(at + 4)..], synapses.Weight[e]);
            record[at + 8] = synapses.Population[e];
        }
        if (seal) Seal(record);
    }
    /// <summary>Compute the record's SHA-256 into its hash slot. Called once per disk write by a sealing store.</summary>
    public static void Seal(Span<byte> record)
    {
        using var attribution = GreyMatter.Poc.Eval.CostProfile.Enter(GreyMatter.Poc.Eval.CostProfile.Kind.Serialization);
        SHA256.HashData(record[..PayloadBytes], record[PayloadBytes..]);
    }
    /// <summary>
    /// Provenance byte for CB count edges. Relay edges (0–2) must stay within [0,1];
    /// a count edge is an exact integer up to 2^24 (float-exact). The byte makes a
    /// record self-describing, so relay records keep their original validation.
    /// </summary>
    public const byte CountPopulation = 3;
    public const float MaxCount = 16777216f;
    public static void Validate(uint id, ReadOnlySpan<byte> record)
    {
        using var attribution = GreyMatter.Poc.Eval.CostProfile.Enter(GreyMatter.Poc.Eval.CostProfile.Kind.Serialization);
        if (record.Length != Bytes) throw new InvalidDataException("Record length");
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(record[..PayloadBytes], hash);
        if (!hash.SequenceEqual(record[PayloadBytes..]) || BinaryPrimitives.ReadUInt32LittleEndian(record) != id)
            throw new InvalidDataException("Missing/corrupt learned record");
        int degree = BinaryPrimitives.ReadInt32LittleEndian(record[4..]);
        if (degree < 0 || degree > Cap) throw new InvalidDataException("Degree");
        for (int e = 0; e < degree; e++)
        {
            float w = BinaryPrimitives.ReadSingleLittleEndian(record[(12 + 9 * e)..]);
            byte population = record[16 + 9 * e];
            bool count = population == CountPopulation;
            if (!float.IsFinite(w) || w < 0 || population > CountPopulation || (count ? w > MaxCount || w != MathF.Floor(w) : w > 1))
                throw new InvalidDataException("Invalid synapse");
        }
    }
    /// <summary>Item 5: <paramref name="validate"/> false trusts a copy the store already verified at its disk boundary.</summary>
    public static void Decode(uint id, ReadOnlySpan<byte> record, SynapseStore synapses, bool validate = true)
    {
        using var attribution = GreyMatter.Poc.Eval.CostProfile.Enter(GreyMatter.Poc.Eval.CostProfile.Kind.Serialization);
        if (validate) Validate(id, record);
        else if (record.Length != RelayRecord.Bytes || BinaryPrimitives.ReadUInt32LittleEndian(record) != id) throw new InvalidDataException("Record identity");
        synapses.Degree[0] = BinaryPrimitives.ReadInt32LittleEndian(record[4..]);
        for (int e = 0; e < synapses.Degree[0]; e++)
        {
            int at = 8 + 9 * e;
            synapses.Target[e] = BinaryPrimitives.ReadUInt32LittleEndian(record[at..]);
            synapses.Weight[e] = BinaryPrimitives.ReadSingleLittleEndian(record[(at + 4)..]);
            synapses.Population[e] = record[at + 8];
        }
    }
}

/// <summary>No resident slot may escape this copying boundary. Single-threaded writer.</summary>
public interface IRelayRecords : IDisposable
{
    uint IdLimit { get; }
    bool Read(uint id, Span<byte> record);
    void Write(uint id, ReadOnlySpan<byte> record);
    void Flush();
    void VisitPresent(Action<uint> visit);
    /// <summary>
    /// Item 5 (2026-09-21): a store that verifies checksums when loading from disk and
    /// seals them when writing to disk. Its cache copies are trusted between those
    /// boundaries, so callers may write unsealed records and decode without re-hashing.
    /// Reference and legacy stores keep per-write validation.
    /// </summary>
    bool SealsAtDiskBoundary => false;
    /// <summary>Existence without copying or validating the record.</summary>
    bool Contains(uint id) { Span<byte> scratch = stackalloc byte[RelayRecord.Bytes]; return Read(id, scratch); }
}

/// <summary>Deliberately unbounded reference backend, never hidden inside disk mode.</summary>
public sealed class ResidentRelayRecords(uint idLimit) : IRelayRecords
{
    private readonly Dictionary<uint, byte[]> _records = new();
    public uint IdLimit { get; } = idLimit;
    public bool Read(uint id, Span<byte> record)
    {
        if (id >= IdLimit) throw new ArgumentOutOfRangeException(nameof(id));
        record.Clear();
        if (!_records.TryGetValue(id, out var bytes)) return false;
        bytes.CopyTo(record); return true;
    }
    public void Write(uint id, ReadOnlySpan<byte> record)
    {
        if (id >= IdLimit) throw new ArgumentOutOfRangeException(nameof(id));
        RelayRecord.Validate(id, record);
        if (!_records.TryGetValue(id, out var bytes)) _records[id] = bytes = new byte[RelayRecord.Bytes];
        record.CopyTo(bytes);
    }
    public void VisitPresent(Action<uint> visit)
    { foreach (uint id in _records.Keys.OrderBy(id => id)) visit(id); }
    public void Flush() { }
    public void Dispose() { }
}
