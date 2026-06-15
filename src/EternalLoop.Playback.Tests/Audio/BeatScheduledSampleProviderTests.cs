using EternalLoop.Playback.Audio;
using EternalLoop.Playback.Models;
using EternalLoop.Playback.Runtime;
using EternalLoop.Playback.Tests.Fixtures;
using EternalLoop.Playback.Tests.Runtime;
using FluentAssertions;

namespace EternalLoop.Playback.Tests.Audio;

public sealed class BeatScheduledSampleProviderTests
{
    [Fact]
    public void ReadShouldReturnRequestedCountAndEmitInitialBeatChanged()
    {
        var provider = new BeatScheduledSampleProvider(
            PlaybackFixtures.LoadedAudio(),
            PlaybackFixtures.BuildTrack(),
            transitionOptions: new BranchTransitionOptions { Enabled = false });
        List<int> beats = [];
        provider.BeatChanged += (_, args) => beats.Add(args.BeatIndex);

        float[] buffer = new float[4];
        int read = provider.Read(buffer, 0, buffer.Length);

        read.Should().Be(4);
        beats.Should().Contain(0);
        buffer.Should().OnlyContain(sample => sample > 0);
    }

    [Fact]
    public void ReadShouldNotWriteOutsideBuffer()
    {
        var provider = new BeatScheduledSampleProvider(
            PlaybackFixtures.LoadedAudio(),
            PlaybackFixtures.BuildTrack(),
            transitionOptions: new BranchTransitionOptions { Enabled = false });
        float[] buffer = Enumerable.Repeat(-1f, 12).ToArray();

        int read = provider.Read(buffer, 4, 4);

        read.Should().Be(4);
        buffer[..4].Should().OnlyContain(sample => sample == -1);
        buffer[8..].Should().OnlyContain(sample => sample == -1);
        buffer[4..8].Should().OnlyContain(sample => sample > 0);
    }

    [Fact]
    public void ReadWithoutBeatTransitionShouldNotAllocateEventLists()
    {
        var provider = new BeatScheduledSampleProvider(
            PlaybackFixtures.LoadedAudio(sampleRate: 1000),
            PlaybackFixtures.BuildTrack(),
            transitionOptions: new BranchTransitionOptions { Enabled = false });
        int beatChangedCount = 0;
        int branchJumpCount = 0;
        provider.BeatChanged += (_, _) => beatChangedCount++;
        provider.BranchJumped += (_, _) => branchJumpCount++;

        provider.Read(new float[10], 0, 10);
        beatChangedCount = 0;
        branchJumpCount = 0;
        provider.Read(new float[10], 0, 10);

        float[] buffer = new float[10];
        long before = GC.GetAllocatedBytesForCurrentThread();
        provider.Read(buffer, 0, buffer.Length);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        allocated.Should().Be(0);
        beatChangedCount.Should().Be(0);
        branchJumpCount.Should().Be(0);
    }

    [Fact]
    public void ReadWithoutBeatTransitionShouldNotRaiseEvents()
    {
        var provider = new BeatScheduledSampleProvider(
            PlaybackFixtures.LoadedAudio(sampleRate: 1000),
            PlaybackFixtures.BuildTrack(),
            transitionOptions: new BranchTransitionOptions { Enabled = false });
        int beatChangedCount = 0;
        int branchJumpCount = 0;
        provider.BeatChanged += (_, _) => beatChangedCount++;
        provider.BranchJumped += (_, _) => branchJumpCount++;

        provider.Read(new float[10], 0, 10);
        beatChangedCount = 0;
        branchJumpCount = 0;
        provider.Read(new float[10], 0, 10);

        beatChangedCount.Should().Be(0);
        branchJumpCount.Should().Be(0);
    }

    [Fact]
    public void InitialReadShouldStillRaiseBeatChangedOnce()
    {
        var provider = new BeatScheduledSampleProvider(
            PlaybackFixtures.LoadedAudio(),
            PlaybackFixtures.BuildTrack(),
            transitionOptions: new BranchTransitionOptions { Enabled = false });
        int beatChangedCount = 0;
        provider.BeatChanged += (_, args) =>
        {
            args.BeatIndex.Should().Be(0);
            beatChangedCount++;
        };

        provider.Read(new float[4], 0, 4);
        provider.Read(new float[4], 0, 4);

        beatChangedCount.Should().Be(1);
    }

    [Fact]
    public void ReadCrossingBeatBoundaryShouldStillRaiseBeatChanged()
    {
        var provider = new BeatScheduledSampleProvider(
            PlaybackFixtures.LoadedAudio(sampleRate: 10),
            PlaybackFixtures.BuildTrack(),
            new BranchDecisionEngine(new BranchDecisionOptions { InfiniteMode = false }),
            new BranchTransitionOptions { Enabled = false });
        List<int> beats = [];
        provider.BeatChanged += (_, args) => beats.Add(args.BeatIndex);

        provider.Read(new float[12], 0, 12);

        beats.Should().ContainInOrder(0, 1);
    }

    [Fact]
    public void ReadShouldAdvanceLinearlyWhenNoBranchIsUsed()
    {
        var provider = new BeatScheduledSampleProvider(
            PlaybackFixtures.LoadedAudio(),
            PlaybackFixtures.BuildTrack(),
            new BranchDecisionEngine(new BranchDecisionOptions { JumpProbability = 0 }),
            new BranchTransitionOptions { Enabled = false });

        provider.Read(new float[12], 0, 12);

        provider.CurrentBeatIndex.Should().Be(1);
    }

    [Fact]
    public void ReadShouldLoopSafelyWithoutBranches()
    {
        var provider = new BeatScheduledSampleProvider(
            PlaybackFixtures.LoadedAudio(),
            PlaybackFixtures.BuildTrack(),
            new BranchDecisionEngine(new BranchDecisionOptions { JumpProbability = 1 }),
            new BranchTransitionOptions { Enabled = false });

        int read = provider.Read(new float[70], 0, 70);

        read.Should().Be(70);
        provider.CurrentBeatIndex.Should().BeInRange(0, 4);
        provider.PositionSeconds.Should().BeInRange(0, provider.DurationSeconds);
    }

    [Fact]
    public void ReadShouldUseBranchWhenDecisionAllows()
    {
        var track = PlaybackFixtures.BuildTrack([PlaybackFixtures.Branch()]);
        var provider = new BeatScheduledSampleProvider(
            PlaybackFixtures.LoadedAudio(),
            track,
            new BranchDecisionEngine(
                new BranchDecisionOptions
                {
                    JumpProbability = 1,
                    MinRandomBranchChance = 1,
                    MaxRandomBranchChance = 1,
                    JumpCooldownBeats = 0,
                    FirstPassLinearPlaybackRatio = 0,
                    EscapeOptions = new BranchEscapeOptions { Enabled = false }
                },
                new FixedBranchRandomProvider(0)),
            new BranchTransitionOptions { Enabled = false });
        List<BranchJumpEventArgs> jumps = [];
        provider.BranchJumped += (_, args) => jumps.Add(args);

        provider.Read(new float[12], 0, 12);

        provider.CurrentBeatIndex.Should().Be(3);
        jumps.Should().ContainSingle();
        jumps[0].FromBeatIndex.Should().Be(0);
        jumps[0].SeedBeatIndex.Should().Be(1);
        jumps[0].ToBeatIndex.Should().Be(3);
    }

    [Fact]
    public void ReadUsingBranchShouldStillRaiseBranchJumped()
    {
        var track = PlaybackFixtures.BuildTrack([PlaybackFixtures.Branch()]);
        var provider = new BeatScheduledSampleProvider(
            PlaybackFixtures.LoadedAudio(),
            track,
            new BranchDecisionEngine(
                new BranchDecisionOptions
                {
                    JumpProbability = 1,
                    MinRandomBranchChance = 1,
                    MaxRandomBranchChance = 1,
                    JumpCooldownBeats = 0,
                    FirstPassLinearPlaybackRatio = 0,
                    EscapeOptions = new BranchEscapeOptions { Enabled = false }
                },
                new FixedBranchRandomProvider(0)),
            new BranchTransitionOptions { Enabled = false });
        int branchJumpCount = 0;
        provider.BranchJumped += (_, args) =>
        {
            args.ToBeatIndex.Should().Be(3);
            branchJumpCount++;
        };

        provider.Read(new float[12], 0, 12);

        branchJumpCount.Should().Be(1);
    }

    [Fact]
    public void BeatScheduledSampleProviderShouldUseBranchWithPrecomputedLinearIndex()
    {
        var track = PlaybackFixtures.BuildTrack([PlaybackFixtures.Branch(fromBeat: 1, toBeat: 4)]);
        var provider = new BeatScheduledSampleProvider(
            PlaybackFixtures.LoadedAudio(sampleRate: 1000),
            track,
            new BranchDecisionEngine(
                new BranchDecisionOptions
                {
                    JumpProbability = 1,
                    MinRandomBranchChance = 1,
                    MaxRandomBranchChance = 1,
                    JumpCooldownBeats = 0,
                    FirstPassLinearPlaybackRatio = 0,
                    EscapeOptions = new BranchEscapeOptions { Enabled = false }
                },
                new FixedBranchRandomProvider(0)),
            new BranchTransitionOptions { Enabled = false });
        List<BranchJumpEventArgs> jumps = [];
        provider.BranchJumped += (_, args) => jumps.Add(args);

        provider.Read(new float[1200], 0, 1200);

        jumps.Should().ContainSingle();
        jumps[0].ToBeatIndex.Should().Be(4);
        provider.CurrentBeatIndex.Should().Be(4);
    }

    [Fact]
    public void BranchJumpShouldLandOnDestinationBeatAtBoundary()
    {
        var track = PlaybackFixtures.BuildTrack([PlaybackFixtures.Branch(fromBeat: 1, toBeat: 4)]);
        var provider = new BeatScheduledSampleProvider(
            PlaybackFixtures.LoadedAudio(sampleRate: 1000),
            track,
            new BranchDecisionEngine(
                new BranchDecisionOptions
                {
                    JumpProbability = 1,
                    MinRandomBranchChance = 1,
                    MaxRandomBranchChance = 1,
                    JumpCooldownBeats = 0,
                    FirstPassLinearPlaybackRatio = 0,
                    EscapeOptions = new BranchEscapeOptions { Enabled = false }
                },
                new FixedBranchRandomProvider(0)),
            new BranchTransitionOptions { Enabled = false });
        List<int> beatChanges = [];
        List<BranchJumpEventArgs> jumps = [];
        provider.BeatChanged += (_, args) => beatChanges.Add(args.BeatIndex);
        provider.BranchJumped += (_, args) => jumps.Add(args);

        provider.Read(new float[1200], 0, 1200);

        jumps.Should().ContainSingle();
        jumps[0].FromBeatIndex.Should().Be(0);
        jumps[0].SeedBeatIndex.Should().Be(1);
        jumps[0].ToBeatIndex.Should().Be(4);
        beatChanges.Should().Contain(4);
        provider.CurrentBeatIndex.Should().Be(4);
        provider.PositionSeconds.Should().BeApproximately(4.2, 0.0001);
    }

    [Fact]
    public void JumpShouldStartAtDestinationBeatFrame()
    {
        var track = PlaybackFixtures.BuildTrack([PlaybackFixtures.Branch(fromBeat: 1, toBeat: 4)]);
        var provider = new BeatScheduledSampleProvider(
            PlaybackFixtures.LoadedAudio(sampleRate: 10),
            track,
            new BranchDecisionEngine(
                new BranchDecisionOptions
                {
                    JumpProbability = 1,
                    MinRandomBranchChance = 1,
                    MaxRandomBranchChance = 1,
                    JumpCooldownBeats = 0,
                    FirstPassLinearPlaybackRatio = 0,
                    EscapeOptions = new BranchEscapeOptions { Enabled = false }
                },
                new FixedBranchRandomProvider(0)),
            new BranchTransitionOptions { Enabled = false });
        float[] buffer = new float[11];

        provider.Read(buffer, 0, buffer.Length);

        buffer[10].Should().Be(41);
        provider.CurrentBeatIndex.Should().Be(4);
    }

    [Fact]
    public void FirstPassRatioShouldBlockEarlyBranch()
    {
        var track = PlaybackFixtures.BuildTrack([PlaybackFixtures.Branch(fromBeat: 1, toBeat: 4)]);
        var provider = new BeatScheduledSampleProvider(
            PlaybackFixtures.LoadedAudio(sampleRate: 1000),
            track,
            new BranchDecisionEngine(
                new BranchDecisionOptions
                {
                    JumpProbability = 1,
                    MinRandomBranchChance = 1,
                    MaxRandomBranchChance = 1,
                    JumpCooldownBeats = 0,
                    FirstPassLinearPlaybackRatio = 0.78,
                    EscapeOptions = new BranchEscapeOptions { Enabled = false }
                },
                new FixedBranchRandomProvider(0)),
            new BranchTransitionOptions { Enabled = false });
        List<BranchJumpEventArgs> jumps = [];
        provider.BranchJumped += (_, args) => jumps.Add(args);

        provider.Read(new float[1200], 0, 1200);

        jumps.Should().BeEmpty();
        provider.CurrentBeatIndex.Should().Be(1);
        provider.PositionSeconds.Should().BeApproximately(1.2, 0.0001);
    }

    [Fact]
    public void JumpCooldownShouldBlockNearbyJump()
    {
        var track = PlaybackFixtures.BuildTrack(
        [
            PlaybackFixtures.Branch(id: 1, fromBeat: 1, toBeat: 3),
            PlaybackFixtures.Branch(id: 2, fromBeat: 4, toBeat: 0)
        ]);
        var provider = new BeatScheduledSampleProvider(
            PlaybackFixtures.LoadedAudio(sampleRate: 1000),
            track,
            new BranchDecisionEngine(
                new BranchDecisionOptions
                {
                    JumpProbability = 1,
                    MinRandomBranchChance = 1,
                    MaxRandomBranchChance = 1,
                    JumpCooldownBeats = 12,
                    FirstPassLinearPlaybackRatio = 0,
                    EscapeOptions = new BranchEscapeOptions { Enabled = false }
                },
                new FixedBranchRandomProvider(0)),
            new BranchTransitionOptions { Enabled = false });
        List<BranchJumpEventArgs> jumps = [];
        provider.BranchJumped += (_, args) => jumps.Add(args);

        provider.Read(new float[2200], 0, 2200);

        jumps.Should().ContainSingle();
        jumps[0].ToBeatIndex.Should().Be(3);
        provider.CurrentBeatIndex.Should().Be(4);
        provider.PositionSeconds.Should().BeApproximately(4.2, 0.0001);
    }

    [Fact]
    public void BringItHomeShouldStopOnLastBeatAndFillSilence()
    {
        var provider = new BeatScheduledSampleProvider(
            PlaybackFixtures.LoadedAudio(sampleRate: 10),
            PlaybackFixtures.BuildTrack(),
            new BranchDecisionEngine(new BranchDecisionOptions { EscapeOptions = new BranchEscapeOptions { Enabled = false } }),
            new BranchTransitionOptions { Enabled = false });
        int completedCount = 0;
        provider.PlaybackCompleted += (_, _) => completedCount++;
        float[] buffer = new float[70];

        provider.SetBringItHome(true);
        provider.Read(buffer, 0, buffer.Length);
        provider.Read(new float[10], 0, 10);

        completedCount.Should().Be(1);
        provider.CurrentBeatIndex.Should().Be(4);
        buffer[..50].Should().OnlyContain(sample => sample > 0);
        buffer[50..].Should().OnlyContain(sample => sample == 0);
    }

    [Fact]
    public void BringItHomeShouldPlayLinearlyWithoutBranchJump()
    {
        var track = PlaybackFixtures.BuildTrack([PlaybackFixtures.Branch()]);
        var provider = new BeatScheduledSampleProvider(
            PlaybackFixtures.LoadedAudio(sampleRate: 10),
            track,
            new BranchDecisionEngine(
                new BranchDecisionOptions
                {
                    JumpProbability = 1,
                    MinRandomBranchChance = 1,
                    MaxRandomBranchChance = 1,
                    JumpCooldownBeats = 0,
                    FirstPassLinearPlaybackRatio = 0,
                    EscapeOptions = new BranchEscapeOptions { Enabled = false }
                },
                new FixedBranchRandomProvider(0)),
            new BranchTransitionOptions { Enabled = false });
        List<BranchJumpEventArgs> jumps = [];

        provider.BranchJumped += (_, args) => jumps.Add(args);
        provider.SetBringItHome(true);
        provider.Read(new float[30], 0, 30);

        jumps.Should().BeEmpty();
        provider.CurrentBeatIndex.Should().Be(2);
    }

    [Fact]
    public void ResetShouldClearBringItHomeAndCompletion()
    {
        var provider = new BeatScheduledSampleProvider(
            PlaybackFixtures.LoadedAudio(sampleRate: 10),
            PlaybackFixtures.BuildTrack(),
            new BranchDecisionEngine(new BranchDecisionOptions { EscapeOptions = new BranchEscapeOptions { Enabled = false } }),
            new BranchTransitionOptions { Enabled = false });
        int completedCount = 0;
        provider.PlaybackCompleted += (_, _) => completedCount++;

        provider.SetBringItHome(true);
        provider.Read(new float[70], 0, 70);
        provider.Reset();
        provider.Read(new float[70], 0, 70);

        completedCount.Should().Be(1);
        provider.CurrentBeatIndex.Should().BeInRange(0, 4);
    }

    [Fact]
    public void ResetShouldClearCompletionAndAllowSamplesAgain()
    {
        var provider = new BeatScheduledSampleProvider(
            PlaybackFixtures.LoadedAudio(sampleRate: 10),
            PlaybackFixtures.BuildTrack(),
            new BranchDecisionEngine(new BranchDecisionOptions { EscapeOptions = new BranchEscapeOptions { Enabled = false } }),
            new BranchTransitionOptions { Enabled = false });
        float[] completedBuffer = new float[70];

        provider.SetBringItHome(true);
        provider.Read(completedBuffer, 0, completedBuffer.Length);

        provider.IsCompleted.Should().BeTrue();

        provider.Reset();
        float[] restartedBuffer = new float[4];
        provider.Read(restartedBuffer, 0, restartedBuffer.Length);

        provider.IsCompleted.Should().BeFalse();
        provider.CurrentBeatIndex.Should().Be(0);
        restartedBuffer.Should().OnlyContain(sample => sample > 0);
    }

    [Fact]
    public void ResetShouldReturnToFirstBeat()
    {
        var provider = new BeatScheduledSampleProvider(
            PlaybackFixtures.LoadedAudio(),
            PlaybackFixtures.BuildTrack(),
            transitionOptions: new BranchTransitionOptions { Enabled = false });

        provider.Read(new float[12], 0, 12);
        provider.Reset();

        provider.CurrentBeatIndex.Should().Be(0);
        provider.PositionSeconds.Should().Be(0);
    }

    [Fact]
    public void ResetShouldStillRaiseBeatChanged()
    {
        var provider = new BeatScheduledSampleProvider(
            PlaybackFixtures.LoadedAudio(),
            PlaybackFixtures.BuildTrack(),
            transitionOptions: new BranchTransitionOptions { Enabled = false });
        int beatChangedCount = 0;
        provider.BeatChanged += (_, args) =>
        {
            args.BeatIndex.Should().Be(0);
            beatChangedCount++;
        };

        provider.Reset();

        beatChangedCount.Should().Be(1);
    }

    [Fact]
    public void ReadShouldExposeIncreasingPosition()
    {
        var provider = new BeatScheduledSampleProvider(
            PlaybackFixtures.LoadedAudio(),
            PlaybackFixtures.BuildTrack(),
            transitionOptions: new BranchTransitionOptions { Enabled = false });

        provider.Read(new float[5], 0, 5);

        provider.PositionSeconds.Should().BeApproximately(0.5, 0.0001);
        provider.DurationSeconds.Should().Be(5);
    }

    [Fact]
    public void SeekShouldMoveToExpectedBeatAndPosition()
    {
        var provider = new BeatScheduledSampleProvider(
            PlaybackFixtures.LoadedAudio(),
            PlaybackFixtures.BuildTrack(),
            transitionOptions: new BranchTransitionOptions { Enabled = false });
        List<int> beats = [];
        provider.BeatChanged += (_, args) => beats.Add(args.BeatIndex);

        provider.Seek(2.4);

        provider.CurrentBeatIndex.Should().Be(2);
        provider.PositionSeconds.Should().BeApproximately(2.4, 0.0001);
        beats.Should().Contain(2);
    }

    [Fact]
    public void SeekShouldStillRaiseBeatChanged()
    {
        var provider = new BeatScheduledSampleProvider(
            PlaybackFixtures.LoadedAudio(),
            PlaybackFixtures.BuildTrack(),
            transitionOptions: new BranchTransitionOptions { Enabled = false });
        int beatChangedCount = 0;
        provider.BeatChanged += (_, args) =>
        {
            args.BeatIndex.Should().Be(2);
            beatChangedCount++;
        };

        provider.Seek(2.4);

        beatChangedCount.Should().Be(1);
    }

    [Fact]
    public void SeekShouldRaiseBeatChangedOnceForTargetBeat()
    {
        var provider = new BeatScheduledSampleProvider(
            PlaybackFixtures.LoadedAudio(),
            PlaybackFixtures.BuildTrack(),
            transitionOptions: new BranchTransitionOptions { Enabled = false });
        List<BeatChangedEventArgs> events = [];
        provider.BeatChanged += (_, args) => events.Add(args);

        provider.Seek(2.4);

        events.Should().ContainSingle();
        events[0].BeatIndex.Should().Be(2);
        events[0].BeatStartSeconds.Should().Be(2);
        events[0].BeatDurationSeconds.Should().Be(1);
        provider.CurrentBeatIndex.Should().Be(2);
    }

    [Fact]
    public void SeekShouldClampNegativePositionToStart()
    {
        var provider = new BeatScheduledSampleProvider(
            PlaybackFixtures.LoadedAudio(),
            PlaybackFixtures.BuildTrack(),
            transitionOptions: new BranchTransitionOptions { Enabled = false });

        provider.Seek(-20);

        provider.CurrentBeatIndex.Should().Be(0);
        provider.PositionSeconds.Should().Be(0);
    }

    [Fact]
    public void SeekShouldClampBeyondDurationToLastSafeBeat()
    {
        var provider = new BeatScheduledSampleProvider(
            PlaybackFixtures.LoadedAudio(),
            PlaybackFixtures.BuildTrack(),
            transitionOptions: new BranchTransitionOptions { Enabled = false });

        provider.Seek(99);

        provider.CurrentBeatIndex.Should().Be(4);
        provider.PositionSeconds.Should().BeLessThanOrEqualTo(5);
    }

    [Fact]
    public void InvalidBranchShouldNotCrashRead()
    {
        RuntimeTrack track = PlaybackFixtures.BuildTrack();
        AddInvalidBranch(track);
        var provider = new BeatScheduledSampleProvider(
            PlaybackFixtures.LoadedAudio(),
            track,
            new BranchDecisionEngine(
                new BranchDecisionOptions
                {
                    JumpProbability = 1,
                    MinRandomBranchChance = 1,
                    MaxRandomBranchChance = 1,
                    FirstPassLinearPlaybackRatio = 0,
                    JumpCooldownBeats = 0,
                    EscapeOptions = new BranchEscapeOptions { Enabled = false }
                },
                new FixedBranchRandomProvider(0)),
            new BranchTransitionOptions { Enabled = false });
        List<BranchJumpEventArgs> jumps = [];
        provider.BranchJumped += (_, args) => jumps.Add(args);

        Action act = () => provider.Read(new float[12], 0, 12);

        act.Should().NotThrow();
        jumps.Should().BeEmpty();
        provider.CurrentBeatIndex.Should().Be(1);
    }

    [Fact]
    public void ReadShouldApplyFadeInForBranchJump()
    {
        var track = PlaybackFixtures.BuildTrack([PlaybackFixtures.Branch()]);
        var provider = new BeatScheduledSampleProvider(
            PlaybackFixtures.LoadedAudio(),
            track,
            new BranchDecisionEngine(
                new BranchDecisionOptions
                {
                    JumpProbability = 1,
                    MinRandomBranchChance = 1,
                    MaxRandomBranchChance = 1,
                    JumpCooldownBeats = 0,
                    FirstPassLinearPlaybackRatio = 0,
                    EscapeOptions = new BranchEscapeOptions { Enabled = false }
                },
                new FixedBranchRandomProvider(0)),
            new BranchTransitionOptions { Enabled = true, FadeMilliseconds = 100 });
        float[] buffer = new float[11];

        provider.Read(buffer, 0, buffer.Length);

        buffer[10].Should().Be(0);
    }

    private static void AddInvalidBranch(RuntimeTrack track)
    {
        RuntimeBeat source = track.Beats[1];
        source.Neighbors.Add(new RuntimeBranchEdge
        {
            Id = 500,
            Status = "active",
            FromBeat = source.Which,
            ToBeat = 99,
            JumpBeats = 98,
            Direction = "forward",
            Distance = double.PositiveInfinity,
            Deleted = false,
            SourceBeat = source,
            DestinationBeat = new RuntimeBeat { Which = 99, Start = 99, Duration = 1, Confidence = 1 }
        });
    }
}
