using System.Text.Json;
using EternalLoop.AnalysisEngine.Core.Models;
using EternalLoop.AnalysisEngine.Core.Options;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.Models;

public sealed class AnalysisModelsTests
{
    [Fact]
    public void TrackAnalysis_serializes_with_pascal_case()
    {
        var analysis = CreateAnalysis();

        var json = JsonSerializer.Serialize(analysis, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        json.Should().Contain("\"Metadata\"");
        json.Should().Contain("\"Segments\"");
        json.Should().Contain("\"Beats\"");
        json.Should().Contain("\"Bars\"");
        json.Should().Contain("\"Tatums\"");
        json.Should().Contain("\"Sections\"");
        json.Should().Contain("\"MicroFingerprints\"");
        json.Should().Contain("\"Ai\": null");
        json.Should().NotContain("\"metadata\"");
        json.Should().NotContain("\"segments\"");
    }

    [Fact]
    public void TrackAnalysis_contains_required_collections()
    {
        var analysis = CreateAnalysis();

        analysis.Metadata.Should().NotBeNull();
        analysis.Segments.Should().ContainSingle();
        analysis.Beats.Should().ContainSingle();
        analysis.Bars.Should().ContainSingle();
        analysis.Tatums.Should().ContainSingle();
        analysis.Sections.Should().ContainSingle();
        analysis.MicroFingerprints.Should().BeEmpty();
        analysis.Ai.Should().BeNull();
    }

    [Fact]
    public void TrackMetadata_contains_schema_version()
    {
        var metadata = CreateMetadata();

        metadata.SchemaVersion.Should().Be(TrackAnalysis.CurrentSchemaVersion);
        metadata.SchemaVersion.Should().Be("analysis-exporter-0.1.0");
        metadata.AnalyzedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Segment_contains_timbre_and_pitches()
    {
        var segment = CreateSegment();

        segment.Timbre.Should().HaveCount(3);
        segment.Pitches.Should().HaveCount(3);
    }

    [Fact]
    public void Beat_contains_bar_position()
    {
        var beat = CreateBeat();

        beat.BarPosition.Should().HaveCount(4);
        beat.Loudness.Should().ContainSingle();
    }

    [Fact]
    public void LoadedAudio_contains_source_file_information()
    {
        var audio = new LoadedAudio(
            [0.1f, -0.1f],
            AnalysisOptions.DefaultTargetSampleRate,
            1.5,
            "hash",
            "C:\\Music\\song.mp3",
            "song.mp3");

        audio.Samples.Should().HaveCount(2);
        audio.SampleRate.Should().Be(AnalysisOptions.DefaultTargetSampleRate);
        audio.FilePath.Should().Be("C:\\Music\\song.mp3");
        audio.FileName.Should().Be("song.mp3");
    }

    [Fact]
    public void FeatureMatrix_contains_expected_feature_arrays()
    {
        var matrix = new FeatureMatrix
        {
            Mfcc = [[1f, 2f, 3f]],
            Chroma = [[0.1f, 0.2f, 0.3f]],
            SpectralFlux = [0.4f],
            Rms = [0.5f],
            HopLengthSamples = 512,
            FrameSizeSamples = 2048,
            SampleRate = AnalysisOptions.DefaultTargetSampleRate
        };

        matrix.Mfcc.Should().ContainSingle();
        matrix.Chroma.Should().ContainSingle();
        matrix.SpectralFlux.Should().ContainSingle();
        matrix.Rms.Should().ContainSingle();
        matrix.SampleRate.Should().Be(AnalysisOptions.DefaultTargetSampleRate);
    }

    [Fact]
    public void AnalysisOptions_uses_standalone_defaults()
    {
        var options = new AnalysisOptions();

        options.TargetSampleRate.Should().Be(22050);
        options.TimeSignature.Should().Be(4);
        options.SchemaVersion.Should().Be(TrackAnalysis.CurrentSchemaVersion);
        options.Artist.Should().Be("Local");
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
            Sections = [CreateSection()]
        };
    }

    private static TrackMetadata CreateMetadata()
    {
        return new TrackMetadata
        {
            FileHash = "hash",
            FilePath = "C:\\Music\\song.mp3",
            DurationSeconds = 10.0,
            SampleRate = AnalysisOptions.DefaultTargetSampleRate,
            Tempo = 120.0,
            TimeSignature = AnalysisOptions.DefaultTimeSignature,
            SchemaVersion = TrackAnalysis.CurrentSchemaVersion,
            AnalyzedAt = DateTime.UtcNow
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
            Timbre = [1f, 2f, 3f],
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
            Timbre = [1f, 2f, 3f],
            Pitches = [0.1f, 0.2f, 0.3f],
            Loudness = [-9f],
            BarPosition = [1f, 0f, 0f, 0f]
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
