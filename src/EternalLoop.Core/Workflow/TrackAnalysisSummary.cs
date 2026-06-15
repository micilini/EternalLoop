namespace EternalLoop.Core.Workflow;

public sealed record TrackAnalysisSummary(
    TimeSpan Duration,
    int BeatCount,
    int SegmentCount,
    int SectionCount)
{
    public bool HasBeats => BeatCount > 0;
}
