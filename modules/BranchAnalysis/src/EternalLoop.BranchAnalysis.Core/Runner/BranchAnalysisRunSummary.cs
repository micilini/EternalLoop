namespace EternalLoop.BranchAnalysis.Core.Runner;

public sealed class BranchAnalysisRunSummary
{
    public int ExitCode { get; init; }
    public int DiscoveredTracks { get; init; }
    public int Succeeded { get; init; }
    public int Failed { get; init; }
    public IReadOnlyList<BranchAnalysisItemResult> Results { get; init; } = [];
}
