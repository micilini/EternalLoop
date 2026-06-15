namespace EternalLoop.BranchAnalysis.Core.Runner;

public sealed class BranchAnalysisItemResult
{
    public string Name { get; init; } = string.Empty;
    public string? TrackId { get; init; }
    public int Beats { get; init; }
    public int Segments { get; init; }
    public int ActiveBranches { get; init; }
    public int CandidateBranches { get; init; }
    public string OutputPath { get; init; } = string.Empty;
}
