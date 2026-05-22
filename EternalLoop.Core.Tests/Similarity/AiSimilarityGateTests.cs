using EternalLoop.Core.Similarity;
using FluentAssertions;

namespace EternalLoop.Core.Tests.Similarity;

public sealed class AiSimilarityGateTests
{
    private const double BaseScore = 0.9;
    private const double RejectionThreshold = 0.58;
    private const double PenaltyStartThreshold = 0.72;
    private const double PenaltyStrength = 0.22;

    [Fact]
    public void TryApply_Should_Reject_WhenSimilarityBelowRejectionThreshold()
    {
        var accepted = AiSimilarityGate.TryApply(
            BaseScore,
            [1.0f, 0.0f],
            [0.0f, 1.0f],
            RejectionThreshold,
            PenaltyStartThreshold,
            PenaltyStrength,
            out var adjustedScore);

        accepted.Should().BeFalse();
        adjustedScore.Should().Be(0.0);
    }

    [Fact]
    public void TryApply_Should_Penalize_WhenSimilarityBetweenRejectionAndPenaltyStart()
    {
        var accepted = AiSimilarityGate.TryApply(
            BaseScore,
            [1.0f, 0.0f],
            [0.65f, 0.760f],
            RejectionThreshold,
            PenaltyStartThreshold,
            PenaltyStrength,
            out var adjustedScore);

        accepted.Should().BeTrue();
        adjustedScore.Should().BeLessThan(BaseScore);
        adjustedScore.Should().BeGreaterThan(0.0);
    }

    [Fact]
    public void TryApply_Should_NotBoost_WhenSimilarityIsHigh()
    {
        var accepted = AiSimilarityGate.TryApply(
            BaseScore,
            [1.0f, 0.0f],
            [1.0f, 0.0f],
            RejectionThreshold,
            PenaltyStartThreshold,
            PenaltyStrength,
            out var adjustedScore);

        accepted.Should().BeTrue();
        adjustedScore.Should().Be(BaseScore);
    }

    [Fact]
    public void TryApply_Should_KeepBaseScore_WhenEmbeddingsAreMissing()
    {
        var accepted = AiSimilarityGate.TryApply(
            BaseScore,
            null,
            [1.0f, 0.0f],
            RejectionThreshold,
            PenaltyStartThreshold,
            PenaltyStrength,
            out var adjustedScore);

        accepted.Should().BeTrue();
        adjustedScore.Should().Be(BaseScore);
    }

    [Fact]
    public void TryApply_Should_KeepBaseScore_WhenVectorsAreZero()
    {
        var accepted = AiSimilarityGate.TryApply(
            BaseScore,
            [0.0f, 0.0f],
            [1.0f, 0.0f],
            RejectionThreshold,
            PenaltyStartThreshold,
            PenaltyStrength,
            out var adjustedScore);

        accepted.Should().BeTrue();
        adjustedScore.Should().Be(BaseScore);
    }

    [Fact]
    public void TryApply_Should_KeepBaseScore_WhenDimensionsAreInvalid()
    {
        var accepted = AiSimilarityGate.TryApply(
            BaseScore,
            [1.0f],
            [1.0f, 0.0f],
            RejectionThreshold,
            PenaltyStartThreshold,
            PenaltyStrength,
            out var adjustedScore);

        accepted.Should().BeTrue();
        adjustedScore.Should().Be(BaseScore);
    }
}
