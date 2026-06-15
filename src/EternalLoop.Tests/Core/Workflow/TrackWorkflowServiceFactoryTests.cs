using EternalLoop.Core.Workflow;
using FluentAssertions;

namespace EternalLoop.Tests.Core.Workflow;

public sealed class TrackWorkflowServiceFactoryTests
{
    [Fact]
    public void CreateDefaultShouldReturnWorkflowService()
    {
        var service = TrackWorkflowServiceFactory.CreateDefault();

        service.Should().NotBeNull();
        service.Should().BeAssignableTo<ITrackWorkflowService>();
    }
}
