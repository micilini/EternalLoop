namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Agreement;

public sealed class BeatGridAgreementConfidenceOptions
{
    public double PrimaryToleranceMs { get; init; } = 70.0;

    public double HighAgreementF1 { get; init; } = 0.85;

    public double VeryHighAgreementF1 { get; init; } = 0.93;

    public double MediumAgreementF1 { get; init; } = 0.65;

    public double LowAgreementF1 { get; init; } = 0.40;

    public double MaxHighConfidenceAbsOffsetMs { get; init; } = 60.0;

    public double MaxMediumConfidenceAbsOffsetMs { get; init; } = 120.0;

    public double MaxCountRatioDeltaHigh { get; init; } = 0.15;

    public double MaxCountRatioDeltaMedium { get; init; } = 0.30;

    public double MaxStableOffsetMadMs { get; init; } = 25.0;

    public int WindowBeatCount { get; init; } = 32;

    public int WindowHopBeatCount { get; init; } = 16;

    public int MinWindowBeats { get; init; } = 12;

    public double FutureFusionReadinessMinHighWindowRatio { get; init; } = 0.55;

    public double FutureFusionReadinessMinGlobalF1 { get; init; } = 0.80;

    public void Validate()
    {
        if (PrimaryToleranceMs <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(PrimaryToleranceMs));
        }

        if (WindowBeatCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(WindowBeatCount));
        }

        if (WindowHopBeatCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(WindowHopBeatCount));
        }

        if (MinWindowBeats <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MinWindowBeats));
        }
    }
}
