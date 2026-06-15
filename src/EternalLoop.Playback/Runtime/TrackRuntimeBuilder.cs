using EternalLoop.Playback.Models;

namespace EternalLoop.Playback.Runtime;

public sealed class TrackRuntimeBuilder
{
    private readonly BranchRuntimeApplier _branchRuntimeApplier;

    public TrackRuntimeBuilder()
        : this(new BranchRuntimeApplier())
    {
    }

    public TrackRuntimeBuilder(BranchRuntimeApplier branchRuntimeApplier)
    {
        _branchRuntimeApplier = branchRuntimeApplier;
    }

    public RuntimeTrackBuildResult Build(TrackRuntimeBuildRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Beats.Count == 0)
        {
            throw new RuntimeBuildException("Runtime track build request must contain at least one beat.");
        }

        List<RuntimeBeat> beats = CreateBeats(request.Beats);
        LinkBeats(beats);

        RuntimeTrack track = new()
        {
            Id = FirstNonEmpty(request.Id, Path.GetFileNameWithoutExtension(request.AudioPath), "runtime-track"),
            Title = FirstNonEmpty(request.Title, Path.GetFileNameWithoutExtension(request.AudioPath), "Unknown Title"),
            Artist = FirstNonEmpty(request.Artist, "Local"),
            AudioPath = request.AudioPath,
            AnalysisPath = request.AnalysisPath,
            BranchesPath = request.BranchesPath,
            DurationSeconds = request.DurationSeconds,
            Beats = beats
        };

        BranchRuntimeApplyResult applyResult = _branchRuntimeApplier.Apply(
            track,
            request.ActiveBranches,
            request.CandidateBranches);

        return new RuntimeTrackBuildResult(
            track,
            applyResult.IgnoredActiveBranches,
            applyResult.IgnoredCandidateBranches);
    }

    private static List<RuntimeBeat> CreateBeats(IReadOnlyList<RuntimeBeatInput> beatInputs)
    {
        List<RuntimeBeat> beats = new(beatInputs.Count);

        for (int index = 0; index < beatInputs.Count; index++)
        {
            RuntimeBeatInput beat = beatInputs[index];

            if (!IsFinite(beat.Start)
                || !IsFinite(beat.Duration)
                || beat.Duration <= 0
                || !IsFinite(beat.Confidence))
            {
                throw new RuntimeBuildException("Runtime track build request contains an invalid beat.");
            }

            beats.Add(new RuntimeBeat
            {
                Which = beat.Which >= 0 ? beat.Which : index,
                Start = beat.Start,
                Duration = beat.Duration,
                Confidence = beat.Confidence
            });
        }

        return beats;
    }

    private static void LinkBeats(IReadOnlyList<RuntimeBeat> beats)
    {
        for (int index = 0; index < beats.Count; index++)
        {
            RuntimeBeat beat = beats[index];
            beat.Prev = index == 0 ? null : beats[index - 1];
            beat.Next = index == beats.Count - 1 ? null : beats[index + 1];
        }
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.First(value => !string.IsNullOrWhiteSpace(value))!;
    }

    private static bool IsFinite(double value)
    {
        return double.IsFinite(value);
    }
}
