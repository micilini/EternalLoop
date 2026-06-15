namespace EternalLoop.Core.Workflow;

public sealed record TrackWorkflowError
{
    public TrackWorkflowError(string code, string message, string? details = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Error code cannot be empty.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Error message cannot be empty.", nameof(message));
        }

        Code = code;
        Message = message;
        Details = details;
    }

    public string Code { get; }

    public string Message { get; }

    public string? Details { get; }
}
