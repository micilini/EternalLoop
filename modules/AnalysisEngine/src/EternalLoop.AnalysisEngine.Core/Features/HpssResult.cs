namespace EternalLoop.AnalysisEngine.Core.Features;

internal sealed class HpssResult
{
    public required StftFrame[] HarmonicFrames { get; init; }

    public required StftFrame[] PercussiveFrames { get; init; }

    public required double HarmonicEnergyRatio { get; init; }

    public required double PercussiveEnergyRatio { get; init; }

    public required string Mode { get; init; }
}
