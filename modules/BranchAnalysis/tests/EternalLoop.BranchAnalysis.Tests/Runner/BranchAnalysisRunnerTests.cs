using System.Text.Json.Nodes;
using EternalLoop.BranchAnalysis.Core.IO;
using EternalLoop.BranchAnalysis.Core.Runner;
using EternalLoop.BranchAnalysis.Tests.Fixtures;
using FluentAssertions;

namespace EternalLoop.BranchAnalysis.Tests.Runner;

public sealed class BranchAnalysisRunnerTests
{
    [Fact]
    public void RunShouldReturnAnalysisRootNotFoundWhenInputRootDoesNotExist()
    {
        using StringWriter output = new();
        using StringWriter error = new();
        BranchAnalysisOptions options = CreateOptions(Path.Combine(CreateTempDirectory(), "missing"), CreateTempDirectory());

        int exitCode = BranchAnalysisRunner.Run(options, output, error);

        exitCode.Should().Be(BranchAnalysisRunnerExitCodes.AnalysisRootNotFound);
        error.ToString().Should().Contain("Analysis root does not exist:");
        output.ToString().Should().BeEmpty();
    }

    [Fact]
    public void RunShouldReturnNoAnalysisFilesFoundWhenRootHasNoAnalysisFiles()
    {
        using StringWriter output = new();
        using StringWriter error = new();
        string analysisRoot = CreateTempDirectory();
        BranchAnalysisOptions options = CreateOptions(analysisRoot, CreateTempDirectory());

        int exitCode = BranchAnalysisRunner.Run(options, output, error);

        exitCode.Should().Be(BranchAnalysisRunnerExitCodes.NoAnalysisFilesFound);
        error.ToString().Should().Contain("No eternalloop-analysis.json files were found");
    }

    [Fact]
    public void RunShouldProcessValidAnalysisAndWriteBranchOutput()
    {
        using StringWriter output = new();
        using StringWriter error = new();
        string analysisRoot = CreateTempDirectory();
        string outputRoot = CreateTempDirectory();
        AnalysisFixtureFactory.WriteValidAnalysisFile(analysisRoot, "song-a");
        BranchAnalysisOptions options = CreateOptions(analysisRoot, outputRoot);

        int exitCode = BranchAnalysisRunner.Run(options, output, error);

        exitCode.Should().Be(BranchAnalysisRunnerExitCodes.Success);
        error.ToString().Should().BeEmpty();
        output.ToString().Should().Contain("EternalLoop Branch Analysis CLI");
        output.ToString().Should().Contain("Analyzing branches: song-a");
        output.ToString().Should().Contain("Branch analysis finished");

        string branchPath = Path.Combine(outputRoot, "song-a", BranchAnalysisWriter.BranchFileName);
        File.Exists(branchPath).Should().BeTrue();
        JsonNode json = BranchAnalysisJsonReader.Read(branchPath);
        JsonArray activeBranches = json["activeBranches"]!.AsArray();
        JsonArray candidateBranches = json["candidateBranches"]!.AsArray();
        json["schemaVersion"]!.GetValue<string>().Should().Be("eternalloop-branch-export-v1");
        json["counts"]!["activeBranches"]!.GetValue<int>().Should().Be(activeBranches.Count);
        json["counts"]!["candidateBranches"]!.GetValue<int>().Should().Be(candidateBranches.Count);
    }

    [Fact]
    public void RunShouldWriteBranchJsonWithoutBom()
    {
        using StringWriter output = new();
        using StringWriter error = new();
        string analysisRoot = CreateTempDirectory();
        string outputRoot = CreateTempDirectory();
        AnalysisFixtureFactory.WriteValidAnalysisFile(analysisRoot, "song-a");
        BranchAnalysisOptions options = CreateOptions(analysisRoot, outputRoot);

        int exitCode = BranchAnalysisRunner.Run(options, output, error);

        exitCode.Should().Be(BranchAnalysisRunnerExitCodes.Success);
        string branchPath = Path.Combine(outputRoot, "song-a", BranchAnalysisWriter.BranchFileName);
        byte[] bytes = File.ReadAllBytes(branchPath);
        bytes.Should().NotBeEmpty();
        (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF).Should().BeFalse();
        bytes[0].Should().Be((byte)'{');
    }

    [Fact]
    public void RunShouldReturnValidationFailedForInvalidAnalysisContract()
    {
        using StringWriter output = new();
        using StringWriter error = new();
        string analysisRoot = CreateTempDirectory();
        AnalysisFixtureFactory.WriteInvalidAnalysisFile(analysisRoot, "song-a");
        BranchAnalysisOptions options = CreateOptions(analysisRoot, CreateTempDirectory());

        int exitCode = BranchAnalysisRunner.Run(options, output, error);

        exitCode.Should().Be(BranchAnalysisRunnerExitCodes.ValidationFailed);
        error.ToString().Should().Contain("Failed to analyze branches for song-a");
        error.ToString().Should().Contain("audio_summary.duration");
    }

    [Fact]
    public void RunShouldReturnExportFailedWhenOutputAlreadyExistsAndForceIsFalse()
    {
        using StringWriter output = new();
        using StringWriter error = new();
        string analysisRoot = CreateTempDirectory();
        string outputRoot = CreateTempDirectory();
        AnalysisFixtureFactory.WriteValidAnalysisFile(analysisRoot, "song-a");
        string existingDirectory = Path.Combine(outputRoot, "song-a");
        Directory.CreateDirectory(existingDirectory);
        File.WriteAllText(Path.Combine(existingDirectory, BranchAnalysisWriter.BranchFileName), "{}");
        BranchAnalysisOptions options = CreateOptions(analysisRoot, outputRoot);
        options.Force = false;

        int exitCode = BranchAnalysisRunner.Run(options, output, error);

        exitCode.Should().Be(BranchAnalysisRunnerExitCodes.ExportFailed);
        error.ToString().Should().Contain("Failed to analyze branches for song-a");
        error.ToString().Should().Contain("Branch output already exists:");
    }

    [Fact]
    public void RunShouldReduceOutputWhenQuietIsTrue()
    {
        using StringWriter output = new();
        using StringWriter error = new();
        string analysisRoot = CreateTempDirectory();
        string outputRoot = CreateTempDirectory();
        AnalysisFixtureFactory.WriteValidAnalysisFile(analysisRoot, "song-a");
        BranchAnalysisOptions options = CreateOptions(analysisRoot, outputRoot);
        options.Quiet = true;

        int exitCode = BranchAnalysisRunner.Run(options, output, error);

        exitCode.Should().Be(BranchAnalysisRunnerExitCodes.Success);
        output.ToString().Should().NotContain("EternalLoop Branch Analysis CLI");
        output.ToString().Should().NotContain("Analyzing branches");
        output.ToString().Should().NotContain("Branch analysis finished");
        error.ToString().Should().BeEmpty();
        File.Exists(Path.Combine(outputRoot, "song-a", BranchAnalysisWriter.BranchFileName)).Should().BeTrue();
    }

    [Fact]
    public void RunShouldContinueProcessingAfterItemFailure()
    {
        using StringWriter output = new();
        using StringWriter error = new();
        string analysisRoot = CreateTempDirectory();
        string outputRoot = CreateTempDirectory();
        AnalysisFixtureFactory.WriteInvalidAnalysisFile(analysisRoot, "bad-song");
        AnalysisFixtureFactory.WriteValidAnalysisFile(analysisRoot, "good-song");
        BranchAnalysisOptions options = CreateOptions(analysisRoot, outputRoot);

        int exitCode = BranchAnalysisRunner.Run(options, output, error);

        exitCode.Should().Be(BranchAnalysisRunnerExitCodes.ValidationFailed);
        error.ToString().Should().Contain("bad-song");
        output.ToString().Should().Contain("Analyzing branches: good-song");
        File.Exists(Path.Combine(outputRoot, "good-song", BranchAnalysisWriter.BranchFileName)).Should().BeTrue();
    }

    private static BranchAnalysisOptions CreateOptions(string analysisRoot, string outputRoot)
    {
        BranchAnalysisOptions options = BranchAnalysisOptions.CreateDefault();
        options.AnalysisRoot = analysisRoot;
        options.OutputRoot = outputRoot;
        return options;
    }

    private static string CreateTempDirectory()
    {
        return Directory.CreateTempSubdirectory("eternalloop-branch-runner-").FullName;
    }
}
