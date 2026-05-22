using System.Collections.Generic;

namespace EternalLoop.Contracts.Models;

public sealed class AiEmbeddingExtractionResult
{
    public required string ModelId { get; init; }

    public required string ModelVersion { get; init; }

    public required int SampleRate { get; init; }

    public required int EmbeddingDimensions { get; init; }

    public required IReadOnlyList<AiEmbeddingFrame> Frames { get; init; }
}
