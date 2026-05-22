namespace EternalLoop.Contracts.Models;

public sealed class AiEmbeddingFrame
{
    public required int Index { get; init; }

    public required double Start { get; init; }

    public required double Duration { get; init; }

    public required float[] Vector { get; init; }
}
