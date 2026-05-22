namespace EternalLoop.Contracts.Models;

public sealed class FeatureMatrix
{
    public required float[][] Mfcc { get; init; }

    public required float[][] Chroma { get; init; }

    public required float[] SpectralFlux { get; init; }

    public required float[] Rms { get; init; }

    public required int HopLengthSamples { get; init; }

    public required int FrameSizeSamples { get; init; }
}
