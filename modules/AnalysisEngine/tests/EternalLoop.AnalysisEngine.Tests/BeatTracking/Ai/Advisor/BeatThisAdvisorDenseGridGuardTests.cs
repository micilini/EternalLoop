using EternalLoop.AnalysisEngine.Core.BeatTracking.Ai.Advisor;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.Ai.Advisor;

public sealed class BeatThisAdvisorDenseGridGuardTests
{
    [Fact]
    public void Postprocessor_rejects_every_frame_as_event()
    {
        var output = CreateOutput(Enumerable.Range(0, 200).Select(_ => 1.0f).ToArray(), frameRate: 50.0);
        var postprocessor = new BeatThisAdvisorPostprocessor(new BeatThisAdvisorPostprocessOptions
        {
            BeatThresholdPercentile = 0.0,
            MinBeatSpacingSeconds = 0.02,
            MaxBpm = 300.0
        });

        var result = postprocessor.Postprocess(output);

        result.IsDenseGrid.Should().BeTrue();
        result.RejectionReason.Should().NotBeNull();
    }

    [Fact]
    public void Postprocessor_rejects_bpm_above_200()
    {
        var logits = SparsePeaks(frameCount: 400, everyFrames: 10);
        var output = CreateOutput(logits, frameRate: 50.0);
        var postprocessor = new BeatThisAdvisorPostprocessor(new BeatThisAdvisorPostprocessOptions
        {
            BeatThresholdPercentile = 0.99,
            MinBeatSpacingSeconds = 0.02
        });

        var result = postprocessor.Postprocess(output);

        result.IsDenseGrid.Should().BeTrue();
        result.RejectionReason.Should().StartWith("bpm-out-of-range");
    }

    [Fact]
    public void Postprocessor_rejects_density_above_limit()
    {
        var logits = SparsePeaks(frameCount: 400, everyFrames: 13);
        var output = CreateOutput(logits, frameRate: 50.0);
        var postprocessor = new BeatThisAdvisorPostprocessor(new BeatThisAdvisorPostprocessOptions
        {
            BeatThresholdPercentile = 0.99,
            MinBeatSpacingSeconds = 0.02,
            MaxBpm = 300.0,
            MaxBeatDensityPerSecond = 2.0
        });

        var result = postprocessor.Postprocess(output);

        result.IsDenseGrid.Should().BeTrue();
        result.RejectionReason.Should().StartWith("beat-density-too-high");
    }

    [Fact]
    public void Postprocessor_rejects_count_ratio_above_limit_when_reference_count_provided()
    {
        var logits = SparsePeaks(frameCount: 400, everyFrames: 25);
        var output = CreateOutput(logits, frameRate: 50.0);
        var postprocessor = new BeatThisAdvisorPostprocessor(new BeatThisAdvisorPostprocessOptions
        {
            BeatThresholdPercentile = 0.99,
            MinBeatSpacingSeconds = 0.02,
            ReferenceBeatCount = 4,
            MaxCountRatio = 1.5,
            MaxBpm = 300.0
        });

        var result = postprocessor.Postprocess(output);

        result.IsDenseGrid.Should().BeTrue();
        result.RejectionReason.Should().StartWith("count-ratio-too-high");
    }

    private static BeatThisAdvisorOutput CreateOutput(float[] beatLogits, double frameRate)
    {
        return new BeatThisAdvisorOutput
        {
            BeatLogits = beatLogits,
            DownbeatLogits = beatLogits.ToArray(),
            FrameCount = beatLogits.Length,
            FrameRate = frameRate,
            DurationSeconds = beatLogits.Length / frameRate,
            ChunkCount = 1,
            OutputMode = "test",
            AggregatePolicy = "keep_first"
        };
    }

    private static float[] SparsePeaks(int frameCount, int everyFrames)
    {
        var logits = Enumerable.Repeat(-10.0f, frameCount).ToArray();

        for (var frame = 0; frame < frameCount; frame += everyFrames)
        {
            logits[frame] = 10.0f;
        }

        return logits;
    }
}
