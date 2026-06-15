using EternalLoop.BranchAnalysis.Cli;
using EternalLoop.BranchAnalysis.Cli.Cli;
using EternalLoop.BranchAnalysis.Core.IO;
using EternalLoop.BranchAnalysis.Tests.Fixtures;
using FluentAssertions;

namespace EternalLoop.BranchAnalysis.Tests.Cli;

public sealed class AnalysisBranchCommandTests
{
    [Fact]
    public void RunShouldReturnSuccessAndWriteHelpForHelpArgument()
    {
        using StringWriter output = new();
        using StringWriter error = new();

        int exitCode = AnalysisBranchCommand.Run(["--help"], output, error);

        exitCode.Should().Be(BranchAnalysisExitCodes.Success);
        output.ToString().Should().Contain("Usage:");
        error.ToString().Should().BeEmpty();
    }

    [Fact]
    public void RunShouldReturnInvalidArgumentsForUnknownArgument()
    {
        using StringWriter output = new();
        using StringWriter error = new();

        int exitCode = AnalysisBranchCommand.Run(["--unknown"], output, error);

        exitCode.Should().Be(BranchAnalysisExitCodes.InvalidArguments);
        output.ToString().Should().BeEmpty();
        error.ToString().Should().Contain("Unknown argument: --unknown");
        error.ToString().Should().Contain("Usage:");
    }

    [Fact]
    public void RunShouldReturnAnalysisRootNotFoundForMissingRoot()
    {
        using StringWriter output = new();
        using StringWriter error = new();
        string missingRoot = Path.Combine(CreateTempDirectory(), "missing");

        int exitCode = AnalysisBranchCommand.Run(
        [
            "--analysis-root", missingRoot,
            "--output-root", CreateTempDirectory()
        ], output, error);

        exitCode.Should().Be(BranchAnalysisExitCodes.AnalysisRootNotFound);
        error.ToString().Should().Contain("Analysis root does not exist:");
    }

    [Fact]
    public void RunShouldReturnSuccessWithFullCommand()
    {
        using StringWriter output = new();
        using StringWriter error = new();
        string analysisRoot = CreateTempDirectory();
        string outputRoot = CreateTempDirectory();
        AnalysisFixtureFactory.WriteValidAnalysisFile(analysisRoot, "song-a");

        int exitCode = AnalysisBranchCommand.Run(
        [
            "--analysis-root", analysisRoot,
            "--output-root", outputRoot,
            "--quantum-type", "beats",
            "--max-branches", "4",
            "--max-threshold", "80",
            "--force",
            "--pretty"
        ], output, error);

        exitCode.Should().Be(BranchAnalysisExitCodes.Success);
        error.ToString().Should().BeEmpty();
        File.Exists(Path.Combine(outputRoot, "song-a", BranchAnalysisWriter.BranchFileName)).Should().BeTrue();
    }

    [Fact]
    public void RunShouldReturnInvalidArgumentsForUnsupportedQuantumType()
    {
        using StringWriter output = new();
        using StringWriter error = new();

        int exitCode = AnalysisBranchCommand.Run(["--quantum-type", "bars"], output, error);

        exitCode.Should().Be(BranchAnalysisExitCodes.InvalidArguments);
        error.ToString().Should().Contain("Unsupported quantum type: bars");
    }

    private static string CreateTempDirectory()
    {
        return Directory.CreateTempSubdirectory("eternalloop-branch-cli-").FullName;
    }
}
