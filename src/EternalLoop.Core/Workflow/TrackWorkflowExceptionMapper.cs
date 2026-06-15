using System.Text.Json;
using EternalLoop.Core.Runtime;

namespace EternalLoop.Core.Workflow;

public static class TrackWorkflowExceptionMapper
{
    public static TrackWorkflowError Map(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            UnauthorizedAccessException => new TrackWorkflowError(
                "access_denied",
                "EternalLoop could not access a required file or folder.",
                exception.Message),
            IOException => new TrackWorkflowError(
                "file_or_cache_io",
                "EternalLoop could not read or write a required file. Check permissions and try again.",
                exception.Message),
            JsonException => new TrackWorkflowError(
                "corrupted_json",
                "EternalLoop found a corrupted analysis, branch or cache file and could not use it.",
                exception.Message),
            RuntimePackageBuildException => new TrackWorkflowError(
                "runtime_package_failed",
                "EternalLoop could not prepare playback for this track.",
                exception.Message),
            ArgumentException or InvalidOperationException => new TrackWorkflowError(
                "workflow_validation_failed",
                "EternalLoop could not prepare this track. Check the file and try again.",
                exception.Message),
            _ => new TrackWorkflowError(
                "workflow_failed",
                "EternalLoop could not prepare this track. Check the file and try again.",
                exception.Message)
        };
    }
}
