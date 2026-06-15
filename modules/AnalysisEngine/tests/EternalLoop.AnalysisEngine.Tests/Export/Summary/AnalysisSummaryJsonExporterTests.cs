using System.Text;
using EternalLoop.AnalysisEngine.Core.Export;
using EternalLoop.AnalysisEngine.Core.Export.Summary;
using EternalLoop.AnalysisEngine.Core.Models;
using EternalLoop.AnalysisEngine.Core.Options;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.Export.Summary;

public sealed class AnalysisSummaryJsonExporterTests : IDisposable
{
    private readonly string _rootDirectory;

    public AnalysisSummaryJsonExporterTests()
    {
        _rootDirectory = Path.Combine(Path.GetTempPath(), "eternalloop-analysis-exporter-tests", Guid.NewGuid().ToString("N"));
    }

    [Fact]
    public async Task SummaryExporter_writes_default_file_name()
    {
        var exporter = new AnalysisSummaryJsonExporter();

        var result = await exporter.ExportAsync(
            CreateAnalysis(),
            _rootDirectory,
            CreateRawResult(),
            CreateLoopAnalysisResult(),
            force: false,
            pretty: true,
            CancellationToken.None);

        Path.GetFileName(result.FilePath).Should().Be(AnalysisSummaryJsonExporter.DefaultFileName);
        result.BytesWritten.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SummaryExporter_creates_output_directory()
    {
        var outputDirectory = Path.Combine(_rootDirectory, "nested", "summary");
        var exporter = new AnalysisSummaryJsonExporter();

        var result = await exporter.ExportAsync(
            CreateAnalysis(),
            outputDirectory,
            CreateRawResult(),
            CreateLoopAnalysisResult(),
            force: false,
            pretty: true,
            CancellationToken.None);

        Directory.Exists(outputDirectory).Should().BeTrue();
        File.Exists(result.FilePath).Should().BeTrue();
    }

    [Fact]
    public async Task SummaryExporter_writes_expected_shape()
    {
        var exporter = new AnalysisSummaryJsonExporter();

        var result = await exporter.ExportAsync(
            CreateAnalysis(),
            _rootDirectory,
            CreateRawResult(),
            CreateLoopAnalysisResult(),
            force: false,
            pretty: true,
            CancellationToken.None);

        var json = await File.ReadAllTextAsync(result.FilePath);

        json.Should().Contain("\"schemaVersion\"");
        json.Should().Contain("\"analysis-exporter-summary-v1\"");
        json.Should().Contain("\"input\"");
        json.Should().Contain("\"fileHash\"");
        json.Should().Contain("\"durationSeconds\"");
        json.Should().Contain("\"sampleRate\"");
        json.Should().Contain("\"tempo\"");
        json.Should().Contain("\"timeSignature\"");
        json.Should().Contain("\"counts\"");
        json.Should().Contain("\"segments\"");
        json.Should().Contain("\"beats\"");
        json.Should().Contain("\"bars\"");
        json.Should().Contain("\"tatums\"");
        json.Should().Contain("\"sections\"");
        json.Should().Contain("\"outputs\"");
        json.Should().Contain("\"raw\"");
        json.Should().Contain("\"loopAnalysis\"");
    }

    [Fact]
    public async Task SummaryExporter_writes_utf8_without_bom()
    {
        var exporter = new AnalysisSummaryJsonExporter();

        var result = await exporter.ExportAsync(
            CreateAnalysis(),
            _rootDirectory,
            CreateRawResult(),
            CreateLoopAnalysisResult(),
            force: false,
            pretty: true,
            CancellationToken.None);

        var bytes = await File.ReadAllBytesAsync(result.FilePath);
        var bom = Encoding.UTF8.GetPreamble();

        bytes.Take(bom.Length).Should().NotEqual(bom);
    }

    [Fact]
    public async Task SummaryExporter_refuses_overwrite_without_force()
    {
        var exporter = new AnalysisSummaryJsonExporter();

        await exporter.ExportAsync(
            CreateAnalysis(),
            _rootDirectory,
            CreateRawResult(),
            CreateLoopAnalysisResult(),
            force: false,
            pretty: true,
            CancellationToken.None);

        Func<Task> act = async () => await exporter.ExportAsync(
            CreateAnalysis(),
            _rootDirectory,
            CreateRawResult(),
            CreateLoopAnalysisResult(),
            force: false,
            pretty: true,
            CancellationToken.None);

        await act.Should().ThrowAsync<IOException>().WithMessage("*already exists*");
    }

    [Fact]
    public async Task SummaryExporter_overwrites_with_force()
    {
        Directory.CreateDirectory(_rootDirectory);
        var outputPath = Path.Combine(_rootDirectory, AnalysisSummaryJsonExporter.DefaultFileName);
        await File.WriteAllTextAsync(outputPath, "old", Encoding.UTF8);
        var exporter = new AnalysisSummaryJsonExporter();

        var result = await exporter.ExportAsync(
            CreateAnalysis(),
            _rootDirectory,
            CreateRawResult(),
            CreateLoopAnalysisResult(),
            force: true,
            pretty: true,
            CancellationToken.None);

        var json = await File.ReadAllTextAsync(result.FilePath);

        json.Should().NotBe("old");
        json.Should().Contain("\"schemaVersion\"");
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }
    }

    private static ExportResult CreateRawResult()
    {
        return new ExportResult
        {
            FilePath = "C:\\Exports\\Song\\eternalloop-raw-analysis.json",
            BytesWritten = 100
        };
    }

    private static ExportResult CreateLoopAnalysisResult()
    {
        return new ExportResult
        {
            FilePath = "C:\\Exports\\Song\\eternalloop-analysis.json",
            BytesWritten = 200
        };
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
