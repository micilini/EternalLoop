using EternalLoop.BranchAnalysis.Core.Application;
using EternalLoop.BranchAnalysis.Core.Runner;
using FluentAssertions;
using ApplicationBranchAnalysisResult = EternalLoop.BranchAnalysis.Core.Application.BranchAnalysisResult;

namespace EternalLoop.BranchAnalysis.Tests.Application;

public sealed class BranchAnalysisResultTests
{
    [Fact]
    public void ConstructorShouldCreateSummaryFromItemResult()
    {
        var itemResult = new BranchAnalysisItemResult
        {
            Name = "song-a",
            TrackId = "track-id",
            Beats = 16,
            Segments = 8,
            ActiveBranches = 3,
            CandidateBranches = 20,
            OutputPath = "output/branches.json"
        };

        var result = new ApplicationBranchAnalysisResult(itemResult);

        result.ItemResult.Should().BeSameAs(itemResult);
        result.Summary.Name.Should().Be("song-a");
        result.Summary.TrackId.Should().Be("track-id");
        result.Summary.Beats.Should().Be(16);
        result.Summary.Segments.Should().Be(8);
        result.Summary.ActiveBranches.Should().Be(3);
        result.Summary.CandidateBranches.Should().Be(20);
        result.Summary.OutputPath.Should().Be("output/branches.json");
        result.Summary.HasActiveBranches.Should().BeTrue();
        result.Summary.HasCandidateBranches.Should().BeTrue();
    }

    [Fact]
    public void ConstructorShouldRejectNullItemResult()
    {
        var act = () => new ApplicationBranchAnalysisResult(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
