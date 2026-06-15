namespace EternalLoop.AnalysisEngine.Core.Options;

public sealed class HpssOptions
{
    public const int DefaultTimeMedianKernelFrames = 17;

    public const int DefaultFrequencyMedianKernelBins = 17;

    public const double DefaultMaskPower = 1.0;

    public const double DefaultPercussiveMargin = 4.0;

    public const double DefaultHarmonicMargin = 1.0;

    public bool UseHpss { get; init; }

    public int TimeMedianKernelFrames { get; init; } = DefaultTimeMedianKernelFrames;

    public int FrequencyMedianKernelBins { get; init; } = DefaultFrequencyMedianKernelBins;

    public double MaskPower { get; init; } = DefaultMaskPower;

    public double PercussiveMargin { get; init; } = DefaultPercussiveMargin;

    public double HarmonicMargin { get; init; } = DefaultHarmonicMargin;

    public bool UseResidual { get; init; }
}
