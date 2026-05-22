namespace EternalLoop.Contracts.Models;

public sealed class AiFailureDiagnosticReport
{
    public required string Id { get; init; }

    public required DateTime CreatedAtUtc { get; init; }

    public required string FilePath { get; init; }

    public required string FileHash { get; init; }

    public required double DurationSeconds { get; init; }

    public required int SampleRate { get; init; }

    public required int SampleCount { get; init; }

    public required int BeatCount { get; init; }

    public required string ExceptionType { get; init; }

    public required string ExceptionMessage { get; init; }

    public required string ExceptionText { get; init; }
}
