namespace GreyMatter.Poc;

/// <summary>
/// Minimal argument reader. Deliberately not a parser framework: the CLI surface
/// is `gm &lt;command&gt; [--flag value]` and nothing else (plan.md §4.6).
/// </summary>
public sealed class Args
{
    private readonly string[] _argv;

    public Args(string[] argv) => _argv = argv;

    public string? Value(string flag, string? fallback)
    {
        for (int i = 0; i < _argv.Length - 1; i++)
            if (_argv[i] == flag) return _argv[i + 1];
        return fallback;
    }

    public int Int(string flag, int fallback) =>
        int.TryParse(Value(flag, null), out var v) ? v : fallback;

    public double Double(string flag, double fallback) =>
        double.TryParse(Value(flag, null), out var v) ? v : fallback;

    public bool Has(string flag) => Array.IndexOf(_argv, flag) >= 0;

    /// <summary>The exact command line, for the RESULTS.md provenance line (rule 9).</summary>
    public string CommandLine => "gm " + string.Join(' ', _argv);
}
