using EternalLoop.BranchAnalysis.Core.Runner;
using FluentAssertions;

namespace EternalLoop.BranchAnalysis.Tests.Runner;

public sealed class BranchAnalysisTuningMapperTests
{
    [Theory]
    [InlineData(0.86, 80)]
    [InlineData(0.92, 70)]
    [InlineData(0.78, 95)]
    public void MapSimilarityToMaxThresholdShouldMatchPresetAnchors(
        double similarityThreshold,
        int expectedMaxThreshold)
    {
        int maxThreshold = BranchAnalysisTuningMapper.MapSimilarityToMaxThreshold(
            similarityThreshold);

        maxThreshold.Should().Be(expectedMaxThreshold);
    }

    [Theory]
    [InlineData(double.NaN, 100)]
    [InlineData(0.10, 100)]
    [InlineData(2.00, 65)]
    public void MapSimilarityToMaxThresholdShouldClampOutOfRangeValues(
        double similarityThreshold,
        int expectedMaxThreshold)
    {
        int maxThreshold = BranchAnalysisTuningMapper.MapSimilarityToMaxThreshold(
            similarityThreshold);

        maxThreshold.Should().Be(expectedMaxThreshold);
    }
}
