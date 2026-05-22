using EternalLoop.Contracts.Options;
using EternalLoop.Core.AI;
using FluentAssertions;

namespace EternalLoop.Core.Tests.AI;

public sealed class AiPatchBatcherTests
{
    private const int SingleBatchRealPatchCount = 3;
    private const int MultipleBatchPatchCount = AiPreprocessingDefaultValues.BatchSize + 2;

    [Fact]
    public void CreateBatches_pads_last_batch_to_64()
    {
        var batcher = new AiPatchBatcher();
        var patches = CreatePatches(SingleBatchRealPatchCount);

        var batches = batcher.CreateBatches(patches, AiPreprocessingDefaultValues.BatchSize);

        batches.Should().HaveCount(1);
        batches[0].Patches.Should().HaveCount(AiPreprocessingDefaultValues.BatchSize);
    }

    [Fact]
    public void CreateBatches_preserves_real_patch_count()
    {
        var batcher = new AiPatchBatcher();
        var patches = CreatePatches(SingleBatchRealPatchCount);

        var batches = batcher.CreateBatches(patches, AiPreprocessingDefaultValues.BatchSize);

        batches[0].RealPatchCount.Should().Be(SingleBatchRealPatchCount);
    }

    [Fact]
    public void CreateBatches_creates_multiple_batches_for_more_than_64_patches()
    {
        var batcher = new AiPatchBatcher();
        var patches = CreatePatches(MultipleBatchPatchCount);

        var batches = batcher.CreateBatches(patches, AiPreprocessingDefaultValues.BatchSize);

        batches.Should().HaveCount(2);
        batches[0].RealPatchCount.Should().Be(AiPreprocessingDefaultValues.BatchSize);
        batches[1].RealPatchCount.Should().Be(MultipleBatchPatchCount - AiPreprocessingDefaultValues.BatchSize);
    }

    [Fact]
    public void CreateBatches_returns_empty_for_empty_patch_list()
    {
        var batcher = new AiPatchBatcher();

        var batches = batcher.CreateBatches([], AiPreprocessingDefaultValues.BatchSize);

        batches.Should().BeEmpty();
    }

    [Fact]
    public void CreateBatches_rejects_invalid_batch_size()
    {
        var batcher = new AiPatchBatcher();

        var act = () => batcher.CreateBatches(CreatePatches(SingleBatchRealPatchCount), 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static float[][][] CreatePatches(int count)
    {
        return Enumerable.Range(0, count)
            .Select(index => new[]
            {
                new[] { (float)index }
            })
            .ToArray();
    }
}
