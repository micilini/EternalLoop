using EternalLoop.AnalysisEngine.Core.Models;

namespace EternalLoop.AnalysisEngine.Core.Analysis;

internal static class FeatureNovelty
{
    private const float MinimumStandardDeviation = 1e-4f;

    public static double[] BuildFrameNovelty(FeatureMatrix features)
    {
        ArgumentNullException.ThrowIfNull(features);

        var frameCount = Math.Min(features.Mfcc.Length, features.Chroma.Length);
        frameCount = Math.Min(frameCount, features.Rms.Length);
        frameCount = Math.Min(frameCount, features.SpectralFlux.Length);

        if (frameCount < 2)
        {
            return new double[frameCount];
        }

        var vectors = new float[frameCount][];
        for (var frame = 0; frame < frameCount; frame++)
        {
            vectors[frame] = features.Mfcc[frame].Concat(features.Chroma[frame]).ToArray();
        }

        var normalized = ZScoreNormalize(vectors);
        var novelty = new double[frameCount];

        for (var frame = 1; frame < frameCount; frame++)
        {
            var percussiveOnset = SelectPercussiveOnset(features, frame);
            var percussiveRmsDelta = features.PercussiveRms.Length > frame
                ? Math.Max(0.0, features.PercussiveRms[frame] - features.PercussiveRms[frame - 1])
                : 0.0;
            novelty[frame] =
                VectorDistance(normalized[frame], normalized[frame - 1]) +
                0.6 * Math.Max(0.0, SelectOnset(features, frame)) +
                0.45 * Math.Max(0.0, percussiveOnset) +
                0.4 * Math.Abs(features.Rms[frame] - features.Rms[frame - 1]) +
                0.25 * percussiveRmsDelta;
        }

        return MovingAverage(novelty, window: 5);
    }

    private static float SelectOnset(FeatureMatrix features, int frame)
    {
        return features.OnsetEnvelope.Length == features.SpectralFlux.Length && frame < features.OnsetEnvelope.Length
            ? features.OnsetEnvelope[frame]
            : features.SpectralFlux[frame];
    }

    private static float SelectPercussiveOnset(FeatureMatrix features, int frame)
    {
        if (!features.HpssApplied)
        {
            return 0.0f;
        }

        if (features.PercussiveOnsetEnvelope.Length > frame)
        {
            return features.PercussiveOnsetEnvelope[frame];
        }

        return features.PercussiveSpectralFlux.Length > frame ? features.PercussiveSpectralFlux[frame] : 0.0f;
    }

    public static double[] MovingAverage(IReadOnlyList<double> values, int window)
    {
        if (values.Count == 0 || window <= 1)
        {
            return values.ToArray();
        }

        var result = new double[values.Count];
        var radius = window / 2;

        for (var index = 0; index < values.Count; index++)
        {
            var start = Math.Max(0, index - radius);
            var end = Math.Min(values.Count - 1, index + radius);
            var sum = 0.0;

            for (var item = start; item <= end; item++)
            {
                sum += values[item];
            }

            result[index] = sum / (end - start + 1);
        }

        return result;
    }

    public static double Normalize(double value, double min, double max)
    {
        if (max <= min || !double.IsFinite(value))
        {
            return 0.0;
        }

        return Math.Clamp((value - min) / (max - min), 0.0, 1.0);
    }

    public static double Variance(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
        {
            return 0.0;
        }

        var average = values.Average();
        return values.Sum(value => Math.Pow(value - average, 2.0)) / values.Count;
    }

    private static float[][] ZScoreNormalize(float[][] rawVectors)
    {
        var dimensions = rawVectors[0].Length;
        var means = new double[dimensions];
        var standardDeviations = new double[dimensions];

        for (var dimension = 0; dimension < dimensions; dimension++)
        {
            means[dimension] = rawVectors.Average(vector => dimension < vector.Length ? vector[dimension] : 0f);
        }

        for (var dimension = 0; dimension < dimensions; dimension++)
        {
            var variance = rawVectors.Average(vector =>
            {
                var value = dimension < vector.Length ? vector[dimension] : 0f;
                return Math.Pow(value - means[dimension], 2.0);
            });
            standardDeviations[dimension] = Math.Max(MinimumStandardDeviation, Math.Sqrt(variance));
        }

        return rawVectors
            .Select(vector =>
            {
                var normalized = new float[dimensions];
                for (var dimension = 0; dimension < dimensions; dimension++)
                {
                    var value = dimension < vector.Length ? vector[dimension] : 0f;
                    normalized[dimension] = (float)((value - means[dimension]) / standardDeviations[dimension]);
                }

                return normalized;
            })
            .ToArray();
    }

    private static double VectorDistance(float[] left, float[] right)
    {
        var length = Math.Min(left.Length, right.Length);
        var sum = 0.0;

        for (var index = 0; index < length; index++)
        {
            var delta = left[index] - right[index];
            sum += delta * delta;
        }

        return Math.Sqrt(sum / Math.Max(1, length));
    }
}
