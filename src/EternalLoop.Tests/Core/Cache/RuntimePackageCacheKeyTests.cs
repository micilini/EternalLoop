using EternalLoop.Core.Cache;
using EternalLoop.Core.Settings;
using FluentAssertions;

namespace EternalLoop.Tests.Core.Cache;

public sealed class RuntimePackageCacheKeyTests
{
    [Theory]
    [InlineData("JumpProbability")]
    [InlineData("JumpCooldown")]
    [InlineData("FirstPassLinearPlaybackRatio")]
    public void RuntimeOnlyChangesShouldOnlyChangeRuntimeKey(string propertyName)
    {
        TrackFileIdentity identity = CreateIdentity();
        LoopTuningSettings previous = LoopTuningSettings.Balanced();
        LoopTuningSettings current = LoopTuningSettings.Balanced();

        switch (propertyName)
        {
            case "JumpProbability":
                current.JumpProbability = 0.88;
                break;
            case "JumpCooldown":
                current.JumpCooldown = 2;
                break;
            case "FirstPassLinearPlaybackRatio":
                current.FirstPassLinearPlaybackRatio = 0.25;
                break;
        }

        RuntimePackageCacheKey.CreateAnalysisKey(identity, current, 4)
            .Should().Be(RuntimePackageCacheKey.CreateAnalysisKey(identity, previous, 4));
        RuntimePackageCacheKey.CreateBranchKey(identity, current, 4)
            .Should().Be(RuntimePackageCacheKey.CreateBranchKey(identity, previous, 4));
        RuntimePackageCacheKey.CreateRuntimeKey(identity, current, 4)
            .Should().NotBe(RuntimePackageCacheKey.CreateRuntimeKey(identity, previous, 4));
    }

    [Theory]
    [InlineData("SimilarityThreshold")]
    [InlineData("LookaheadDepth")]
    [InlineData("MinJumpDistance")]
    [InlineData("MaxBranchesPerBeat")]
    [InlineData("BranchQuantumType")]
    [InlineData("BranchMaxThreshold")]
    public void BranchChangesShouldChangeBranchAndRuntimeKeys(string propertyName)
    {
        TrackFileIdentity identity = CreateIdentity();
        LoopTuningSettings previous = LoopTuningSettings.Balanced();
        LoopTuningSettings current = LoopTuningSettings.Balanced();

        switch (propertyName)
        {
            case "SimilarityThreshold":
                current.SimilarityThreshold = 0.8;
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

        RuntimePackageCacheKey.CreateAnalysisKey(identity, current, 4)
            .Should().Be(RuntimePackageCacheKey.CreateAnalysisKey(identity, previous, 4));
        RuntimePackageCacheKey.CreateBranchKey(identity, current, 4)
            .Should().NotBe(RuntimePackageCacheKey.CreateBranchKey(identity, previous, 4));
        RuntimePackageCacheKey.CreateRuntimeKey(identity, current, 4)
            .Should().NotBe(RuntimePackageCacheKey.CreateRuntimeKey(identity, previous, 4));
    }

    [Fact]
    public void AnalysisChangesShouldChangeAllKeys()
    {
        TrackFileIdentity identity = CreateIdentity();
        LoopTuningSettings previous = LoopTuningSettings.Balanced();
        LoopTuningSettings current = LoopTuningSettings.Balanced();
        current.AnalysisMusicalQuality = false;

        RuntimePackageCacheKey.CreateAnalysisKey(identity, current, 4)
            .Should().NotBe(RuntimePackageCacheKey.CreateAnalysisKey(identity, previous, 4));
        RuntimePackageCacheKey.CreateBranchKey(identity, current, 4)
            .Should().NotBe(RuntimePackageCacheKey.CreateBranchKey(identity, previous, 4));
        RuntimePackageCacheKey.CreateRuntimeKey(identity, current, 4)
            .Should().NotBe(RuntimePackageCacheKey.CreateRuntimeKey(identity, previous, 4));
    }

    private static TrackFileIdentity CreateIdentity()
    {
        return new TrackFileIdentity(
            @"C:\Music\track.mp3",
            "track.mp3",
            @"C:\Music",
            123,
            new DateTime(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc),
            "sha256");
    }
}
