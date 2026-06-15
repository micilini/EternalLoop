namespace EternalLoop.Core.Workflow;

public sealed record TrackWorkflowProgress
{
    public TrackWorkflowProgress(
        TrackWorkflowStatus status,
        string message,
        double? percent = null)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Progress message cannot be empty.", nameof(message));
        }

        if (percent is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percent), "Progress percent must be between 0 and 100.");
        }

        Status = status;
        Message = message;
        Percent = percent;
    }

    public TrackWorkflowStatus Status { get; }

    public string Message { get; }

    public double? Percent { get; }
}
