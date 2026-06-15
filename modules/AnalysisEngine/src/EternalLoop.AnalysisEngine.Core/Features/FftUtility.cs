using System.Numerics;

namespace EternalLoop.AnalysisEngine.Core.Features;

internal static class FftUtility
{
    public static void Forward(Complex[] buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        var length = buffer.Length;

        if (length == 0 || (length & (length - 1)) != 0)
        {
            throw new ArgumentException("FFT buffer length must be a power of two.", nameof(buffer));
        }

        BitReverse(buffer);

        for (var step = 2; step <= length; step <<= 1)
        {
            var angle = -2.0 * Math.PI / step;
            var root = new Complex(Math.Cos(angle), Math.Sin(angle));

            for (var start = 0; start < length; start += step)
            {
                var twiddle = Complex.One;
                var halfStep = step / 2;

                for (var offset = 0; offset < halfStep; offset++)
                {
                    var even = buffer[start + offset];
                    var odd = buffer[start + offset + halfStep] * twiddle;

                    buffer[start + offset] = even + odd;
                    buffer[start + offset + halfStep] = even - odd;

                    twiddle *= root;
                }
            }
        }
    }

    private static void BitReverse(Complex[] buffer)
    {
        var length = buffer.Length;
        var reversed = 0;

        for (var index = 1; index < length; index++)
        {
            var bit = length >> 1;

            while ((reversed & bit) != 0)
            {
                reversed ^= bit;
                bit >>= 1;
            }

            reversed ^= bit;

            if (index < reversed)
            {
                (buffer[index], buffer[reversed]) = (buffer[reversed], buffer[index]);
            }
        }
    }
}
