namespace EternalLoop.Core.Workflow;

public sealed record TrackWorkflowRequest
{
    public TrackWorkflowRequest(
        TrackInput input,
        bool forceReanalysis = false,
        string? correlationId = null)
    {
        Input = input ?? throw new ArgumentNullException(nameof(input));
        ForceReanalysis = forceReanalysis;
        CorrelationId = string.IsNullOrWhiteSpace(correlationId)
            ? Guid.NewGuid().ToString("N")
            : correlationId;
    }

    public TrackInput Input { get; }

    public bool ForceReanalysis { get; }

    public string CorrelationId { get; }
}
