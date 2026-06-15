using EternalLoop.BranchAnalysis.Core.Runner;

namespace EternalLoop.BranchAnalysis.Cli.Cli;

public sealed class BranchAnalysisParseResult
{
    public bool Help { get; init; }
    public BranchAnalysisOptions Options { get; init; } = BranchAnalysisOptions.CreateDefault();
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    public bool IsValid => Errors.Count == 0;
}
