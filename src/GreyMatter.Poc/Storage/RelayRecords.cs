using System.Buffers.Binary;
using System.Security.Cryptography;
using GreyMatter.Poc.Substrate;

namespace GreyMatter.Poc.Storage;

/// <summary>R2 version-1 ordered adjacency. Exactly the A1 default degree-32 channel.</summary>
public static class RelayRecord
{
    public const int Cap = 32, PayloadBytes = 8 + 9 * Cap, Bytes = PayloadBytes + 32;
    public static void Encode(uint id, SynapseStore synapses, Span<byte> record)
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
    public static void Decode(uint id, ReadOnlySpan<byte> record, SynapseStore synapses)
    {
        using var attribution = GreyMatter.Poc.Eval.CostProfile.Enter(GreyMatter.Poc.Eval.CostProfile.Kind.Serialization);
        Validate(id, record);
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
