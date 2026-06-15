using EternalLoop.Core.Workflow;
using FluentAssertions;

namespace EternalLoop.Tests.Core.Workflow;

public sealed class TrackWorkflowResultTests
{
    [Fact]
    public void CompletedShouldCreateSuccessfulResult()
    {
        var input = TrackInput.FromFilePath("track.mp3");
        var analysis = new TrackAnalysisSummary(TimeSpan.FromSeconds(120), 240, 16, 4);
        var branches = new TrackBranchSummary(10, 30);
        var runtime = new TrackRuntimePreparationSummary(240, 10, IsPlayable: true);

        var result = TrackWorkflowResult.Completed(input, analysis, branches, runtime);

        result.Status.Should().Be(TrackWorkflowStatus.Completed);
        result.IsSuccess.Should().BeTrue();
        result.Input.Should().Be(input);
        result.AnalysisSummary.Should().Be(analysis);
        result.BranchSummary.Should().Be(branches);
        result.RuntimeSummary.Should().Be(runtime);
        result.Error.Should().BeNull();
    }

    [Fact]
    public void FailedShouldCreateFailedResult()
    {
        var input = TrackInput.FromFilePath("track.mp3");
        var error = new TrackWorkflowError("invalid_audio", "Audio could not be loaded.");

        var result = TrackWorkflowResult.Failed(input, error);

        result.Status.Should().Be(TrackWorkflowStatus.Failed);
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void CanceledShouldCreateCanceledResultWithoutError()
    {
        var input = TrackInput.FromFilePath("track.mp3");

        var result = TrackWorkflowResult.Canceled(input);

        result.Status.Should().Be(TrackWorkflowStatus.Canceled);
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().BeNull();
    }
}
