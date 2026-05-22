namespace EternalLoop.Core.Analysis;

internal sealed class StftFrame
{
    public required float[] Magnitudes { get; init; }

    public required float[] PowerSpectrum { get; init; }
}
