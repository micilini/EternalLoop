using System.Text.Json;
using EternalLoop.AnalysisEngine.Core.BeatTracking;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Shadow;
using EternalLoop.AnalysisEngine.Core.Export;
using EternalLoop.AnalysisEngine.Core.Export.LoopAnalysis;
using EternalLoop.AnalysisEngine.Core.Models;
using EternalLoop.AnalysisEngine.Core.Options;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.Export.LoopAnalysis;

public sealed class LoopAnalysisMapperTests
{
    [Fact]
    public void LoopAnalysisMapper_creates_info_block()
    {
        var document = LoopAnalysisMapper.Map(
            CreateAnalysis(),
            "gangnam-eternalloop",
            "Gangnam Style - EternalLoop Analysis",
            "PSY");

        document.Info.Service.Should().Be(LoopAnalysisMapper.LocalService);
        document.Info.Id.Should().Be("gangnam-eternalloop");
        document.Info.Title.Should().Be("Gangnam Style - EternalLoop Analysis");
        document.Info.Name.Should().Be("Gangnam Style - EternalLoop Analysis");
        document.Info.Artist.Should().Be("PSY");
        document.Info.Url.Should().Be("local://song.mp3");
    }

    [Fact]
    public void LoopAnalysisMapper_converts_duration_to_milliseconds()
    {
        var document = LoopAnalysisMapper.Map(
            CreateAnalysis(durationSeconds: 10.456),
            "track-id",
            "Song",
            "Artist");

        document.Info.Duration.Should().Be(10456);
        document.AudioSummary.Duration.Should().Be(10.456);
    }

    [Fact]
    public void LoopAnalysisMapper_maps_beats_to_lowercase_fields()
    {
        var document = LoopAnalysisMapper.Map(
            CreateAnalysis(),
            "track-id",
            "Song",
            "Artist");

        var json = JsonSerializer.Serialize(document, JsonWriterOptionsFactory.Create(pretty: true));

        json.Should().Contain("\"beats\"");
        json.Should().Contain("\"start\"");
        json.Should().Contain("\"duration\"");
        json.Should().Contain("\"confidence\"");
        json.Should().NotContain("\"Beats\"");
        json.Should().NotContain("\"Start\"");
    }

    [Fact]
    public void LoopAnalysisMapper_maps_segments_to_snake_case_fields()
    {
        var document = LoopAnalysisMapper.Map(
            CreateAnalysis(),
            "track-id",
            "Song",
            "Artist");

        var json = JsonSerializer.Serialize(document, JsonWriterOptionsFactory.Create(pretty: true));

        json.Should().Contain("\"segments\"");
        json.Should().Contain("\"loudness_start\"");
        json.Should().Contain("\"loudness_max\"");
        json.Should().Contain("\"loudness_max_time\"");
        json.Should().NotContain("\"LoudnessStart\"");
        json.Should().NotContain("\"LoudnessMax\"");
        json.Should().NotContain("\"LoudnessMaxTime\"");
    }

    [Fact]
    public void LoopAnalysisMapper_preserves_pitches_array()
    {
        var document = LoopAnalysisMapper.Map(
            CreateAnalysis(),
            "track-id",
            "Song",
            "Artist");

        document.Analysis.Segments[0].Pitches.Should().Equal(0.1f, 0.2f, 0.3f);
    }

    [Fact]
    public void LoopAnalysisMapper_preserves_timbre_array()
    {
        var document = LoopAnalysisMapper.Map(
            CreateAnalysis(),
            "track-id",
            "Song",
            "Artist");

        document.Analysis.Segments[0].Timbre.Should().Equal(1.0f, 2.0f, 3.0f);
    }

    [Fact]
    public void LoopAnalysisMapper_maps_sections_with_defaults()
    {
        var document = LoopAnalysisMapper.Map(
            CreateAnalysis(),
            "track-id",
            "Song",
            "Artist");

        var section = document.Analysis.Sections[0];

        section.Start.Should().Be(0.0);
        section.Duration.Should().Be(10.0);
        section.Confidence.Should().Be(1.0);
        section.Loudness.Should().Be(-9.0);
        section.Tempo.Should().Be(120.0);
        section.TempoConfidence.Should().Be(1.0);
        section.Key.Should().Be(0);
        section.KeyConfidence.Should().Be(0.0);
        section.Mode.Should().Be(1);
        section.ModeConfidence.Should().Be(0.0);
        section.TimeSignature.Should().Be(4);
        section.TimeSignatureConfidence.Should().Be(1.0);
    }

    [Fact]
    public void LoopAnalysisMapper_uses_defaults_when_metadata_arguments_are_missing()
    {
        var document = LoopAnalysisMapper.Map(
            CreateAnalysis(),
            trackId: null,
            title: null,
            artist: null);

        document.Info.Id.Should().Be("song");
        document.Info.Title.Should().Be("song");
        document.Info.Name.Should().Be("song");
        document.Info.Artist.Should().Be(AnalysisOptions.DefaultArtist);
    }

    [Fact]
    public void LoopAnalysisMapper_maps_beat_provider_diagnostics()
    {
        var document = LoopAnalysisMapper.Map(
            CreateAnalysis(),
            "track-id",
            "Song",
            "Artist");

        document.BeatProvider.Name.Should().Be("beat-this");
        document.BeatProvider.Mode.Should().Be("onnx-local");
        document.BeatProvider.ModelName.Should().Be("beat-this-large");
        document.BeatProvider.UsedAi.Should().BeTrue();
        document.BeatProvider.UsedBuiltIn.Should().BeFalse();
        document.BeatProvider.DownbeatCount.Should().Be(2);
        document.BeatProvider.BeatNumberCount.Should().Be(8);
        document.BeatProvider.EstimatedMeter.Should().Be(4);
        document.BeatProvider.TatumMode.Should().Be("fixed-two-per-beat");
        document.BeatProvider.BarPhaseMode.Should().Be("provider-downbeats");
    }

    [Fact]
    public void LoopAnalysisMapper_preserves_shadow_diagnostics()
    {
        var shadow = BeatGridShadowDiagnostics.NotConfigured(CreateBeatTrackingResult());
        var document = LoopAnalysisMapper.Map(
            CreateAnalysis(10.0, CreateBeatProvider(shadow)),
            "track-id",
            "Song",
            "Artist");

        document.BeatProvider.Shadow.Should().BeSameAs(shadow);
    }

    private static TrackAnalysis CreateAnalysis(
        double durationSeconds = 10.0,
        BeatProviderExportDiagnostics? beatProvider = null)
    {
        return new TrackAnalysis
        {
            Metadata = CreateMetadata(durationSeconds),
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
            Version = "1.0",
            License = "MIT",
            ModelName = "beat-this-large",
            ModelSha256 = "abc123",
            UsedAi = true,
            UsedBuiltIn = false,
            UsedFallback = false,
            DownbeatCount = 2,
            BeatNumberCount = 8,
            EstimatedMeter = 4,
            BeatGridMode = "beat-this-onnx-musical-v1",
            TatumMode = "fixed-two-per-beat",
            RequestedTatumMode = "Default",
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

    private static TrackMetadata CreateMetadata(double durationSeconds)
    {
        return new TrackMetadata
        {
            FileHash = "file-hash",
            FilePath = "C:\\Music\\song.mp3",
            DurationSeconds = durationSeconds,
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
