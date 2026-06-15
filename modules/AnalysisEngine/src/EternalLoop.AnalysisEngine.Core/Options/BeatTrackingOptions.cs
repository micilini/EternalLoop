namespace EternalLoop.AnalysisEngine.Core.Options;

public sealed class BeatTrackingOptions
{
    public const double DefaultMinBpm = 60.0;

    public const double DefaultMaxBpm = 200.0;

    public const double DefaultTightnessLambda = 100.0;

    public const int DefaultOdfSmoothWindow = 7;

    public const double DefaultTempoCenterBpm = 120.0;

    public const double DefaultTempoPriorStdOctaves = 1.4;

    public const double DefaultHalfTimeCompetitivenessThreshold = 0.60;

    public const double DefaultBeatSnapWindowRatio = 0.10;

    public const double DefaultBeatEvidenceLogMelOnsetWeight = 0.35;

    public const double DefaultBeatEvidenceLowBandOnsetWeight = 0.20;

    public const double DefaultBeatEvidenceMidBandOnsetWeight = 0.15;

    public const double DefaultBeatEvidenceHighBandOnsetWeight = 0.0;

    public const double DefaultBeatEvidenceRmsDeltaWeight = 0.10;

    public const double DefaultBeatEvidenceMfccDeltaWeight = 0.10;

    public const double DefaultBeatEvidenceChromaDeltaWeight = 0.05;

    public const double DefaultBeatEvidenceNoveltyWeight = 0.05;

    public const double DefaultFullMixOnsetWeight = 0.0;

    public const double DefaultPercussiveOnsetWeight = 1.0;

    public const double DefaultHarmonicOnsetWeight = 0.0;

    public double MinBpm { get; init; } = DefaultMinBpm;

    public double MaxBpm { get; init; } = DefaultMaxBpm;

    public double TightnessLambda { get; init; } = DefaultTightnessLambda;

    public int OdfSmoothWindow { get; init; } = DefaultOdfSmoothWindow;

    public double TempoCenterBpm { get; init; } = DefaultTempoCenterBpm;

    public double TempoPriorStdOctaves { get; init; } = DefaultTempoPriorStdOctaves;

    public double HalfTimeCompetitivenessThreshold { get; init; } = DefaultHalfTimeCompetitivenessThreshold;

    public double BeatSnapWindowRatio { get; init; } = DefaultBeatSnapWindowRatio;

    public double? ForcedTempoBpm { get; init; }

    public bool UseGridTempoSelector { get; init; }

    public bool EnableElasticBeatGrid { get; init; }

    public double ElasticSearchWindowRatio { get; init; } = 0.18;

    public double ElasticMaxShiftMs { get; init; } = 130.0;

    public bool EnablePiecewiseBeatGrid { get; init; }

    public int PiecewiseWindowBeats { get; init; } = 32;

    public int PiecewiseWindowHopBeats { get; init; } = 16;

    public double PiecewiseMaxOffsetBeatRatio { get; init; } = 0.45;

    public double PiecewiseOffsetStepBeatRatio { get; init; } = 0.05;

    public double PiecewiseTransitionPenalty { get; init; } = 0.35;

    public double PiecewiseMinOnsetGain { get; init; } = 0.015;

    public double PiecewiseMaxMedianShiftMs { get; init; } = 180.0;

    public double PiecewiseMaxSingleShiftMs { get; init; } = 260.0;

    public bool EnableCompositeBeatTracking { get; init; }

    public bool UseHpss { get; init; }

    public string HpssMode { get; init; } = "percussive-beat-only";

    public int HpssTimeMedianKernelFrames { get; init; } = HpssOptions.DefaultTimeMedianKernelFrames;

    public int HpssFrequencyMedianKernelBins { get; init; } = HpssOptions.DefaultFrequencyMedianKernelBins;

    public double HpssMaskPower { get; init; } = HpssOptions.DefaultMaskPower;

    public double HpssPercussiveMargin { get; init; } = HpssOptions.DefaultPercussiveMargin;

    public double HpssHarmonicMargin { get; init; } = HpssOptions.DefaultHarmonicMargin;

    public double FullMixOnsetWeight { get; init; } = DefaultFullMixOnsetWeight;

    public double PercussiveOnsetWeight { get; init; } = DefaultPercussiveOnsetWeight;

    public double HarmonicOnsetWeight { get; init; } = DefaultHarmonicOnsetWeight;

    public double BeatEvidenceLogMelOnsetWeight { get; init; } = DefaultBeatEvidenceLogMelOnsetWeight;

    public double BeatEvidenceLowBandOnsetWeight { get; init; } = DefaultBeatEvidenceLowBandOnsetWeight;

    public double BeatEvidenceMidBandOnsetWeight { get; init; } = DefaultBeatEvidenceMidBandOnsetWeight;

    public double BeatEvidenceHighBandOnsetWeight { get; init; } = DefaultBeatEvidenceHighBandOnsetWeight;

    public double BeatEvidenceRmsDeltaWeight { get; init; } = DefaultBeatEvidenceRmsDeltaWeight;

    public double BeatEvidenceMfccDeltaWeight { get; init; } = DefaultBeatEvidenceMfccDeltaWeight;

    public double BeatEvidenceChromaDeltaWeight { get; init; } = DefaultBeatEvidenceChromaDeltaWeight;

    public double BeatEvidenceNoveltyWeight { get; init; } = DefaultBeatEvidenceNoveltyWeight;

    public bool BeatMicroSnap { get; init; }

    public bool EvidenceConfidences { get; init; }
}
