namespace EternalLoop.Core.Cache;

public sealed class TrackRuntimePackageManifest
{
    public int SchemaVersion { get; init; } = 1;

    public TrackRuntimeMetadataDto Metadata { get; init; } = new();

    public TrackRuntimeFileSetDto Files { get; init; } = new();

    public TrackRuntimeTuningSnapshotDto Tuning { get; init; } = new();

    public BranchDecisionOptionsDto BranchDecisionOptions { get; init; } = new();

    public TrackRuntimePreparationSummaryDto Summary { get; init; } = new();

    public List<RuntimeBeatInputDto> Beats { get; init; } = [];

    public List<RuntimeBranchInputDto> ActiveBranches { get; init; } = [];

    public List<RuntimeBranchInputDto> CandidateBranches { get; init; } = [];

    public int IgnoredActiveBranches { get; init; }

    public int IgnoredCandidateBranches { get; init; }
}

public sealed class TrackRuntimeMetadataDto
{
    public string TrackId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Artist { get; init; } = string.Empty;
    public string FileHash { get; init; } = string.Empty;
    public double DurationSeconds { get; init; }
    public double Tempo { get; init; }
    public int TimeSignature { get; init; }
    public string AnalysisSchemaVersion { get; init; } = string.Empty;
    public int SettingsSchemaVersion { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}

public sealed class TrackRuntimeFileSetDto
{
    public string RunRoot { get; init; } = string.Empty;
    public string AudioPath { get; init; } = string.Empty;
    public string AnalysisJsonPath { get; init; } = string.Empty;
    public string BranchesJsonPath { get; init; } = string.Empty;
}

public sealed class TrackRuntimeTuningSnapshotDto
{
    public string Preset { get; init; } = string.Empty;
    public double SimilarityThreshold { get; init; }
    public int LookaheadDepth { get; init; }
    public int MinJumpDistance { get; init; }
    public int MaxBranchesPerBeat { get; init; }
    public string BranchQuantumType { get; init; } = string.Empty;
    public int BranchMaxThreshold { get; init; }
    public bool AnalysisMusicalQuality { get; init; }
    public double JumpProbability { get; init; }
    public int JumpCooldown { get; init; }
    public double FirstPassLinearPlaybackRatio { get; init; }
}

public sealed class BranchDecisionOptionsDto
{
    public double JumpProbability { get; init; }
    public int JumpCooldownBeats { get; init; }
    public double FirstPassLinearPlaybackRatio { get; init; }
}

public sealed class TrackRuntimePreparationSummaryDto
{
    public int RuntimeBeatCount { get; init; }
    public int RuntimeBranchCount { get; init; }
    public bool IsPlayable { get; init; }
    public int IgnoredActiveBranches { get; init; }
    public int IgnoredCandidateBranches { get; init; }
}

public sealed class RuntimeBeatInputDto
{
    public int Which { get; init; }
    public double Start { get; init; }
    public double Duration { get; init; }
    public double Confidence { get; init; }
}

public sealed class RuntimeBranchInputDto
{
    public int Id { get; init; }
    public string Status { get; init; } = string.Empty;
    public int FromBeat { get; init; }
    public int ToBeat { get; init; }
    public int JumpBeats { get; init; }
    public string Direction { get; init; } = string.Empty;
    public double Distance { get; init; }
    public bool Deleted { get; init; }
}
