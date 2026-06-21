using EternalLoop.Core.Settings;
using FluentAssertions;

namespace EternalLoop.Tests.Core.Settings;

public sealed class LoopTuningImpactClassifierTests
{
    [Fact]
    public void CompareShouldReturnNoneForEquivalentSettings()
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

        LoopTuningImpactClassifier.Compare(previous, current).Should().Be(LoopTuningImpact.Analysis);
    }

    [Fact]
    public void CompareShouldReturnAnalysisWhenAnalysisBeatProviderChanges()
    {
        LoopTuningSettings previous = LoopTuningSettings.Balanced();
        LoopTuningSettings current = LoopTuningSettings.Balanced();
        current.AnalysisBeatProvider = AnalysisBeatModeCatalog.ClassicId;

        LoopTuningImpactClassifier.Compare(previous, current).Should().Be(LoopTuningImpact.Analysis);
    }

    [Theory]
    [InlineData(nameof(LoopTuningSettings.SimilarityThreshold))]
    [InlineData(nameof(LoopTuningSettings.LookaheadDepth))]
    [InlineData(nameof(LoopTuningSettings.MinJumpDistance))]
    [InlineData(nameof(LoopTuningSettings.MaxBranchesPerBeat))]
    [InlineData(nameof(LoopTuningSettings.BranchQuantumType))]
    [InlineData(nameof(LoopTuningSettings.BranchMaxThreshold))]
    public void CompareShouldReturnBranchesWhenBranchTuningChanges(string propertyName)
    {
        LoopTuningSettings previous = LoopTuningSettings.Balanced();
        LoopTuningSettings current = LoopTuningSettings.Balanced();

        switch (propertyName)
        {
            case nameof(LoopTuningSettings.SimilarityThreshold):
                current.SimilarityThreshold = 0.77;
                break;
            case nameof(LoopTuningSettings.LookaheadDepth):
                current.LookaheadDepth = 2;
                break;
            case nameof(LoopTuningSettings.MinJumpDistance):
                current.MinJumpDistance = 12;
                break;
            case nameof(LoopTuningSettings.MaxBranchesPerBeat):
                current.MaxBranchesPerBeat = 3;
                break;
            case nameof(LoopTuningSettings.BranchQuantumType):
                current.BranchQuantumType = "bars";
                break;
            case nameof(LoopTuningSettings.BranchMaxThreshold):
                current.BranchMaxThreshold = 90;
                break;
        }

        LoopTuningImpactClassifier.Compare(previous, current).Should().Be(LoopTuningImpact.Branches);
    }

    [Theory]
    [InlineData(nameof(LoopTuningSettings.JumpProbability))]
    [InlineData(nameof(LoopTuningSettings.JumpCooldown))]
    [InlineData(nameof(LoopTuningSettings.FirstPassLinearPlaybackRatio))]
    public void CompareShouldReturnRuntimeOnlyWhenRuntimeTuningChanges(string propertyName)
    {
        LoopTuningSettings previous = LoopTuningSettings.Balanced();
        LoopTuningSettings current = LoopTuningSettings.Balanced();

        switch (propertyName)
        {
            case nameof(LoopTuningSettings.JumpProbability):
                current.JumpProbability = 0.5;
                break;
            case nameof(LoopTuningSettings.JumpCooldown):
                current.JumpCooldown = 9;
                break;
            case nameof(LoopTuningSettings.FirstPassLinearPlaybackRatio):
                current.FirstPassLinearPlaybackRatio = 0.25;
                break;
        }

        LoopTuningImpactClassifier.Compare(previous, current).Should().Be(LoopTuningImpact.RuntimeOnly);
    }
}
