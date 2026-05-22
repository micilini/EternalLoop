using EternalLoop.Contracts.Enums;

namespace EternalLoop.Contracts.Models;

public sealed class AiAnalysisRunInfo
{
    public static AiAnalysisRunInfo Disabled { get; } = new()
    {
        Status = AiAnalysisRunStatus.Disabled,
        Message = "AI similarity is off. Using classic analysis."
    };

    public static AiAnalysisRunInfo Completed { get; } = new()
    {
        Status = AiAnalysisRunStatus.Completed,
        Message = "AI similarity was used for this analysis."
    };

    public static AiAnalysisRunInfo LoadedFromCache { get; } = new()
    {
        Status = AiAnalysisRunStatus.LoadedFromCache,
        Message = "AI similarity was loaded from cache."
    };

    public required AiAnalysisRunStatus Status { get; init; }

    public required string Message { get; init; }

    public string? FailureReason { get; init; }

    public string? DiagnosticFilePath { get; init; }

    public bool UsedAi => Status is AiAnalysisRunStatus.Completed or AiAnalysisRunStatus.LoadedFromCache;

    public bool FellBackToClassic => Status == AiAnalysisRunStatus.FailedFallback;

    public static AiAnalysisRunInfo FailedFallback(string failureReason, string? diagnosticFilePath = null)
    {
        if (string.IsNullOrWhiteSpace(failureReason))
        {
            throw new ArgumentException("AI failure reason is required.", nameof(failureReason));
        }

        return new AiAnalysisRunInfo
        {
            Status = AiAnalysisRunStatus.FailedFallback,
            Message = "AI similarity failed. Using classic analysis.",
            FailureReason = failureReason,
            DiagnosticFilePath = diagnosticFilePath
        };
    }
}
