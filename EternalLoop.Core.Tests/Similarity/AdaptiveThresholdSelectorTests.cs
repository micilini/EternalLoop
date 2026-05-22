using EternalLoop.Core.Similarity;
using FluentAssertions;

namespace EternalLoop.Core.Tests.Similarity;

public sealed class AdaptiveThresholdSelectorTests
{
    [Fact]
    public void Select_Should_ReturnFallback_WhenMatrixIsEmpty()
    {
        var threshold = AdaptiveThresholdSelector.Select(new double[0, 0], 1, 0, fallbackThreshold: 0.7);

        threshold.Should().Be(0.7);
    }

    [Fact]
    public void Select_Should_ReturnFallback_WhenNoCandidatesExist()
    {
        var threshold = AdaptiveThresholdSelector.Select(new double[2, 2], 10, 0, fallbackThreshold: 0.6);

        threshold.Should().Be(0.6);
    }

    [Fact]
    public void Select_Should_ReturnValueBetweenZeroAndOne()
    {
        var threshold = AdaptiveThresholdSelector.Select(CreateMatrix(10, 0.5), 1, 0);

        threshold.Should().BeInRange(0.0, 1.0);
    }

    [Fact]
    public void Select_Should_PreferHigherThreshold_WhenManyHighScoresExist()
    {
        var threshold = AdaptiveThresholdSelector.Select(CreateMatrix(30, 0.95), 1, 0, targetMaxEdges: 20);

        threshold.Should().BeGreaterThan(0.9);
    }

    [Fact]
    public void Select_Should_IgnoreNaNAndInfinity()
    {
        var matrix = CreateMatrix(5, 0.8);
        matrix[0, 2] = double.NaN;
        matrix[1, 3] = double.PositiveInfinity;

        var threshold = AdaptiveThresholdSelector.Select(matrix, 1, 0);

        threshold.Should().BeInRange(0.0, 1.0);
    }

    [Fact]
    public void Select_Should_RespectMinJumpDistance()
    {
        var matrix = new double[6, 6];
        matrix[0, 1] = 0.99;
        matrix[1, 0] = 0.99;
        matrix[0, 5] = 0.4;
        matrix[5, 0] = 0.4;

        var threshold = AdaptiveThresholdSelector.Select(matrix, 5, 0, targetMaxEdges: 1);

        threshold.Should().Be(0.4);
    }

    [Fact]
    public void Select_Should_BeEquivalentToIndividualPairs_When_LookaheadIsZero()
    {
        var matrix = new double[10, 10];
        for (var i = 0; i < 10; i++)
        {
            for (var j = 0; j < 10; j++)
            {
                matrix[i, j] = i == j ? 1.0 : 0.5 + (Math.Abs(i - j) / 20.0);
            }
        }

        var threshold = AdaptiveThresholdSelector.Select(
            matrix,
            minJumpDistance: 2,
            lookaheadDepth: 0,
            targetMaxEdges: 10);

        threshold.Should().BeInRange(0.0, 1.0);
        threshold.Should().BeGreaterThan(0.0);
    }

    [Fact]
    public void Select_Should_ProduceLowerOrEqualThreshold_WithHigherLookahead()
    {
        var matrix = BuildSymmetricSimilarityMatrix(size: 60, seed: 42);

        var thresholdNoLookahead = AdaptiveThresholdSelector.Select(
            matrix,
            minJumpDistance: 4,
            lookaheadDepth: 0,
            targetMaxEdges: 50);

        var thresholdWithLookahead = AdaptiveThresholdSelector.Select(
            matrix,
            minJumpDistance: 4,
            lookaheadDepth: 3,
            targetMaxEdges: 50);

        thresholdWithLookahead.Should().BeLessThanOrEqualTo(thresholdNoLookahead + 1e-9);
    }

    [Fact]
    public void Select_Should_NotCollapseToNearOne_When_ManyPairsExceedHighThreshold_ButLookaheadStrict()
    {
        var matrix = BuildPopMusicLikeMatrix(size: 80);

        var threshold = AdaptiveThresholdSelector.Select(
            matrix,
            minJumpDistance: 8,
            lookaheadDepth: 3,
            targetMaxEdges: 50);

        threshold.Should().BeLessThan(0.97);
    }

    [Fact]
    public void Select_Should_ReturnFallback_When_NoValidPairsExist()
    {
        var matrix = new double[3, 3];
        matrix[0, 0] = 1.0;
        matrix[1, 1] = 1.0;
        matrix[2, 2] = 1.0;

        var threshold = AdaptiveThresholdSelector.Select(
            matrix,
            minJumpDistance: 10,
            lookaheadDepth: 5,
            fallbackThreshold: 0.77);

        threshold.Should().BeApproximately(0.77, precision: 1e-9);
    }

    private static double[,] CreateMatrix(int size, double value)
    {
        var matrix = new double[size, size];

        for (var i = 0; i < size; i++)
        {
            for (var j = 0; j < size; j++)
            {
                matrix[i, j] = i == j ? 1.0 : value;
            }
        }

        return matrix;
    }

    private static double[,] BuildSymmetricSimilarityMatrix(int size, int seed)
    {
        var rng = new Random(seed);
        var matrix = new double[size, size];

        for (var i = 0; i < size; i++)
        {
            matrix[i, i] = 1.0;
            for (var j = i + 1; j < size; j++)
            {
                var value = rng.NextDouble() * 0.5 + 0.5;
                matrix[i, j] = value;
                matrix[j, i] = value;
            }
        }

        return matrix;
    }

    private static double[,] BuildPopMusicLikeMatrix(int size)
    {
        var matrix = new double[size, size];
        var rng = new Random(123);

        for (var i = 0; i < size; i++)
        {
            matrix[i, i] = 1.0;
            for (var j = i + 1; j < size; j++)
            {
                var sameSection = i % 8 == j % 8;
                var baseSimilarity = sameSection ? 0.90 : 0.55;
                var noise = (rng.NextDouble() - 0.5) * 0.15;
                var value = Math.Clamp(baseSimilarity + noise, 0.0, 1.0);
                matrix[i, j] = value;
                matrix[j, i] = value;
            }
        }

        return matrix;
    }
}
