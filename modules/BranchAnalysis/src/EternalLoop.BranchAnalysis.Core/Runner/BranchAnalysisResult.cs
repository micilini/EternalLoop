namespace EternalLoop.BranchAnalysis.Core.Runner;

public sealed class BranchAnalysisResult
{
    public int ExitCode { get; init; }
    public string Message { get; init; } = string.Empty;
    public int DiscoveredTracks { get; init; }
    public int ProcessedTracks { get; init; }
    public int ExportedTracks { get; init; }
}
