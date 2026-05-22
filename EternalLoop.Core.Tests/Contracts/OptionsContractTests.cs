using EternalLoop.Contracts.Enums;
using EternalLoop.Contracts.Options;
using FluentAssertions;

namespace EternalLoop.Core.Tests.Contracts;

public sealed class OptionsContractTests
{
    [Fact]
    public void FeatureExtractionOptions_Should_ExposeExpectedDefaults()
    {
        var options = new FeatureExtractionOptions();

        options.FrameSize.Should().Be(2048);
        options.HopLength.Should().Be(512);
        options.MfccCount.Should().Be(13);
        options.ComputeDeltas.Should().BeTrue();
        options.FilterBankSize.Should().Be(26);
        options.PreEmphasis.Should().Be(0.97);
    }

    [Fact]
    public void BeatTrackingOptions_Should_ExposeExpectedDefaults()
    {
        var options = new BeatTrackingOptions();

        options.MinBpm.Should().Be(60);
        options.MaxBpm.Should().Be(200);
        options.TightnessLambda.Should().Be(100);
        options.OdfSmoothWindow.Should().Be(7);
    }

    [Fact]
    public void BranchFindingOptions_Should_ExposeExpectedDefaults()
    {
        var options = new BranchFindingOptions();

        options.SimilarityThreshold.Should().Be(0.86);
        options.LookaheadDepth.Should().Be(4);
        options.MinJumpDistance.Should().Be(20);
        options.TimbreWeight.Should().Be(0.45);
        options.PitchWeight.Should().Be(0.35);
        options.LoudnessWeight.Should().Be(0.20);
        options.BarPositionWeight.Should().Be(0.18);
        options.MaxBranchesPerBeat.Should().Be(3);
        options.LandingOffsetBeats.Should().Be(1);
        options.ContinuationLookaheadDepth.Should().Be(6);
        options.ContinuationThresholdMargin.Should().Be(0.02);
        options.UseAiSimilarity.Should().BeTrue();
        options.AiRejectionThreshold.Should().Be(0.58);
        options.AiPenaltyStartThreshold.Should().Be(0.72);
        options.AiPenaltyStrength.Should().Be(0.22);
    }

    [Fact]
    public void UserSettings_Should_ExposeExpectedTuningDefaults()
    {
        var settings = new EternalLoop.Contracts.Models.UserSettings();

        settings.Preset.Should().Be("Balanced");
        settings.SettingsSchemaVersion.Should().Be(3);
        settings.MinJumpDistance.Should().Be(20);
        settings.MaxBranchesPerBeat.Should().Be(3);
        settings.FirstPassLinearPlaybackRatio.Should().Be(0.78);
        settings.UseAiSimilarity.Should().BeTrue();
    }

    [Fact]
    public void JukeboxEngineOptions_Should_ExposeExpectedDefaults()
    {
        var options = new JukeboxEngineOptions();

        options.JumpProbability.Should().Be(0.3);
        options.MinBeatsBeforeFirstJump.Should().Be(16);
        options.JumpCooldown.Should().Be(8);
        options.SteeringLookaheadDepth.Should().Be(5);
        options.Strategy.Should().Be(JumpStrategy.LeastPlayed);
        options.FirstPassLinearPlaybackRatio.Should().Be(0.75);
        options.EndGuardStartRatio.Should().Be(0.88);
        options.MinimumBeatsBeforeEndForJumpDestination.Should().Be(24);
        options.TerminalEscapeLookaheadBeats.Should().Be(32);
        options.ForceJumpInEndGuard.Should().BeTrue();
        options.RepeatedJumpAvoidancePasses.Should().Be(2);
        options.AllowRepeatedJumpForTerminalEscape.Should().BeTrue();
    }

    [Fact]
    public void PlaybackOptions_Should_ExposeExpectedDefaults()
    {
        var options = new PlaybackOptions();

        options.CrossfadeMilliseconds.Should().Be(20);
        options.Shape.Should().Be(CrossfadeShape.EqualPower);
    }
}
