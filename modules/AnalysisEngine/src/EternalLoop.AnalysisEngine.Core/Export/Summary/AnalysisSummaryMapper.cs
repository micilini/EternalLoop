using EternalLoop.AnalysisEngine.Core.Models;

namespace EternalLoop.AnalysisEngine.Core.Export.Summary;

public static class AnalysisSummaryMapper
{
    public const string SchemaVersion = "analysis-exporter-summary-v1";

    public static AnalysisSummaryDocument Map(
        TrackAnalysis analysis,
        string? rawOutputPath,
        string? loopAnalysisOutputPath)
    {
        ArgumentNullException.ThrowIfNull(analysis);

        return new AnalysisSummaryDocument
        {
            SchemaVersion = SchemaVersion,
            Input = analysis.Metadata.FilePath,
            FileHash = analysis.Metadata.FileHash,
            DurationSeconds = analysis.Metadata.DurationSeconds,
            SampleRate = analysis.Metadata.SampleRate,
            Tempo = analysis.Metadata.Tempo,
            TimeSignature = analysis.Metadata.TimeSignature,
            Counts = new AnalysisSummaryCountsDocument
            {
                Segments = analysis.Segments.Count,
                Beats = analysis.Beats.Count,
                Bars = analysis.Bars.Count,
                Tatums = analysis.Tatums.Count,
                Sections = analysis.Sections.Count
            },
            Outputs = new AnalysisSummaryOutputsDocument
            {
                Raw = rawOutputPath,
                LoopAnalysis = loopAnalysisOutputPath
            }
        };
    }
}
