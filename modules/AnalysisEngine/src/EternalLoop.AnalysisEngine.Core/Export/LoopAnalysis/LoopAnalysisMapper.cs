using EternalLoop.AnalysisEngine.Core.Models;
using EternalLoop.AnalysisEngine.Core.Options;

namespace EternalLoop.AnalysisEngine.Core.Export.LoopAnalysis;

public static class LoopAnalysisMapper
{
    public const string LocalService = "LOCAL";

    private const int MillisecondsPerSecond = 1000;
    private const int DefaultKey = 0;
    private const int DefaultMode = 1;
    private const double DefaultConfidence = 1.0;
    private const double UnknownConfidence = 0.0;
    private const string DefaultArtist = AnalysisOptions.DefaultArtist;

    public static LoopAnalysisDocument Map(
        TrackAnalysis analysis,
        string? trackId,
        string? title,
        string? artist)
    {
        ArgumentNullException.ThrowIfNull(analysis);

        var fileName = GetFileName(analysis.Metadata.FilePath);
        var titleFallback = GetTitleFallback(fileName);
        var resolvedTitle = ResolveText(title, titleFallback);

        return new LoopAnalysisDocument
        {
            Info = new LoopAnalysisInfoDocument
            {
                Service = LocalService,
                Id = ResolveText(trackId, titleFallback),
                Title = resolvedTitle,
                Name = resolvedTitle,
                Artist = ResolveText(artist, DefaultArtist),
                Url = $"local://{fileName}",
                Duration = ConvertDurationToMilliseconds(analysis.Metadata.DurationSeconds)
            },
            Analysis = new LoopAnalysisBodyDocument
            {
                Sections = analysis.Sections.Select(section => MapSection(section, analysis.Metadata)).ToArray(),
                Bars = analysis.Bars.Select(MapTimeQuantum).ToArray(),
                Beats = analysis.Beats.Select(MapTimeQuantum).ToArray(),
                Tatums = analysis.Tatums.Select(MapTimeQuantum).ToArray(),
                Segments = analysis.Segments.Select(MapSegment).ToArray()
            },
            AudioSummary = new LoopAnalysisAudioSummaryDocument
            {
                Duration = analysis.Metadata.DurationSeconds
            },
            BeatProvider = LoopAnalysisBeatProviderDocument.From(analysis.BeatProvider)
        };
    }

    public static long ConvertDurationToMilliseconds(double durationSeconds)
    {
        return Convert.ToInt64(Math.Round(durationSeconds * MillisecondsPerSecond, MidpointRounding.AwayFromZero));
    }

    private static LoopAnalysisTimeQuantumDocument MapTimeQuantum(Beat beat)
    {
        return new LoopAnalysisTimeQuantumDocument
        {
            Start = beat.Start,
            Duration = beat.Duration,
            Confidence = beat.Confidence
        };
    }

    private static LoopAnalysisTimeQuantumDocument MapTimeQuantum(Bar bar)
    {
        return new LoopAnalysisTimeQuantumDocument
        {
            Start = bar.Start,
            Duration = bar.Duration,
            Confidence = bar.Confidence
        };
    }

    private static LoopAnalysisTimeQuantumDocument MapTimeQuantum(Tatum tatum)
    {
        return new LoopAnalysisTimeQuantumDocument
        {
            Start = tatum.Start,
            Duration = tatum.Duration,
            Confidence = tatum.Confidence
        };
    }

    private static LoopAnalysisSegmentDocument MapSegment(Segment segment)
    {
        return new LoopAnalysisSegmentDocument
        {
            Start = segment.Start,
            Duration = segment.Duration,
            Confidence = segment.Confidence,
            LoudnessStart = segment.LoudnessStart,
            LoudnessMax = segment.LoudnessMax,
            LoudnessMaxTime = segment.LoudnessMaxTime,
            Pitches = segment.Pitches.ToArray(),
            Timbre = segment.Timbre.ToArray()
        };
    }

    private static LoopAnalysisSectionDocument MapSection(Section section, TrackMetadata metadata)
    {
        var tempo = section.Tempo > 0.0 ? section.Tempo : metadata.Tempo;
        var timeSignature = metadata.TimeSignature > 0 ? metadata.TimeSignature : AnalysisOptions.DefaultTimeSignature;

        return new LoopAnalysisSectionDocument
        {
            Start = section.Start,
            Duration = section.Duration,
            Confidence = section.Confidence,
            Loudness = section.Loudness,
            Tempo = tempo,
            TempoConfidence = DefaultConfidence,
            Key = DefaultKey,
            KeyConfidence = UnknownConfidence,
            Mode = DefaultMode,
            ModeConfidence = UnknownConfidence,
            TimeSignature = timeSignature,
            TimeSignatureConfidence = DefaultConfidence
        };
    }

    private static string ResolveText(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string GetFileName(string filePath)
    {
        var normalized = filePath.Replace('\\', '/');
        var fileName = Path.GetFileName(normalized);

        return string.IsNullOrWhiteSpace(fileName) ? "local-audio" : fileName;
    }

    private static string GetTitleFallback(string fileName)
    {
        var title = Path.GetFileNameWithoutExtension(fileName);

        return string.IsNullOrWhiteSpace(title) ? "local-audio" : title;
    }
}
