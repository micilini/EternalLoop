namespace EternalLoop.Core.AI;

public sealed class AiPatchBatcher
{
    public IReadOnlyList<AiPatchBatch> CreateBatches(
        IReadOnlyList<float[][]> patches,
        int batchSize)
    {
        ArgumentNullException.ThrowIfNull(patches);

        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize));
        }

        if (patches.Count == 0)
        {
            return [];
        }

        var batches = new List<AiPatchBatch>();

        for (var batchStart = 0; batchStart < patches.Count; batchStart += batchSize)
        {
            var realPatchCount = Math.Min(batchSize, patches.Count - batchStart);
            var batchPatches = new float[batchSize][][];

            for (var batchIndex = 0; batchIndex < batchSize; batchIndex++)
            {
                var sourcePatchIndex = batchStart + Math.Min(batchIndex, realPatchCount - 1);
                batchPatches[batchIndex] = patches[sourcePatchIndex];
            }

            batches.Add(new AiPatchBatch
            {
                Patches = batchPatches,
                RealPatchCount = realPatchCount
            });
        }

        return batches;
    }
}
