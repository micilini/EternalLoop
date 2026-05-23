using EternalLoop.Core.Similarity;
using FluentAssertions;

namespace EternalLoop.Core.Tests.Similarity;

public sealed class BeatDurationSimilarityGateTests
{
    [Fact]
    public void TryApply_keeps_score_when_durations_are_close()
    {
        var accepted = BeatDurationSimilarityGate.TryApply(
            0.90,
            0.50,
            0.49,
            0.90,
            0.80,
            0.25,
            out var adjustedScore);

        accepted.Should().BeTrue();
        adjustedScore.Should().BeApproximately(0.90, 1e-6);
    }

    [Fact]
    public void TryApply_penalizes_when_duration_ratio_is_between_thresholds()
    {
        var accepted = BeatDurationSimilarityGate.TryApply(
            0.90,
            0.50,
            0.43,
            0.90,
            0.80,
            0.25,
            out var adjustedScore);

        accepted.Should().BeTrue();
        adjustedScore.Should().BeLessThan(0.90);
        adjustedScore.Should().BeGreaterThan(0.0);
    }

    [Fact]
    public void TryApply_rejects_when_duration_ratio_is_below_rejection()
    {
        var accepted = BeatDurationSimilarityGate.TryApply(
            0.90,
            0.50,
            0.35,
            0.90,
            0.80,
            0.25,
            out var adjustedScore);

        accepted.Should().BeFalse();
        adjustedScore.Should().Be(0.0);
    }

    [Fact]
    public void TryApply_never_boosts_score()
    {
        _ = BeatDurationSimilarityGate.TryApply(
            0.40,
            0.50,
            0.49,
            0.90,
            0.80,
            1.0,
            out var adjustedScore);

        adjustedScore.Should().BeLessThanOrEqualTo(0.40);
    }

    [Fact]
    public void TryApply_is_neutral_for_invalid_durations()
    {
        var accepted = BeatDurationSimilarityGate.TryApply(
            0.90,
            double.NaN,
            0.50,
            0.90,
            0.80,
            0.25,
            out var adjustedScore);

        accepted.Should().BeTrue();
        adjustedScore.Should().Be(0.90);
    }

    [Fact]
    public void TryApply_clamps_thresholds_and_strength()
    {
        var accepted = BeatDurationSimilarityGate.TryApply(
            2.0,
            0.50,
            0.25,
            2.0,
            -1.0,
            2.0,
            out var adjustedScore);

        accepted.Should().BeTrue();
        adjustedScore.Should().BeInRange(0.0, 1.0);
        adjustedScore.Should().BeLessThanOrEqualTo(1.0);
    }
}
