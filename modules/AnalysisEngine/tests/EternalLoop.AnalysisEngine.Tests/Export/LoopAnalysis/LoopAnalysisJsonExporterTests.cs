using System.Text;
using EternalLoop.AnalysisEngine.Core.Export.LoopAnalysis;
using EternalLoop.AnalysisEngine.Core.Models;
using EternalLoop.AnalysisEngine.Core.Options;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.Export.LoopAnalysis;

public sealed class LoopAnalysisJsonExporterTests : IDisposable
{
    private readonly string _rootDirectory;

    public LoopAnalysisJsonExporterTests()
    {
        _rootDirectory = Path.Combine(Path.GetTempPath(), "eternalloop-analysis-exporter-tests", Guid.NewGuid().ToString("N"));
    }

    [Fact]
    public async Task LoopAnalysisExporter_writes_default_file_name()
    {
        var exporter = new LoopAnalysisJsonExporter();

        var result = await exporter.ExportAsync(
            CreateAnalysis(),
            _rootDirectory,
            "track-id",
            "Song",
            "Artist",
            force: false,
            pretty: true,
            CancellationToken.None);

        Path.GetFileName(result.FilePath).Should().Be(LoopAnalysisJsonExporter.DefaultFileName);
        result.BytesWritten.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task LoopAnalysisExporter_creates_output_directory()
    {
        var outputDirectory = Path.Combine(_rootDirectory, "nested", "loop-analysis");
        var exporter = new LoopAnalysisJsonExporter();

        var result = await exporter.ExportAsync(
            CreateAnalysis(),
            outputDirectory,
            "track-id",
            "Song",
            "Artist",
            force: false,
            pretty: true,
            CancellationToken.None);

        Directory.Exists(outputDirectory).Should().BeTrue();
        File.Exists(result.FilePath).Should().BeTrue();
    }

    [Fact]
    public async Task LoopAnalysisExporter_writes_expected_root_shape()
    {
        var exporter = new LoopAnalysisJsonExporter();

        var result = await exporter.ExportAsync(
            CreateAnalysis(),
            _rootDirectory,
            "track-id",
            "Song",
            "Artist",
            force: false,
            pretty: true,
            CancellationToken.None);

        var json = await File.ReadAllTextAsync(result.FilePath);

        json.Should().Contain("\"info\"");
        json.Should().Contain("\"analysis\"");
        json.Should().Contain("\"audio_summary\"");
        json.Should().Contain("\"sections\"");
        json.Should().Contain("\"bars\"");
        json.Should().Contain("\"beats\"");
        json.Should().Contain("\"tatums\"");
        json.Should().Contain("\"segments\"");
        json.Should().NotContain("\"Metadata\"");
        json.Should().NotContain("\"MicroFingerprints\"");
        json.Should().NotContain("\"Ai\"");
    }

    [Fact]
    public async Task LoopAnalysisExporter_writes_utf8_without_bom()
    {
        var exporter = new LoopAnalysisJsonExporter();

        var result = await exporter.ExportAsync(
            CreateAnalysis(),
            _rootDirectory,
            "track-id",
            "Song",
            "Artist",
            force: false,
            pretty: true,
            CancellationToken.None);

        var bytes = await File.ReadAllBytesAsync(result.FilePath);
        var bom = Encoding.UTF8.GetPreamble();

        bytes.Take(bom.Length).Should().NotEqual(bom);
    }

    [Fact]
    public async Task LoopAnalysisExporter_refuses_overwrite_without_force()
    {
        var exporter = new LoopAnalysisJsonExporter();

        await exporter.ExportAsync(
            CreateAnalysis(),
            _rootDirectory,
            "track-id",
            "Song",
            "Artist",
            force: false,
            pretty: true,
            CancellationToken.None);

        Func<Task> act = async () => await exporter.ExportAsync(
            CreateAnalysis(),
            _rootDirectory,
            "track-id",
            "Song",
            "Artist",
            force: false,
            pretty: true,
            CancellationToken.None);

        await act.Should().ThrowAsync<IOException>().WithMessage("*already exists*");
    }

    [Fact]
    public async Task LoopAnalysisExporter_overwrites_with_force()
    {
        Directory.CreateDirectory(_rootDirectory);
        var outputPath = Path.Combine(_rootDirectory, LoopAnalysisJsonExporter.DefaultFileName);
        await File.WriteAllTextAsync(outputPath, "old", Encoding.UTF8);
        var exporter = new LoopAnalysisJsonExporter();

        var result = await exporter.ExportAsync(
            CreateAnalysis(),
            _rootDirectory,
            "track-id",
            "Song",
            "Artist",
            force: true,
            pretty: true,
            CancellationToken.None);

        var json = await File.ReadAllTextAsync(result.FilePath);

        json.Should().NotBe("old");
        json.Should().Contain("\"info\"");
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }
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
