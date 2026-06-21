using EternalLoop.AnalysisEngine.Core.BeatTracking.Ai;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Candidates;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Shadow;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking;

public sealed class BeatTrackingResult
{
    public required double EstimatedBpm { get; init; }

    public required double[] BeatTimes { get; init; }

    public required double[] Confidences { get; init; }

    public double[] DownbeatTimes { get; init; } = [];

    public int[] BeatNumbers { get; init; } = [];

    public int? EstimatedMeter { get; init; }

    public string ProviderName { get; init; } = "built-in";

    public string ProviderVersion { get; init; } = "analysisengine-built-in";

    public string ProviderLicense { get; init; } = "MIT";

    public string ModelName { get; init; } = "none";

    public string ModelSha256 { get; init; } = "none";

    public bool UsedAiProvider { get; init; }

    public bool UsedBuiltInProvider { get; init; } = true;

    public bool UsedFallbackProvider { get; init; }

    public bool UsedHybridProvider { get; init; }

    public string? FallbackReason { get; init; }

    public string BeatGridMode { get; init; } = "unknown";

    public IReadOnlyList<string> ProviderWarnings { get; init; } = [];

    public bool DownbeatSanitized { get; init; }

    public string BeatProviderOutputMode { get; init; } = "none";

    public int BeatProviderChunkCount { get; init; }

    public int BeatProviderValidFrameCount { get; init; }

    public double BeatProviderCoverageSeconds { get; init; }

    public double BeatProviderCoverageRatio { get; init; }

    public BeatThisActivationSummary? BeatActivationSummary { get; init; }

    public BeatThisActivationSummary? DownbeatActivationSummary { get; init; }

    public IReadOnlyList<TempoCandidate> TempoCandidates { get; init; } = [];

    public double? ForcedTempoBpm { get; init; }

    public ElasticBeatGridRefinementResult? ElasticRefinement { get; init; }

    public PiecewiseBeatGridRefinementResult? PiecewiseRefinement { get; init; }

    public CompositeDpBeatTrackingResult? CompositeDpTracking { get; init; }

    public IReadOnlyDictionary<string, double> BeatEvidenceWeights { get; init; } = new Dictionary<string, double>();

    public double BeatEvidenceMean { get; init; }

    public double BeatEvidenceVariance { get; init; }

    public bool HpssRequested { get; init; }

    public bool HpssApplied { get; init; }

    public string HpssMode { get; init; } = "none";

    public bool HpssAcceptedByGuardrails { get; init; }

    public string? HpssRejectionReason { get; init; }

    public string BeatEvidenceSource { get; init; } = "full";

    public string[] TempoCandidateSources { get; init; } = ["full"];

    public string SelectedTempoSource { get; init; } = "full";

    public BeatGridShadowDiagnostics? ShadowDiagnostics { get; init; }

    public BeatGridCandidateSet? CandidateSet { get; init; }
}
