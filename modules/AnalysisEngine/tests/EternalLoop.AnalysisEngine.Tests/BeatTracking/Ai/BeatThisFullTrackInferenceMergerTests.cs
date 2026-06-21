using EternalLoop.AnalysisEngine.Core.BeatTracking.Ai;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.Ai;

public sealed class BeatThisFullTrackInferenceMergerTests
{
    [Fact]
    public void Merge_stitches_chunks_by_start_frame_index()
    {
        var chunks = new[]
        {
            new BeatThisInferenceResult
            {
                BeatActivations = [0.1f, 0.2f, 0.3f],
                DownbeatActivations = [0.9f, 0.8f, 0.7f],
                FrameRate = 100.0,
                ValidFrameCount = 3,
                StartFrameIndex = 0,
                StartTimeSeconds = 0.0,
                AudioDurationSeconds = 0.05,
                OutputMode = "chunk"
            },
            new BeatThisInferenceResult
            {
                BeatActivations = [0.4f, 0.5f],
                DownbeatActivations = [0.6f, 0.5f],
                FrameRate = 100.0,
                ValidFrameCount = 2,
                StartFrameIndex = 3,
                StartTimeSeconds = 0.03,
                AudioDurationSeconds = 0.05,
                OutputMode = "chunk"
            }
        };

        var merged = BeatThisFullTrackInferenceMerger.Merge(chunks);

        merged.BeatActivations.Should().Equal(0.1f, 0.2f, 0.3f, 0.4f, 0.5f);
        merged.DownbeatActivations.Should().Equal(0.9f, 0.8f, 0.7f, 0.6f, 0.5f);
        merged.ValidFrameCount.Should().Be(5);
        merged.ChunkCount.Should().Be(2);
        merged.OutputMode.Should().StartWith("full-track-chunked:");
    }

    [Fact]
    public void Merge_rejects_empty_chunk_list()
    {
        var act = () => BeatThisFullTrackInferenceMerger.Merge([]);

        act.Should().Throw<InvalidDataException>()
            .WithMessage("Cannot merge empty Beat This inference chunk list.");
    }

    [Fact]
    public void Merge_rejects_mismatched_frame_rates()
    {
        var chunks = new[]
        {
            CreateChunk(frameRate: 100.0),
            CreateChunk(frameRate: 50.0)
        };

        var act = () => BeatThisFullTrackInferenceMerger.Merge(chunks);

        act.Should().Throw<InvalidDataException>()
            .WithMessage("Cannot merge Beat This chunks with different frame rates.");
    }

    private static BeatThisInferenceResult CreateChunk(double frameRate)
    {
        return new BeatThisInferenceResult
        {
            BeatActivations = [0.1f],
            DownbeatActivations = [0.2f],
            FrameRate = frameRate,
            ValidFrameCount = 1,
            AudioDurationSeconds = 1.0
        };
    }
}
