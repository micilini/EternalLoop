namespace EternalLoop.AnalysisEngine.Core.Models;

public sealed class FeatureMatrix
{
    public required float[][] Mfcc { get; init; }

    public required float[][] Chroma { get; init; }

    public required float[] SpectralFlux { get; init; }

    public float[] OnsetEnvelope { get; init; } = [];

    public float[] PercussiveSpectralFlux { get; init; } = [];

    public float[] PercussiveOnsetEnvelope { get; init; } = [];

    public float[] PercussiveRms { get; init; } = [];

    public float[] HarmonicSpectralFlux { get; init; } = [];

    public float[] HarmonicOnsetEnvelope { get; init; } = [];

    public float[] HarmonicRms { get; init; } = [];

    public bool HpssApplied { get; init; }

    public string HpssMode { get; init; } = "none";

    public double HpssPercussiveEnergyRatio { get; init; }

    public double HpssHarmonicEnergyRatio { get; init; }

    public required float[] Rms { get; init; }

    public required int HopLengthSamples { get; init; }

    public required int FrameSizeSamples { get; init; }

    public required int SampleRate { get; init; }
}
