namespace EternalLoop.AnalysisEngine.Core.BeatTracking;

public sealed class BeatTrackingResult
{
    public required double EstimatedBpm { get; init; }

    public required double[] BeatTimes { get; init; }

    public required double[] Confidences { get; init; }

    public string BeatGridMode { get; init; } = "unknown";

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
}
