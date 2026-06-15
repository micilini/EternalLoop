namespace EternalLoop.AnalysisEngine.Core.Features;

internal static class SpectralFluxExtractor
{
    private const float SilenceThreshold = 1e-12f;

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

        for (var frameIndex = 1; frameIndex < normalized.Length; frameIndex++)
        {
            float sum = 0.0f;
            var length = Math.Min(normalized[frameIndex].Length, normalized[frameIndex - 1].Length);

            for (var bin = 0; bin < length; bin++)
            {
                var difference = normalized[frameIndex][bin] - normalized[frameIndex - 1][bin];

                if (difference > 0.0f)
                {
                    sum += difference;
                }
            }

            flux[frameIndex] = sum;
        }

        NormalizeFluxInPlace(flux);
        return flux;
    }

    private static float[] Normalize(float[] magnitudes)
    {
        var result = new float[magnitudes.Length];
        var sum = magnitudes.Sum(value => Math.Abs(value));

        if (sum <= SilenceThreshold)
        {
            return result;
        }

        for (var index = 0; index < magnitudes.Length; index++)
        {
            result[index] = magnitudes[index] / sum;
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

        for (var index = 0; index < flux.Length; index++)
        {
            flux[index] /= max;
        }
    }
}
