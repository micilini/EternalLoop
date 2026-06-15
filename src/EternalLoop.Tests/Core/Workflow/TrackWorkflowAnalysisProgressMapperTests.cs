using EternalLoop.AnalysisEngine.Core.Progress;
using EternalLoop.Core.Workflow;
using FluentAssertions;

namespace EternalLoop.Tests.Core.Workflow;

public sealed class TrackWorkflowAnalysisProgressMapperTests
{
    [Theory]
    [InlineData(AnalysisStage.LoadingAudio, 0, 10)]
    [InlineData(AnalysisStage.LoadingAudio, 1, 15)]
    [InlineData(AnalysisStage.ExtractingFeatures, 0, 15)]
    [InlineData(AnalysisStage.ExtractingFeatures, 1, 40)]
    [InlineData(AnalysisStage.TrackingBeats, 0, 40)]
    [InlineData(AnalysisStage.TrackingBeats, 1, 58)]
    [InlineData(AnalysisStage.BuildingAnalysis, 1, 65)]
    [InlineData(AnalysisStage.Validating, 1, 68)]
    [InlineData(AnalysisStage.Done, 0, 68)]
    public void Map_ShouldAssignExpectedStageRanges(AnalysisStage stage, double progress, double expected)
    {
        var mapper = new TrackWorkflowAnalysisProgressMapper();

        double result = mapper.Map(stage, progress);

        result.Should().Be(expected);
    }

    [Fact]
    public void Map_ShouldGiveTrackingBeatsHigherRangeThanExtractingFeatures()
    {
        var mapper = new TrackWorkflowAnalysisProgressMapper();

        double extracting = mapper.Map(AnalysisStage.ExtractingFeatures, 0);
        double tracking = mapper.Map(AnalysisStage.TrackingBeats, 0);

        tracking.Should().BeGreaterThan(extracting);
    }

    [Fact]
    public void Map_ShouldNeverMoveBackward()
    {
        var mapper = new TrackWorkflowAnalysisProgressMapper();

        double high = mapper.Map(AnalysisStage.TrackingBeats, 0.5);
        double lowerStageReport = mapper.Map(AnalysisStage.ExtractingFeatures, 0);

        lowerStageReport.Should().Be(high);
    }
}
