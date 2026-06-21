namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Alignment;

public sealed class BeatGridPhaseAlignmentOptions
{
    public double MinOffsetMs { get; init; } = -250.0;

    public double MaxOffsetMs { get; init; } = 250.0;

    public double OffsetStepMs { get; init; } = 5.0;

    public double PrimaryToleranceMs { get; init; } = 70.0;

    public double[] AgreementTolerancesMs { get; init; } = [50.0, 70.0, 100.0];

    public int LocalWindowBeatCount { get; init; } = 32;

    public int LocalWindowHopBeatCount { get; init; } = 16;

    public int MinBeatsForLocalWindow { get; init; } = 12;

    public double MaxReliableAbsOffsetMs { get; init; } = 180.0;

    public double MaxReliableCountRatioDelta { get; init; } = 0.35;

    public double HighConfidenceMinBestF1 { get; init; } = 0.85;

    public double MediumConfidenceMinBestF1 { get; init; } = 0.65;

    public double LowConfidenceMinBestF1 { get; init; } = 0.40;

    public double StableOffsetMaxMadMs { get; init; } = 20.0;

    public void Validate()
    {
        if (MinOffsetMs >= MaxOffsetMs)
        {
            throw new ArgumentOutOfRangeException(nameof(MinOffsetMs));
        }

        if (OffsetStepMs <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(OffsetStepMs));
        }

        if (PrimaryToleranceMs <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(PrimaryToleranceMs));
        }

        if (AgreementTolerancesMs.Length == 0 || AgreementTolerancesMs.Any(tolerance => tolerance <= 0.0))
        {
            throw new ArgumentOutOfRangeException(nameof(AgreementTolerancesMs));
        }

        if (LocalWindowBeatCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(LocalWindowBeatCount));
        }

        if (LocalWindowHopBeatCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(LocalWindowHopBeatCount));
        }

        if (MinBeatsForLocalWindow <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MinBeatsForLocalWindow));
        }
    }
}
