using System.Text;

namespace GreyMatter.Poc.Pipeline;

/// <summary>
/// plan.md §4.6 / P0 — streaming corpus access, ported from legacy
/// <c>TrainingDataProvider</c> + <c>TatoebaReader</c> + <c>SimpleTextParser</c>.
///
/// Two departures from the legacy version, both deliberate:
///   • it streams (IEnumerable all the way down) instead of materialising a
///     List — a 50k-sentence P5 run must not hold the corpus in RAM;
///   • no NAS copy, no shuffle-by-Random-without-seed. Shuffling is the caller's
///     job with the run's seeded RNG (rule 8: determinism everywhere).
///
/// <c>tatoeba_small</c> reproduces the legacy load path exactly (no language
/// filter, first N well-formed lines) because the P0 gate is reproduction of the
/// legacy encoder ceiling measured on that stream.
/// </summary>
public sealed class Corpus
{
    private readonly string _root;
    private readonly bool _localSample;

    public Corpus(string trainingDataRoot, bool localSample = false)
    {
        _root = trainingDataRoot;
        _localSample = localSample;
    }

    public static readonly string[] KnownDatasets = { "tatoeba_small", "tatoeba", "simplewiki", "cbt" };

    /// <summary>Resolve a dataset key to its on-disk path, or null when unavailable.</summary>
    public string? PathFor(string dataset) => dataset switch
    {
        "tatoeba_small" => FileIfExists(Path.Combine(_root, "Tatoeba/sentences_eng_small.csv")),
        "tatoeba" => FileIfExists(Path.Combine(_root, "Tatoeba/sentences.csv")),
        "simplewiki" => FileIfExists(Path.Combine(_root, "SimpleWiki/simplewiki-latest-pages-articles-multistream.xml")),
        "cbt" => DirIfExists(Path.Combine(_root, "CBT/CBTest/data")) ?? DirIfExists(Path.Combine(_root, "CBT/CBTest")),
        _ => null
    };

    public bool IsAvailable(string dataset) => PathFor(dataset) is not null;

    /// <summary>
    /// Stream sentences from a dataset. Falls back to the built-in local sample
    /// when <c>--local-sample</c> was passed or the NAS is not mounted, so absence
    /// of the NAS never blocks development (P0 requirement).
    /// </summary>
    public IEnumerable<string> Sentences(string dataset, int maxSentences)
    {
        var path = _localSample ? null : PathFor(dataset);
        if (path is null)
        {
            if (!_localSample)
                Console.Error.WriteLine($"⚠️  dataset '{dataset}' not found under {_root} — falling back to --local-sample.");
            return LocalSample.Sentences.Take(maxSentences);
        }

        var stream = dataset switch
        {
            "tatoeba_small" => ReadTatoeba(path, languageFilter: null),
            "tatoeba" => ReadTatoeba(path, languageFilter: "eng"),
            "simplewiki" => ReadWikiXml(path),
            "cbt" => ReadDirectoryText(path),
            _ => throw new ArgumentException($"Unknown dataset '{dataset}'. Known: {string.Join(", ", KnownDatasets)}")
        };
        return stream.Take(maxSentences);
    }

    /// <summary>Source description for the RESULTS.md provenance line (rule 9).</summary>
    public string Describe(string dataset) =>
        _localSample || PathFor(dataset) is null
            ? $"{dataset} (LOCAL SAMPLE — {LocalSample.Sentences.Length} built-in sentences)"
            : $"{dataset} ({PathFor(dataset)})";

    // ── Readers ─────────────────────────────────────────────────────────────

    /// Tatoeba export: id \t lang \t text. languageFilter null ⇒ accept every row,
    /// which is what the legacy loader did for the pre-filtered _eng_small file.
    private static IEnumerable<string> ReadTatoeba(string path, string? languageFilter)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1 << 16);
        using var sr = new StreamReader(fs, DetectEncoding(fs) ?? new UTF8Encoding(false));

        string? line;
        while ((line = sr.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = SplitTsv(line, 3);
            if (parts.Count < 3) continue;

            if (languageFilter is not null &&
                !string.Equals(parts[1], languageFilter, StringComparison.OrdinalIgnoreCase))
                continue;

            var text = parts[2].Trim();
            if (text.Length == 0) continue;
            yield return text;
        }
    }

    /// MediaWiki multistream XML: take &lt;text&gt; bodies, strip the heaviest markup,
    /// split into sentences. Coarse on purpose — this is training input, not a parser.
    private static IEnumerable<string> ReadWikiXml(string path)
    {
        using var sr = new StreamReader(path, System.Text.Encoding.UTF8, true, 1 << 16);
        var buffer = new StringBuilder();
        bool inText = false;

        string? line;
        while ((line = sr.ReadLine()) != null)
        {
            if (!inText)
            {
                int open = line.IndexOf("<text", StringComparison.Ordinal);
                if (open < 0) continue;
                int gt = line.IndexOf('>', open);
                if (gt < 0) continue;
                inText = true;
                line = line[(gt + 1)..];
            }

            int close = line.IndexOf("</text>", StringComparison.Ordinal);
            if (close >= 0) { buffer.Append(line[..close]); inText = false; }
            else { buffer.Append(line).Append(' '); continue; }

            foreach (var s in SplitSentences(StripWikiMarkup(buffer.ToString())))
                yield return s;
            buffer.Clear();
        }
    }

    /// CBT and other plain-text trees: every .txt under the root, in sorted order
    /// (sorted, not directory order — rule 8).
    private static IEnumerable<string> ReadDirectoryText(string root)
    {
        var files = Directory.GetFiles(root, "*.txt", SearchOption.AllDirectories);
        Array.Sort(files, StringComparer.Ordinal);
        foreach (var file in files)
            foreach (var line in File.ReadLines(file))
            {
                var text = StripCbtLineNumber(line).Trim();
                if (text.Length == 0) continue;
                foreach (var s in SplitSentences(text)) yield return s;
            }
    }

    // ── Text helpers ────────────────────────────────────────────────────────

    /// CBT lines are "<n> <text>"; the leading ordinal is not language.
    private static string StripCbtLineNumber(string line)
    {
        int i = 0;
        while (i < line.Length && char.IsDigit(line[i])) i++;
        return (i > 0 && i < line.Length && line[i] == ' ') ? line[(i + 1)..] : line;
    }

    private static string StripWikiMarkup(string s)
    {
        var sb = new StringBuilder(s.Length);
        int braces = 0, angles = 0;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c == '{') { braces++; continue; }
            if (c == '}') { if (braces > 0) braces--; continue; }
            if (c == '<') { angles++; continue; }
            if (c == '>') { if (angles > 0) angles--; continue; }
            if (braces > 0 || angles > 0) continue;
            if (c == '[' || c == ']' || c == '\'' || c == '=' || c == '|') { sb.Append(' '); continue; }
            sb.Append(c);
        }
        return sb.ToString();
    }

    private static IEnumerable<string> SplitSentences(string text)
    {
        int start = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] != '.' && text[i] != '!' && text[i] != '?') continue;
            var s = text[start..(i + 1)].Trim();
            start = i + 1;
            if (s.Length >= 8 && s.Contains(' ')) yield return s;
        }
        var tail = text[start..].Trim();
        if (tail.Length >= 8 && tail.Contains(' ')) yield return tail;
    }

    /// <summary>Tokenisation used by every eval — legacy-identical (lowercase, drop 1-char tokens).</summary>
    public static List<string> Tokenize(string sentence) =>
        sentence.ToLowerInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 1)
                .ToList();

    private static System.Text.Encoding? DetectEncoding(FileStream fs)
    {
        var bom = new byte[4];
        var read = fs.Read(bom, 0, 4);
        fs.Position = 0;
        if (read >= 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF) return new UTF8Encoding(true);
        if (read >= 2 && bom[0] == 0xFF && bom[1] == 0xFE) return System.Text.Encoding.Unicode;
        if (read >= 2 && bom[0] == 0xFE && bom[1] == 0xFF) return System.Text.Encoding.BigEndianUnicode;
        return null;
    }

    private static List<string> SplitTsv(string line, int expected)
    {
        var result = new List<string>(expected);
        int start = 0;
        for (int i = 0; i < expected - 1; i++)
        {
            int idx = line.IndexOf('\t', start);
            if (idx < 0) { result.Add(line[start..]); return result; }
            result.Add(line[start..idx]);
            start = idx + 1;
        }
        result.Add(start <= line.Length ? line[start..] : string.Empty);
        return result;
    }

    private static string? FileIfExists(string p) => File.Exists(p) ? p : null;
    private static string? DirIfExists(string p) => Directory.Exists(p) ? p : null;
}
