namespace EternalLoop.Core.Analysis;

internal static class SpectralFluxExtractor
{
    public static float[] Compute(StftFrame[] frames)
    {
        ArgumentNullException.ThrowIfNull(frames);

        if (frames.Length == 0)
        {
            return [];
        }

        var normalized = frames
            .Select(frame => Normalize(frame.Magnitudes))
            .ToArray();

        var flux = new float[frames.Length];
        flux[0] = 0.0f;

        for (var i = 1; i < normalized.Length; i++)
        {
            float sum = 0.0f;
            var length = Math.Min(normalized[i].Length, normalized[i - 1].Length);

            for (var bin = 0; bin < length; bin++)
            {
                var diff = normalized[i][bin] - normalized[i - 1][bin];

                if (diff > 0.0f)
                {
                    sum += diff;
                }
            }

            flux[i] = sum;
        }

        NormalizeFluxInPlace(flux);

        return flux;
    }

    private static float[] Normalize(float[] magnitudes)
    {
        var result = new float[magnitudes.Length];
        var sum = magnitudes.Sum(value => Math.Abs(value));

        if (sum <= 1e-12f)
        {
            return result;
        }

        for (var i = 0; i < magnitudes.Length; i++)
        {
            result[i] = magnitudes[i] / sum;
        }

        return result;
    }

    private static void NormalizeFluxInPlace(float[] flux)
    {
        var max = flux.Max();

        if (max <= 0.0f)
        {
            return;
        }

        for (var i = 0; i < flux.Length; i++)
        {
            flux[i] /= max;
        }
    }
}
