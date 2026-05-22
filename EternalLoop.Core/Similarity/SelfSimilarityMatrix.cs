using EternalLoop.Contracts.Models;

namespace EternalLoop.Core.Similarity;

public static class SelfSimilarityMatrix
{
    private const double SimilarityEpsilon = 1e-12;
    private const double MaximumMetricPenaltyStrength = 0.5;
    private const double EqualBaseWeight = 1.0 / 3.0;

    public static double[,] Compute(
        IReadOnlyList<Beat> beats,
        double timbreWeight,
        double pitchWeight,
        double loudnessWeight,
        double barPositionWeight)
    {
        ArgumentNullException.ThrowIfNull(beats);

        if (beats.Count == 0)
        {
            return new double[0, 0];
        }

        var weights = NormalizeBaseWeights(timbreWeight, pitchWeight, loudnessWeight);
        var metricPenaltyStrength = Math.Clamp(barPositionWeight, 0.0, MaximumMetricPenaltyStrength);
        var matrix = new double[beats.Count, beats.Count];

        for (var i = 0; i < beats.Count; i++)
        {
            matrix[i, i] = 1.0;

            for (var j = i + 1; j < beats.Count; j++)
            {
                var timbre = CosineSimilarity(beats[i].Timbre, beats[j].Timbre);
                var pitch = CosineSimilarity(beats[i].Pitches, beats[j].Pitches);
                var loudness = CosineSimilarity(beats[i].Loudness, beats[j].Loudness);
                var barPosition = CosineSimilarityOrNeutral(beats[i].BarPosition, beats[j].BarPosition);

                var baseScore = Math.Clamp(
                    timbre * weights.Timbre +
                    pitch * weights.Pitch +
                    loudness * weights.Loudness,
                    0.0,
                    1.0);

                var metricPenalty = 1.0 - (metricPenaltyStrength * (1.0 - barPosition));
                var score = Math.Clamp(baseScore * metricPenalty, 0.0, 1.0);

                matrix[i, j] = score;
                matrix[j, i] = score;
            }
        }

        return matrix;
    }

    private static double CosineSimilarity(float[]? a, float[]? b)
    {
        if (!TryCosineSimilarity(a, b, out var similarity))
        {
            return 0.0;
        }

        return similarity;
    }

    private static double CosineSimilarityOrNeutral(float[]? a, float[]? b)
    {
        if (!TryCosineSimilarity(a, b, out var similarity))
        {
            return 1.0;
        }

        return similarity;
    }

    private static bool TryCosineSimilarity(float[]? a, float[]? b, out double similarity)
    {
        similarity = 0.0;

        if (a is null || b is null)
        {
            return false;
        }

        var length = Math.Min(a.Length, b.Length);
        if (length == 0)
        {
            return false;
        }

        double dot = 0.0;
        double aNorm = 0.0;
        double bNorm = 0.0;

        for (var i = 0; i < length; i++)
        {
            dot += a[i] * b[i];
            aNorm += a[i] * a[i];
            bNorm += b[i] * b[i];
        }

        var denominator = Math.Sqrt(aNorm) * Math.Sqrt(bNorm);
        if (denominator <= SimilarityEpsilon)
        {
            return false;
        }

        similarity = Math.Clamp(dot / denominator, 0.0, 1.0);
        return true;
    }

    private static (double Timbre, double Pitch, double Loudness) NormalizeBaseWeights(
        double timbreWeight,
        double pitchWeight,
        double loudnessWeight)
    {
        var timbre = Math.Max(0.0, timbreWeight);
        var pitch = Math.Max(0.0, pitchWeight);
        var loudness = Math.Max(0.0, loudnessWeight);
        var sum = timbre + pitch + loudness;

        if (sum <= SimilarityEpsilon)
        {
            return (EqualBaseWeight, EqualBaseWeight, EqualBaseWeight);
        }

        return (timbre / sum, pitch / sum, loudness / sum);
    }
}
