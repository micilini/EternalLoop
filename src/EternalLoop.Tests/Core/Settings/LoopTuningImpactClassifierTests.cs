using EternalLoop.Core.Settings;
using FluentAssertions;

namespace EternalLoop.Tests.Core.Settings;

public sealed class LoopTuningImpactClassifierTests
{
    [Fact]
    public void CompareShouldReturnNoneForIdenticalSettings()
    {
        LoopTuningSettings previous = LoopTuningSettings.Balanced();
        LoopTuningSettings current = LoopTuningSettings.Balanced();

        LoopTuningImpactClassifier.Compare(previous, current).Should().Be(LoopTuningImpact.None);
    }

    [Fact]
    public void CompareShouldReturnAnalysisWhenAnalysisMusicalQualityChanges()
    {
        LoopTuningSettings previous = LoopTuningSettings.Balanced();
        LoopTuningSettings current = LoopTuningSettings.Balanced();
        current.AnalysisMusicalQuality = !previous.AnalysisMusicalQuality;
        current.JumpProbability = 0.9;

        LoopTuningImpactClassifier.Compare(previous, current).Should().Be(LoopTuningImpact.Analysis);
    }

    [Theory]
    [InlineData("SimilarityThreshold")]
    [InlineData("LookaheadDepth")]
    [InlineData("MinJumpDistance")]
    [InlineData("MaxBranchesPerBeat")]
    [InlineData("BranchQuantumType")]
    [InlineData("BranchMaxThreshold")]
    public void CompareShouldReturnBranchesWhenBranchTuningChanges(string propertyName)
    {
        LoopTuningSettings previous = LoopTuningSettings.Balanced();
        LoopTuningSettings current = LoopTuningSettings.Balanced();

        switch (propertyName)
        {
            case "SimilarityThreshold":
                current.SimilarityThreshold = 0.82;
                break;
            case "LookaheadDepth":
                current.LookaheadDepth = 2;
                break;
            case "MinJumpDistance":
                current.MinJumpDistance = 8;
                break;
            case "MaxBranchesPerBeat":
                current.MaxBranchesPerBeat = 8;
                break;
            case "BranchQuantumType":
                current.BranchQuantumType = "segments";
                break;
            case "BranchMaxThreshold":
                current.BranchMaxThreshold = 90;
                break;
        }

        current.JumpCooldown = 2;

        LoopTuningImpactClassifier.Compare(previous, current).Should().Be(LoopTuningImpact.Branches);
    }

    [Theory]
    [InlineData("JumpProbability")]
    [InlineData("JumpCooldown")]
    [InlineData("FirstPassLinearPlaybackRatio")]
    public void CompareShouldReturnRuntimeOnlyWhenRuntimeTuningChanges(string propertyName)
    {
        LoopTuningSettings previous = LoopTuningSettings.Balanced();
        LoopTuningSettings current = LoopTuningSettings.Balanced();

        switch (propertyName)
        {
            case "JumpProbability":
                current.JumpProbability = 0.55;
                break;
            case "JumpCooldown":
                current.JumpCooldown = 3;
                break;
            case "FirstPassLinearPlaybackRatio":
                current.FirstPassLinearPlaybackRatio = 0.25;
                break;
        }

        LoopTuningImpactClassifier.Compare(previous, current).Should().Be(LoopTuningImpact.RuntimeOnly);
    }
}
