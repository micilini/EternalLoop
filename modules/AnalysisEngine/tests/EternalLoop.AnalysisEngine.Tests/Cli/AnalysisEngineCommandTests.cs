using EternalLoop.AnalysisEngine.Cli;
using EternalLoop.AnalysisEngine.Tests.TestData;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.Cli;

public sealed class AnalysisEngineCommandTests : IDisposable
{
    private readonly string _rootDirectory;

    public AnalysisEngineCommandTests()
    {
        _rootDirectory = Path.Combine(
            Path.GetTempPath(),
            "EternalLoopAnalysisEngineTests",
            Guid.NewGuid().ToString("N"));
    }

    [Fact]
    public void Command_returns_input_not_found()
    {
        var output = Path.Combine(_rootDirectory, "out");

        var exitCode = AnalysisEngineProgram.Run(
        [
            "--input",
            Path.Combine(_rootDirectory, "missing.wav"),
            "--output-dir",
            output,
            "--quiet"
        ]);

        exitCode.Should().Be(AnalysisEngineExitCodes.InputFileNotFound);
    }

    [Fact]
    public void Command_writes_raw_when_format_raw()
    {
        var input = CreateInputWaveFile("raw.wav");
        var output = Path.Combine(_rootDirectory, "raw-out");

        var exitCode = AnalysisEngineProgram.Run(
        [
            "--input",
            input,
            "--output-dir",
            output,
            "--format",
            "raw",
            "--force",
            "--quiet"
        ]);

        exitCode.Should().Be(AnalysisEngineExitCodes.Success);
        File.Exists(Path.Combine(output, "eternalloop-raw-analysis.json")).Should().BeTrue();
        File.Exists(Path.Combine(output, "eternalloop-analysis.json")).Should().BeFalse();
        File.Exists(Path.Combine(output, "analysis-summary.json")).Should().BeTrue();
    }

    [Fact]
    public void Command_writes_loop_when_format_loop()
    {
        var input = CreateInputWaveFile("loop.wav");
        var output = Path.Combine(_rootDirectory, "loop-out");

        var exitCode = AnalysisEngineProgram.Run(
        [
            "--input",
            input,
            "--output-dir",
            output,
            "--format",
            "loop",
            "--force",
            "--quiet"
        ]);

        exitCode.Should().Be(AnalysisEngineExitCodes.Success);
        File.Exists(Path.Combine(output, "eternalloop-raw-analysis.json")).Should().BeFalse();
        File.Exists(Path.Combine(output, "eternalloop-analysis.json")).Should().BeTrue();
        File.Exists(Path.Combine(output, "analysis-summary.json")).Should().BeTrue();
    }

    [Fact]
    public void Command_writes_both_when_format_both()
    {
        var input = CreateInputWaveFile("both.wav");
        var output = Path.Combine(_rootDirectory, "both-out");

        var exitCode = AnalysisEngineProgram.Run(
        [
            "--input",
            input,
            "--output-dir",
            output,
            "--format",
            "both",
            "--force",
            "--quiet"
        ]);

        exitCode.Should().Be(AnalysisEngineExitCodes.Success);
        File.Exists(Path.Combine(output, "eternalloop-raw-analysis.json")).Should().BeTrue();
        File.Exists(Path.Combine(output, "eternalloop-analysis.json")).Should().BeTrue();
        File.Exists(Path.Combine(output, "analysis-summary.json")).Should().BeTrue();
    }

    [Fact]
    public void Command_refuses_overwrite_without_force()
    {
        var input = CreateInputWaveFile("overwrite.wav");
        var output = Path.Combine(_rootDirectory, "overwrite-out");
        var args = new[]
        {
            "--input",
            input,
            "--output-dir",
            output,
            "--format",
            "raw",
            "--quiet"
        };

        AnalysisEngineProgram.Run(args).Should().Be(AnalysisEngineExitCodes.Success);

        var exitCode = AnalysisEngineProgram.Run(args);

        exitCode.Should().Be(AnalysisEngineExitCodes.OutputAlreadyExists);
    }

    [Fact]
    public void Command_overwrites_with_force()
    {
        var input = CreateInputWaveFile("force.wav");
        var output = Path.Combine(_rootDirectory, "force-out");
        var args = new[]
        {
            "--input",
            input,
            "--output-dir",
            output,
            "--format",
            "raw",
            "--force",
            "--quiet"
        };

        AnalysisEngineProgram.Run(args).Should().Be(AnalysisEngineExitCodes.Success);
        var exitCode = AnalysisEngineProgram.Run(args);

        exitCode.Should().Be(AnalysisEngineExitCodes.Success);
    }

    [Fact]
    public void Command_writes_summary_for_successful_run()
    {
        var input = CreateInputWaveFile("summary.wav");
        var output = Path.Combine(_rootDirectory, "summary-out");

        var exitCode = AnalysisEngineProgram.Run(
        [
            "--input",
            input,
            "--output-dir",
            output,
            "--format",
            "both",
            "--force",
            "--quiet"
        ]);

        exitCode.Should().Be(AnalysisEngineExitCodes.Success);

        var summaryPath = Path.Combine(output, "analysis-summary.json");
        File.Exists(summaryPath).Should().BeTrue();

        var json = File.ReadAllText(summaryPath);

        json.Should().Contain("\"schemaVersion\"");
        json.Should().Contain("\"analysis-exporter-summary-v1\"");
        json.Should().Contain("\"counts\"");
        json.Should().Contain("\"outputs\"");
        json.Should().Contain("\"raw\"");
        json.Should().Contain("\"loopAnalysis\"");
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }
    }

    private string CreateInputWaveFile(string fileName)
    {
        return TestWaveFileFactory.CreateSineWaveFile(
            _rootDirectory,
            fileName,
            sampleRate: TestSignalFactory.DefaultSampleRate,
            channels: 1,
            durationSeconds: 1.0);
    }
}
