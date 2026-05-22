namespace EternalLoop.Contracts.Options;

public sealed class BeatTrackingOptions
{
    public double MinBpm { get; init; } = 60;

    public double MaxBpm { get; init; } = 200;

    public double TightnessLambda { get; init; } = 100;

    public int OdfSmoothWindow { get; init; } = 7;
}
