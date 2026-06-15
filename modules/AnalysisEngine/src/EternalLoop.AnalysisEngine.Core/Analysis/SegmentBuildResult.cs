using EternalLoop.AnalysisEngine.Core.Models;

namespace EternalLoop.AnalysisEngine.Core.Analysis;

public sealed class SegmentBuildResult
{
    public required IReadOnlyList<Segment> Segments { get; init; }

    public required string Mode { get; init; }

    public double NoveltyBoundaryRatio { get; init; }

    public double TargetDensity { get; init; }

    public double ActualDensity { get; init; }

    public int CandidateCount { get; init; }

    public int SelectedCount { get; init; }
}
