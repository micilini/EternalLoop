namespace EternalLoop.AnalysisEngine.Core.BeatTracking;

public static class OnsetDetectionFunction
{
    private const float MinimumEnergy = 1e-9f;

    public static float[] Build(float[] spectralFlux, int smoothWindow)
    {
        ArgumentNullException.ThrowIfNull(spectralFlux);

        if (spectralFlux.Length == 0)
        {
            return [];
        }

        var nonNegative = new float[spectralFlux.Length];

        for (var index = 0; index < spectralFlux.Length; index++)
        {
            nonNegative[index] = Math.Max(0f, spectralFlux[index]);
        }

        var localWindow = Math.Max(3, smoothWindow * 4);
        var localAverage = MovingAverage(nonNegative, localWindow);
        var whitened = new float[nonNegative.Length];

        for (var index = 0; index < nonNegative.Length; index++)
        {
            whitened[index] = Math.Max(0f, nonNegative[index] - localAverage[index]);
        }

        var smoothed = MovingAverage(whitened, Math.Max(1, smoothWindow));
        var max = smoothed.Max();

        if (max <= MinimumEnergy)
        {
            return smoothed;
        }

        for (var index = 0; index < smoothed.Length; index++)
        {
            smoothed[index] /= max;
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

        for (var index = 0; index < values.Length; index++)
        {
            var start = Math.Max(0, index - radius);
            var end = Math.Min(values.Length - 1, index + radius);
            double sum = 0.0;

            for (var cursor = start; cursor <= end; cursor++)
            {
                sum += values[cursor];
            }

            result[index] = (float)(sum / Math.Max(1, end - start + 1));
        }

        return result;
    }
}
