using EternalLoop.Core.Workflow;
using FluentAssertions;

namespace EternalLoop.Tests.Core.Workflow;

public sealed class TrackWorkflowRequestTests
{
    [Fact]
    public void ConstructorShouldGenerateCorrelationIdWhenMissing()
    {
        var input = TrackInput.FromFilePath("track.mp3");

        var request = new TrackWorkflowRequest(input);

        request.Input.Should().Be(input);
        request.ForceReanalysis.Should().BeFalse();
        request.CorrelationId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ConstructorShouldPreserveExplicitCorrelationId()
    {
        var input = TrackInput.FromFilePath("track.mp3");

        var request = new TrackWorkflowRequest(input, forceReanalysis: true, correlationId: "abc");

        request.ForceReanalysis.Should().BeTrue();
        request.CorrelationId.Should().Be("abc");
    }

    [Fact]
    public void ConstructorShouldRejectNullInput()
    {
        var act = () => new TrackWorkflowRequest(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
