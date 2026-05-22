namespace EternalLoop.Contracts.Models;

public sealed class Section
{
    public required int Index { get; init; }

    public required double Start { get; init; }

    public required double Duration { get; init; }

    public required double Confidence { get; init; }

    public required double Loudness { get; init; }

    public required double Tempo { get; init; }

    public string? Label { get; init; }
}
