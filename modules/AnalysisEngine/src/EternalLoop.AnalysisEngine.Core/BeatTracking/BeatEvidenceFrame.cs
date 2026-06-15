namespace EternalLoop.AnalysisEngine.Core.BeatTracking;

public sealed record BeatEvidenceFrame(
    double Onset,
    double LogMelOnset,
    double LowBandOnset,
    double MidBandOnset,
    double HighBandOnset,
    double RmsDelta,
    double MfccDelta,
    double ChromaDelta,
    double Novelty,
    double PercussiveLogMelOnset,
    double PercussiveLowBandOnset,
    double PercussiveRmsDelta,
    double PercussiveSpectralFlux,
    double HarmonicOnset,
    double Composite);

public sealed record CompositeBeatEvidence(
    IReadOnlyList<BeatEvidenceFrame> Frames,
    float[] Composite,
    IReadOnlyDictionary<string, double> Weights);
