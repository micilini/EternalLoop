using EternalLoop.Contracts.Enums;
using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;
using FluentAssertions;

namespace EternalLoop.Core.Tests.Contracts;

public sealed class TuningOptionsMapperTests
{
    [Fact]
    public void ToBranchFindingOptions_Should_Map_UserSettings()
    {
        var settings = new UserSettings
        {
            SimilarityThreshold = 0.72,
            LookaheadDepth = 2,
            MinJumpDistance = 8,
            MaxBranchesPerBeat = 8,
            UseAiSimilarity = false
        };

        var options = TuningOptionsMapper.ToBranchFindingOptions(settings);

        options.SimilarityThreshold.Should().Be(0.72);
        options.LookaheadDepth.Should().Be(2);
        options.MinJumpDistance.Should().Be(8);
        options.MaxBranchesPerBeat.Should().Be(8);
        options.LandingOffsetBeats.Should().Be(1);
        options.TimbreWeight.Should().Be(0.45);
        options.PitchWeight.Should().Be(0.35);
        options.LoudnessWeight.Should().Be(0.20);
        options.BarPositionWeight.Should().Be(0.18);
        options.ContinuationLookaheadDepth.Should().Be(6);
        options.ContinuationThresholdMargin.Should().Be(0.02);
        options.UseAiSimilarity.Should().BeFalse();
        options.AiRejectionThreshold.Should().Be(0.58);
        options.AiPenaltyStartThreshold.Should().Be(0.72);
        options.AiPenaltyStrength.Should().Be(0.22);
        options.UseDurationSimilarityGate.Should().BeTrue();
        options.DurationPenaltyStartRatio.Should().Be(0.90);
        options.DurationRejectionRatio.Should().Be(0.80);
        options.DurationPenaltyStrength.Should().Be(0.25);
        options.UseConfidencePenalty.Should().BeTrue();
        options.ConfidencePenaltyStart.Should().Be(0.50);
        options.ConfidenceRejectionThreshold.Should().Be(0.25);
        options.ConfidencePenaltyStrength.Should().Be(0.20);
        options.MetricPositionMode.Should().Be(MetricPositionMode.StrongPenalty);
        options.MetricPositionPenaltyStrength.Should().Be(0.32);
        options.MetricPositionRejectionThreshold.Should().Be(0.20);
        options.TargetBranchSourceRatio.Should().Be(0.16);
        options.MaxBranchSourceRatio.Should().Be(0.22);
        options.UseMicrosegmentSimilarity.Should().BeTrue();
        options.MicrosegmentCount.Should().Be(4);
        options.MicrosegmentPenaltyStartThreshold.Should().Be(0.80);
        options.MicrosegmentRejectionThreshold.Should().Be(0.64);
        options.MicrosegmentPenaltyStrength.Should().Be(0.18);
    }

    [Fact]
    public void ToBranchFindingOptions_Should_Map_BranchQualityOptions_FromBalancedPreset()
    {
        var settings = new UserSettings
        {
            Preset = TuningPresetCatalog.BalancedId
        };

        var options = TuningOptionsMapper.ToBranchFindingOptions(settings);

        options.MetricPositionMode.Should().Be(MetricPositionMode.StrongPenalty);
        options.DurationPenaltyStartRatio.Should().Be(0.90);
        options.TargetBranchSourceRatio.Should().Be(0.16);
        options.MicrosegmentPenaltyStrength.Should().Be(0.18);
    }

    [Fact]
    public void ToBranchFindingOptions_Should_Map_BranchQualityOptions_FromConservativePreset()
    {
        var settings = new UserSettings
        {
            Preset = TuningPresetCatalog.ConservativeId
        };

        var options = TuningOptionsMapper.ToBranchFindingOptions(settings);

        options.MetricPositionMode.Should().Be(MetricPositionMode.StrictGate);
        options.DurationPenaltyStartRatio.Should().Be(0.94);
        options.TargetBranchSourceRatio.Should().Be(0.10);
        options.MicrosegmentPenaltyStrength.Should().Be(0.35);
    }

    [Fact]
    public void ToBranchFindingOptions_Should_Map_BranchQualityOptions_FromWildPreset()
    {
        var settings = new UserSettings
        {
            Preset = TuningPresetCatalog.WildId
        };

        var options = TuningOptionsMapper.ToBranchFindingOptions(settings);

        options.MetricPositionMode.Should().Be(MetricPositionMode.SoftPenalty);
        options.DurationPenaltyStartRatio.Should().Be(0.84);
        options.TargetBranchSourceRatio.Should().Be(0.25);
        options.MicrosegmentPenaltyStrength.Should().Be(0.10);
    }

    [Fact]
    public void ToBranchFindingOptions_Should_FallbackBranchQualityOptions_ToBalanced_WhenPresetIsUnknown()
    {
        var settings = new UserSettings
        {
            Preset = "Unknown"
        };

        var options = TuningOptionsMapper.ToBranchFindingOptions(settings);

        options.MetricPositionMode.Should().Be(MetricPositionMode.StrongPenalty);
        options.DurationPenaltyStartRatio.Should().Be(0.90);
        options.TargetBranchSourceRatio.Should().Be(0.16);
        options.MicrosegmentPenaltyStrength.Should().Be(0.18);
    }

    [Fact]
    public void ToAiAnalysisOptions_Should_Map_UserSettings()
    {
        var settings = new UserSettings
        {
            UseAiSimilarity = false
        };

        var options = TuningOptionsMapper.ToAiAnalysisOptions(settings);

        options.IsEnabled.Should().BeFalse();
        options.ModelId.Should().Be(AiModelDefaultValues.DiscogsEffNetModelId);
        options.RejectionThreshold.Should().Be(TuningDefaultValues.AiRejectionThreshold);
        options.PenaltyStartThreshold.Should().Be(TuningDefaultValues.AiPenaltyStartThreshold);
        options.PenaltyStrength.Should().Be(TuningDefaultValues.AiPenaltyStrength);
        options.BeatContextBefore.Should().Be(TuningDefaultValues.AiBeatContextBefore);
        options.BeatContextAfter.Should().Be(TuningDefaultValues.AiBeatContextAfter);
    }

    [Fact]
    public void ToBranchFindingOptions_Uses_AiToggle_FromSettings()
    {
        var disabledSettings = new UserSettings
        {
            UseAiSimilarity = false
        };
        var enabledSettings = new UserSettings
        {
            UseAiSimilarity = true
        };

        var disabledOptions = TuningOptionsMapper.ToBranchFindingOptions(disabledSettings);
        var enabledOptions = TuningOptionsMapper.ToBranchFindingOptions(enabledSettings);

        disabledOptions.UseAiSimilarity.Should().BeFalse();
        enabledOptions.UseAiSimilarity.Should().BeTrue();
    }

    [Fact]
    public void ToJukeboxEngineOptions_Should_Map_UserSettings()
    {
        var settings = new UserSettings
        {
            JumpProbability = 0.55,
            JumpCooldown = 4,
            FirstPassLinearPlaybackRatio = 0.65
        };

        var options = TuningOptionsMapper.ToJukeboxEngineOptions(settings);

        options.JumpProbability.Should().Be(0.55);
        options.JumpCooldown.Should().Be(4);
        options.FirstPassLinearPlaybackRatio.Should().Be(0.65);
        options.ForceJumpInEndGuard.Should().BeTrue();
        options.RepeatedJumpAvoidancePasses.Should().Be(2);
        options.AllowRepeatedJumpForTerminalEscape.Should().BeTrue();
    }

    [Fact]
    public void Mapper_Should_Clamp_InvalidValues()
    {
        var settings = new UserSettings
        {
            SimilarityThreshold = 9,
            LookaheadDepth = -1,
            MinJumpDistance = -10,
            MaxBranchesPerBeat = 100,
            JumpProbability = -2,
            JumpCooldown = -4,
            FirstPassLinearPlaybackRatio = 4
        };

        var branch = TuningOptionsMapper.ToBranchFindingOptions(settings);
        var engine = TuningOptionsMapper.ToJukeboxEngineOptions(settings);

        branch.SimilarityThreshold.Should().Be(1.0);
        branch.LookaheadDepth.Should().Be(1);
        branch.MinJumpDistance.Should().Be(1);
        branch.MaxBranchesPerBeat.Should().Be(24);
        engine.JumpProbability.Should().Be(0.0);
        engine.JumpCooldown.Should().Be(0);
        engine.FirstPassLinearPlaybackRatio.Should().Be(0.95);
    }

    [Fact]
    public void ToBranchFindingOptions_Should_SetLoudnessWeight_ToDefault()
    {
        var options = TuningOptionsMapper.ToBranchFindingOptions(new UserSettings());

        options.LoudnessWeight.Should().Be(0.20);
    }

    [Fact]
    public void ToBranchFindingOptions_Should_SetBarPositionWeight_ToDefault()
    {
        var options = TuningOptionsMapper.ToBranchFindingOptions(new UserSettings());

        options.BarPositionWeight.Should().Be(0.18);
    }
}
