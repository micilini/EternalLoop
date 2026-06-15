using EternalLoop.AnalysisEngine.Core.Models;
using EternalLoop.BranchAnalysis.Core.Export;
using EternalLoop.BranchAnalysis.Core.Runner;
using EternalLoop.Core.Settings;
using EternalLoop.Core.Workflow;
using EternalLoop.Playback.Runtime;

namespace EternalLoop.Core.Runtime;

public sealed class TrackRuntimePackageBuilder
{
    private readonly TrackRuntimeBuilder _runtimeBuilder;

    public TrackRuntimePackageBuilder()
        : this(new TrackRuntimeBuilder())
    {
    }

    public TrackRuntimePackageBuilder(TrackRuntimeBuilder runtimeBuilder)
    {
        _runtimeBuilder = runtimeBuilder
            ?? throw new ArgumentNullException(nameof(runtimeBuilder));
    }

    public TrackRuntimePackage Build(TrackRuntimePackageBuildRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Input);
        ArgumentNullException.ThrowIfNull(request.Analysis);
        ArgumentNullException.ThrowIfNull(request.BranchPayload);
        ArgumentNullException.ThrowIfNull(request.Tuning);

        if (request.Analysis.Beats.Count == 0)
        {
            throw new RuntimePackageBuildException("Cannot build runtime package because analysis contains no beats.");
        }

        if (string.IsNullOrWhiteSpace(request.BranchesJsonPath)
            || !File.Exists(request.BranchesJsonPath))
        {
            throw new RuntimePackageBuildException("Cannot build runtime package because the branch export file is missing.");
        }

        TrackRuntimeMetadata metadata = CreateMetadata(request);
        TrackRuntimeFileSet files = CreateFileSet(request);
        TrackRuntimeTuningSnapshot tuningSnapshot = CreateTuningSnapshot(request.Tuning);
        BranchDecisionOptions branchDecisionOptions = CreateBranchDecisionOptions(request.Tuning);

        IReadOnlyList<RuntimeBeatInput> beats = request.Analysis.Beats
            .Select(beat => new RuntimeBeatInput(
                beat.Index,
                beat.Start,
                beat.Duration,
                beat.Confidence))
            .ToList();

        BranchMappingResult activeBranches = MapBranches(
            request.BranchPayload.ActiveBranches,
            fallbackStatus: "active");

        BranchMappingResult candidateBranches = MapBranches(
            request.BranchPayload.CandidateBranches,
            fallbackStatus: "candidate");

        RuntimeTrackBuildResult buildResult;

        try
        {
            buildResult = _runtimeBuilder.Build(new TrackRuntimeBuildRequest
            {
                Id = metadata.TrackId,
                Title = metadata.Title,
                Artist = metadata.Artist,
                AudioPath = files.AudioPath,
                AnalysisPath = files.AnalysisJsonPath,
                BranchesPath = files.BranchesJsonPath,
                DurationSeconds = metadata.DurationSeconds,
                Beats = beats,
                ActiveBranches = activeBranches.Branches,
                CandidateBranches = candidateBranches.Branches
            });
        }
        catch (RuntimeBuildException exception)
        {
            throw new RuntimePackageBuildException("Playback runtime track could not be built.", exception);
        }

        int ignoredActiveBranches = activeBranches.IgnoredBranches + buildResult.IgnoredActiveBranches;
        int ignoredCandidateBranches = candidateBranches.IgnoredBranches + buildResult.IgnoredCandidateBranches;

        TrackRuntimePreparationSummary summary = new(
            buildResult.Track.Beats.Count,
            buildResult.Track.ActiveBranchCount,
            IsPlayable: buildResult.Track.Beats.Count > 0,
            ignoredActiveBranches,
            ignoredCandidateBranches);

        return new TrackRuntimePackage(
            metadata,
            files,
            tuningSnapshot,
            buildResult.Track,
            branchDecisionOptions,
            summary,
            ignoredActiveBranches,
            ignoredCandidateBranches);
    }

    private static TrackRuntimeMetadata CreateMetadata(TrackRuntimePackageBuildRequest request)
    {
        TrackMetadata metadata = request.Analysis.Metadata;
        BranchExportTrack branchTrack = request.BranchPayload.Track;
        string fallbackTitle = Path.GetFileNameWithoutExtension(request.Input.FileName);

        string trackId = FirstNonEmpty(
            branchTrack.Id,
            Path.GetFileNameWithoutExtension(request.Input.FileName),
            metadata.FileHash,
            "runtime-track");

        return new TrackRuntimeMetadata(
            trackId,
            FirstNonEmpty(branchTrack.Title, branchTrack.FixedTitle, fallbackTitle, "Unknown Title"),
            FirstNonEmpty(branchTrack.Artist, "Local"),
            metadata.FileHash,
            metadata.DurationSeconds,
            metadata.Tempo,
            metadata.TimeSignature,
            metadata.SchemaVersion,
            request.SettingsSchemaVersion,
            DateTime.UtcNow);
    }

    private static TrackRuntimeFileSet CreateFileSet(TrackRuntimePackageBuildRequest request)
    {
        return new TrackRuntimeFileSet(
            Path.GetFullPath(request.RunRoot),
            request.Input.FilePath,
            Path.GetFullPath(request.AnalysisJsonPath),
            Path.GetFullPath(request.BranchesJsonPath));
    }

    private static TrackRuntimeTuningSnapshot CreateTuningSnapshot(LoopTuningSettings tuning)
    {
        double similarityThreshold = Clamp(tuning.SimilarityThreshold, 0.65, 0.95);

        return new TrackRuntimeTuningSnapshot(
            string.IsNullOrWhiteSpace(tuning.Preset)
                ? LoopTuningPresetCatalog.BalancedId
                : tuning.Preset,
            similarityThreshold,
            Clamp(tuning.LookaheadDepth, 1, 5),
            Clamp(tuning.MinJumpDistance, 4, 64),
            Clamp(tuning.MaxBranchesPerBeat, 1, 12),
            string.IsNullOrWhiteSpace(tuning.BranchQuantumType) ? "beats" : tuning.BranchQuantumType,
            BranchAnalysisTuningMapper.MapSimilarityToMaxThreshold(similarityThreshold),
            tuning.AnalysisMusicalQuality,
            Clamp(tuning.JumpProbability, 0, 1),
            Clamp(tuning.JumpCooldown, 0, 64),
            Clamp(tuning.FirstPassLinearPlaybackRatio, 0, 1));
    }

    private static BranchDecisionOptions CreateBranchDecisionOptions(LoopTuningSettings tuning)
    {
        return new BranchDecisionOptions
        {
            JumpProbability = Clamp(tuning.JumpProbability, 0, 1),
            JumpCooldownBeats = Math.Max(0, tuning.JumpCooldown),
            FirstPassLinearPlaybackRatio = Clamp(tuning.FirstPassLinearPlaybackRatio, 0, 1)
        };
    }

    private static BranchMappingResult MapBranches(
        IReadOnlyList<BranchExportBranch> branches,
        string fallbackStatus)
    {
        List<RuntimeBranchInput> runtimeBranches = [];
        int ignoredBranches = 0;

        for (int index = 0; index < branches.Count; index++)
        {
            BranchExportBranch branch = branches[index];

            if (!TryMapBranch(branch, fallbackStatus, index, out RuntimeBranchInput? runtimeBranch))
            {
                ignoredBranches++;
                continue;
            }

            runtimeBranches.Add(runtimeBranch!);
        }

        return new BranchMappingResult(runtimeBranches, ignoredBranches);
    }

    private static bool TryMapBranch(
        BranchExportBranch branch,
        string fallbackStatus,
        int fallbackId,
        out RuntimeBranchInput? runtimeBranch)
    {
        runtimeBranch = null;

        if (branch.Deleted
            || branch.FromBeat is not int fromBeat
            || branch.ToBeat is not int toBeat
            || fromBeat == toBeat)
        {
            return false;
        }

        double? distance = branch.Distance
            ?? branch.Quality?.AcousticDistance
            ?? branch.Quality?.BranchScore;

        if (distance is not double finiteDistance || !double.IsFinite(finiteDistance))
        {
            return false;
        }

        int jumpBeats = branch.JumpBeats ?? toBeat - fromBeat;

        runtimeBranch = new RuntimeBranchInput(
            branch.Id ?? fallbackId,
            string.IsNullOrWhiteSpace(branch.Status) ? fallbackStatus : branch.Status,
            fromBeat,
            toBeat,
            jumpBeats,
            string.IsNullOrWhiteSpace(branch.Direction) ? InferDirection(jumpBeats) : branch.Direction,
            finiteDistance,
            branch.Deleted);

        return true;
    }

    private static string InferDirection(int jumpBeats)
    {
        return jumpBeats < 0 ? "backward" : "forward";
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.First(value => !string.IsNullOrWhiteSpace(value))!;
    }

    private static int Clamp(int value, int minimum, int maximum)
    {
        return Math.Min(Math.Max(value, minimum), maximum);
    }

    private static double Clamp(double value, double minimum, double maximum)
    {
        if (!double.IsFinite(value))
        {
            return minimum;
        }

        return Math.Min(Math.Max(value, minimum), maximum);
    }

    private sealed record BranchMappingResult(
        IReadOnlyList<RuntimeBranchInput> Branches,
        int IgnoredBranches);
}
