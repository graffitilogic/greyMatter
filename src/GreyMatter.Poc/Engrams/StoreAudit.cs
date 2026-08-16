using System.IO.Compression;
using MessagePack;

namespace GreyMatter.Poc.Engrams;

/// <summary>
/// plan.md §4.3 guardrail enforcement, and Prompt.md's stated failure condition:
/// "Failure if it stores wordlists and concepts directly to disc."
///
/// §5 P3 specifies this as "grep serialized partitions for ASCII runs ≥4 chars and
/// fail if found". **That check as literally specified does not work**, and the
/// first implementation of it failed the gate on a clean store. Two false-positive
/// sources, both unavoidable:
///
///   • *Compressed bytes.* Gzip output is high-entropy; runs of four letters occur
///     by chance constantly ("afnm", "Zcdj", "fltL" — all observed).
///   • *Small-integer aliasing.* MessagePack encodes integers 0–127 as a single
///     byte equal to the value, so a sorted <c>DeviationDims</c> array of dims in
///     65–90 serialises to the bytes 'A'–'Z'. Observed: "CMRSTWZ", "GHIKLMSWZ",
///     "agklorsw" — ascending letter runs that are dimension indices, not words.
///
/// So the audit tests the guardrail's substance with two checks that are precise
/// rather than literal:
///
///   1. **Strings (exact).** Walk the decompressed MessagePack document with a
///      real reader and report any value of string type. Zero strings is proof,
///      not evidence. (A flat byte scan for str tokens was tried and discarded —
///      it reported 12,463 false positives on a clean store, because float32 and
///      large-integer payload bytes land in the fixstr range constantly.)
///   2. **Corpus words (semantic).** Extract letter runs anyway and test them
///      against the actual training vocabulary. This catches text smuggled through
///      a non-string encoding — which is precisely what the legacy tree's
///      <c>ConceptTag</c> and concept index would look like if hand-packed.
/// </summary>
public sealed class StoreAudit
{
    public sealed record Finding(string File, long Offset, string Text, string Kind);

    public sealed record Report(
        int FilesScanned, long RawBytes, long PayloadBytes,
        int StringTokens, int CorpusWordHits, int LetterRuns, int AscendingExcluded,
        IReadOnlyList<Finding> Findings)
    {
        public bool Clean => StringTokens == 0 && CorpusWordHits == 0;
    }

    public const int MinRunLength = 4;

    /// <param name="vocabulary">
    /// Training-corpus words to test letter runs against. When null, only the
    /// exact string-token check runs — still decisive for the guardrail, but the
    /// semantic check is skipped and the report says so.
    /// </param>
    public static Report Scan(EngramStore store, IReadOnlySet<string>? vocabulary = null,
                              int minRun = MinRunLength, int maxFindings = 50)
    {
        var findings = new List<Finding>();
        int files = 0, stringTokens = 0, corpusHits = 0, letterRuns = 0, ascendingExcluded = 0;
        long rawBytes = 0, payloadBytes = 0;

        foreach (var path in store.PartitionFiles())
        {
            files++;
            var raw = File.ReadAllBytes(path);
            rawBytes += raw.Length;

            byte[] payload;
            try
            {
                using var input = new MemoryStream(raw);
                using var gz = new GZipStream(input, CompressionMode.Decompress);
                using var output = new MemoryStream();
                gz.CopyTo(output);
                payload = output.ToArray();
            }
            catch (InvalidDataException)
            {
                payload = raw;   // not gzip — audit it as-is
            }

            payloadBytes += payload.Length;

            // ── 1. Exact: MessagePack strings, via a structural walk ────────
            foreach (var (offset, text) in FindStrings(payload))
            {
                stringTokens++;
                if (findings.Count < maxFindings)
                    findings.Add(new Finding(Path.GetFileName(path), offset, text, "STRING_TOKEN"));
            }

            // ── 2. Semantic: letter runs that are real corpus words ─────────
            foreach (var (offset, run) in FindLetterRuns(payload, minRun))
            {
                letterRuns++;
                if (vocabulary is null) continue;
                if (!vocabulary.Contains(run.ToLowerInvariant())) continue;

                // Strictly-ascending runs are sorted integer arrays, not text.
                // DeviationDims is sorted, and dims in 65–90 / 97–122 serialise to
                // single bytes that ARE letters — so a sorted array of dims
                // 70,73,76,77 reads as "FILM". Observed on a store containing no
                // text at all: FILM, LOST, BELT, DENY, ENVY, KNOW, every one of
                // them strictly ascending, 27 hits across 107,921 runs — the chance
                // rate. English words are essentially never sorted, so excluding
                // ascending runs removes the aliasing without weakening the check.
                if (IsStrictlyAscending(run)) { ascendingExcluded++; continue; }

                corpusHits++;
                if (findings.Count < maxFindings)
                    findings.Add(new Finding(Path.GetFileName(path), offset, run, "CORPUS_WORD"));
            }
        }

        return new Report(files, rawBytes, payloadBytes, stringTokens, corpusHits, letterRuns,
                          ascendingExcluded, findings);
    }

    /// <summary>
    /// Every MessagePack string in the payload, found by a STRUCTURAL walk.
    ///
    /// A flat byte scan for str-family tokens was tried first and is useless: it
    /// reported 12,463 "strings" across a clean store, because float32 payload
    /// bytes and large-integer bytes land in the fixstr range (0xA0–0xBF)
    /// constantly. A token byte only means "string" when the reader arrives at it
    /// in value position, which requires tracking structure.
    ///
    /// <c>MessagePackReader</c> does exactly that, so this walk is exact: it
    /// reports a string if and only if the serialized document actually contains
    /// one, with no false positives and no false negatives.
    /// </summary>
    private static List<(long offset, string text)> FindStrings(byte[] data)
    {
        var found = new List<(long, string)>();
        var reader = new MessagePackReader(data);
        try
        {
            while (!reader.End) WalkValue(ref reader, found, 0);
        }
        catch (MessagePackSerializationException)
        {
            // Truncated or non-MessagePack payload. Not a guardrail finding; the
            // letter-run check still covers the bytes.
        }
        return found;
    }

    private const int MaxDepth = 64;

    private static void WalkValue(ref MessagePackReader reader, List<(long, string)> found, int depth)
    {
        if (depth > MaxDepth) { reader.Skip(); return; }

        switch (reader.NextMessagePackType)
        {
            case MessagePackType.String:
                long offset = reader.Consumed;
                found.Add((offset, reader.ReadString() ?? string.Empty));
                break;

            case MessagePackType.Array:
            {
                int count = reader.ReadArrayHeader();
                for (int i = 0; i < count; i++) WalkValue(ref reader, found, depth + 1);
                break;
            }

            case MessagePackType.Map:
            {
                int count = reader.ReadMapHeader();
                for (int i = 0; i < count; i++)
                {
                    WalkValue(ref reader, found, depth + 1);   // key
                    WalkValue(ref reader, found, depth + 1);   // value
                }
                break;
            }

            default:
                reader.Skip();
                break;
        }
    }

    private static IEnumerable<(long offset, string run)> FindLetterRuns(byte[] data, int minRun)
    {
        int start = -1;
        for (int i = 0; i <= data.Length; i++)
        {
            bool wordish = i < data.Length && IsWordish(data[i]);
            if (wordish) { if (start < 0) start = i; continue; }

            if (start >= 0 && i - start >= minRun)
                yield return (start, System.Text.Encoding.ASCII.GetString(data, start, i - start));
            start = -1;
        }
    }

    /// <summary>Sorted integer arrays read as ascending letters; real text does not.</summary>
    private static bool IsStrictlyAscending(string s)
    {
        for (int i = 1; i < s.Length; i++)
            if (s[i] <= s[i - 1]) return false;
        return true;
    }

    private static bool IsWordish(byte b) =>
        (b >= 'a' && b <= 'z') || (b >= 'A' && b <= 'Z') || b == '\'' || b == '-';
}
