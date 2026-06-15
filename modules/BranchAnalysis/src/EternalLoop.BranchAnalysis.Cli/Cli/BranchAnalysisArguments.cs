namespace EternalLoop.BranchAnalysis.Cli.Cli;

public static class BranchAnalysisArguments
{
    public const string HelpLong = "--help";
    public const string HelpShort = "-h";
    public const string HelpWindows = "/?";
    public const string Force = "--force";
    public const string Pretty = "--pretty";
    public const string Quiet = "--quiet";
    public const string DisableStructuralPolicy = "--disable-structural-policy";
    public const string AnalysisRoot = "--analysis-root";
    public const string OutputRoot = "--output-root";
    public const string QuantumType = "--quantum-type";
    public const string SimilarityThreshold = "--similarity-threshold";
    public const string LookaheadDepth = "--lookahead-depth";
    public const string MinJumpDistance = "--min-jump-distance";
    public const string MaxBranches = "--max-branches";
    public const string MaxThreshold = "--max-threshold";

    public static bool IsHelpFlag(string argument)
    {
        return string.Equals(argument, HelpLong, StringComparison.Ordinal)
            || string.Equals(argument, HelpShort, StringComparison.Ordinal)
            || string.Equals(argument, HelpWindows, StringComparison.Ordinal);
    }
}
