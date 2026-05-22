using System;
using System.Collections.Generic;

namespace EternalLoop.Contracts.Abstractions;

public interface ILocalMusicEmbeddingModel : IDisposable
{
    string ModelId { get; }

    int BatchSize { get; }

    int MelBands { get; }

    int PatchFrames { get; }

    int EmbeddingDimensions { get; }

    IReadOnlyList<float[]> Predict(IReadOnlyList<float[][]> patches);
}
