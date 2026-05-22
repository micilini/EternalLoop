namespace EternalLoop.Core.Analysis;

internal static class MelScale
{
    public static double HertzToMel(double hertz)
    {
        return 2595.0 * Math.Log10(1.0 + hertz / 700.0);
    }

    public static double MelToHertz(double mel)
    {
        return 700.0 * (Math.Pow(10.0, mel / 2595.0) - 1.0);
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

        for (var i = 0; i < melPoints.Length; i++)
        {
            melPoints[i] = minMel + (maxMel - minMel) * i / (filterCount + 1);
        }

        var hzPoints = melPoints.Select(MelToHertz).ToArray();
        var binPoints = hzPoints
            .Select(hz => Math.Clamp((int)Math.Floor((frameSize + 1) * hz / sampleRate), 0, frameSize / 2))
            .ToArray();

        var filters = new double[filterCount][];
        var bins = frameSize / 2 + 1;

        for (var filter = 0; filter < filterCount; filter++)
        {
            var weights = new double[bins];

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

            for (var bin = left; bin < center && bin < bins; bin++)
            {
                weights[bin] = (bin - left) / (double)(center - left);
            }

            for (var bin = center; bin < right && bin < bins; bin++)
            {
                weights[bin] = (right - bin) / (double)(right - center);
            }

            filters[filter] = weights;
        }

        return filters;
    }
}
