namespace EternalLoop.Core.AI;

public sealed class AiPatchBatch
{
    public required IReadOnlyList<float[][]> Patches { get; init; }

    public required int RealPatchCount { get; init; }
}
