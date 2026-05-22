using EternalLoop.Contracts.Models;
using EternalLoop.Core.Similarity;
using FluentAssertions;

namespace EternalLoop.Core.Tests.Similarity;

public sealed class SelfSimilarityMatrixTests
{
    [Fact]
    public void Compute_Should_ReturnEmptyMatrix_WhenNoBeats()
    {
        var matrix = SelfSimilarityMatrix.Compute([], 0.5, 0.5, 0.0, 0.0);

        matrix.GetLength(0).Should().Be(0);
        matrix.GetLength(1).Should().Be(0);
    }

    [Fact]
    public void Compute_Should_SetDiagonalToOne()
    {
        var matrix = SelfSimilarityMatrix.Compute(CreateBeats(), 0.5, 0.5, 0.0, 0.0);

        matrix[0, 0].Should().Be(1.0);
        matrix[1, 1].Should().Be(1.0);
    }

    [Fact]
    public void Compute_Should_BeSymmetric()
    {
        var matrix = SelfSimilarityMatrix.Compute(CreateBeats(), 0.5, 0.5, 0.0, 0.0);

        matrix[0, 1].Should().Be(matrix[1, 0]);
    }

    [Fact]
    public void Compute_Should_ReturnHighSimilarity_ForIdenticalFeatures()
    {
        var beats = new[]
        {
            CreateBeat(0, [1f, 2f], [0f, 1f]),
            CreateBeat(1, [1f, 2f], [0f, 1f])
        };

        var matrix = SelfSimilarityMatrix.Compute(beats, 0.5, 0.5, 0.0, 0.0);

        matrix[0, 1].Should().BeApproximately(1.0, 0.0001);
    }

    [Fact]
    public void Compute_Should_ReturnLowSimilarity_ForOrthogonalFeatures()
    {
        var beats = new[]
        {
            CreateBeat(0, [1f, 0f], [1f, 0f]),
            CreateBeat(1, [0f, 1f], [0f, 1f])
        };

        var matrix = SelfSimilarityMatrix.Compute(beats, 0.5, 0.5, 0.0, 0.0);

        matrix[0, 1].Should().Be(0.0);
    }

    [Fact]
    public void Compute_Should_NormalizeWeights()
    {
        var beats = new[]
        {
            CreateBeat(0, [1f, 0f], [1f, 0f]),
            CreateBeat(1, [1f, 0f], [0f, 1f])
        };

        var matrix = SelfSimilarityMatrix.Compute(beats, 2.0, 0.0, 0.0, 0.0);

        matrix[0, 1].Should().BeApproximately(1.0, 0.0001);
    }

    [Fact]
    public void Compute_Should_FallbackToEqualWeights_WhenWeightsAreZero()
    {
        var beats = new[]
        {
            CreateBeat(0, [1f, 0f], [1f, 0f]),
            CreateBeat(1, [1f, 0f], [0f, 1f])
        };

        var matrix = SelfSimilarityMatrix.Compute(beats, 0.0, 0.0, 0.0, 0.0);

        matrix[0, 1].Should().BeApproximately(1.0 / 3.0, 0.0001);
    }

    [Fact]
    public void Compute_Should_NotThrow_WhenVectorsHaveDifferentLengths()
    {
        var beats = new[]
        {
            CreateBeat(0, [1f, 2f, 3f], [1f]),
            CreateBeat(1, [1f], [1f, 2f])
        };

        var act = () => SelfSimilarityMatrix.Compute(beats, 0.5, 0.5, 0.0, 0.0);

        act.Should().NotThrow();
    }

    [Fact]
    public void Compute_Should_NotReturnNaN()
    {
        var matrix = SelfSimilarityMatrix.Compute(CreateBeats(), 0.5, 0.5, 0.0, 0.0);

        matrix[0, 1].Should().NotBe(double.NaN);
    }

    private static Beat[] CreateBeats()
    {
        return
        [
            CreateBeat(0, [1f, 0f], [1f, 0f]),
            CreateBeat(1, [0f, 1f], [0f, 1f])
        ];
    }

    [Fact]
    public void Compute_Should_AcceptLoudnessWeight_AndAffectSimilarity()
    {
        var beats = new[]
        {
            CreateBeat(0, [1f, 0f], [1f, 0f], [2f, 2f, 2f]),
            CreateBeat(1, [1f, 0f], [1f, 0f], [-2f, -2f, -2f])
        };

        var matrixWithoutLoudness = SelfSimilarityMatrix.Compute(beats, 0.5, 0.5, 0.0, 0.0);
        var matrixWithLoudness = SelfSimilarityMatrix.Compute(beats, 0.45, 0.35, 0.20, 0.0);

        matrixWithoutLoudness[0, 1].Should().BeApproximately(1.0, 1e-6);
        matrixWithLoudness[0, 1].Should().BeLessThan(matrixWithoutLoudness[0, 1]);
    }

    [Fact]
    public void Compute_Should_HandleAllZeroLoudness_Gracefully()
    {
        var beats = new[]
        {
            CreateBeat(0, [1f, 0f], [1f, 0f], [0f, 0f, 0f]),
            CreateBeat(1, [1f, 0f], [1f, 0f], [0f, 0f, 0f])
        };

        var matrix = SelfSimilarityMatrix.Compute(beats, 0.45, 0.35, 0.20, 0.0);

        matrix[0, 1].Should().BeInRange(0.0, 1.0);
        matrix[0, 1].Should().BeGreaterThan(0.5);
    }

    [Fact]
    public void Compute_Should_NormalizeWeights_WhenSumDiffersFromOne()
    {
        var beats = new[]
        {
            CreateBeat(0, [1f, 0f], [1f, 0f], [1f, 1f, 1f]),
            CreateBeat(1, [1f, 0f], [1f, 0f], [1f, 1f, 1f])
        };

        var matrixA = SelfSimilarityMatrix.Compute(beats, 1.0, 1.0, 1.0, 0.0);
        var matrixB = SelfSimilarityMatrix.Compute(beats, 0.333, 0.333, 0.334, 0.0);

        matrixA[0, 1].Should().BeApproximately(matrixB[0, 1], 1e-3);
    }

    [Fact]
    public void Compute_Should_AcceptBarPositionWeight_AndAffectSimilarity()
    {
        var beats = new[]
        {
            CreateBeat(0, [1f, 0f], [1f, 0f], [0f, 0f, 0f], [0f, 1f]),
            CreateBeat(1, [1f, 0f], [1f, 0f], [0f, 0f, 0f], [0f, -1f])
        };

        var matrixWithoutBarPosition = SelfSimilarityMatrix.Compute(beats, 0.5, 0.5, 0.0, 0.0);
        var matrixWithBarPosition = SelfSimilarityMatrix.Compute(beats, 0.40, 0.30, 0.18, 0.12);

        matrixWithoutBarPosition[0, 1].Should().BeApproximately(1.0, 1e-6);
        matrixWithBarPosition[0, 1].Should().BeLessThan(matrixWithoutBarPosition[0, 1]);
    }

    [Fact]
    public void Compute_Should_NotBoostSimilarity_WhenBarPositionMatches()
    {
        var beats = new[]
        {
            CreateBeat(0, [1f, 0f], [1f, 0f], [1f, 1f, 1f], [0f, 1f]),
            CreateBeat(4, [0.7f, 0.3f], [0.7f, 0.3f], [1f, 1f, 1f], [0f, 1f])
        };

        var withoutMetricPenalty = SelfSimilarityMatrix.Compute(beats, 0.45, 0.35, 0.20, 0.0);
        var withMetricPenalty = SelfSimilarityMatrix.Compute(beats, 0.45, 0.35, 0.20, 0.18);

        withMetricPenalty[0, 1].Should().BeApproximately(withoutMetricPenalty[0, 1], 1e-6);
    }

    [Fact]
    public void Compute_Should_PenalizeMetricMismatch_WithoutBoostingMetricMatch()
    {
        var sameMetric = new[]
        {
            CreateBeat(0, [1f, 0f], [1f, 0f], [1f, 1f, 1f], [0f, 1f]),
            CreateBeat(4, [1f, 0f], [1f, 0f], [1f, 1f, 1f], [0f, 1f])
        };
        var wrongMetric = new[]
        {
            CreateBeat(0, [1f, 0f], [1f, 0f], [1f, 1f, 1f], [0f, 1f]),
            CreateBeat(2, [1f, 0f], [1f, 0f], [1f, 1f, 1f], [0f, -1f])
        };

        var sameMatrix = SelfSimilarityMatrix.Compute(sameMetric, 0.45, 0.35, 0.20, 0.18);
        var wrongMatrix = SelfSimilarityMatrix.Compute(wrongMetric, 0.45, 0.35, 0.20, 0.18);

        sameMatrix[0, 1].Should().BeApproximately(1.0, 1e-6);
        wrongMatrix[0, 1].Should().BeLessThan(sameMatrix[0, 1]);
    }

    [Fact]
    public void Compute_Should_TreatZeroBarPositionVectorAsNeutral()
    {
        var beats = new[]
        {
            CreateBeat(0, [1f, 0f], [1f, 0f], [1f, 1f, 1f], [0f, 0f]),
            CreateBeat(1, [1f, 0f], [1f, 0f], [1f, 1f, 1f], [0f, 0f])
        };

        var matrix = SelfSimilarityMatrix.Compute(beats, 0.45, 0.35, 0.20, 0.18);

        matrix[0, 1].Should().BeApproximately(1.0, 1e-6);
    }

    [Fact]
    public void Compute_Should_NotPenalize_BeatsAtSameMetricPosition()
    {
        var beats = new[]
        {
            CreateBeat(0, [1f, 0f], [1f, 0f], [1f, 1f, 1f], [0f, 1f]),
            CreateBeat(4, [1f, 0f], [1f, 0f], [1f, 1f, 1f], [0f, 1f])
        };

        var matrix = SelfSimilarityMatrix.Compute(beats, 0.40, 0.30, 0.18, 0.12);

        matrix[0, 1].Should().BeApproximately(1.0, 1e-6);
    }

    [Fact]
    public void Compute_Should_NormalizeFourWeights_WhenSumDiffersFromOne()
    {
        var beats = new[]
        {
            CreateBeat(0, [1f, 0f], [1f, 0f], [1f, 1f, 1f], [1f, 0f]),
            CreateBeat(1, [1f, 0f], [1f, 0f], [1f, 1f, 1f], [1f, 0f])
        };

        var matrixA = SelfSimilarityMatrix.Compute(beats, 1.0, 1.0, 1.0, 1.0);
        var matrixB = SelfSimilarityMatrix.Compute(beats, 0.25, 0.25, 0.25, 0.25);

        matrixA[0, 1].Should().BeApproximately(matrixB[0, 1], 1e-6);
    }

    private static Beat CreateBeat(
        int index,
        float[] timbre,
        float[] pitches,
        float[]? loudness = null,
        float[]? barPosition = null)
    {
        return new Beat
        {
            Index = index,
            Start = index * 0.5,
            Duration = 0.5,
            Confidence = 1.0,
            Timbre = timbre,
            Pitches = pitches,
            Loudness = loudness ?? [0f, 0f, 0f],
            BarPosition = barPosition ?? [0f, 0f]
        };
    }
}
