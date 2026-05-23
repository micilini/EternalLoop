using EternalLoop.Core.Similarity;
using FluentAssertions;

namespace EternalLoop.Core.Tests.Similarity;

public sealed class BeatConfidencePenaltyTests
{
    [Fact]
    public void Apply_keeps_score_when_pair_confidence_is_high()
    {
        var adjustedScore = BeatConfidencePenalty.Apply(
            0.90,
            0.90,
            0.80,
            0.50,
            0.25,
            0.20);

        adjustedScore.Should().Be(0.90);
    }

    [Fact]
    public void Apply_penalizes_when_pair_confidence_is_between_thresholds()
    {
        var adjustedScore = BeatConfidencePenalty.Apply(
            0.90,
            0.40,
            0.80,
            0.50,
            0.25,
            0.20);

        adjustedScore.Should().BeLessThan(0.90);
        adjustedScore.Should().BeGreaterThan(0.0);
    }

    [Fact]
    public void Apply_applies_max_penalty_when_pair_confidence_is_below_rejection_threshold()
    {
        var adjustedScore = BeatConfidencePenalty.Apply(
            0.90,
            0.10,
            0.80,
            0.50,
            0.25,
            0.20);

        adjustedScore.Should().BeApproximately(0.90 * (1.0 - 0.20), 1e-6);
    }

    [Fact]
    public void Apply_never_boosts_score()
    {
        var adjustedScore = BeatConfidencePenalty.Apply(
            0.40,
            1.0,
            1.0,
            0.50,
            0.25,
            0.20);

        adjustedScore.Should().BeLessThanOrEqualTo(0.40);
    }

    [Fact]
    public void Apply_treats_invalid_confidence_as_zero()
    {
        var adjustedScore = BeatConfidencePenalty.Apply(
            0.90,
            double.NaN,
            0.80,
            0.50,
            0.25,
            0.20);

        adjustedScore.Should().BeApproximately(0.90 * (1.0 - 0.20), 1e-6);
    }

    [Fact]
    public void Apply_clamps_confidence_thresholds_and_strength()
    {
        var adjustedScore = BeatConfidencePenalty.Apply(
            2.0,
            -1.0,
            3.0,
            2.0,
            -1.0,
            2.0);

        adjustedScore.Should().BeInRange(0.0, 1.0);
        adjustedScore.Should().BeLessThanOrEqualTo(1.0);
    }
}
