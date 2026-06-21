namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Hybrid;

public sealed class BeatGridHybridSelectionOptions
{
    public string CalibrationProfileName { get; init; } = "strict-production";

    public bool AllowCorrectedExperimentalAsFinal { get; init; } = true;

    public double MaxCorrectedVsLegacyCountRatioDelta { get; init; } = 0.25;

    public double MaxCorrectedBpm { get; init; } = 200.0;

    public double MinCorrectedMedianIntervalSeconds { get; init; } = 0.25;

    public double MaxCorrectedBeatDensityPerSecond { get; init; } = 4.0;

    public int MinAcceptedCorrectionWindows { get; init; } = 1;

    public bool RequireCorrectionDiagnostics { get; init; } = true;

    public bool RequireCorrectedCandidateNotDense { get; init; } = true;

    public bool RequireMadmomClaimNotEvaluated { get; init; } = true;

    public void Validate()
    {
        if (MaxCorrectedVsLegacyCountRatioDelta < 0.0) throw new ArgumentOutOfRangeException(nameof(MaxCorrectedVsLegacyCountRatioDelta));
        if (MaxCorrectedBpm <= 0.0) throw new ArgumentOutOfRangeException(nameof(MaxCorrectedBpm));
        if (MinCorrectedMedianIntervalSeconds <= 0.0) throw new ArgumentOutOfRangeException(nameof(MinCorrectedMedianIntervalSeconds));
        if (MaxCorrectedBeatDensityPerSecond <= 0.0) throw new ArgumentOutOfRangeException(nameof(MaxCorrectedBeatDensityPerSecond));
        if (MinAcceptedCorrectionWindows < 0) throw new ArgumentOutOfRangeException(nameof(MinAcceptedCorrectionWindows));
    }
}
