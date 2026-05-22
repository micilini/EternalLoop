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
            MaxBranchesPerBeat = 8
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
