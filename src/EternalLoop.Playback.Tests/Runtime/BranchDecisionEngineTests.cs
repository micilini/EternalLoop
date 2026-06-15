using EternalLoop.Playback.Models;
using EternalLoop.Playback.Runtime;
using EternalLoop.Playback.Tests.Fixtures;
using FluentAssertions;

namespace EternalLoop.Playback.Tests.Runtime;

public sealed class BranchDecisionEngineTests
{
    [Fact]
    public void FirstPassRatioBlocksEarlyJump()
    {
        var track = PlaybackFixtures.BuildTrack([PlaybackFixtures.Branch()]);
        var engine = CreateEngine(
            jumpProbability: 0.22,
            randomValue: 0,
            minChance: 1,
            maxChance: 1,
            firstPassRatio: 0.78);

        BranchDecisionResult result = engine.DecideNextBeat(track.Beats[0], track.Beats[0]);

        result.UsedBranch.Should().BeFalse();
        result.BlockedByFirstPass.Should().BeTrue();
        result.Reason.Should().Be("First pass linear playback");
        result.NextBeat.Should().BeSameAs(track.Beats[1]);
    }

    [Fact]
    public void JumpCooldownBlocksBackToBackJumps()
    {
        var track = PlaybackFixtures.BuildTrack(
        [
            PlaybackFixtures.Branch(id: 1, fromBeat: 1, toBeat: 3),
            PlaybackFixtures.Branch(id: 2, fromBeat: 3, toBeat: 4)
        ]);
        var engine = CreateEngine(
            jumpProbability: 0.22,
            randomValue: 0,
            minChance: 1,
            maxChance: 1,
            cooldown: 12);

        BranchDecisionResult first = engine.DecideNextBeat(track.Beats[0], track.Beats[0]);
        BranchDecisionResult second = engine.DecideNextBeat(track.Beats[2], track.Beats[0]);

        first.UsedBranch.Should().BeTrue();
        second.UsedBranch.Should().BeFalse();
        second.BlockedByCooldown.Should().BeTrue();
        second.Reason.Should().Be("Jump cooldown active");
        second.NextBeat.Should().BeSameAs(track.Beats[3]);
    }

    [Fact]
    public void JumpProbabilityScalesEffectiveChance()
    {
        var track = PlaybackFixtures.BuildTrack([PlaybackFixtures.Branch()]);
        var engine = CreateEngine(
            jumpProbability: 0.25,
            randomProvider: new SequenceBranchRandomProvider(0.2, 0.12),
            minChance: 0.5,
            maxChance: 0.5,
            firstPassRatio: 0);

        BranchDecisionResult rejected = engine.DecideNextBeat(track.Beats[0], track.Beats[0]);
        BranchDecisionResult accepted = engine.DecideNextBeat(track.Beats[0], track.Beats[0]);

        rejected.UsedBranch.Should().BeFalse();
        rejected.Reason.Should().Be("Random rejected branch");
        rejected.ChanceBeforeDecision.Should().Be(0.5);
        accepted.UsedBranch.Should().BeTrue();
        accepted.Reason.Should().Be("Branch selected");
    }

    [Fact]
    public void BranchChanceShouldIncreaseBeyondBalancedJumpProbability()
    {
        var track = PlaybackFixtures.BuildTrack([PlaybackFixtures.Branch()]);
        var engine = CreateEngine(
            jumpProbability: 0.22,
            randomValue: 1,
            minChance: 0.18,
            maxChance: 0.50,
            delta: 0.018);

        for (int index = 0; index < 40; index++)
        {
            engine.DecideNextBeat(track.Beats[0], track.Beats[0]);
        }

        engine.BranchChance.Should().Be(0.50);
    }

    [Fact]
    public void JumpProbabilityZeroDisablesJump()
    {
        var track = PlaybackFixtures.BuildTrack([PlaybackFixtures.Branch()]);
        var engine = CreateEngine(
            jumpProbability: 0,
            randomValue: 0,
            minChance: 1,
            maxChance: 1);

        BranchDecisionResult result = engine.DecideNextBeat(track.Beats[0], track.Beats[0]);

        result.UsedBranch.Should().BeFalse();
        result.Reason.Should().Be("Random rejected branch");
    }

    [Fact]
    public void LegacyModeReproducesFlatDeltaRampAndPrivateRotation()
    {
        RuntimeTrack track = PlaybackFixtures.BuildTrack(
        [
            PlaybackFixtures.Branch(id: 1, fromBeat: 1, toBeat: 3),
            PlaybackFixtures.Branch(id: 2, fromBeat: 1, toBeat: 4)
        ]);
        var engine = CreateEngine(
            jumpProbability: 0,
            randomProvider: new SequenceBranchRandomProvider(1, 0, 0),
            minChance: 0.18,
            maxChance: 1,
            delta: 0.018,
            cooldown: 12,
            firstPassRatio: 0.78,
            enableJumpShapingKnobs: false,
            normalizeChanceDeltaByTempo: false,
            weightedBranchSelection: false);

        BranchDecisionResult rejected = engine.DecideNextBeat(track.Beats[0], track.Beats[0]);
        BranchDecisionResult first = engine.DecideNextBeat(track.Beats[0], track.Beats[0]);
        BranchDecisionResult second = engine.DecideNextBeat(track.Beats[0], track.Beats[0]);

        rejected.UsedBranch.Should().BeFalse();
        rejected.ChanceAfterDecision.Should().BeApproximately(0.198, 0.000001);
        first.NextBeat.Should().BeSameAs(track.Beats[3]);
        second.NextBeat.Should().BeSameAs(track.Beats[4]);
    }

    [Fact]
    public void LegacyModeConsumesSameRngCountWithSingleCandidate()
    {
        var track = PlaybackFixtures.BuildTrack([PlaybackFixtures.Branch()]);
        var random = new CountingBranchRandomProvider(1);
        var engine = CreateEngine(
            jumpProbability: 0,
            randomProvider: random,
            minChance: 0.18,
            maxChance: 1,
            cooldown: 12,
            firstPassRatio: 0.78,
            enableJumpShapingKnobs: false,
            normalizeChanceDeltaByTempo: false,
            weightedBranchSelection: false);

        engine.DecideNextBeat(track.Beats[0], track.Beats[0]);

        random.Count.Should().Be(1);
    }

    [Fact]
    public void JumpProbabilityOneShouldAllowSafeRandomJump()
    {
        var track = PlaybackFixtures.BuildTrack([PlaybackFixtures.Branch()]);
        var engine = CreateEngine(jumpProbability: 1, randomValue: 0.5, minChance: 1, maxChance: 1);

        BranchDecisionResult result = engine.DecideNextBeat(track.Beats[0], track.Beats[0]);

        result.UsedBranch.Should().BeTrue();
        result.NextBeat.Should().BeSameAs(track.Beats[3]);
    }

    [Fact]
    public void EscapeGuardShouldStillBlockUnsafeTerminalBranch()
    {
        RuntimeTrack track = BuildTrack(
            10,
            [
                PlaybackFixtures.Branch(id: 1, fromBeat: 1, toBeat: 9)
            ]);
        var engine = CreateEngine(
            jumpProbability: 1,
            randomValue: 0,
            minChance: 1,
            maxChance: 1,
            escapeOptions: EndGuardOptions(forceJump: true));

        BranchDecisionResult result = engine.DecideNextBeat(track.Beats[0], track.Beats[0]);

        result.UsedBranch.Should().BeFalse();
        result.WasBlockedByEscapeGuard.Should().BeTrue();
        result.EscapeGuardReason.Should().Be("Destination has no terminal escape");
        result.Reason.Should().Be("Escape guard blocked branch");
    }

    [Fact]
    public void BranchDecisionEngineShouldStillSelectSameSafeBranchWithLinearIndex()
    {
        RuntimeTrack track = PlaybackFixtures.BuildTrack([PlaybackFixtures.Branch()]);
        RuntimeLinearBeatIndex index = RuntimeLinearBeatIndex.FromTrack(track);
        var engine = CreateEngine(
            jumpProbability: 1,
            randomValue: 0,
            minChance: 1,
            maxChance: 1);

        BranchDecisionResult result = engine.DecideNextBeat(track.Beats[0], index);

        result.UsedBranch.Should().BeTrue();
        result.NextBeat.Should().BeSameAs(track.Beats[3]);
    }

    [Fact]
    public void BranchDecisionEngineShouldStillBlockUnsafeTerminalBranchWithLinearIndex()
    {
        RuntimeTrack track = BuildTrack(
            10,
            [
                PlaybackFixtures.Branch(id: 1, fromBeat: 1, toBeat: 9)
            ]);
        RuntimeLinearBeatIndex index = RuntimeLinearBeatIndex.FromTrack(track);
        var engine = CreateEngine(
            jumpProbability: 1,
            randomValue: 0,
            minChance: 1,
            maxChance: 1,
            escapeOptions: EndGuardOptions(forceJump: true));

        BranchDecisionResult result = engine.DecideNextBeat(track.Beats[0], index);

        result.UsedBranch.Should().BeFalse();
        result.WasBlockedByEscapeGuard.Should().BeTrue();
        result.EscapeGuardReason.Should().Be("Destination has no terminal escape");
    }

    [Fact]
    public void BranchDecisionEngineShouldHandleLargeDenseTrackWithPrecomputedLinearIndex()
    {
        RuntimeBranchInput[] branches = Enumerable.Range(1, 200)
            .Select(index => PlaybackFixtures.Branch(
                id: index,
                fromBeat: 1,
                toBeat: 10 + index,
                distance: index))
            .ToArray();
        RuntimeTrack track = BuildTrack(1200, branches);
        RuntimeLinearBeatIndex linearBeatIndex = RuntimeLinearBeatIndex.FromTrack(track);
        var engine = CreateEngine(
            jumpProbability: 1,
            randomValue: 0,
            minChance: 1,
            maxChance: 1,
            escapeOptions: new BranchEscapeOptions { Enabled = false });

        for (int index = 0; index < 100; index++)
        {
            BranchDecisionResult result = engine.DecideNextBeat(track.Beats[0], linearBeatIndex);

            result.UsedBranch.Should().BeTrue();
            linearBeatIndex.Contains(result.NextBeat).Should().BeTrue();
        }
    }

    [Fact]
    public void ForceJumpInEndGuardShouldStillForceSafeBranch()
    {
        RuntimeTrack track = BuildTrack(
            10,
            [
                PlaybackFixtures.Branch(id: 1, fromBeat: 8, toBeat: 1)
            ]);
        var engine = CreateEngine(
            jumpProbability: 0,
            randomValue: 1,
            minChance: 0,
            maxChance: 0,
            cooldown: 12,
            firstPassRatio: 0.9,
            escapeOptions: EndGuardOptions(forceJump: true));

        BranchDecisionResult result = engine.DecideNextBeat(track.Beats[7], track.Beats[0]);

        result.UsedBranch.Should().BeTrue();
        result.ForcedEndGuardJump.Should().BeTrue();
        result.Reason.Should().Be("Forced safe branch in end guard");
        result.NextBeat.Should().BeSameAs(track.Beats[1]);
    }

    [Fact]
    public void ForceJumpInEndGuardShouldNotForceWhenDisabled()
    {
        RuntimeTrack track = BuildTrack(
            10,
            [
                PlaybackFixtures.Branch(id: 1, fromBeat: 8, toBeat: 1)
            ]);
        var engine = CreateEngine(
            jumpProbability: 1,
            randomValue: 1,
            minChance: 0.18,
            maxChance: 0.50,
            escapeOptions: EndGuardOptions(forceJump: false));

        BranchDecisionResult result = engine.DecideNextBeat(track.Beats[7], track.Beats[0]);

        result.UsedBranch.Should().BeFalse();
        result.ForcedEndGuardJump.Should().BeFalse();
        result.Reason.Should().Be("Random rejected branch");
    }

    [Fact]
    public void DecideNextBeatShouldNotMutateSeedBeatNeighborsWhenRotating()
    {
        RuntimeTrack track = PlaybackFixtures.BuildTrack(
        [
            PlaybackFixtures.Branch(id: 1, fromBeat: 1, toBeat: 3),
            PlaybackFixtures.Branch(id: 2, fromBeat: 1, toBeat: 4)
        ]);
        RuntimeBeat seedBeat = track.Beats[1];
        RuntimeBranchEdge[] originalNeighbors = seedBeat.Neighbors.ToArray();
        var engine = CreateEngine(
            jumpProbability: 1,
            randomValue: 0,
            minChance: 1,
            maxChance: 1,
            weightedBranchSelection: false);

        BranchDecisionResult result = engine.DecideNextBeat(track.Beats[0], track.Beats[0]);

        result.UsedBranch.Should().BeTrue();
        seedBeat.Neighbors.Should().Equal(originalNeighbors, (neighbor, original) => ReferenceEquals(neighbor, original));
    }

    [Fact]
    public void DecideNextBeatShouldUsePrivateRotationAcrossAcceptedBranches()
    {
        RuntimeTrack track = PlaybackFixtures.BuildTrack(
        [
            PlaybackFixtures.Branch(id: 1, fromBeat: 1, toBeat: 3),
            PlaybackFixtures.Branch(id: 2, fromBeat: 1, toBeat: 4)
        ]);
        RuntimeBeat seedBeat = track.Beats[1];
        RuntimeBranchEdge[] originalNeighbors = seedBeat.Neighbors.ToArray();
        var engine = CreateEngine(
            jumpProbability: 1,
            randomValue: 0,
            minChance: 1,
            maxChance: 1,
            weightedBranchSelection: false);

        BranchDecisionResult first = engine.DecideNextBeat(track.Beats[0], track.Beats[0]);
        BranchDecisionResult second = engine.DecideNextBeat(track.Beats[0], track.Beats[0]);

        first.NextBeat.Should().BeSameAs(track.Beats[3]);
        second.NextBeat.Should().BeSameAs(track.Beats[4]);
        seedBeat.Neighbors.Should().Equal(originalNeighbors, (neighbor, original) => ReferenceEquals(neighbor, original));
    }

    [Fact]
    public void DecideNextBeatShouldPreserveNeighborOrderWhenRotateBranchesIsFalse()
    {
        RuntimeTrack track = PlaybackFixtures.BuildTrack(
        [
            PlaybackFixtures.Branch(id: 1, fromBeat: 1, toBeat: 3),
            PlaybackFixtures.Branch(id: 2, fromBeat: 1, toBeat: 4)
        ]);
        RuntimeBeat seedBeat = track.Beats[1];
        RuntimeBranchEdge[] originalNeighbors = seedBeat.Neighbors.ToArray();
        var engine = CreateEngine(
            jumpProbability: 1,
            randomValue: 0,
            minChance: 1,
            maxChance: 1,
            rotateBranches: false,
            weightedBranchSelection: false);

        BranchDecisionResult first = engine.DecideNextBeat(track.Beats[0], track.Beats[0]);
        BranchDecisionResult second = engine.DecideNextBeat(track.Beats[0], track.Beats[0]);

        first.NextBeat.Should().BeSameAs(track.Beats[3]);
        second.NextBeat.Should().BeSameAs(track.Beats[3]);
        seedBeat.Neighbors.Should().Equal(originalNeighbors, (neighbor, original) => ReferenceEquals(neighbor, original));
    }

    [Fact]
    public void ForcedEndGuardJumpShouldNotMutateSeedBeatNeighbors()
    {
        RuntimeTrack track = BuildTrack(
            10,
            [
                PlaybackFixtures.Branch(id: 1, fromBeat: 8, toBeat: 1),
                PlaybackFixtures.Branch(id: 2, fromBeat: 8, toBeat: 2)
            ]);
        RuntimeBeat seedBeat = track.Beats[8];
        RuntimeBranchEdge[] originalNeighbors = seedBeat.Neighbors.ToArray();
        var engine = CreateEngine(
            jumpProbability: 0,
            randomValue: 1,
            minChance: 0,
            maxChance: 0,
            escapeOptions: EndGuardOptions(forceJump: true));

        BranchDecisionResult result = engine.DecideNextBeat(track.Beats[7], track.Beats[0]);

        result.UsedBranch.Should().BeTrue();
        result.ForcedEndGuardJump.Should().BeTrue();
        seedBeat.Neighbors.Should().Equal(originalNeighbors, (neighbor, original) => ReferenceEquals(neighbor, original));
    }

    [Fact]
    public void RejectedRandomBranchShouldNotAdvancePrivateRotation()
    {
        RuntimeTrack track = PlaybackFixtures.BuildTrack(
        [
            PlaybackFixtures.Branch(id: 1, fromBeat: 1, toBeat: 3),
            PlaybackFixtures.Branch(id: 2, fromBeat: 1, toBeat: 4)
        ]);
        var engine = CreateEngine(
            jumpProbability: 1,
            randomProvider: new SequenceBranchRandomProvider(1, 0),
            minChance: 0.5,
            maxChance: 1,
            weightedBranchSelection: false);

        BranchDecisionResult rejected = engine.DecideNextBeat(track.Beats[0], track.Beats[0]);
        BranchDecisionResult accepted = engine.DecideNextBeat(track.Beats[0], track.Beats[0]);

        rejected.UsedBranch.Should().BeFalse();
        accepted.UsedBranch.Should().BeTrue();
        accepted.NextBeat.Should().BeSameAs(track.Beats[3]);
    }

    [Fact]
    public void NormalizedChanceDeltaScalesByBeatDuration()
    {
        RuntimeTrack track = BuildTrackWithDurations([1.0, 1.0, 1.0, 1.0, 1.0], [PlaybackFixtures.Branch()]);
        var engine = CreateEngine(
            jumpProbability: 1,
            randomValue: 1,
            minChance: 0.10,
            maxChance: 1,
            delta: 0.05,
            normalizeChanceDeltaByTempo: true);

        BranchDecisionResult result = engine.DecideNextBeat(track.Beats[0], track.Beats[0]);

        result.ChanceAfterDecision.Should().BeApproximately(0.20, 0.000001);
    }

    [Fact]
    public void FlatChanceDeltaIgnoresBeatDurationWhenNormalizationDisabled()
    {
        RuntimeTrack track = BuildTrackWithDurations([1.0, 1.0, 1.0, 1.0, 1.0], [PlaybackFixtures.Branch()]);
        var engine = CreateEngine(
            jumpProbability: 1,
            randomValue: 1,
            minChance: 0.10,
            maxChance: 1,
            delta: 0.05,
            normalizeChanceDeltaByTempo: false);

        BranchDecisionResult result = engine.DecideNextBeat(track.Beats[0], track.Beats[0]);

        result.ChanceAfterDecision.Should().BeApproximately(0.15, 0.000001);
    }

    [Fact]
    public void WeightedBranchSelectionFavorsSmallerDistance()
    {
        RuntimeTrack track = PlaybackFixtures.BuildTrack(
        [
            PlaybackFixtures.Branch(id: 1, fromBeat: 1, toBeat: 3, distance: 1),
            PlaybackFixtures.Branch(id: 2, fromBeat: 1, toBeat: 4, distance: 100)
        ]);
        var engine = CreateEngine(
            jumpProbability: 1,
            randomProvider: new SequenceBranchRandomProvider(0, 0),
            minChance: 1,
            maxChance: 1,
            weightedBranchSelection: true);

        BranchDecisionResult result = engine.DecideNextBeat(track.Beats[0], track.Beats[0]);

        result.UsedBranch.Should().BeTrue();
        result.NextBeat.Should().BeSameAs(track.Beats[3]);
        result.CandidateCountConsidered.Should().Be(2);
    }

    [Fact]
    public void RepeatPenaltyAvoidsRepeatingLastDestinationFromSameSeed()
    {
        RuntimeTrack track = PlaybackFixtures.BuildTrack(
        [
            PlaybackFixtures.Branch(id: 1, fromBeat: 1, toBeat: 3, distance: 1),
            PlaybackFixtures.Branch(id: 2, fromBeat: 1, toBeat: 4, distance: 4)
        ]);
        var engine = CreateEngine(
            jumpProbability: 1,
            randomProvider: new SequenceBranchRandomProvider(0, 0, 0.5, 0),
            minChance: 1,
            maxChance: 1,
            weightedBranchSelection: true,
            repeatPenalty: 0.35);

        BranchDecisionResult first = engine.DecideNextBeat(track.Beats[0], track.Beats[0]);
        BranchDecisionResult second = engine.DecideNextBeat(track.Beats[0], track.Beats[0]);

        first.NextBeat.Should().BeSameAs(track.Beats[3]);
        second.NextBeat.Should().BeSameAs(track.Beats[4]);
    }

    [Fact]
    public void WeightedBranchSelectionWithSingleCandidateDoesNotConsumeSelectionRng()
    {
        var track = PlaybackFixtures.BuildTrack([PlaybackFixtures.Branch()]);
        var random = new CountingBranchRandomProvider(0);
        var engine = CreateEngine(
            jumpProbability: 1,
            randomProvider: random,
            minChance: 1,
            maxChance: 1,
            weightedBranchSelection: true);

        engine.DecideNextBeat(track.Beats[0], track.Beats[0]);

        random.Count.Should().Be(1);
    }

    [Fact]
    public void EnumeratingSeedBeatNeighborsWhileDecisionsRotateShouldNotThrow()
    {
        RuntimeTrack track = PlaybackFixtures.BuildTrack(
        [
            PlaybackFixtures.Branch(id: 1, fromBeat: 1, toBeat: 3),
            PlaybackFixtures.Branch(id: 2, fromBeat: 1, toBeat: 4)
        ]);
        RuntimeBeat seedBeat = track.Beats[1];
        var engine = CreateEngine(jumpProbability: 1, randomValue: 0, minChance: 1, maxChance: 1);
        using List<RuntimeBranchEdge>.Enumerator enumerator = seedBeat.Neighbors.GetEnumerator();
        enumerator.MoveNext().Should().BeTrue();

        engine.DecideNextBeat(track.Beats[0], track.Beats[0]);
        Action continueEnumeration = () =>
        {
            while (enumerator.MoveNext())
            {
            }
        };

        continueEnumeration.Should().NotThrow();
    }

    [Fact]
    public void InvalidOptionsShouldNormalizeToSafeDefaults()
    {
        var track = PlaybackFixtures.BuildTrack([PlaybackFixtures.Branch()]);
        var engine = new BranchDecisionEngine(
            new BranchDecisionOptions
            {
                JumpProbability = double.NaN,
                JumpCooldownBeats = -1,
                FirstPassLinearPlaybackRatio = double.PositiveInfinity
            },
            new FixedBranchRandomProvider(1));

        engine.BranchChance.Should().BeGreaterThan(0);
        engine.DecideNextBeat(track.Beats[0], track.Beats[0]).Should().NotBeNull();
    }

    [Fact]
    public void BranchChanceShouldNotBecomeNaNOrInfinity()
    {
        var track = PlaybackFixtures.BuildTrack([PlaybackFixtures.Branch()]);
        var engine = new BranchDecisionEngine(
            new BranchDecisionOptions
            {
                JumpProbability = 2,
                MinRandomBranchChance = -10,
                MaxRandomBranchChance = double.PositiveInfinity,
                RandomBranchChanceDelta = double.NaN,
                FirstPassLinearPlaybackRatio = -1,
                JumpCooldownBeats = 0,
                EscapeOptions = new BranchEscapeOptions { Enabled = false }
            },
            new FixedBranchRandomProvider(1));

        for (int index = 0; index < 8; index++)
        {
            engine.DecideNextBeat(track.Beats[0], track.Beats[0]);
            engine.BranchChance.Should().BeGreaterThanOrEqualTo(0);
            engine.BranchChance.Should().BeLessThanOrEqualTo(1);
            double.IsFinite(engine.BranchChance).Should().BeTrue();
        }
    }

    [Fact]
    public void NoBranchesShouldReturnLinearDecision()
    {
        var track = PlaybackFixtures.BuildTrack();
        var engine = CreateEngine(jumpProbability: 1, randomValue: 0, minChance: 1, maxChance: 1);

        BranchDecisionResult result = engine.DecideNextBeat(track.Beats[0], track.Beats[0]);

        result.UsedBranch.Should().BeFalse();
        result.NextBeat.Should().BeSameAs(track.Beats[1]);
        result.Reason.Should().Be("No valid branch");
    }

    [Fact]
    public void InvalidBranchShouldBeIgnored()
    {
        var track = PlaybackFixtures.BuildTrack();
        RuntimeBeat source = track.Beats[1];
        source.Neighbors.Add(new RuntimeBranchEdge
        {
            Id = 99,
            Status = "active",
            FromBeat = 1,
            ToBeat = 99,
            JumpBeats = 98,
            Direction = "forward",
            Distance = double.NaN,
            Deleted = false,
            SourceBeat = source,
            DestinationBeat = new RuntimeBeat { Which = 99, Start = 99, Duration = 1, Confidence = 1 }
        });
        var engine = CreateEngine(jumpProbability: 1, randomValue: 0, minChance: 1, maxChance: 1);

        BranchDecisionResult result = engine.DecideNextBeat(track.Beats[0], track.Beats[0]);

        result.UsedBranch.Should().BeFalse();
        result.NextBeat.Should().BeSameAs(track.Beats[1]);
        result.Reason.Should().Be("No valid branch");
    }

    private static BranchDecisionEngine CreateEngine(
        double jumpProbability,
        double randomValue,
        double minChance,
        double maxChance,
        double delta = 0.018,
        int cooldown = 0,
        double firstPassRatio = 0,
        BranchEscapeOptions? escapeOptions = null,
        bool rotateBranches = true,
        bool enableJumpShapingKnobs = true,
        bool normalizeChanceDeltaByTempo = true,
        bool weightedBranchSelection = true,
        double repeatPenalty = 0.35)
    {
        return CreateEngine(
            jumpProbability,
            new FixedBranchRandomProvider(randomValue),
            minChance,
            maxChance,
            delta,
            cooldown,
            firstPassRatio,
            escapeOptions,
            rotateBranches,
            enableJumpShapingKnobs,
            normalizeChanceDeltaByTempo,
            weightedBranchSelection,
            repeatPenalty);
    }

    private static BranchDecisionEngine CreateEngine(
        double jumpProbability,
        IBranchRandomProvider randomProvider,
        double minChance,
        double maxChance,
        double delta = 0.018,
        int cooldown = 0,
        double firstPassRatio = 0,
        BranchEscapeOptions? escapeOptions = null,
        bool rotateBranches = true,
        bool enableJumpShapingKnobs = true,
        bool normalizeChanceDeltaByTempo = true,
        bool weightedBranchSelection = true,
        double repeatPenalty = 0.35)
    {
        return new BranchDecisionEngine(
            new BranchDecisionOptions
            {
                JumpProbability = jumpProbability,
                MinRandomBranchChance = minChance,
                MaxRandomBranchChance = maxChance,
                RandomBranchChanceDelta = delta,
                JumpCooldownBeats = cooldown,
                FirstPassLinearPlaybackRatio = firstPassRatio,
                RotateBranches = rotateBranches,
                EnableJumpShapingKnobs = enableJumpShapingKnobs,
                NormalizeChanceDeltaByTempo = normalizeChanceDeltaByTempo,
                WeightedBranchSelection = weightedBranchSelection,
                RepeatPenalty = repeatPenalty,
                EscapeOptions = escapeOptions ?? new BranchEscapeOptions { Enabled = false }
            },
            randomProvider);
    }

    private static RuntimeTrack BuildTrack(int beatCount, IReadOnlyList<RuntimeBranchInput> activeBranches)
    {
        RuntimeBeatInput[] beats = Enumerable.Range(0, beatCount)
            .Select(index => new RuntimeBeatInput(index, index, 1, 1))
            .ToArray();

        return new TrackRuntimeBuilder().Build(new TrackRuntimeBuildRequest
        {
            Id = "fixture",
            Title = "Fixture",
            Artist = "Local",
            AudioPath = "fixture.wav",
            DurationSeconds = beatCount,
            Beats = beats,
            ActiveBranches = activeBranches,
            CandidateBranches = []
        }).Track;
    }

    private static RuntimeTrack BuildTrackWithDurations(
        IReadOnlyList<double> durations,
        IReadOnlyList<RuntimeBranchInput> activeBranches)
    {
        double start = 0;
        List<RuntimeBeatInput> beats = [];

        for (int index = 0; index < durations.Count; index++)
        {
            double duration = durations[index];
            beats.Add(new RuntimeBeatInput(index, start, duration, 1));
            start += duration;
        }

        return new TrackRuntimeBuilder().Build(new TrackRuntimeBuildRequest
        {
            Id = "fixture",
            Title = "Fixture",
            Artist = "Local",
            AudioPath = "fixture.wav",
            DurationSeconds = start,
            Beats = beats,
            ActiveBranches = activeBranches,
            CandidateBranches = []
        }).Track;
    }

    private static BranchEscapeOptions EndGuardOptions(bool forceJump)
    {
        return new BranchEscapeOptions
        {
            Enabled = true,
            EndGuardStartRatio = 0.80,
            MinimumBeatsBeforeEndForJumpDestination = 1,
            TerminalEscapeLookaheadBeats = 3,
            ForceJumpInEndGuard = forceJump,
            MaxEscapeSearchDepth = 3
        };
    }

    private sealed class SequenceBranchRandomProvider(params double[] values) : IBranchRandomProvider
    {
        private int _index;

        public double NextDouble()
        {
            double value = values[Math.Min(_index, values.Length - 1)];
            _index++;
            return value;
        }
    }

    private sealed class CountingBranchRandomProvider(double value) : IBranchRandomProvider
    {
        public int Count { get; private set; }

        public double NextDouble()
        {
            Count++;
            return value;
        }
    }
}
