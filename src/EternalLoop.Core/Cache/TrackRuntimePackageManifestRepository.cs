using System.Text.Json;
using EternalLoop.Core.Runtime;
using EternalLoop.Core.Workflow;
using EternalLoop.Playback.Models;
using EternalLoop.Playback.Runtime;

namespace EternalLoop.Core.Cache;

public sealed class TrackRuntimePackageManifestRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly TrackRuntimeBuilder _runtimeBuilder;

    public TrackRuntimePackageManifestRepository()
        : this(new TrackRuntimeBuilder())
    {
    }

    public TrackRuntimePackageManifestRepository(TrackRuntimeBuilder runtimeBuilder)
    {
        _runtimeBuilder = runtimeBuilder
            ?? throw new ArgumentNullException(nameof(runtimeBuilder));
    }

    public async Task SaveAsync(
        TrackRuntimePackage package,
        string manifestPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(package);

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(manifestPath))!);
        TrackRuntimePackageManifest manifest = ToManifest(package);

        await using FileStream stream = File.Create(manifestPath);
        await JsonSerializer.SerializeAsync(stream, manifest, JsonOptions, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TrackRuntimePackage> LoadAsync(
        string manifestPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(manifestPath) || !File.Exists(manifestPath))
        {
            throw new FileNotFoundException("Runtime package manifest was not found.", manifestPath);
        }

        await using FileStream stream = File.OpenRead(manifestPath);
        TrackRuntimePackageManifest? manifest = await JsonSerializer.DeserializeAsync<TrackRuntimePackageManifest>(
            stream,
            JsonOptions,
            cancellationToken).ConfigureAwait(false);

        if (manifest is null || manifest.SchemaVersion != 1)
        {
            throw new InvalidDataException("Runtime package manifest is invalid.");
        }

        RuntimeTrackBuildResult buildResult = _runtimeBuilder.Build(new TrackRuntimeBuildRequest
        {
            Id = manifest.Metadata.TrackId,
            Title = manifest.Metadata.Title,
            Artist = manifest.Metadata.Artist,
            AudioPath = manifest.Files.AudioPath,
            AnalysisPath = manifest.Files.AnalysisJsonPath,
            BranchesPath = manifest.Files.BranchesJsonPath,
            DurationSeconds = manifest.Metadata.DurationSeconds,
            Beats = manifest.Beats.Select(ToRuntimeBeatInput).ToList(),
            ActiveBranches = manifest.ActiveBranches.Select(ToRuntimeBranchInput).ToList(),
            CandidateBranches = manifest.CandidateBranches.Select(ToRuntimeBranchInput).ToList()
        });

        TrackRuntimePreparationSummary summary = new(
            buildResult.Track.Beats.Count,
            buildResult.Track.ActiveBranchCount,
            buildResult.Track.Beats.Count > 0,
            manifest.Summary.IgnoredActiveBranches + buildResult.IgnoredActiveBranches,
            manifest.Summary.IgnoredCandidateBranches + buildResult.IgnoredCandidateBranches);

        return new TrackRuntimePackage(
            new TrackRuntimeMetadata(
                manifest.Metadata.TrackId,
                manifest.Metadata.Title,
                manifest.Metadata.Artist,
                manifest.Metadata.FileHash,
                manifest.Metadata.DurationSeconds,
                manifest.Metadata.Tempo,
                manifest.Metadata.TimeSignature,
                manifest.Metadata.AnalysisSchemaVersion,
                manifest.Metadata.SettingsSchemaVersion,
                manifest.Metadata.CreatedAtUtc),
            new TrackRuntimeFileSet(
                manifest.Files.RunRoot,
                manifest.Files.AudioPath,
                manifest.Files.AnalysisJsonPath,
                manifest.Files.BranchesJsonPath),
            new TrackRuntimeTuningSnapshot(
                manifest.Tuning.Preset,
                manifest.Tuning.SimilarityThreshold,
                manifest.Tuning.LookaheadDepth,
                manifest.Tuning.MinJumpDistance,
                manifest.Tuning.MaxBranchesPerBeat,
                manifest.Tuning.BranchQuantumType,
                manifest.Tuning.BranchMaxThreshold,
                manifest.Tuning.AnalysisMusicalQuality,
                manifest.Tuning.JumpProbability,
                manifest.Tuning.JumpCooldown,
                manifest.Tuning.FirstPassLinearPlaybackRatio),
            buildResult.Track,
            new BranchDecisionOptions
            {
                JumpProbability = manifest.BranchDecisionOptions.JumpProbability,
                JumpCooldownBeats = manifest.BranchDecisionOptions.JumpCooldownBeats,
                FirstPassLinearPlaybackRatio = manifest.BranchDecisionOptions.FirstPassLinearPlaybackRatio
            },
            summary,
            summary.IgnoredActiveBranches,
            summary.IgnoredCandidateBranches);
    }

    private static TrackRuntimePackageManifest ToManifest(TrackRuntimePackage package)
    {
        return new TrackRuntimePackageManifest
        {
            Metadata = new TrackRuntimeMetadataDto
            {
                TrackId = package.Metadata.TrackId,
                Title = package.Metadata.Title,
                Artist = package.Metadata.Artist,
                FileHash = package.Metadata.FileHash,
                DurationSeconds = package.Metadata.DurationSeconds,
                Tempo = package.Metadata.Tempo,
                TimeSignature = package.Metadata.TimeSignature,
                AnalysisSchemaVersion = package.Metadata.AnalysisSchemaVersion,
                SettingsSchemaVersion = package.Metadata.SettingsSchemaVersion,
                CreatedAtUtc = package.Metadata.CreatedAtUtc
            },
            Files = new TrackRuntimeFileSetDto
            {
                RunRoot = package.Files.RunRoot,
                AudioPath = package.Files.AudioPath,
                AnalysisJsonPath = package.Files.AnalysisJsonPath,
                BranchesJsonPath = package.Files.BranchesJsonPath
            },
            Tuning = new TrackRuntimeTuningSnapshotDto
            {
                Preset = package.Tuning.Preset,
                SimilarityThreshold = package.Tuning.SimilarityThreshold,
                LookaheadDepth = package.Tuning.LookaheadDepth,
                MinJumpDistance = package.Tuning.MinJumpDistance,
                MaxBranchesPerBeat = package.Tuning.MaxBranchesPerBeat,
                BranchQuantumType = package.Tuning.BranchQuantumType,
                BranchMaxThreshold = package.Tuning.BranchMaxThreshold,
                AnalysisMusicalQuality = package.Tuning.AnalysisMusicalQuality,
                JumpProbability = package.Tuning.JumpProbability,
                JumpCooldown = package.Tuning.JumpCooldown,
                FirstPassLinearPlaybackRatio = package.Tuning.FirstPassLinearPlaybackRatio
            },
            BranchDecisionOptions = new BranchDecisionOptionsDto
            {
                JumpProbability = package.BranchDecisionOptions.JumpProbability,
                JumpCooldownBeats = package.BranchDecisionOptions.JumpCooldownBeats,
                FirstPassLinearPlaybackRatio = package.BranchDecisionOptions.FirstPassLinearPlaybackRatio
            },
            Summary = new TrackRuntimePreparationSummaryDto
            {
                RuntimeBeatCount = package.Summary.RuntimeBeatCount,
                RuntimeBranchCount = package.Summary.RuntimeBranchCount,
                IsPlayable = package.Summary.IsPlayable,
                IgnoredActiveBranches = package.Summary.IgnoredActiveBranches,
                IgnoredCandidateBranches = package.Summary.IgnoredCandidateBranches
            },
            Beats = package.RuntimeTrack.Beats.Select(beat => new RuntimeBeatInputDto
            {
                Which = beat.Which,
                Start = beat.Start,
                Duration = beat.Duration,
                Confidence = beat.Confidence
            }).ToList(),
            ActiveBranches = package.RuntimeTrack.Beats
                .SelectMany(beat => beat.Neighbors)
                .Select(ToDto)
                .ToList(),
            CandidateBranches = package.RuntimeTrack.Beats
                .SelectMany(beat => beat.AllNeighbors)
                .Select(ToDto)
                .ToList(),
            IgnoredActiveBranches = package.IgnoredActiveBranches,
            IgnoredCandidateBranches = package.IgnoredCandidateBranches
        };
    }

    private static RuntimeBeatInput ToRuntimeBeatInput(RuntimeBeatInputDto dto)
    {
        return new RuntimeBeatInput(dto.Which, dto.Start, dto.Duration, dto.Confidence);
    }

    private static RuntimeBranchInput ToRuntimeBranchInput(RuntimeBranchInputDto dto)
    {
        return new RuntimeBranchInput(
            dto.Id,
            dto.Status,
            dto.FromBeat,
            dto.ToBeat,
            dto.JumpBeats,
            dto.Direction,
            dto.Distance,
            dto.Deleted);
    }

    private static RuntimeBranchInputDto ToDto(RuntimeBranchEdge edge)
    {
        return new RuntimeBranchInputDto
        {
            Id = edge.Id,
            Status = edge.Status,
            FromBeat = edge.FromBeat,
            ToBeat = edge.ToBeat,
            JumpBeats = edge.JumpBeats,
            Direction = edge.Direction,
            Distance = edge.Distance,
            Deleted = edge.Deleted
        };
    }
}
