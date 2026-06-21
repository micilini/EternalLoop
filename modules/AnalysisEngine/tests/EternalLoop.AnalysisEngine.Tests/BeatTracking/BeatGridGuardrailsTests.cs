using EternalLoop.AnalysisEngine.Core.BeatTracking;
using EternalLoop.AnalysisEngine.Core.Models;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking;

public sealed class BeatGridGuardrailsTests
{
    [Fact]
    public void Validate_accepts_reasonable_ai_grid()
    {
        var guardrails = new BeatGridGuardrails();

        var result = guardrails.Validate(CreateResult(), CreateAudio());

        result.IsValid.Should().BeTrue();
        result.Reason.Should().Be("ok");
    }

    [Fact]
    public void Validate_rejects_too_few_beats()
    {
        var guardrails = new BeatGridGuardrails();

        var result = guardrails.Validate(
            CreateResult(beatTimes: [0.0], confidences: [0.9]),
            CreateAudio());

        result.IsValid.Should().BeFalse();
        result.Reason.Should().StartWith("beat-count-too-low");
    }

    [Fact]
    public void Validate_rejects_confidence_count_mismatch()
    {
        var guardrails = new BeatGridGuardrails();

        var result = guardrails.Validate(
            CreateResult(confidences: [0.9]),
            CreateAudio());

        result.IsValid.Should().BeFalse();
        result.Reason.Should().StartWith("confidence-count-mismatch");
    }

    [Fact]
    public void Validate_rejects_out_of_range_bpm()
    {
        var guardrails = new BeatGridGuardrails();

        var result = guardrails.Validate(
            CreateResult(estimatedBpm: 320.0),
            CreateAudio());

        result.IsValid.Should().BeFalse();
        result.Reason.Should().StartWith("bpm-out-of-range");
    }

    [Fact]
    public void Validate_rejects_non_increasing_beats()
    {
        var guardrails = new BeatGridGuardrails();

        var result = guardrails.Validate(
            CreateResult(beatTimes: [0.0, 0.5, 0.5], confidences: [0.9, 0.8, 0.7]),
            CreateAudio());

        result.IsValid.Should().BeFalse();
        result.Reason.Should().Be("beat-times-not-strictly-increasing");
    }

    [Fact]
    public void Validate_rejects_high_beat_density()
    {
        var guardrails = new BeatGridGuardrails(new BeatGridGuardrailOptions
        {
            MaxBeatsPerSecond = 2.0
        });

        var result = guardrails.Validate(
            CreateResult(beatTimes: [0.0, 0.1, 0.2, 0.3], confidences: [0.9, 0.8, 0.7, 0.6]),
            CreateAudio(durationSeconds: 1.0));

        result.IsValid.Should().BeFalse();
        result.Reason.Should().StartWith("beat-density-too-high");
    }

    [Fact]
    public void Validate_rejects_high_interval_stddev()
    {
        var guardrails = new BeatGridGuardrails(new BeatGridGuardrailOptions
        {
            MaxBeatIntervalStdDevRatio = 0.25
        });

        var result = guardrails.Validate(
            CreateResult(
                beatTimes: [0.0, 0.5, 1.0, 3.5],
                confidences: [0.9, 0.8, 0.7, 0.6]),
            CreateAudio(durationSeconds: 4.0));

        result.IsValid.Should().BeFalse();
        result.Reason.Should().StartWith("beat-interval-stddev-too-high");
    }

    [Fact]
    public void Validate_rejects_downbeat_not_aligned_to_beat()
    {
        var guardrails = new BeatGridGuardrails(new BeatGridGuardrailOptions
        {
            MaxDownbeatToBeatDistanceSeconds = 0.01
        });

        var result = guardrails.Validate(
            CreateResult(downbeatTimes: [0.07]),
            CreateAudio());

        result.IsValid.Should().BeFalse();
        result.Reason.Should().StartWith("downbeat-not-aligned-to-beat");
    }

    [Fact]
    public void Validate_rejects_low_mean_confidence()
    {
        var guardrails = new BeatGridGuardrails(new BeatGridGuardrailOptions
        {
            MinMeanConfidence = 0.5
        });

        var result = guardrails.Validate(
            CreateResult(confidences: [0.1, 0.2, 0.1, 0.2]),
            CreateAudio());

        result.IsValid.Should().BeFalse();
        result.Reason.Should().StartWith("confidence-too-low");
    }

    private static BeatTrackingResult CreateResult(
        double[]? beatTimes = null,
        double[]? confidences = null,
        double[]? downbeatTimes = null,
        double estimatedBpm = 120.0)
    {
        return new BeatTrackingResult
        {
            EstimatedBpm = estimatedBpm,
            BeatTimes = beatTimes ?? [0.0, 0.5, 1.0, 1.5],
            Confidences = confidences ?? [0.9, 0.8, 0.7, 0.6],
            DownbeatTimes = downbeatTimes ?? [0.0, 1.0],
            UsedAiProvider = true,
            UsedBuiltInProvider = false,
            ProviderName = "beat-this",
            BeatGridMode = "beat-this-onnx-musical-v1"
        };
    }

    [Fact]
    public void Validate_rejects_ai_result_with_low_coverage()
    {
        var audio = CreateAudio(180.0);
        var result = new BeatTrackingResult
        {
            EstimatedBpm = 120.0,
            BeatTimes = [0.0, 0.5, 1.0, 1.5],
            Confidences = [0.9, 0.9, 0.9, 0.9],
            UsedAiProvider = true,
            UsedBuiltInProvider = false,
            BeatProviderCoverageRatio = 0.10
        };
        var guardrails = new BeatGridGuardrails();

        var validation = guardrails.Validate(result, audio);

        validation.IsValid.Should().BeFalse();
        validation.Reason.Should().StartWith("ai-coverage-too-low:");
    }

    private static LoadedAudio CreateAudio(double durationSeconds = 4.0)
    {
        return new LoadedAudio([], 22050, durationSeconds, "hash", "song.wav", "song.wav");
    }
}