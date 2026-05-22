namespace EternalLoop.Contracts.Models;

public sealed class AiBeatEmbedding
{
    public required int BeatIndex { get; init; }

    public required float[] Vector { get; init; }
}
