using System;

namespace EternalLoop.Contracts.Models;

public sealed class TrackMetadata
{
    public required string FileHash { get; init; }

    public required string FilePath { get; init; }

    public required double DurationSeconds { get; init; }

    public required int SampleRate { get; init; }

    public required double Tempo { get; init; }

    public required int TimeSignature { get; init; }

    public required string SchemaVersion { get; init; }

    public DateTime AnalyzedAt { get; init; } = DateTime.UtcNow;
}
