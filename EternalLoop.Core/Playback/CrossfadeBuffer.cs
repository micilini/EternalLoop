using EternalLoop.Contracts.Enums;

namespace EternalLoop.Core.Playback;

internal static class CrossfadeBuffer
{
    public static void Mix(
        ReadOnlySpan<float> fadeOut,
        Span<float> fadeIn,
        int fadeOffset,
        CrossfadeShape shape)
    {
        if (fadeOut.Length == 0 || fadeIn.Length == 0)
        {
            return;
        }

        var available = Math.Min(fadeOut.Length - fadeOffset, fadeIn.Length);
        if (available <= 0)
        {
            return;
        }

        var total = fadeOut.Length;

        for (var i = 0; i < available; i++)
        {
            var position = fadeOffset + i;
            var t = Math.Clamp((position + 1) / (double)total, 0.0, 1.0);

            float gainOut;
            float gainIn;

            if (shape == CrossfadeShape.EqualPower)
            {
                gainOut = (float)Math.Cos(t * Math.PI / 2.0);
                gainIn = (float)Math.Sin(t * Math.PI / 2.0);
            }
            else
            {
                gainOut = (float)(1.0 - t);
                gainIn = (float)t;
            }

            fadeIn[i] = fadeOut[position] * gainOut + fadeIn[i] * gainIn;
        }
    }
}
