using EternalLoop.BranchAnalysis.Core.Runner;

namespace EternalLoop.BranchAnalysis.Core.Application;

public sealed record BranchAnalysisSummary
{
    private BranchAnalysisSummary(
        string name,
        string? trackId,
        int beats,
        int segments,
        int activeBranches,
        int candidateBranches,
        string outputPath)
    {
        Name = name;
        TrackId = trackId;
        Beats = beats;
        Segments = segments;
        ActiveBranches = activeBranches;
        CandidateBranches = candidateBranches;
        OutputPath = outputPath;
    }

    public string Name { get; }

    public string? TrackId { get; }

    public int Beats { get; }

    public int Segments { get; }

    public int ActiveBranches { get; }

    public int CandidateBranches { get; }

    public string OutputPath { get; }

    public bool HasActiveBranches => ActiveBranches > 0;

    public bool HasCandidateBranches => CandidateBranches > 0;

    public static BranchAnalysisSummary From(BranchAnalysisItemResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new BranchAnalysisSummary(
            result.Name,
            result.TrackId,
            result.Beats,
            result.Segments,
            result.ActiveBranches,
            result.CandidateBranches,
            result.OutputPath);
    }
}
