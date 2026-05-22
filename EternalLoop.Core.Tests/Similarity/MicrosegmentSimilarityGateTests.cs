using EternalLoop.Contracts.Models;
using EternalLoop.Core.Similarity;
using FluentAssertions;

namespace EternalLoop.Core.Tests.Similarity;

public sealed class MicrosegmentSimilarityGateTests
{
    [Fact]
    public void TryApply_Should_KeepScore_WhenMicrostructureMatches()
    {
        _ = MicrosegmentSimilarityGate.TryApply(
            0.90,
            CreateFingerprint([1f, 1f, 1f, 1f]),
            CreateFingerprint([1f, 1f, 1f, 1f]),
            4,
            0.82,
            0.70,
            0.25,
            out var adjustedScore);

        adjustedScore.Should().Be(0.90);
    }

    [Fact]
    public void TryApply_Should_Penalize_WhenMicrostructurePartiallyMismatches()
    {
        _ = MicrosegmentSimilarityGate.TryApply(
            0.90,
            CreateFingerprint([1f, 1f, 1f, 1f]),
            CreateFingerprint([1f, 1f, -1f, 1f]),
            4,
            0.82,
            0.40,
            0.25,
            out var adjustedScore);

        adjustedScore.Should().BeLessThan(0.90);
        adjustedScore.Should().BeGreaterThan(0.0);
    }

    [Fact]
    public void TryApply_Should_Reject_WhenMicrostructureStronglyMismatches()
    {
        var accepted = MicrosegmentSimilarityGate.TryApply(
            0.90,
            CreateFingerprint([1f, 1f, 1f, 1f]),
            CreateFingerprint([-1f, -1f, -1f, -1f]),
            4,
            0.82,
            0.70,
            0.25,
            out var adjustedScore);

        accepted.Should().BeFalse();
        adjustedScore.Should().Be(0.0);
    }

    [Fact]
    public void TryApply_Should_NotBoost_WhenMicrostructureMatches()
    {
        _ = MicrosegmentSimilarityGate.TryApply(
            0.40,
            CreateFingerprint([1f, 1f, 1f, 1f]),
            CreateFingerprint([1f, 1f, 1f, 1f]),
            4,
            0.82,
            0.70,
            0.25,
            out var adjustedScore);

        adjustedScore.Should().BeLessThanOrEqualTo(0.40);
    }

    [Fact]
    public void TryApply_Should_KeepScore_WhenFingerprintsAreMissing()
    {
        _ = MicrosegmentSimilarityGate.TryApply(0.90, null, null, 4, 0.82, 0.70, 0.25, out var adjustedScore);

        adjustedScore.Should().Be(0.90);
    }

    [Fact]
    public void TryApply_Should_KeepScore_WhenMicrosegmentsAreEmpty()
    {
        var fingerprint = new BeatMicroFingerprint
        {
            BeatIndex = 0,
            Microsegments = []
        };

        _ = MicrosegmentSimilarityGate.TryApply(0.90, fingerprint, fingerprint, 4, 0.82, 0.70, 0.25, out var adjustedScore);

        adjustedScore.Should().Be(0.90);
    }

    [Fact]
    public void TryApply_Should_HandleDifferentMicrosegmentCounts()
    {
        _ = MicrosegmentSimilarityGate.TryApply(
            0.90,
            CreateFingerprint([1f, 1f, 1f, 1f]),
            CreateFingerprint([1f, 1f]),
            4,
            0.82,
            0.70,
            0.25,
            out var adjustedScore);

        adjustedScore.Should().BeInRange(0.0, 0.90);
    }

    [Fact]
    public void TryApply_Should_NotReturnNaN()
    {
        _ = MicrosegmentSimilarityGate.TryApply(
            double.NaN,
            CreateFingerprint([float.NaN]),
            CreateFingerprint([float.PositiveInfinity]),
            1,
            0.82,
            0.70,
            0.25,
            out var adjustedScore);

        adjustedScore.Should().NotBe(double.NaN);
        adjustedScore.Should().BeInRange(0.0, 1.0);
    }

    private static BeatMicroFingerprint CreateFingerprint(float[] values)
    {
        return new BeatMicroFingerprint
        {
            BeatIndex = 0,
            Microsegments = values
                .Select((value, index) => new BeatMicrosegment
                {
                    BeatIndex = 0,
                    SegmentIndex = index,
                    Start = index * 0.125,
                    Duration = 0.125,
                    RelativePosition = values.Length <= 1 ? 0f : index / (float)(values.Length - 1),
                    Timbre = [value, 1f],
                    Pitches = [value, 1f],
                    Loudness = [value, 1f, 1f],
                    Flux = value
                })
                .ToArray()
        };
    }
}
