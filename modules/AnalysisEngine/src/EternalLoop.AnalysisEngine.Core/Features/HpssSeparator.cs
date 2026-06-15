using EternalLoop.AnalysisEngine.Core.Options;

namespace EternalLoop.AnalysisEngine.Core.Features;

internal static class HpssSeparator
{
    private const double Epsilon = 1e-12;

    public static HpssResult Separate(StftFrame[] frames, HpssOptions options)
    {
        ArgumentNullException.ThrowIfNull(frames);
        ArgumentNullException.ThrowIfNull(options);

        if (frames.Length == 0)
        {
            return new HpssResult
            {
                HarmonicFrames = [],
                PercussiveFrames = [],
                HarmonicEnergyRatio = 0.0,
                PercussiveEnergyRatio = 0.0,
                Mode = "empty"
            };
        }

        var frameCount = frames.Length;
        var binCount = frames[0].Magnitudes.Length;
        if (binCount == 0)
        {
            return EmptyLike(frames, "empty-bins");
        }

        var timeRadius = Math.Max(0, NormalizeKernel(options.TimeMedianKernelFrames) / 2);
        var frequencyRadius = Math.Max(0, NormalizeKernel(options.FrequencyMedianKernelBins) / 2);
        var harmonicFrames = new StftFrame[frameCount];
        var percussiveFrames = new StftFrame[frameCount];
        var totalEnergy = 0.0;
        var harmonicEnergy = 0.0;
        var percussiveEnergy = 0.0;

        for (var frame = 0; frame < frameCount; frame++)
        {
            var harmonicMagnitudes = new float[binCount];
            var percussiveMagnitudes = new float[binCount];
            var harmonicPower = new float[binCount];
            var percussivePower = new float[binCount];

            for (var bin = 0; bin < binCount; bin++)
            {
                var magnitude = Math.Max(0.0, frames[frame].Magnitudes[bin]);
                var harmonicEstimate = MedianAcrossTime(frames, frame, bin, timeRadius);
                var percussiveEstimate = MedianAcrossFrequency(frames, frame, bin, frequencyRadius);

                var harmonicMaskSource = Math.Pow(Math.Max(0.0, harmonicEstimate / Math.Max(Epsilon, options.HarmonicMargin)), options.MaskPower);
                var percussiveMaskSource = Math.Pow(Math.Max(0.0, percussiveEstimate / Math.Max(Epsilon, options.PercussiveMargin)), options.MaskPower);
                var denominator = harmonicMaskSource + percussiveMaskSource + Epsilon;
                var harmonicMask = harmonicMaskSource / denominator;
                var percussiveMask = percussiveMaskSource / denominator;

                var h = magnitude * harmonicMask;
                var p = magnitude * percussiveMask;
                harmonicMagnitudes[bin] = (float)h;
                percussiveMagnitudes[bin] = (float)p;
                harmonicPower[bin] = (float)(h * h);
                percussivePower[bin] = (float)(p * p);
                totalEnergy += magnitude * magnitude;
                harmonicEnergy += h * h;
                percussiveEnergy += p * p;
            }

            harmonicFrames[frame] = new StftFrame
            {
                Magnitudes = harmonicMagnitudes,
                PowerSpectrum = harmonicPower
            };
            percussiveFrames[frame] = new StftFrame
            {
                Magnitudes = percussiveMagnitudes,
                PowerSpectrum = percussivePower
            };
        }

        return new HpssResult
        {
            HarmonicFrames = harmonicFrames,
            PercussiveFrames = percussiveFrames,
            HarmonicEnergyRatio = totalEnergy > Epsilon ? harmonicEnergy / totalEnergy : 0.0,
            PercussiveEnergyRatio = totalEnergy > Epsilon ? percussiveEnergy / totalEnergy : 0.0,
            Mode = "median-soft-mask"
        };
    }

    private static HpssResult EmptyLike(StftFrame[] frames, string mode)
    {
        return new HpssResult
        {
            HarmonicFrames = frames,
            PercussiveFrames = frames,
            HarmonicEnergyRatio = 0.0,
            PercussiveEnergyRatio = 0.0,
            Mode = mode
        };
    }

    private static int NormalizeKernel(int kernel)
    {
        if (kernel <= 1)
        {
            return 1;
        }

        return kernel % 2 == 0 ? kernel + 1 : kernel;
    }

    private static double MedianAcrossTime(StftFrame[] frames, int centerFrame, int bin, int radius)
    {
        var start = Math.Max(0, centerFrame - radius);
        var end = Math.Min(frames.Length - 1, centerFrame + radius);
        var values = new double[end - start + 1];

        for (var index = start; index <= end; index++)
        {
            values[index - start] = bin < frames[index].Magnitudes.Length ? frames[index].Magnitudes[bin] : 0.0;
        }

        return Median(values);
    }

    private static double MedianAcrossFrequency(StftFrame[] frames, int frame, int centerBin, int radius)
    {
        var magnitudes = frames[frame].Magnitudes;
        var start = Math.Max(0, centerBin - radius);
        var end = Math.Min(magnitudes.Length - 1, centerBin + radius);
        var values = new double[end - start + 1];

        for (var index = start; index <= end; index++)
        {
            values[index - start] = magnitudes[index];
        }

        return Median(values);
    }

    private static double Median(double[] values)
    {
        if (values.Length == 0)
        {
            return 0.0;
        }

        Array.Sort(values);
        var middle = values.Length / 2;
        return values.Length % 2 == 1
            ? values[middle]
            : (values[middle - 1] + values[middle]) / 2.0;
    }
}
