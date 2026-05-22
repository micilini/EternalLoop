namespace EternalLoop.Core.Analysis;

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

        for (var n = 0; n < size; n++)
        {
            window[n] = 0.5 - 0.5 * Math.Cos(2.0 * Math.PI * n / (size - 1));
        }

        return window;
    }
}
