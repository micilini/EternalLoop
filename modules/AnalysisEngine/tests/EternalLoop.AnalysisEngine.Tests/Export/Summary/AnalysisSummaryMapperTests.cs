using EternalLoop.AnalysisEngine.Core.BeatTracking;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Shadow;
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

    [Fact]
    public void AnalysisSummaryMapper_maps_beat_provider_diagnostics()
    {
        var summary = AnalysisSummaryMapper.Map(
            CreateAnalysis(),
            rawOutputPath: "raw.json",
            loopAnalysisOutputPath: "loop.json");

        summary.BeatProvider.Name.Should().Be("beat-this");
        summary.BeatProvider.Mode.Should().Be("onnx-local");
        summary.BeatProvider.ModelName.Should().Be("beat-this-large");
        summary.BeatProvider.UsedAi.Should().BeTrue();
        summary.BeatProvider.UsedBuiltIn.Should().BeFalse();
        summary.BeatProvider.UsedFallback.Should().BeFalse();
        summary.BeatProvider.DownbeatCount.Should().Be(2);
        summary.BeatProvider.BeatNumberCount.Should().Be(8);
        summary.BeatProvider.EstimatedMeter.Should().Be(4);
        summary.BeatProvider.BeatGridMode.Should().Be("beat-this-onnx-musical-v1");
        summary.BeatProvider.TatumMode.Should().Be("fixed-two-per-beat");
        summary.BeatProvider.BarPhaseMode.Should().Be("provider-downbeats");
    }

    [Fact]
    public void SummaryMapper_preserves_shadow_diagnostics()
    {
        var shadow = BeatGridShadowDiagnostics.NotConfigured(CreateBeatTrackingResult());
        var summary = AnalysisSummaryMapper.Map(
            CreateAnalysis(CreateBeatProvider(shadow)),
            rawOutputPath: "raw.json",
            loopAnalysisOutputPath: "loop.json");

        summary.BeatProvider.Shadow.Should().BeSameAs(shadow);
    }

    private static TrackAnalysis CreateAnalysis(BeatProviderExportDiagnostics? beatProvider = null)
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
            Ai = null,
            BeatProvider = beatProvider ?? CreateBeatProvider()
        };
    }

    private static BeatProviderExportDiagnostics CreateBeatProvider(BeatGridShadowDiagnostics? shadow = null)
    {
        return new BeatProviderExportDiagnostics
        {
            Name = "beat-this",
            Mode = "onnx-local",
            ModelName = "beat-this-large",
            UsedAi = true,
            UsedBuiltIn = false,
            UsedFallback = false,
            DownbeatCount = 2,
            BeatNumberCount = 8,
            EstimatedMeter = 4,
            BeatGridMode = "beat-this-onnx-musical-v1",
            TatumMode = "fixed-two-per-beat",
            BarPhaseMode = "provider-downbeats",
            Shadow = shadow
        };
    }

    private static BeatTrackingResult CreateBeatTrackingResult()
    {
        return new BeatTrackingResult
        {
            EstimatedBpm = 120.0,
            BeatTimes = [0.0, 0.5, 1.0, 1.5],
            Confidences = [1.0, 0.9, 0.8, 0.7]
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
