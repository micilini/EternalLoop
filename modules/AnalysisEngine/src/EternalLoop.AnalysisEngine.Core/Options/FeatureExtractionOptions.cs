namespace EternalLoop.AnalysisEngine.Core.Options;

public sealed class FeatureExtractionOptions
{
    public const int DefaultFrameSize = 2048;

    public const int DefaultHopLength = 512;

    public const int DefaultMfccCount = 13;

    public const int DefaultFilterBankSize = 26;

    public const double DefaultPreEmphasis = 0.97;

    public int FrameSize { get; init; } = DefaultFrameSize;

    public int HopLength { get; init; } = DefaultHopLength;

    public int MfccCount { get; init; } = DefaultMfccCount;

    public bool ComputeDeltas { get; init; } = true;

    public int FilterBankSize { get; init; } = DefaultFilterBankSize;

    public double PreEmphasis { get; init; } = DefaultPreEmphasis;

    public HpssOptions Hpss { get; init; } = new();
}
