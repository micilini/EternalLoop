using System.Numerics;

namespace EternalLoop.Core.Analysis;

internal static class FftUtility
{
    public static void Forward(Complex[] buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        var n = buffer.Length;

        if (n == 0 || (n & (n - 1)) != 0)
        {
            throw new ArgumentException("FFT buffer length must be a power of two.", nameof(buffer));
        }

        BitReverse(buffer);

        for (var length = 2; length <= n; length <<= 1)
        {
            var angle = -2.0 * Math.PI / length;
            var wLength = new Complex(Math.Cos(angle), Math.Sin(angle));

            for (var i = 0; i < n; i += length)
            {
                var w = Complex.One;
                var halfLength = length / 2;

                for (var j = 0; j < halfLength; j++)
                {
                    var even = buffer[i + j];
                    var odd = buffer[i + j + halfLength] * w;

                    buffer[i + j] = even + odd;
                    buffer[i + j + halfLength] = even - odd;

                    w *= wLength;
                }
            }
        }
    }

    private static void BitReverse(Complex[] buffer)
    {
        var n = buffer.Length;
        var j = 0;

        for (var i = 1; i < n; i++)
        {
            var bit = n >> 1;

            while ((j & bit) != 0)
            {
                j ^= bit;
                bit >>= 1;
            }

            j ^= bit;

            if (i < j)
            {
                (buffer[i], buffer[j]) = (buffer[j], buffer[i]);
            }
        }
    }
}
