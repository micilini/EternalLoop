namespace EternalLoop.AnalysisEngine.Core.Features;

internal static class MelScale
{
    private const double MelScaleMultiplier = 2595.0;
    private const double MelScaleReference = 700.0;

    public static double HertzToMel(double hertz)
    {
        return MelScaleMultiplier * Math.Log10(1.0 + (hertz / MelScaleReference));
    }

    public static double MelToHertz(double mel)
    {
        return MelScaleReference * (Math.Pow(10.0, mel / MelScaleMultiplier) - 1.0);
    }

    public static double[][] CreateFilterBank(
        int sampleRate,
        int frameSize,
        int filterCount,
        double minFrequency = 20.0,
        double? maxFrequency = null)
    {
        if (sampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate));
        }

        if (frameSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frameSize));
        }

        if (filterCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(filterCount));
        }

        var nyquist = sampleRate / 2.0;
        var maxHz = Math.Min(maxFrequency ?? nyquist, nyquist);
        var minMel = HertzToMel(minFrequency);
        var maxMel = HertzToMel(maxHz);
        var melPoints = new double[filterCount + 2];

        for (var index = 0; index < melPoints.Length; index++)
        {
            melPoints[index] = minMel + ((maxMel - minMel) * index / (filterCount + 1));
        }

        var binPoints = melPoints
            .Select(MelToHertz)
            .Select(hertz => Math.Clamp((int)Math.Floor((frameSize + 1) * hertz / sampleRate), 0, frameSize / 2))
            .ToArray();

        var filters = new double[filterCount][];
        var binCount = (frameSize / 2) + 1;

        for (var filter = 0; filter < filterCount; filter++)
        {
            var weights = new double[binCount];
            var left = binPoints[filter];
            var center = binPoints[filter + 1];
            var right = binPoints[filter + 2];

            if (center == left)
            {
                center++;
            }

            if (right == center)
            {
                right++;
            }

            for (var bin = left; bin < center && bin < binCount; bin++)
            {
                weights[bin] = (bin - left) / (double)(center - left);
            }

            for (var bin = center; bin < right && bin < binCount; bin++)
            {
                weights[bin] = (right - bin) / (double)(right - center);
            }

            filters[filter] = weights;
        }

        return filters;
    }
}
