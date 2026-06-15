using EternalLoop.Playback.Audio;
using EternalLoop.Playback.Models;
using EternalLoop.Playback.Runtime;

namespace EternalLoop.Playback.Tests.Fixtures;

internal static class PlaybackFixtures
{
    public static TrackRuntimeBuildRequest BuildRequest(
        IReadOnlyList<RuntimeBranchInput>? activeBranches = null,
        IReadOnlyList<RuntimeBranchInput>? candidateBranches = null)
    {
        return new TrackRuntimeBuildRequest
        {
            Id = "fixture",
            Title = "Fixture",
            Artist = "Local",
            AudioPath = "fixture.wav",
            DurationSeconds = 5,
            Beats =
            [
                new RuntimeBeatInput(0, 0, 1, 1),
                new RuntimeBeatInput(1, 1, 1, 1),
                new RuntimeBeatInput(2, 2, 1, 1),
                new RuntimeBeatInput(3, 3, 1, 1),
                new RuntimeBeatInput(4, 4, 1, 1)
            ],
            ActiveBranches = activeBranches ?? [],
            CandidateBranches = candidateBranches ?? []
        };
    }

    public static RuntimeTrack BuildTrack(
        IReadOnlyList<RuntimeBranchInput>? activeBranches = null,
        IReadOnlyList<RuntimeBranchInput>? candidateBranches = null)
    {
        return new TrackRuntimeBuilder().Build(BuildRequest(activeBranches, candidateBranches)).Track;
    }

    public static RuntimeBranchInput Branch(
        int id = 10,
        int fromBeat = 1,
        int toBeat = 3,
        double distance = 12,
        bool deleted = false)
    {
        return new RuntimeBranchInput(
            id,
            "active",
            fromBeat,
            toBeat,
            toBeat - fromBeat,
            toBeat >= fromBeat ? "forward" : "backward",
            distance,
            deleted);
    }

    public static LoadedAudio LoadedAudio(int sampleRate = 10, int channels = 1, int seconds = 5)
    {
        int sampleCount = sampleRate * channels * seconds;
        float[] samples = Enumerable.Range(0, sampleCount)
            .Select(index => (float)(index + 1))
            .ToArray();

        return new LoadedAudio
        {
            SourcePath = "fixture.wav",
            Samples = samples,
            SampleRate = sampleRate,
            Channels = channels,
            DurationSeconds = seconds,
            TotalSampleFrames = sampleRate * seconds
        };
    }
}
