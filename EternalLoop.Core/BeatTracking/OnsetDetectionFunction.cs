namespace EternalLoop.Core.BeatTracking;

public static class OnsetDetectionFunction
{
    public static float[] Build(float[] spectralFlux, int smoothWindow)
    {
        ArgumentNullException.ThrowIfNull(spectralFlux);

        if (spectralFlux.Length == 0)
        {
            return [];
        }

        var nonNegative = new float[spectralFlux.Length];

        for (var i = 0; i < spectralFlux.Length; i++)
        {
            nonNegative[i] = Math.Max(0f, spectralFlux[i]);
        }

        var localWindow = Math.Max(3, smoothWindow * 4);
        var localAverage = MovingAverage(nonNegative, localWindow);

        var whitened = new float[nonNegative.Length];

        for (var i = 0; i < nonNegative.Length; i++)
        {
            whitened[i] = Math.Max(0f, nonNegative[i] - localAverage[i]);
        }

        var smoothed = MovingAverage(whitened, Math.Max(1, smoothWindow));
        var max = smoothed.Max();

        if (max <= 1e-9f)
        {
            return smoothed;
        }

        for (var i = 0; i < smoothed.Length; i++)
        {
            smoothed[i] /= max;
        }

        return smoothed;
    }

    internal static float[] MovingAverage(float[] values, int windowSize)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (values.Length == 0)
        {
            return [];
        }

        var size = Math.Max(1, windowSize);
        var radius = size / 2;
        var result = new float[values.Length];

        for (var i = 0; i < values.Length; i++)
        {
            var start = Math.Max(0, i - radius);
            var end = Math.Min(values.Length - 1, i + radius);
            double sum = 0.0;

            for (var j = start; j <= end; j++)
            {
                sum += values[j];
            }

            result[i] = (float)(sum / Math.Max(1, end - start + 1));
        }

        return result;
    }
}
