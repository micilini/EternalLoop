using EternalLoop.BranchAnalysis.Core.Application;
using EternalLoop.BranchAnalysis.Core.IO;
using EternalLoop.BranchAnalysis.Core.Runner;
using EternalLoop.BranchAnalysis.Tests.Fixtures;
using FluentAssertions;
using ApplicationBranchAnalysisResult = EternalLoop.BranchAnalysis.Core.Application.BranchAnalysisResult;

namespace EternalLoop.BranchAnalysis.Tests.Application;

public sealed class BranchAnalysisServiceTests
{
    [Fact]
    public async Task AnalyzeAsyncShouldProcessAnalysisAndWriteBranchOutput()
    {
        string analysisRoot = CreateTempDirectory();
        string outputRoot = CreateTempDirectory();
        string analysisPath = AnalysisFixtureFactory.WriteValidAnalysisFile(analysisRoot, "song-a");
        var service = new BranchAnalysisService();
        var request = new BranchAnalysisRequest(analysisPath, outputRoot);

        ApplicationBranchAnalysisResult result = await service.AnalyzeAsync(request);

        result.Summary.Name.Should().Be("song-a");
        result.Summary.TrackId.Should().Be("fixture-track");
        result.Summary.Beats.Should().BeGreaterThan(0);
        result.Summary.Segments.Should().BeGreaterThan(0);
        result.Summary.OutputPath.Should().EndWith(BranchAnalysisWriter.BranchFileName);
        File.Exists(result.Summary.OutputPath).Should().BeTrue();
    }

    [Fact]
    public async Task AnalyzeAsyncShouldPreserveForceOption()
    {
        string analysisRoot = CreateTempDirectory();
        string outputRoot = CreateTempDirectory();
        string analysisPath = AnalysisFixtureFactory.WriteValidAnalysisFile(analysisRoot, "song-a");
        string outputDirectory = Path.Combine(outputRoot, "song-a");
        Directory.CreateDirectory(outputDirectory);
        File.WriteAllText(Path.Combine(outputDirectory, BranchAnalysisWriter.BranchFileName), "{}");

        BranchAnalysisOptions options = BranchAnalysisOptions.CreateDefault();
        options.Force = true;

        var service = new BranchAnalysisService();
        var request = new BranchAnalysisRequest(analysisPath, outputRoot, options: options);

        ApplicationBranchAnalysisResult result = await service.AnalyzeAsync(request);

        File.Exists(result.Summary.OutputPath).Should().BeTrue();
    }

    [Fact]
    public async Task AnalyzeAsyncShouldRejectNullRequest()
    {
        var service = new BranchAnalysisService();

        var act = async () => await service.AnalyzeAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task AnalyzeAsyncShouldHonorPreCanceledToken()
    {
        var service = new BranchAnalysisService();
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        var request = new BranchAnalysisRequest(
            "eternalloop-analysis.json",
            CreateTempDirectory());

        var act = async () => await service.AnalyzeAsync(request, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static string CreateTempDirectory()
    {
        return Directory.CreateTempSubdirectory("eternalloop-branch-facade-").FullName;
    }
}
