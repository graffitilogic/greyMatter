using System.Text;

namespace GreyMatter.Poc.Encoding;

/// <summary>
/// plan.md §4.2 stage 1 — a faithful port of legacy <c>Core/FeatureEncoder.cs</c>.
///
/// This is the NULL MODEL, not a component we hope works. It encodes surface form
/// only (spelling, n-grams, phonetics) and requires no training. Every later
/// result is reported as lift over what this alone achieves (§1.3, rule 6).
///
/// Ported verbatim in arithmetic — including the `double` accumulators and the
/// legacy StableHash — because the P0 gate is reproduction of the legacy ceiling
/// numbers. Changing the arithmetic here would make that comparison meaningless.
/// float32 discipline (§7) applies to the substrate, not to this boundary stage.
/// </summary>
public sealed class SurfaceEncoder
{
    private readonly int _dimensions;

    public SurfaceEncoder(int dimensions = 128) => _dimensions = dimensions;

    public int Dimensions => _dimensions;

    public double[] Encode(string word)
    {
        if (string.IsNullOrWhiteSpace(word)) return new double[_dimensions];

        var normalized = word.ToLowerInvariant().Trim();
        var f = new double[_dimensions];

        EncodeOrthographic(normalized, f, offset: 0, count: 32);
        EncodeCharNGrams(normalized, f, offset: 32, count: 32);
        EncodePhonetic(normalized, f, offset: 64, count: 32);
        EncodeStatistical(normalized, f, offset: 96, count: 32);

        Normalize(f);
        return f;
    }

    private static void EncodeOrthographic(string word, double[] f, int offset, int count)
    {
        f[offset + 0] = Math.Tanh(word.Length / 10.0);

        int vowels = 0, consonants = 0, digits = 0, special = 0;
        foreach (var c in word)
        {
            bool isVowel = "aeiou".Contains(c);
            if (isVowel) vowels++;
            else if (char.IsLetter(c)) consonants++;
            if (char.IsDigit(c)) digits++;
            if (!char.IsLetterOrDigit(c)) special++;
        }

        double len = Math.Max(1, word.Length);
        f[offset + 1] = vowels / len;
        f[offset + 2] = consonants / len;
        f[offset + 3] = digits / len;
        f[offset + 4] = special / len;

        bool anyUpper = false, allUpper = true;
        foreach (var c in word) { if (char.IsUpper(c)) anyUpper = true; else allUpper = false; }
        f[offset + 5] = char.IsUpper(word[0]) ? 1.0 : 0.0;
        f[offset + 6] = allUpper ? 1.0 : 0.0;
        f[offset + 7] = anyUpper && !allUpper ? 1.0 : 0.0;

        int maxRepeat = 1, run = 1;
        for (int i = 1; i < word.Length; i++)
        {
            if (word[i] == word[i - 1]) { run++; if (run > maxRepeat) maxRepeat = run; }
            else run = 1;
        }
        f[offset + 8] = Math.Tanh(maxRepeat / 3.0);

        for (int i = 9; i < count; i++)
            f[offset + i] = (StableHash(word + "_orth_" + i) % 1000) / 1000.0 - 0.5;
    }

    private static void EncodeCharNGrams(string word, double[] f, int offset, int count)
    {
        var bigrams = new HashSet<string>();
        var trigrams = new HashSet<string>();
        for (int i = 0; i < word.Length - 1; i++)
        {
            bigrams.Add(word.Substring(i, 2));
            if (i < word.Length - 2) trigrams.Add(word.Substring(i, 3));
        }

        int half = count / 2;
        int taken = 0;
        foreach (var bigram in bigrams)
        {
            if (taken++ >= half) break;
            f[offset + StableHash(bigram) % half] += 0.5;
        }
        taken = 0;
        foreach (var trigram in trigrams)
        {
            if (taken++ >= half) break;
            f[offset + half + StableHash(trigram) % half] += 0.5;
        }
    }

    private static void EncodePhonetic(string word, double[] f, int offset, int count)
    {
        f[offset + 0] = Math.Tanh(EstimateSyllables(word) / 4.0);

        if (word.Length > 0)
        {
            var first = word[0].ToString().ToLowerInvariant();
            var last = word[^1].ToString().ToLowerInvariant();

            bool startsWithCluster = word.Length > 1 && !"aeiou".Contains(word[0]) && !"aeiou".Contains(word[1]);
            bool endsWithCluster = word.Length > 1 && !"aeiou".Contains(word[^1]) && !"aeiou".Contains(word[^2]);

            f[offset + 1] = startsWithCluster ? 1.0 : 0.0;
            f[offset + 2] = endsWithCluster ? 1.0 : 0.0;
            f[offset + 3] = (StableHash("first_" + first) % 1000) / 1000.0 - 0.5;
            f[offset + 4] = (StableHash("last_" + last) % 1000) / 1000.0 - 0.5;
        }

        for (int i = 5; i < count; i++)
            f[offset + i] = (StableHash(word + "_phon_" + i) % 1000) / 1000.0 - 0.5;
    }

    private static void EncodeStatistical(string word, double[] f, int offset, int count)
    {
        var freq = GetFrequencyEstimate(word);
        f[offset + 0] = Math.Tanh(Math.Log(freq + 1) / 10.0);

        var rank = EstimateRank(word);
        f[offset + 1] = Math.Tanh(Math.Log(rank + 1) / 10.0);

        f[offset + 2] = (StableHash(GetWordShape(word)) % 1000) / 1000.0 - 0.5;

        for (int i = 3; i < count; i++)
            f[offset + i] = (StableHash(word + "_stat_" + i) % 1000) / 1000.0 - 0.5;
    }

    private static int EstimateSyllables(string word)
    {
        int count = 0;
        bool inVowelGroup = false;
        foreach (var c in word.ToLowerInvariant())
        {
            bool isVowel = "aeiouy".Contains(c);
            if (isVowel && !inVowelGroup) { count++; inVowelGroup = true; }
            else if (!isVowel) inVowelGroup = false;
        }
        if (word.EndsWith('e') && count > 1) count--;
        return Math.Max(1, count);
    }

    private static readonly string[] CommonPatterns = { "the", "ing", "er", "ed", "ly", "s" };

    private static double GetFrequencyEstimate(string word)
    {
        var lengthScore = Math.Max(0, 10 - word.Length) / 10.0;
        int hits = 0;
        foreach (var p in CommonPatterns) if (word.Contains(p)) hits++;
        return Math.Max(1.0, lengthScore * 10 + hits * 0.1 * 5);
    }

    private static int EstimateRank(string word)
    {
        var freq = GetFrequencyEstimate(word);
        if (freq > 5.0) return (int)(1000 * (10.0 / freq));
        if (freq > 1.0) return (int)(1000 + 9000 * (5.0 - freq) / 4.0);
        return 10000 + (int)(1000 * (1.0 / Math.Max(0.1, freq)));
    }

    private static string GetWordShape(string word)
    {
        var sb = new StringBuilder(word.Length);
        foreach (var c in word)
        {
            if (char.IsUpper(c)) sb.Append('X');
            else if (char.IsLower(c)) sb.Append('x');
            else if (char.IsDigit(c)) sb.Append('9');
            else sb.Append(c);
        }
        return sb.ToString();
    }

    private static void Normalize(double[] v)
    {
        double sum = 0;
        for (int i = 0; i < v.Length; i++) sum += v[i] * v[i];
        var mag = Math.Sqrt(sum);
        if (mag > 0)
            for (int i = 0; i < v.Length; i++) v[i] /= mag;
    }

    /// <summary>Legacy-identical deterministic hash. Do not "improve" — it defines the baseline.</summary>
    internal static int StableHash(string input)
    {
        unchecked
        {
            int hash = 17;
            foreach (char c in input) hash = hash * 31 + c;
            return Math.Abs(hash);
        }
    }
}
