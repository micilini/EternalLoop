using EternalLoop.AnalysisEngine.Core.BeatTracking;
using EternalLoop.AnalysisEngine.Core.BeatTracking.BeatThis;
using EternalLoop.AnalysisEngine.Core.Models;
using FluentAssertions;
using Xunit;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.BeatThis;

public sealed class BeatThisBeatFirstGuardrailTests
{
    [Fact]
    public void Guardrail_accepts_plausible_beats_after_downbeats_were_sanitized()
    {
        var sanitizer = new BeatThisDownbeatSanitizer();
        var sanitization = sanitizer.Sanitize(
            beats: [0.0, 0.5, 1.0, 1.5],
            downbeats: [0.0, 0.9],
            maxDistanceToNearestBeatSeconds: 0.03);

        var guardrails = new BeatGridGuardrails();
        var result = new BeatTrackingResult
        {
            EstimatedBpm = 120.0,
            BeatTimes = [0.0, 0.5, 1.0, 1.5],
            Confidences = [0.9, 0.9, 0.9, 0.9],
            DownbeatTimes = sanitization.Downbeats.ToArray(),
            UsedAiProvider = true,
            UsedBuiltInProvider = false
        };
        var audio = new LoadedAudio([], 22050, 4.0, "hash", "song.wav", "song.wav");

        var validation = guardrails.Validate(result, audio);

        validation.IsValid.Should().BeTrue();
        sanitization.Sanitized.Should().BeTrue();
        sanitization.Warnings.Should().ContainSingle()
            .Which.Should().StartWith("beat-this-warning:downbeats-discarded:not-aligned-to-beat");
    }

    [Fact]
    public void Generic_guardrail_still_rejects_unsanitized_bad_downbeats()
    {
        var guardrails = new BeatGridGuardrails();
        var result = new BeatTrackingResult
        {
            EstimatedBpm = 120.0,
            BeatTimes = [0.0, 0.5, 1.0, 1.5],
            Confidences = [0.9, 0.9, 0.9, 0.9],
            DownbeatTimes = [0.0, 0.9],
            UsedAiProvider = true,
            UsedBuiltInProvider = false
        };
        var audio = new LoadedAudio([], 22050, 4.0, "hash", "song.wav", "song.wav");

        var validation = guardrails.Validate(result, audio);

        validation.IsValid.Should().BeFalse();
        validation.Reason.Should().StartWith("downbeat-not-aligned-to-beat");
    }

    [Fact]
    public void Guardrail_rejects_empty_beats_even_when_downbeats_exist()
    {
        var guardrails = new BeatGridGuardrails();
        var result = new BeatTrackingResult
        {
            EstimatedBpm = 120.0,
            BeatTimes = [],
            Confidences = [],
            DownbeatTimes = [0.0],
            UsedAiProvider = true,
            UsedBuiltInProvider = false
        };
        var audio = new LoadedAudio([], 22050, 4.0, "hash", "song.wav", "song.wav");

        var validation = guardrails.Validate(result, audio);

        validation.IsValid.Should().BeFalse();
        validation.Reason.Should().StartWith("beat-count-too-low");
    }

    [Fact]
    public void Guardrail_rejects_dense_beats()
    {
        var guardrails = new BeatGridGuardrails(new BeatGridGuardrailOptions
        {
            MaxBeatsPerSecond = 2.0
        });
        var result = new BeatTrackingResult
        {
            EstimatedBpm = 120.0,
            BeatTimes = [0.0, 0.1, 0.2, 0.3],
            Confidences = [0.9, 0.9, 0.9, 0.9],
            UsedAiProvider = true,
            UsedBuiltInProvider = false
        };
        var audio = new LoadedAudio([], 22050, 1.0, "hash", "song.wav", "song.wav");

        var validation = guardrails.Validate(result, audio);

        validation.IsValid.Should().BeFalse();
        validation.Reason.Should().StartWith("beat-density-too-high");
    }

    [Fact]
    public void Guardrail_rejects_nan_or_infinite_beats()
    {
        var guardrails = new BeatGridGuardrails();
        var result = new BeatTrackingResult
        {
            EstimatedBpm = 120.0,
            BeatTimes = [0.0, double.NaN, 1.0],
            Confidences = [0.9, 0.9, 0.9],
            UsedAiProvider = true,
            UsedBuiltInProvider = false
        };
        var audio = new LoadedAudio([], 22050, 4.0, "hash", "song.wav", "song.wav");

        var validation = guardrails.Validate(result, audio);

        validation.IsValid.Should().BeFalse();
        validation.Reason.Should().Be("beat-times-not-strictly-increasing");
    }

    [Fact]
    public void Guardrail_rejects_absurd_bpm()
    {
        var guardrails = new BeatGridGuardrails(new BeatGridGuardrailOptions
        {
            MinBpm = 50.0,
            MaxBpm = 200.0
        });
        var result = new BeatTrackingResult
        {
            EstimatedBpm = 300.0,
            BeatTimes = [0.0, 0.5, 1.0, 1.5],
            Confidences = [0.9, 0.9, 0.9, 0.9],
            UsedAiProvider = true,
            UsedBuiltInProvider = false
        };
        var audio = new LoadedAudio([], 22050, 4.0, "hash", "song.wav", "song.wav");

        var validation = guardrails.Validate(result, audio);

        validation.IsValid.Should().BeFalse();
        validation.Reason.Should().StartWith("bpm-out-of-range");
    }
}
