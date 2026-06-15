using EternalLoop.Core.Settings;
using FluentAssertions;

namespace EternalLoop.Tests.Core.Settings;

public sealed class LoopTuningPresetCatalogTests
{
    [Fact]
    public void GetByIdShouldReturnBalancedForNullOrInvalidValues()
    {
        LoopTuningPresetCatalog.GetById(null).Id.Should().Be(LoopTuningPresetCatalog.BalancedId);
        LoopTuningPresetCatalog.GetById("invalid").Id.Should().Be(LoopTuningPresetCatalog.BalancedId);
    }

    [Fact]
    public void BalancedPresetShouldBePlayableEarly()
    {
        LoopTuningPresetDefinition preset = LoopTuningPresetCatalog.GetById(
            LoopTuningPresetCatalog.BalancedId);

        preset.SimilarityThreshold.Should().Be(0.86);
        preset.LookaheadDepth.Should().Be(1);
        preset.MinJumpDistance.Should().Be(4);
        preset.MaxBranchesPerBeat.Should().Be(6);
        preset.JumpProbability.Should().Be(0.85);
        preset.JumpCooldown.Should().Be(4);
        preset.FirstPassLinearPlaybackRatio.Should().Be(0.10);
        preset.BranchQuantumType.Should().Be("beats");
        preset.BranchMaxThreshold.Should().Be(80);
        preset.AnalysisMusicalQuality.Should().BeTrue();
    }

    [Fact]
    public void WildPresetShouldAllowImmediateBranching()
    {
        LoopTuningPresetDefinition preset = LoopTuningPresetCatalog.GetById(
            LoopTuningPresetCatalog.WildId);

        preset.JumpProbability.Should().Be(1.00);
        preset.JumpCooldown.Should().Be(0);
        preset.FirstPassLinearPlaybackRatio.Should().Be(0.00);
        preset.MaxBranchesPerBeat.Should().Be(8);
    }

    [Fact]
    public void ConservativePresetShouldStillPreserveStructure()
    {
        LoopTuningPresetDefinition preset = LoopTuningPresetCatalog.GetById(
            LoopTuningPresetCatalog.ConservativeId);

        preset.JumpProbability.Should().Be(0.35);
        preset.JumpCooldown.Should().Be(12);
        preset.FirstPassLinearPlaybackRatio.Should().Be(0.50);
        preset.MaxBranchesPerBeat.Should().Be(2);
    }
}
