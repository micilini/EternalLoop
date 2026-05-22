using EternalLoop.Contracts.Options;
using FluentAssertions;

namespace EternalLoop.Core.Tests.Contracts;

public sealed class TuningPresetCatalogTests
{
    [Fact]
    public void All_Should_Contain_ExpectedPresets()
    {
        TuningPresetCatalog.All.Should().HaveCount(3);
        TuningPresetCatalog.All.Select(preset => preset.Id).Should().Equal(
            TuningPresetCatalog.ConservativeId,
            TuningPresetCatalog.BalancedId,
            TuningPresetCatalog.WildId);
    }

    [Fact]
    public void Presets_Should_Order_Strictness_AsExpected()
    {
        var conservative = TuningPresetCatalog.GetById(TuningPresetCatalog.ConservativeId);
        var balanced = TuningPresetCatalog.GetById(TuningPresetCatalog.BalancedId);
        var wild = TuningPresetCatalog.GetById(TuningPresetCatalog.WildId);

        wild.SimilarityThreshold.Should().BeLessThan(balanced.SimilarityThreshold);
        conservative.SimilarityThreshold.Should().BeGreaterThan(balanced.SimilarityThreshold);
        wild.JumpProbability.Should().BeGreaterThan(balanced.JumpProbability);
        conservative.JumpProbability.Should().BeLessThan(balanced.JumpProbability);
    }

    [Fact]
    public void Conservative_Should_ExposeExpectedValues()
    {
        var preset = TuningPresetCatalog.GetById(TuningPresetCatalog.ConservativeId);

        preset.SimilarityThreshold.Should().Be(0.92);
        preset.LookaheadDepth.Should().Be(5);
        preset.MinJumpDistance.Should().Be(28);
        preset.MaxBranchesPerBeat.Should().Be(2);
        preset.JumpProbability.Should().Be(0.14);
        preset.JumpCooldown.Should().Be(16);
        preset.FirstPassLinearPlaybackRatio.Should().Be(0.82);
    }

    [Fact]
    public void Balanced_Should_ExposeExpectedValues()
    {
        var preset = TuningPresetCatalog.GetById(TuningPresetCatalog.BalancedId);

        preset.SimilarityThreshold.Should().Be(0.86);
        preset.LookaheadDepth.Should().Be(4);
        preset.MinJumpDistance.Should().Be(20);
        preset.MaxBranchesPerBeat.Should().Be(3);
        preset.JumpProbability.Should().Be(0.22);
        preset.JumpCooldown.Should().Be(12);
        preset.FirstPassLinearPlaybackRatio.Should().Be(0.78);
    }

    [Fact]
    public void Wild_Should_ExposeExpectedValues()
    {
        var preset = TuningPresetCatalog.GetById(TuningPresetCatalog.WildId);

        preset.SimilarityThreshold.Should().Be(0.78);
        preset.LookaheadDepth.Should().Be(3);
        preset.MinJumpDistance.Should().Be(12);
        preset.MaxBranchesPerBeat.Should().Be(5);
        preset.JumpProbability.Should().Be(0.42);
        preset.JumpCooldown.Should().Be(6);
        preset.FirstPassLinearPlaybackRatio.Should().Be(0.70);
    }
}
