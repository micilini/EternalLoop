namespace EternalLoop.AnalysisEngine.Core.Analysis;

public sealed class AnalysisDiagnostics
{
    public bool RequestedAcousticSegmentation { get; init; }

    public bool RequestedBeatMicroSnap { get; init; }

    public bool RequestedAdaptiveTatums { get; init; }

    public bool RequestedStructuralSections { get; init; }

    public bool RequestedEvidenceConfidences { get; init; }

    public string SegmentationMode { get; init; } = "temporal-fallback";

    public string BeatGridMode { get; init; } = "unknown";

    public string TatumMode { get; init; } = "uniform-fallback";

    public string SectionMode { get; init; } = "single-fallback";

    public double NoveltyBoundaryRatio { get; init; }

    public double BeatStdDevRatio { get; init; }

    public double BeatConfidenceVariance { get; init; }

    public double SegmentConfidenceVariance { get; init; }

    public double SectionConfidenceVariance { get; init; }

    public double SelectedTempo { get; init; }

    public double? ForcedTempoBpm { get; init; }

    public IReadOnlyList<BeatTracking.TempoCandidate> TempoCandidates { get; init; } = [];

    public string BarPhaseMode { get; init; } = "phase-zero";

    public int SelectedBarPhase { get; init; }

    public double BarPhaseScore { get; init; }

    public IReadOnlyList<BarPhaseCandidate> BarPhaseCandidates { get; init; } = [];

    public string BeatDriftMode { get; init; } = "none";

    public bool BeatElasticRefinementApplied { get; init; }

    public double BeatElasticMedianShiftMs { get; init; }

    public double BeatElasticP90ShiftMs { get; init; }

    public double BeatElasticIntervalStdDevRatioBefore { get; init; }

    public double BeatElasticIntervalStdDevRatioAfter { get; init; }

    public double BeatElasticOnsetScoreBefore { get; init; }

    public double BeatElasticOnsetScoreAfter { get; init; }

    public bool BeatPiecewiseRefinementApplied { get; init; }

    public string BeatPiecewiseMode { get; init; } = "none";

    public int BeatPiecewiseWindowCount { get; init; }

    public int BeatPiecewiseAcceptedWindows { get; init; }

    public double BeatPiecewiseMeanShiftMs { get; init; }

    public double BeatPiecewiseMaxShiftMs { get; init; }

    public double BeatPiecewiseOnsetScoreBefore { get; init; }

    public double BeatPiecewiseOnsetScoreAfter { get; init; }

    public double BeatPiecewiseRegularityBefore { get; init; }

    public double BeatPiecewiseRegularityAfter { get; init; }

    public string BeatEvidenceMode { get; init; } = "none";

    public IReadOnlyDictionary<string, double> BeatEvidenceWeights { get; init; } = new Dictionary<string, double>();

    public string BeatEvidenceSelectedChannel { get; init; } = "none";

    public double BeatEvidenceMean { get; init; }

    public double BeatEvidenceVariance { get; init; }

    public bool BeatCompositeDpApplied { get; init; }

    public string BeatCompositeDpMode { get; init; } = "none";

    public double BeatCompositeDpEvidenceBefore { get; init; }

    public double BeatCompositeDpEvidenceAfter { get; init; }

    public double BeatCompositeDpRegularityBefore { get; init; }

    public double BeatCompositeDpRegularityAfter { get; init; }

    public bool HpssRequested { get; init; }

    public bool HpssApplied { get; init; }

    public string HpssMode { get; init; } = "none";

    public int HpssTimeKernelFrames { get; init; }

    public int HpssFrequencyKernelBins { get; init; }

    public double HpssMaskPower { get; init; }

    public double HpssPercussiveMargin { get; init; }

    public double HpssHarmonicMargin { get; init; }

    public double HpssPercussiveEnergyRatio { get; init; }

    public double HpssHarmonicEnergyRatio { get; init; }

    public IReadOnlyList<string> TempoCandidateSources { get; init; } = ["full"];

    public string SelectedTempoSource { get; init; } = "full";

    public string BeatEvidenceSource { get; init; } = "full";

    public bool HpssAcceptedByGuardrails { get; init; }

    public string? HpssRejectionReason { get; init; }

    public string SectionFeatureResolution { get; init; } = "unknown";

    public int SectionCandidateCount { get; init; }

    public int SectionSelectedCount { get; init; }

    public double SectionNoveltyMean { get; init; }

    public double SectionNoveltyMax { get; init; }

    public string SectionScaleUsed { get; init; } = "none";

    public double SegmentTargetDensity { get; init; }

    public double SegmentActualDensity { get; init; }

    public int SegmentCandidateCount { get; init; }

    public int SegmentSelectedCount { get; init; }

    public double SegmentNoveltyBoundaryRatio { get; init; }
}
