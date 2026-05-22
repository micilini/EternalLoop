using EternalLoop.Contracts.Enums;
using EternalLoop.Core.Similarity;
using FluentAssertions;

namespace EternalLoop.Core.Tests.Similarity;

public sealed class MetricPositionGateTests
{
    [Fact]
    public void TryApply_keeps_score_when_disabled()
    {
        var accepted = MetricPositionGate.TryApply(
            0.90,
            0.00,
            MetricPositionMode.Disabled,
            1.00,
            0.95,
            out var adjustedScore);

        accepted.Should().BeTrue();
        adjustedScore.Should().Be(0.90);
    }

    [Fact]
    public void TryApply_rejects_mismatch_in_strict_mode()
    {
        var accepted = MetricPositionGate.TryApply(
            0.90,
            0.50,
            MetricPositionMode.StrictGate,
            0.45,
            0.95,
            out var adjustedScore);

        accepted.Should().BeFalse();
        adjustedScore.Should().Be(0.0);
    }

    [Fact]
    public void TryApply_keeps_exact_match_in_strict_mode()
    {
        var accepted = MetricPositionGate.TryApply(
            0.90,
            1.00,
            MetricPositionMode.StrictGate,
            0.45,
            0.95,
            out var adjustedScore);

        accepted.Should().BeTrue();
        adjustedScore.Should().Be(0.90);
    }

    [Fact]
    public void TryApply_penalizes_mismatch_in_strong_mode()
    {
        var accepted = MetricPositionGate.TryApply(
            0.90,
            0.50,
            MetricPositionMode.StrongPenalty,
            0.32,
            0.20,
            out var adjustedScore);

        accepted.Should().BeTrue();
        adjustedScore.Should().BeLessThan(0.90);
        adjustedScore.Should().BeGreaterThan(0.0);
    }

    [Fact]
    public void TryApply_penalizes_less_in_soft_mode_when_strength_is_lower()
    {
        _ = MetricPositionGate.TryApply(
            0.90,
            0.50,
            MetricPositionMode.StrongPenalty,
            0.32,
            0.20,
            out var strongScore);
        _ = MetricPositionGate.TryApply(
            0.90,
            0.50,
            MetricPositionMode.SoftPenalty,
            0.16,
            0.20,
            out var softScore);

        softScore.Should().BeLessThan(0.90);
        softScore.Should().BeGreaterThan(strongScore);
    }

    [Fact]
    public void TryApply_never_boosts_score()
    {
        _ = MetricPositionGate.TryApply(
            0.40,
            1.00,
            MetricPositionMode.StrongPenalty,
            0.32,
            0.20,
            out var adjustedScore);

        adjustedScore.Should().BeLessThanOrEqualTo(0.40);
    }

    [Fact]
    public void TryApply_clamps_invalid_inputs()
    {
        var accepted = MetricPositionGate.TryApply(
            2.00,
            -1.00,
            MetricPositionMode.StrongPenalty,
            2.00,
            -1.00,
            out var adjustedScore);

        accepted.Should().BeTrue();
        adjustedScore.Should().BeInRange(0.0, 1.0);
    }

    [Fact]
    public void TryApply_treats_nan_similarity_as_neutral()
    {
        var accepted = MetricPositionGate.TryApply(
            0.90,
            double.NaN,
            MetricPositionMode.StrictGate,
            0.45,
            0.95,
            out var adjustedScore);

        accepted.Should().BeTrue();
        adjustedScore.Should().Be(0.90);
    }
}
