namespace EternalLoop.AnalysisEngine.Core.Features;

internal static class WindowFunctions
{
    public static double[] Hann(int size)
    {
        if (size <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Window size must be greater than zero.");
        }

        if (size == 1)
        {
            return [1.0];
        }

        var window = new double[size];

        for (var index = 0; index < size; index++)
        {
            window[index] = 0.5 - (0.5 * Math.Cos(2.0 * Math.PI * index / (size - 1)));
        }

        return window;
    }
}
