using EternalLoop.BranchAnalysis.Core.Application;
using EternalLoop.BranchAnalysis.Core.Runner;
using FluentAssertions;

namespace EternalLoop.BranchAnalysis.Tests.Application;

public sealed class BranchAnalysisRequestTests
{
    [Fact]
    public void ConstructorShouldNormalizePathsAndResolveNameFromParentDirectory()
    {
        string analysisPath = Path.Combine("analysis-root", "song-a", "eternalloop-analysis.json");
        string outputRoot = Path.Combine("output-root");

        var request = new BranchAnalysisRequest(analysisPath, outputRoot);

        request.AnalysisPath.Should().EndWith(Path.Combine("analysis-root", "song-a", "eternalloop-analysis.json"));
        request.OutputRoot.Should().EndWith("output-root");
        request.AnalysisName.Should().Be("song-a");
        request.AnalysisDirectory.Should().EndWith(Path.Combine("analysis-root", "song-a"));
        request.Options.Should().NotBeNull();
    }

    [Fact]
    public void ConstructorShouldPreserveExplicitNameAndOptions()
    {
        var options = BranchAnalysisOptions.CreateDefault();
        options.Force = true;

        var request = new BranchAnalysisRequest(
            "eternalloop-analysis.json",
            "output",
            analysisName: "custom-name",
            options);

        request.AnalysisName.Should().Be("custom-name");
        request.Options.Should().BeSameAs(options);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void ConstructorShouldRejectEmptyAnalysisPath(string analysisPath)
    {
        var act = () => new BranchAnalysisRequest(analysisPath, "output");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void ConstructorShouldRejectEmptyOutputRoot(string outputRoot)
    {
        var act = () => new BranchAnalysisRequest("analysis.json", outputRoot);

        act.Should().Throw<ArgumentException>();
    }
}
