using System.Text;
using EternalLoop.AnalysisEngine.Core.Export;
using EternalLoop.AnalysisEngine.Core.Models;
using EternalLoop.AnalysisEngine.Core.Options;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.Export;

public sealed class RawAnalysisJsonExporterTests : IDisposable
{
    private readonly string _rootDirectory;

    public RawAnalysisJsonExporterTests()
    {
        _rootDirectory = Path.Combine(Path.GetTempPath(), "eternalloop-analysis-exporter-tests", Guid.NewGuid().ToString("N"));
    }

    [Fact]
    public async Task RawExporter_creates_output_directory()
    {
        var outputDirectory = Path.Combine(_rootDirectory, "nested", "raw");
        var exporter = new RawAnalysisJsonExporter();

        var result = await exporter.ExportAsync(
            CreateAnalysis(),
            outputDirectory,
            force: false,
            pretty: true,
            CancellationToken.None);

        Directory.Exists(outputDirectory).Should().BeTrue();
        File.Exists(result.FilePath).Should().BeTrue();
    }

    [Fact]
    public async Task RawExporter_writes_default_file_name()
    {
        var exporter = new RawAnalysisJsonExporter();

        var result = await exporter.ExportAsync(
            CreateAnalysis(),
            _rootDirectory,
            force: false,
            pretty: true,
            CancellationToken.None);

        Path.GetFileName(result.FilePath).Should().Be(RawAnalysisJsonExporter.DefaultFileName);
        result.BytesWritten.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RawExporter_preserves_pascal_case()
    {
        var exporter = new RawAnalysisJsonExporter();

        var result = await exporter.ExportAsync(
            CreateAnalysis(),
            _rootDirectory,
            force: false,
            pretty: true,
            CancellationToken.None);

        var json = await File.ReadAllTextAsync(result.FilePath);

        json.Should().Contain("\"Metadata\"");
        json.Should().Contain("\"Segments\"");
        json.Should().Contain("\"Beats\"");
        json.Should().Contain("\"Bars\"");
        json.Should().Contain("\"Tatums\"");
        json.Should().Contain("\"Sections\"");
        json.Should().Contain("\"MicroFingerprints\"");
        json.Should().NotContain("\"metadata\"");
        json.Should().NotContain("\"segments\"");
    }

    [Fact]
    public async Task RawExporter_writes_utf8_without_bom()
    {
        var exporter = new RawAnalysisJsonExporter();

        var result = await exporter.ExportAsync(
            CreateAnalysis(),
            _rootDirectory,
            force: false,
            pretty: true,
            CancellationToken.None);

        var bytes = await File.ReadAllBytesAsync(result.FilePath);
        var bom = Encoding.UTF8.GetPreamble();

        bytes.Take(bom.Length).Should().NotEqual(bom);
    }

    [Fact]
    public async Task RawExporter_refuses_overwrite_without_force()
    {
        var exporter = new RawAnalysisJsonExporter();

        await exporter.ExportAsync(
            CreateAnalysis(),
            _rootDirectory,
            force: false,
            pretty: true,
            CancellationToken.None);

        Func<Task> act = async () => await exporter.ExportAsync(
            CreateAnalysis(),
            _rootDirectory,
            force: false,
            pretty: true,
            CancellationToken.None);

        await act.Should().ThrowAsync<IOException>().WithMessage("*already exists*");
    }

    [Fact]
    public async Task RawExporter_overwrites_with_force()
    {
        Directory.CreateDirectory(_rootDirectory);
        var outputPath = Path.Combine(_rootDirectory, RawAnalysisJsonExporter.DefaultFileName);
        await File.WriteAllTextAsync(outputPath, "old", Encoding.UTF8);
        var exporter = new RawAnalysisJsonExporter();

        var result = await exporter.ExportAsync(
            CreateAnalysis(),
            _rootDirectory,
            force: true,
            pretty: true,
            CancellationToken.None);

        var json = await File.ReadAllTextAsync(result.FilePath);

        json.Should().NotBe("old");
        json.Should().Contain("\"Metadata\"");
    }

    [Fact]
    public async Task RawExporter_preserves_ai_null()
    {
        var exporter = new RawAnalysisJsonExporter();

        var result = await exporter.ExportAsync(
            CreateAnalysis(),
            _rootDirectory,
            force: false,
            pretty: true,
            CancellationToken.None);

        var json = await File.ReadAllTextAsync(result.FilePath);

        json.Should().Contain("\"Ai\": null");
        json.Should().Contain("\"MicroFingerprints\": []");
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
