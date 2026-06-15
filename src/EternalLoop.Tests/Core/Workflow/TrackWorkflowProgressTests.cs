using EternalLoop.Core.Workflow;
using FluentAssertions;

namespace EternalLoop.Tests.Core.Workflow;

public sealed class TrackWorkflowProgressTests
{
    [Fact]
    public void ConstructorShouldAcceptProgressWithoutPercent()
    {
        var progress = new TrackWorkflowProgress(
            TrackWorkflowStatus.AnalyzingAudio,
            "Analyzing audio");

        progress.Status.Should().Be(TrackWorkflowStatus.AnalyzingAudio);
        progress.Message.Should().Be("Analyzing audio");
        progress.Percent.Should().BeNull();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void ConstructorShouldRejectInvalidPercent(double percent)
    {
        var act = () => new TrackWorkflowProgress(
            TrackWorkflowStatus.AnalyzingAudio,
            "Analyzing audio",
            percent);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ConstructorShouldRejectEmptyMessage()
    {
        var act = () => new TrackWorkflowProgress(
            TrackWorkflowStatus.AnalyzingAudio,
            "");

        act.Should().Throw<ArgumentException>();
    }
}
