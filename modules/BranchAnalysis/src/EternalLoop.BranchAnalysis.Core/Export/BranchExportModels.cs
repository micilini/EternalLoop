using System.Text.Json.Serialization;
using EternalLoop.BranchAnalysis.Core.Branching;

namespace EternalLoop.BranchAnalysis.Core.Export;

public sealed class BranchExportPayload
{
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; init; } = string.Empty;

    [JsonPropertyName("exportedAt")]
    public string ExportedAt { get; init; } = string.Empty;

    [JsonPropertyName("sourcePage")]
    public string SourcePage { get; init; } = string.Empty;

    [JsonPropertyName("branchSource")]
    public string BranchSource { get; init; } = string.Empty;

    [JsonPropertyName("track")]
    public BranchExportTrack Track { get; init; } = new();

    [JsonPropertyName("tuning")]
    public BranchExportTuning Tuning { get; init; } = new();

    [JsonPropertyName("policy")]
    public StructuralPolicySummary Policy { get; init; } = new();

    [JsonPropertyName("counts")]
    public BranchExportCounts Counts { get; init; } = new();

    [JsonPropertyName("diagnostics")]
    public BranchExportDiagnostics Diagnostics { get; init; } = new();

    [JsonPropertyName("activeBranches")]
    public List<BranchExportBranch> ActiveBranches { get; init; } = [];

    [JsonPropertyName("candidateBranches")]
    public List<BranchExportBranch> CandidateBranches { get; init; } = [];
}

public sealed class BranchExportTrack
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("artist")]
    public string? Artist { get; init; }

    [JsonPropertyName("fixedTitle")]
    public string? FixedTitle { get; init; }

    [JsonPropertyName("duration")]
    public double? Duration { get; init; }
}

public sealed class BranchExportTuning
{
    [JsonPropertyName("quantumType")]
    public string QuantumType { get; init; } = string.Empty;

    [JsonPropertyName("currentThreshold")]
    public double? CurrentThreshold { get; init; }

    [JsonPropertyName("computedThreshold")]
    public double? ComputedThreshold { get; init; }

    [JsonPropertyName("maxBranches")]
    public int? MaxBranches { get; init; }

    [JsonPropertyName("similarityThreshold")]
    public double? SimilarityThreshold { get; init; }

    [JsonPropertyName("lookaheadDepth")]
    public int? LookaheadDepth { get; init; }

    [JsonPropertyName("minJumpDistance")]
    public int? MinJumpDistance { get; init; }

    [JsonPropertyName("maxBranchThreshold")]
    public double? MaxBranchThreshold { get; init; }

    [JsonPropertyName("addLastEdge")]
    public bool AddLastEdge { get; init; }

    [JsonPropertyName("justBackwards")]
    public bool JustBackwards { get; init; }

    [JsonPropertyName("justLongBranches")]
    public bool JustLongBranches { get; init; }

    [JsonPropertyName("removeSequentialBranches")]
    public bool RemoveSequentialBranches { get; init; }

    [JsonPropertyName("minLongBranch")]
    public double? MinLongBranch { get; init; }

    [JsonPropertyName("lastBranchPoint")]
    public int? LastBranchPoint { get; init; }

    [JsonPropertyName("longestReach")]
    public double? LongestReach { get; init; }

    [JsonPropertyName("structuralPolicy")]
    public bool StructuralPolicy { get; init; }

    [JsonPropertyName("antiLocalLoopPolicy")]
    public bool AntiLocalLoopPolicy { get; init; }

    [JsonPropertyName("shortBranchPolicy")]
    public string? ShortBranchPolicy { get; init; }

    [JsonPropertyName("scoreGate")]
    public string ScoreGate { get; init; } = string.Empty;

    [JsonPropertyName("structuralMode")]
    public string StructuralMode { get; init; } = string.Empty;

    [JsonPropertyName("lateAnchorRouting")]
    public bool LateAnchorRouting { get; init; }

    [JsonPropertyName("earlyReturnTargetPercent")]
    public int? EarlyReturnTargetPercent { get; init; }

    [JsonPropertyName("lateAnchorPreferredStartPercent")]
    public int? LateAnchorPreferredStartPercent { get; init; }

    [JsonPropertyName("lateAnchorFallbackStartPercent")]
    public int? LateAnchorFallbackStartPercent { get; init; }
}

public sealed class BranchExportCounts
{
    [JsonPropertyName("sections")]
    public int Sections { get; init; }

    [JsonPropertyName("bars")]
    public int Bars { get; init; }

    [JsonPropertyName("beats")]
    public int Beats { get; init; }

    [JsonPropertyName("tatums")]
    public int Tatums { get; init; }

    [JsonPropertyName("segments")]
    public int Segments { get; init; }

    [JsonPropertyName("activeBranches")]
    public int ActiveBranches { get; init; }

    [JsonPropertyName("candidateBranches")]
    public int CandidateBranches { get; init; }

    [JsonPropertyName("visualBranchCount")]
    public int? VisualBranchCount { get; init; }

    [JsonPropertyName("deletedBranches")]
    public int? DeletedBranches { get; init; }

    [JsonPropertyName("shortActiveBranches")]
    public int ShortActiveBranches { get; init; }

    [JsonPropertyName("veryShortActiveBranches")]
    public int VeryShortActiveBranches { get; init; }

    [JsonPropertyName("localLoopRiskBranches")]
    public int LocalLoopRiskBranches { get; init; }

    [JsonPropertyName("structurallyRejectedBranches")]
    public int StructurallyRejectedBranches { get; init; }

    [JsonPropertyName("antiMRemovedBranches")]
    public int AntiMRemovedBranches { get; init; }
}

public sealed class BranchExportDiagnostics
{
    [JsonPropertyName("structurallyRejectedBranches")]
    public int StructurallyRejectedBranches { get; init; }

    [JsonPropertyName("antiMRemovedBranches")]
    public int AntiMRemovedBranches { get; init; }

    [JsonPropertyName("localLoopRiskBranches")]
    public int LocalLoopRiskBranches { get; init; }

    [JsonPropertyName("lateAnchorDecision")]
    public string LateAnchorDecision { get; init; } = string.Empty;

    [JsonPropertyName("lateAnchorReason")]
    public string LateAnchorReason { get; init; } = string.Empty;

    [JsonPropertyName("lateAnchorEarlyReturnTargetBeat")]
    public int? LateAnchorEarlyReturnTargetBeat { get; init; }

    [JsonPropertyName("lateAnchorBranchesToTarget")]
    public int? LateAnchorBranchesToTarget { get; init; }

    [JsonPropertyName("lateAnchorEarliestReachableBeat")]
    public int? LateAnchorEarliestReachableBeat { get; init; }

    [JsonPropertyName("lateAnchorImmediateBackwardBeats")]
    public int? LateAnchorImmediateBackwardBeats { get; init; }

    [JsonPropertyName("lateAnchorDistance")]
    public double? LateAnchorDistance { get; init; }

    [JsonPropertyName("lateAnchorInsertedEdgeId")]
    public int? LateAnchorInsertedEdgeId { get; init; }

    [JsonPropertyName("lateAnchorSelectedEdgeId")]
    public int? LateAnchorSelectedEdgeId { get; init; }
}

public sealed class BranchExportBranch
{
    [JsonPropertyName("id")]
    public int? Id { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("fromBeat")]
    public int? FromBeat { get; init; }

    [JsonPropertyName("toBeat")]
    public int? ToBeat { get; init; }

    [JsonPropertyName("jumpBeats")]
    public int? JumpBeats { get; init; }

    [JsonPropertyName("direction")]
    public string? Direction { get; init; }

    [JsonPropertyName("distance")]
    public double? Distance { get; init; }

    [JsonPropertyName("quality")]
    public BranchExportQuality? Quality { get; init; }

    [JsonPropertyName("deleted")]
    public bool Deleted { get; init; }

    [JsonPropertyName("source")]
    public BranchExportQuantum? Source { get; init; }

    [JsonPropertyName("destination")]
    public BranchExportQuantum? Destination { get; init; }
}

public sealed class BranchExportQuality
{
    [JsonPropertyName("acousticDistance")]
    public double? AcousticDistance { get; init; }

    [JsonPropertyName("branchScore")]
    public double? BranchScore { get; init; }

    [JsonPropertyName("structuralPenalty")]
    public double? StructuralPenalty { get; init; }

    [JsonPropertyName("structuralBonus")]
    public double? StructuralBonus { get; init; }

    [JsonPropertyName("structuralBonusDiagnosticOnly")]
    public double? StructuralBonusDiagnosticOnly { get; init; }

    [JsonPropertyName("thresholdGate")]
    public string ThresholdGate { get; init; } = string.Empty;

    [JsonPropertyName("jumpBeatsAbs")]
    public double? JumpBeatsAbs { get; init; }

    [JsonPropertyName("jumpBars")]
    public double? JumpBars { get; init; }

    [JsonPropertyName("sameBarPhase")]
    public bool SameBarPhase { get; init; }

    [JsonPropertyName("samePhrase4Phase")]
    public bool SamePhrase4Phase { get; init; }

    [JsonPropertyName("samePhrase8Phase")]
    public bool SamePhrase8Phase { get; init; }

    [JsonPropertyName("samePhrase16Phase")]
    public bool SamePhrase16Phase { get; init; }

    [JsonPropertyName("sectionChange")]
    public bool SectionChange { get; init; }

    [JsonPropertyName("shortLocalRisk")]
    public bool ShortLocalRisk { get; init; }

    [JsonPropertyName("localLoopRisk")]
    public bool LocalLoopRisk { get; init; }

    [JsonPropertyName("policyDecision")]
    public string? PolicyDecision { get; init; }

    [JsonPropertyName("policyReasons")]
    public List<string> PolicyReasons { get; init; } = [];
}

public sealed class BranchExportQuantum
{
    [JsonPropertyName("which")]
    public int? Which { get; init; }

    [JsonPropertyName("start")]
    public double? Start { get; init; }

    [JsonPropertyName("duration")]
    public double? Duration { get; init; }

    [JsonPropertyName("confidence")]
    public double? Confidence { get; init; }

    [JsonPropertyName("indexInParent")]
    public int? IndexInParent { get; init; }

    [JsonPropertyName("overlappingSegmentCount")]
    public int OverlappingSegmentCount { get; init; }

    [JsonPropertyName("overlappingSegments")]
    public List<BranchExportSegment> OverlappingSegments { get; init; } = [];
}

public sealed class BranchExportSegment
{
    [JsonPropertyName("which")]
    public int? Which { get; init; }

    [JsonPropertyName("start")]
    public double? Start { get; init; }

    [JsonPropertyName("duration")]
    public double? Duration { get; init; }

    [JsonPropertyName("confidence")]
    public double? Confidence { get; init; }

    [JsonPropertyName("loudness_start")]
    public double? LoudnessStart { get; init; }

    [JsonPropertyName("loudness_max")]
    public double? LoudnessMax { get; init; }

    [JsonPropertyName("loudness_max_time")]
    public double? LoudnessMaxTime { get; init; }
}
