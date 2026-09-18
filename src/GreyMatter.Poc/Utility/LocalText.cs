using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using GreyMatter.Poc.Encoding;
using GreyMatter.Poc.Substrate;

namespace GreyMatter.Poc.Utility;

/// <summary>Version1 text boundary. No vocabulary or learned encoder table.</summary>
public static class LocalText
{
    public const int MaxLine = 16384, MaxToken = 256, MaxCandidates = 4096;
    public const uint Space = 16000000;
    public static IEnumerable<string> Lines(string path)
    {
        using var reader = new StreamReader(path, new UTF8Encoding(false, true), true, 4096);
        var line = new StringBuilder(); int c;
        while ((c = reader.Read()) >= 0)
        {
            if (c == '\n') { yield return line.ToString().TrimEnd('\r'); line.Clear(); }
            else { if (line.Length == MaxLine) throw new InvalidDataException("Line exceeds 16384 characters"); line.Append((char)c); }
        }
        if (line.Length > 0) yield return line.ToString().TrimEnd('\r');
    }
    public static string[] Tokens(string line)
    {
        using var attribution = GreyMatter.Poc.Eval.CostProfile.Enter(GreyMatter.Poc.Eval.CostProfile.Kind.Tokenization);
        if (line.Length > MaxLine) throw new InvalidDataException("Line too long");
        string normalized = line.Normalize(NormalizationForm.FormKC).ToLowerInvariant();
        var words = new List<string>(); var token = new StringBuilder();
        foreach (char c in normalized)
        {
            if (char.IsLetterOrDigit(c)) { if (token.Length == MaxToken) throw new InvalidDataException("Token exceeds 256 characters"); token.Append(c); }
            else Finish();
        }
        Finish(); return words.ToArray();
        void Finish() { if (token.Length > 0) { words.Add(token.ToString()); token.Clear(); } }
    }
    public static IEnumerable<string[]> Sentences(string path, string format)
    {
        if (format != "text" && format != "tatoeba") throw new ArgumentException("Format must be text or tatoeba");
        foreach (string line in Lines(path))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            string text = line;
            if (format == "tatoeba")
            {
                string[] fields = line.Split('\t', 3);
                if (fields.Length != 3) throw new InvalidDataException("Expected Tatoeba id<TAB>language<TAB>text");
                if (fields[1] != "eng") continue;
                text = fields[2];
            }
            var tokens = Tokens(text); if (tokens.Length > 0) yield return tokens;
        }
    }
    public static string Token(string value)
    {
        var tokens = Tokens(value);
        if (tokens.Length != 1) throw new ArgumentException("Supply one token for each cue/candidate");
        return tokens[0];
    }
    public static string[] Candidates(string path)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal); var result = new List<string>();
        foreach (string line in Lines(path))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            string word = Token(line);
            if (seen.Add(word)) { if (result.Count == MaxCandidates) throw new ArgumentException("At most 4096 candidates"); result.Add(word); }
        }
        if (result.Count == 0) throw new ArgumentException("Empty candidate list"); return result.ToArray();
    }
    public static uint Identity(string canonical) => BinaryPrimitives.ReadUInt32LittleEndian(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(canonical)));
    public static uint[] Members(string canonical, int seed)
    {
        using var attribution = GreyMatter.Poc.Eval.CostProfile.Enter(GreyMatter.Poc.Eval.CostProfile.Kind.Encoding);
        uint identity = Identity(canonical); var dims = new int[32];
        for (int d = 0; d < 32; d++) dims[d] = 64*d + (int)(Rng.Bits(seed,Rng.Purpose.Projection,identity,(uint)d)%64);
        var members = new uint[8]; Runtime.Assembly.Members(new SparseCode(dims),checked((int)Space),members);
        return members.Distinct().ToArray();
    }
}
