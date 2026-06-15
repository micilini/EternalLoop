namespace EternalLoop.Core.Workflow;

public interface ITrackWorkflowService
{
    Task<TrackWorkflowResult> RunAsync(
        TrackWorkflowRequest request,
        ITrackWorkflowProgressReporter? progressReporter = null,
        CancellationToken cancellationToken = default);
}
