using EternalLoop.Contracts.Enums;
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
    public void Presets_Should_ExposeBranchQualityValues()
    {
        foreach (var preset in TuningPresetCatalog.All)
        {
            preset.UseDurationSimilarityGate.Should().BeTrue();
            preset.DurationPenaltyStartRatio.Should().BeGreaterThan(0);
            preset.DurationRejectionRatio.Should().BeGreaterThan(0);
            preset.DurationPenaltyStrength.Should().BeGreaterThan(0);
            preset.UseConfidencePenalty.Should().BeTrue();
            preset.ConfidencePenaltyStart.Should().BeGreaterThan(0);
            preset.ConfidenceRejectionThreshold.Should().BeGreaterThan(0);
            preset.ConfidencePenaltyStrength.Should().BeGreaterThan(0);
            preset.TargetBranchSourceRatio.Should().BeGreaterThan(0);
            preset.MaxBranchSourceRatio.Should().BeGreaterThan(0);
            preset.UseMicrosegmentSimilarity.Should().BeTrue();
            preset.MicrosegmentCount.Should().BeGreaterThan(0);
            preset.MicrosegmentPenaltyStartThreshold.Should().BeGreaterThan(0);
            preset.MicrosegmentRejectionThreshold.Should().BeGreaterThan(0);
            preset.MicrosegmentPenaltyStrength.Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public void Presets_Should_Order_BranchQualityStrictness_AsExpected()
    {
        var conservative = TuningPresetCatalog.GetById(TuningPresetCatalog.ConservativeId);
        var balanced = TuningPresetCatalog.GetById(TuningPresetCatalog.BalancedId);
        var wild = TuningPresetCatalog.GetById(TuningPresetCatalog.WildId);

        conservative.MetricPositionMode.Should().Be(MetricPositionMode.StrictGate);
        balanced.MetricPositionMode.Should().Be(MetricPositionMode.StrongPenalty);
        wild.MetricPositionMode.Should().Be(MetricPositionMode.SoftPenalty);

        conservative.DurationPenaltyStartRatio.Should().BeGreaterThan(balanced.DurationPenaltyStartRatio);
        balanced.DurationPenaltyStartRatio.Should().BeGreaterThan(wild.DurationPenaltyStartRatio);
        conservative.DurationRejectionRatio.Should().BeGreaterThan(balanced.DurationRejectionRatio);
        balanced.DurationRejectionRatio.Should().BeGreaterThan(wild.DurationRejectionRatio);
        conservative.DurationPenaltyStrength.Should().BeGreaterThan(balanced.DurationPenaltyStrength);
        balanced.DurationPenaltyStrength.Should().BeGreaterThan(wild.DurationPenaltyStrength);

        conservative.ConfidencePenaltyStart.Should().BeGreaterThan(balanced.ConfidencePenaltyStart);
        balanced.ConfidencePenaltyStart.Should().BeGreaterThan(wild.ConfidencePenaltyStart);
        conservative.ConfidenceRejectionThreshold.Should().BeGreaterThan(balanced.ConfidenceRejectionThreshold);
        balanced.ConfidenceRejectionThreshold.Should().BeGreaterThan(wild.ConfidenceRejectionThreshold);
        conservative.ConfidencePenaltyStrength.Should().BeGreaterThan(balanced.ConfidencePenaltyStrength);
        balanced.ConfidencePenaltyStrength.Should().BeGreaterThan(wild.ConfidencePenaltyStrength);

        conservative.TargetBranchSourceRatio.Should().BeLessThan(balanced.TargetBranchSourceRatio);
        balanced.TargetBranchSourceRatio.Should().BeLessThan(wild.TargetBranchSourceRatio);
        conservative.MaxBranchSourceRatio.Should().BeLessThan(balanced.MaxBranchSourceRatio);
        balanced.MaxBranchSourceRatio.Should().BeLessThan(wild.MaxBranchSourceRatio);

        conservative.MicrosegmentPenaltyStartThreshold.Should().BeGreaterThan(balanced.MicrosegmentPenaltyStartThreshold);
        balanced.MicrosegmentPenaltyStartThreshold.Should().BeGreaterThan(wild.MicrosegmentPenaltyStartThreshold);
        conservative.MicrosegmentRejectionThreshold.Should().BeGreaterThan(balanced.MicrosegmentRejectionThreshold);
        balanced.MicrosegmentRejectionThreshold.Should().BeGreaterThan(wild.MicrosegmentRejectionThreshold);
        conservative.MicrosegmentPenaltyStrength.Should().BeGreaterThan(balanced.MicrosegmentPenaltyStrength);
        balanced.MicrosegmentPenaltyStrength.Should().BeGreaterThan(wild.MicrosegmentPenaltyStrength);
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
    public void Conservative_Should_ExposeExpectedBranchQualityValues()
    {
        var preset = TuningPresetCatalog.GetById(TuningPresetCatalog.ConservativeId);

        preset.UseDurationSimilarityGate.Should().BeTrue();
        preset.DurationPenaltyStartRatio.Should().Be(0.94);
        preset.DurationRejectionRatio.Should().Be(0.86);
        preset.DurationPenaltyStrength.Should().Be(0.35);
        preset.UseConfidencePenalty.Should().BeTrue();
        preset.ConfidencePenaltyStart.Should().Be(0.60);
        preset.ConfidenceRejectionThreshold.Should().Be(0.35);
        preset.ConfidencePenaltyStrength.Should().Be(0.30);
        preset.MetricPositionMode.Should().Be(MetricPositionMode.StrictGate);
        preset.MetricPositionPenaltyStrength.Should().Be(0.45);
        preset.MetricPositionRejectionThreshold.Should().Be(0.95);
        preset.TargetBranchSourceRatio.Should().Be(0.10);
        preset.MaxBranchSourceRatio.Should().Be(0.14);
        preset.UseMicrosegmentSimilarity.Should().BeTrue();
        preset.MicrosegmentCount.Should().Be(4);
        preset.MicrosegmentPenaltyStartThreshold.Should().Be(0.86);
        preset.MicrosegmentRejectionThreshold.Should().Be(0.76);
        preset.MicrosegmentPenaltyStrength.Should().Be(0.35);
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
    public void Balanced_Should_ExposeExpectedBranchQualityValues()
    {
        var preset = TuningPresetCatalog.GetById(TuningPresetCatalog.BalancedId);

        preset.UseDurationSimilarityGate.Should().BeTrue();
        preset.DurationPenaltyStartRatio.Should().Be(0.90);
        preset.DurationRejectionRatio.Should().Be(0.80);
        preset.DurationPenaltyStrength.Should().Be(0.25);
        preset.UseConfidencePenalty.Should().BeTrue();
        preset.ConfidencePenaltyStart.Should().Be(0.50);
        preset.ConfidenceRejectionThreshold.Should().Be(0.25);
        preset.ConfidencePenaltyStrength.Should().Be(0.20);
        preset.MetricPositionMode.Should().Be(MetricPositionMode.StrongPenalty);
        preset.MetricPositionPenaltyStrength.Should().Be(0.32);
        preset.MetricPositionRejectionThreshold.Should().Be(0.20);
        preset.TargetBranchSourceRatio.Should().Be(0.16);
        preset.MaxBranchSourceRatio.Should().Be(0.22);
        preset.UseMicrosegmentSimilarity.Should().BeTrue();
        preset.MicrosegmentCount.Should().Be(4);
        preset.MicrosegmentPenaltyStartThreshold.Should().Be(0.82);
        preset.MicrosegmentRejectionThreshold.Should().Be(0.70);
        preset.MicrosegmentPenaltyStrength.Should().Be(0.25);
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

    [Fact]
    public void Wild_Should_ExposeExpectedBranchQualityValues()
    {
        var preset = TuningPresetCatalog.GetById(TuningPresetCatalog.WildId);

        preset.UseDurationSimilarityGate.Should().BeTrue();
        preset.DurationPenaltyStartRatio.Should().Be(0.84);
        preset.DurationRejectionRatio.Should().Be(0.72);
        preset.DurationPenaltyStrength.Should().Be(0.15);
        preset.UseConfidencePenalty.Should().BeTrue();
        preset.ConfidencePenaltyStart.Should().Be(0.40);
        preset.ConfidenceRejectionThreshold.Should().Be(0.15);
        preset.ConfidencePenaltyStrength.Should().Be(0.12);
        preset.MetricPositionMode.Should().Be(MetricPositionMode.SoftPenalty);
        preset.MetricPositionPenaltyStrength.Should().Be(0.16);
        preset.MetricPositionRejectionThreshold.Should().Be(0.0);
        preset.TargetBranchSourceRatio.Should().Be(0.25);
        preset.MaxBranchSourceRatio.Should().Be(0.34);
        preset.UseMicrosegmentSimilarity.Should().BeTrue();
        preset.MicrosegmentCount.Should().Be(3);
        preset.MicrosegmentPenaltyStartThreshold.Should().Be(0.76);
        preset.MicrosegmentRejectionThreshold.Should().Be(0.62);
        preset.MicrosegmentPenaltyStrength.Should().Be(0.16);
    }
}
