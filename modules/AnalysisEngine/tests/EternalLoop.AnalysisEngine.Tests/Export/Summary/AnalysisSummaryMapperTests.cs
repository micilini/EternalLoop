using EternalLoop.AnalysisEngine.Core.Export.Summary;
using EternalLoop.AnalysisEngine.Core.Models;
using EternalLoop.AnalysisEngine.Core.Options;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.Export.Summary;

public sealed class AnalysisSummaryMapperTests
{
    [Fact]
    public void SummaryMapper_includes_counts()
    {
        var document = AnalysisSummaryMapper.Map(
            CreateAnalysis(),
            "C:\\Exports\\Song\\eternalloop-raw-analysis.json",
            "C:\\Exports\\Song\\eternalloop-analysis.json");

        document.SchemaVersion.Should().Be(AnalysisSummaryMapper.SchemaVersion);
        document.Input.Should().Be("C:\\Music\\song.mp3");
        document.FileHash.Should().Be("file-hash");
        document.DurationSeconds.Should().Be(10.0);
        document.SampleRate.Should().Be(AnalysisOptions.DefaultTargetSampleRate);
        document.Tempo.Should().Be(120.0);
        document.TimeSignature.Should().Be(AnalysisOptions.DefaultTimeSignature);
        document.Counts.Segments.Should().Be(1);
        document.Counts.Beats.Should().Be(1);
        document.Counts.Bars.Should().Be(1);
        document.Counts.Tatums.Should().Be(1);
        document.Counts.Sections.Should().Be(1);
    }

    [Fact]
    public void SummaryMapper_includes_output_paths()
    {
        var document = AnalysisSummaryMapper.Map(
            CreateAnalysis(),
            "C:\\Exports\\Song\\eternalloop-raw-analysis.json",
            "C:\\Exports\\Song\\eternalloop-analysis.json");

        document.Outputs.Raw.Should().Be("C:\\Exports\\Song\\eternalloop-raw-analysis.json");
        document.Outputs.LoopAnalysis.Should().Be("C:\\Exports\\Song\\eternalloop-analysis.json");
    }

    [Fact]
    public void SummaryMapper_allows_missing_optional_outputs()
    {
        var document = AnalysisSummaryMapper.Map(
            CreateAnalysis(),
            rawOutputPath: null,
            loopAnalysisOutputPath: "C:\\Exports\\Song\\eternalloop-analysis.json");

        document.Outputs.Raw.Should().BeNull();
        document.Outputs.LoopAnalysis.Should().Be("C:\\Exports\\Song\\eternalloop-analysis.json");
    }

    private static TrackAnalysis CreateAnalysis()
    {
        return new TrackAnalysis
        {
            Metadata = CreateMetadata(),
            Segments = [CreateSegment()],
            Beats = [CreateBeat()],
            Bars = [CreateBar()],
            Tatums = [CreateTatum()],
            Sections = [CreateSection()],
            MicroFingerprints = [],
            Ai = null
        };
    }

    private static TrackMetadata CreateMetadata()
    {
        return new TrackMetadata
        {
            FileHash = "file-hash",
            FilePath = "C:\\Music\\song.mp3",
            DurationSeconds = 10.0,
            SampleRate = AnalysisOptions.DefaultTargetSampleRate,
            Tempo = 120.0,
            TimeSignature = AnalysisOptions.DefaultTimeSignature,
            SchemaVersion = TrackAnalysis.CurrentSchemaVersion,
            AnalyzedAt = DateTime.SpecifyKind(new DateTime(2026, 5, 24, 0, 0, 0), DateTimeKind.Utc)
        };
    }

    private static Segment CreateSegment()
    {
        return new Segment
        {
            Start = 0.0,
            Duration = 0.25,
            Confidence = 1.0,
            LoudnessStart = -12.0,
            LoudnessMax = -6.0,
            LoudnessMaxTime = 0.1,
            Timbre = [1.0f, 2.0f, 3.0f],
            Pitches = [0.1f, 0.2f, 0.3f]
        };
    }

    private static Beat CreateBeat()
    {
        return new Beat
        {
            Index = 0,
            Start = 0.0,
            Duration = 0.5,
            Confidence = 1.0,
            Timbre = [1.0f, 2.0f, 3.0f],
            Pitches = [0.1f, 0.2f, 0.3f],
            Loudness = [-9.0f],
            BarPosition = [1.0f, 0.0f, 0.0f, 0.0f]
        };
    }

    private static Bar CreateBar()
    {
        return new Bar
        {
            Index = 0,
            Start = 0.0,
            Duration = 2.0,
            Confidence = 1.0
        };
    }

    private static Tatum CreateTatum()
    {
        return new Tatum
        {
            Index = 0,
            Start = 0.0,
            Duration = 0.5,
            Confidence = 1.0
        };
    }

    private static Section CreateSection()
    {
        return new Section
        {
            Index = 0,
            Start = 0.0,
            Duration = 10.0,
            Confidence = 1.0,
            Loudness = -9.0,
            Tempo = 120.0,
            Label = "Full Track"
        };
    }
}
