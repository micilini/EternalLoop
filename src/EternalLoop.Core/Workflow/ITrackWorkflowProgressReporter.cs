namespace EternalLoop.Core.Workflow;

public interface ITrackWorkflowProgressReporter
{
    ValueTask ReportAsync(
        TrackWorkflowProgress progress,
        CancellationToken cancellationToken = default);
}
