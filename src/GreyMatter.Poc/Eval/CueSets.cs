namespace GreyMatter.Poc.Eval;

/// <summary>
/// The cue and control word lists, ported verbatim from legacy <c>Program.cs</c>.
///
/// Hoisted into one place for the same reason the legacy code hoisted them: the
/// ceiling diagnostic must measure EXACTLY the cues the recall gate judges. Two
/// drifting copies would make the lift comparison (rule 6) meaningless.
/// </summary>
public static class CueSets
{
    public static readonly string[] Trained =
    {
        "the", "you", "we", "are", "to", "it", "in", "so",
        "time", "people", "know", "think", "sleep", "water"
    };

    public static readonly string[] Mash = { "qwertyuiop", "zxcvbnmasd", "xkcdvbnm", "qqzzxxjj" };

    public static readonly string[] Pseudo = { "blorp", "thrumble", "flendish", "grastic" };

    public static string[] AllControls() => Mash.Concat(Pseudo).ToArray();

    public static string Tier(string w) => Array.IndexOf(Mash, w) >= 0 ? "mash  " : "pseudo";
}
