using EternalLoop.AnalysisEngine.Core.Models;

namespace EternalLoop.AnalysisEngine.Core.Application;

public sealed record AnalysisEngineSummary
{
    private AnalysisEngineSummary(
        TimeSpan duration,
        double tempo,
        int sampleRate,
        int beatCount,
        int barCount,
        int tatumCount,
        int segmentCount,
        int sectionCount)
    {
        Duration = duration;
        Tempo = tempo;
        SampleRate = sampleRate;
        BeatCount = beatCount;
        BarCount = barCount;
        TatumCount = tatumCount;
        SegmentCount = segmentCount;
        SectionCount = sectionCount;
    }

    public TimeSpan Duration { get; }

    public double Tempo { get; }

    public int SampleRate { get; }

    public int BeatCount { get; }

    public int BarCount { get; }

    public int TatumCount { get; }

    public int SegmentCount { get; }

    public int SectionCount { get; }

    public bool HasBeats => BeatCount > 0;

    public bool HasSegments => SegmentCount > 0;

    public static AnalysisEngineSummary From(TrackAnalysis analysis)
    {
        ArgumentNullException.ThrowIfNull(analysis);

        return new AnalysisEngineSummary(
            TimeSpan.FromSeconds(analysis.Metadata.DurationSeconds),
            analysis.Metadata.Tempo,
            analysis.Metadata.SampleRate,
            analysis.Beats.Count,
            analysis.Bars.Count,
            analysis.Tatums.Count,
            analysis.Segments.Count,
            analysis.Sections.Count);
    }
}
