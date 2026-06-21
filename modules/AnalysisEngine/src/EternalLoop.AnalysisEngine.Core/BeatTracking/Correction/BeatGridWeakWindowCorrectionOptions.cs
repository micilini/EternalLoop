namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Correction;

public sealed class BeatGridWeakWindowCorrectionOptions
{
    public string CalibrationProfile { get; init; } = "strict-production";
    public BeatGridWeakWindowCorrectionMode Mode { get; init; } = BeatGridWeakWindowCorrectionMode.ExperimentalCandidate;
    public int MaxWindowsToCorrect { get; init; } = 8;
    public int MaxDiagnosticWindows { get; init; } = 12;
    public double MaxWindowDurationSeconds { get; init; } = 20.0;
    public double BoundaryBlendSeconds { get; init; } = 0.25;
    public double MaxAllowedBpmDelta { get; init; } = 20.0;
    public double MaxAllowedCountRatioDelta { get; init; } = 0.25;
    public double MaxAllowedOffsetMs { get; init; } = 120.0;
    public double MinAdvisorStrengthScore { get; init; } = 0.70;
    public double MinCorrectionReadinessScore { get; init; } = 0.72;
    public double MaxCorrectedIntervalCoefficientOfVariation { get; init; } = 0.25;
    public double MaxCorrectedBeatDensityPerSecond { get; init; } = 4.0;
    public double MinCorrectedMedianIntervalSeconds { get; init; } = 0.25;
    public bool RequireFutureCorrectionCandidate { get; init; } = true;
    public bool KeepLegacyOutsideWeakWindows { get; init; } = true;
    public bool AllowBoundaryBlend { get; init; } = true;

    public void Validate()
    {
        if (MaxWindowsToCorrect < 0) throw new ArgumentOutOfRangeException(nameof(MaxWindowsToCorrect));
        if (MaxDiagnosticWindows < 0) throw new ArgumentOutOfRangeException(nameof(MaxDiagnosticWindows));
        if (MaxWindowDurationSeconds <= 0.0) throw new ArgumentOutOfRangeException(nameof(MaxWindowDurationSeconds));
        if (BoundaryBlendSeconds < 0.0) throw new ArgumentOutOfRangeException(nameof(BoundaryBlendSeconds));
        if (MaxAllowedBpmDelta < 0.0) throw new ArgumentOutOfRangeException(nameof(MaxAllowedBpmDelta));
        if (MaxAllowedCountRatioDelta < 0.0) throw new ArgumentOutOfRangeException(nameof(MaxAllowedCountRatioDelta));
        if (MaxAllowedOffsetMs < 0.0) throw new ArgumentOutOfRangeException(nameof(MaxAllowedOffsetMs));
    }
}
