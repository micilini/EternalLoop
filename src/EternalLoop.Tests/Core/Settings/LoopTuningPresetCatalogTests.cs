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
    public void BalancedPresetShouldMatchValidatedScriptDefaults()
    {
        LoopTuningPresetDefinition preset = LoopTuningPresetCatalog.GetById(
            LoopTuningPresetCatalog.BalancedId);

        preset.SimilarityThreshold.Should().Be(0.86);
        preset.LookaheadDepth.Should().Be(1);
        preset.MinJumpDistance.Should().Be(4);
        preset.MaxBranchesPerBeat.Should().Be(4);
        preset.JumpProbability.Should().Be(0.22);
        preset.JumpCooldown.Should().Be(12);
        preset.FirstPassLinearPlaybackRatio.Should().Be(0.78);
        preset.BranchQuantumType.Should().Be("beats");
        preset.BranchMaxThreshold.Should().Be(80);
        preset.AnalysisMusicalQuality.Should().BeTrue();
    }
}
