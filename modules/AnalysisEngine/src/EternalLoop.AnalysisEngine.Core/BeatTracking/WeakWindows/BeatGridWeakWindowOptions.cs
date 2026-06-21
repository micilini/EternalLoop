namespace EternalLoop.AnalysisEngine.Core.BeatTracking.WeakWindows;

public sealed class BeatGridWeakWindowOptions
{
    public int WindowBeatCount { get; init; } = 32;
    public int WindowHopBeatCount { get; init; } = 16;
    public int MinWindowBeats { get; init; } = 12;
    public double MaxLegacyIntervalCoefficientOfVariation { get; init; } = 0.18;
    public double StrongLegacyIntervalCoefficientOfVariation { get; init; } = 0.10;
    public double MaxAdvisorIntervalCoefficientOfVariation { get; init; } = 0.14;
    public double MinAdvisorAgreementF1_70Ms { get; init; } = 0.70;
    public double StrongAdvisorAgreementF1_70Ms { get; init; } = 0.85;
    public double MaxAdvisorAbsOffsetMs { get; init; } = 120.0;
    public double StrongAdvisorAbsOffsetMs { get; init; } = 60.0;
    public double MaxCountRatioDelta { get; init; } = 0.35;
    public double StrongCountRatioDelta { get; init; } = 0.15;
    public double MinWeaknessScore { get; init; } = 0.55;
    public double MinAdvisorStrengthScore { get; init; } = 0.60;
    public double MinExperimentalCorrectionReadinessScore { get; init; } = 0.72;

    public void Validate()
    {
        if (WindowBeatCount <= 0) throw new ArgumentOutOfRangeException(nameof(WindowBeatCount));
        if (WindowHopBeatCount <= 0) throw new ArgumentOutOfRangeException(nameof(WindowHopBeatCount));
        if (MinWindowBeats <= 0) throw new ArgumentOutOfRangeException(nameof(MinWindowBeats));
        if (MinWeaknessScore is < 0.0 or > 1.0) throw new ArgumentOutOfRangeException(nameof(MinWeaknessScore));
        if (MinAdvisorStrengthScore is < 0.0 or > 1.0) throw new ArgumentOutOfRangeException(nameof(MinAdvisorStrengthScore));
    }
}
