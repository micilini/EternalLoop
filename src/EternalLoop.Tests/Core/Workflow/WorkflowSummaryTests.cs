using EternalLoop.Core.Workflow;
using FluentAssertions;

namespace EternalLoop.Tests.Core.Workflow;

public sealed class WorkflowSummaryTests
{
    [Fact]
    public void TrackAnalysisSummaryShouldExposeHasBeats()
    {
        var summary = new TrackAnalysisSummary(TimeSpan.FromSeconds(60), 120, 8, 2);

        summary.HasBeats.Should().BeTrue();
    }

    [Fact]
    public void TrackBranchSummaryShouldExposeHasActiveBranches()
    {
        var summary = new TrackBranchSummary(ActiveBranchCount: 3, CandidateBranchCount: 20);

        summary.HasActiveBranches.Should().BeTrue();
    }

    [Fact]
    public void TrackRuntimePreparationSummaryShouldExposeHasRuntimeBranches()
    {
        var summary = new TrackRuntimePreparationSummary(
            RuntimeBeatCount: 120,
            RuntimeBranchCount: 3,
            IsPlayable: true);

        summary.HasRuntimeBranches.Should().BeTrue();
    }

    [Fact]
    public void TrackWorkflowErrorShouldRejectEmptyCode()
    {
        var act = () => new TrackWorkflowError("", "Message");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TrackWorkflowErrorShouldRejectEmptyMessage()
    {
        var act = () => new TrackWorkflowError("code", "");

        act.Should().Throw<ArgumentException>();
    }
}
