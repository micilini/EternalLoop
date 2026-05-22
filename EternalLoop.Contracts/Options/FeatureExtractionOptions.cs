namespace EternalLoop.Contracts.Options;

public sealed class FeatureExtractionOptions
{
    public int FrameSize { get; init; } = 2048;

    public int HopLength { get; init; } = 512;

    public int MfccCount { get; init; } = 13;

    public bool ComputeDeltas { get; init; } = true;

    public int FilterBankSize { get; init; } = 26;

    public double PreEmphasis { get; init; } = 0.97;
}
