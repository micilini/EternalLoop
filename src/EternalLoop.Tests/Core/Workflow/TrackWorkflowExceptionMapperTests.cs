using System.Text.Json;
using EternalLoop.Core.Runtime;
using EternalLoop.Core.Workflow;
using FluentAssertions;

namespace EternalLoop.Tests.Core.Workflow;

public sealed class TrackWorkflowExceptionMapperTests
{
    [Theory]
    [InlineData(typeof(IOException), "file_or_cache_io")]
    [InlineData(typeof(UnauthorizedAccessException), "access_denied")]
    [InlineData(typeof(RuntimePackageBuildException), "runtime_package_failed")]
    [InlineData(typeof(InvalidOperationException), "workflow_validation_failed")]
    public void MapShouldReturnFriendlyErrorCode(Type exceptionType, string expectedCode)
    {
        Exception exception = exceptionType == typeof(RuntimePackageBuildException)
            ? new RuntimePackageBuildException("runtime failed")
            : (Exception)Activator.CreateInstance(exceptionType, "failure")!;

        TrackWorkflowError error = TrackWorkflowExceptionMapper.Map(exception);

        error.Code.Should().Be(expectedCode);
        error.Message.Should().NotContain(" at ");
    }

    [Fact]
    public void MapShouldReturnCorruptedJsonForJsonException()
    {
        TrackWorkflowError error = TrackWorkflowExceptionMapper.Map(new JsonException("bad json"));

        error.Code.Should().Be("corrupted_json");
    }
}
