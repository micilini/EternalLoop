using EternalLoop.AnalysisEngine.Core.Models;

namespace EternalLoop.AnalysisEngine.Core.Analysis;

public sealed class TrackAnalysisSummary
{
    public required string FileHash { get; init; }

    public required double DurationSeconds { get; init; }

    public required int SampleRate { get; init; }

    public required double Tempo { get; init; }

    public required int TimeSignature { get; init; }

    public required int SegmentCount { get; init; }

    public required int BeatCount { get; init; }

    public required int BarCount { get; init; }

    public required int TatumCount { get; init; }

    public required int SectionCount { get; init; }

    public static TrackAnalysisSummary From(TrackAnalysis analysis)
    {
        ArgumentNullException.ThrowIfNull(analysis);

        return new TrackAnalysisSummary
        {
            FileHash = analysis.Metadata.FileHash,
            DurationSeconds = analysis.Metadata.DurationSeconds,
            SampleRate = analysis.Metadata.SampleRate,
            Tempo = analysis.Metadata.Tempo,
            TimeSignature = analysis.Metadata.TimeSignature,
            SegmentCount = analysis.Segments.Count,
            BeatCount = analysis.Beats.Count,
            BarCount = analysis.Bars.Count,
            TatumCount = analysis.Tatums.Count,
            SectionCount = analysis.Sections.Count
        };
    }
}
