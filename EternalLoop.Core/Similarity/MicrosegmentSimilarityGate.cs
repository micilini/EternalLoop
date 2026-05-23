using EternalLoop.Contracts.Models;

namespace EternalLoop.Core.Similarity;

public static class MicrosegmentSimilarityGate
{
    private const double SimilarityEpsilon = 0.000001;
    private const double TimbreWeight = 0.35;
    private const double PitchWeight = 0.35;
    private const double LoudnessWeight = 0.20;
    private const double FluxWeight = 0.10;
    private const double AverageWeight = 0.70;
    private const double WorstSegmentWeight = 0.30;

    public static bool TryApply(
        double baseScore,
        BeatMicroFingerprint? source,
        BeatMicroFingerprint? destination,
        int requestedMicrosegmentCount,
        double penaltyStartThreshold,
        double rejectionThreshold,
        double penaltyStrength,
        out double adjustedScore)
    {
        adjustedScore = double.IsFinite(baseScore) ? Math.Clamp(baseScore, 0.0, 1.0) : 0.0;

        if (source?.Microsegments is null ||
            destination?.Microsegments is null ||
            source.Microsegments.Count == 0 ||
            destination.Microsegments.Count == 0)
        {
            return true;
        }

        var alignedCount = Math.Min(
            Math.Min(source.Microsegments.Count, destination.Microsegments.Count),
            Math.Max(0, requestedMicrosegmentCount));

        if (alignedCount <= 0)
        {
            return true;
        }

        var sum = 0.0;
        var worst = 1.0;

        for (var i = 0; i < alignedCount; i++)
        {
            var segmentScore = ComputeSegmentSimilarity(source.Microsegments[i], destination.Microsegments[i]);
            sum += segmentScore;
            if (segmentScore < worst)
            {
                worst = segmentScore;
            }
        }

        var average = sum / alignedCount;
        var microScore = Math.Clamp((average * AverageWeight) + (worst * WorstSegmentWeight), 0.0, 1.0);
        var penaltyStart = Math.Clamp(penaltyStartThreshold, 0.0, 1.0);
        var rejection = Math.Clamp(rejectionThreshold, 0.0, penaltyStart);
        var strength = Math.Clamp(penaltyStrength, 0.0, 1.0);

        if (microScore < rejection)
        {
            adjustedScore = 0.0;
            return false;
        }

        if (microScore < penaltyStart && strength > 0.0)
        {
            var severity = (penaltyStart - microScore) / Math.Max(SimilarityEpsilon, penaltyStart - rejection);
            var multiplier = 1.0 - (Math.Clamp(severity, 0.0, 1.0) * strength);
            adjustedScore = Math.Clamp(adjustedScore * multiplier, 0.0, adjustedScore);
        }

        return true;
    }

    private static double ComputeSegmentSimilarity(BeatMicrosegment source, BeatMicrosegment destination)
    {
        var timbre = CosineSimilarity(source.Timbre, destination.Timbre);
        var pitches = CosineSimilarity(source.Pitches, destination.Pitches);
        var loudness = CosineSimilarity(source.Loudness, destination.Loudness);
        var flux = 1.0 - Math.Abs(SanitizeScalar(source.Flux) - SanitizeScalar(destination.Flux));

        return Math.Clamp(
            (timbre * TimbreWeight) +
            (pitches * PitchWeight) +
            (loudness * LoudnessWeight) +
            (Math.Clamp(flux, 0.0, 1.0) * FluxWeight),
            0.0,
            1.0);
    }

    private static double CosineSimilarity(float[]? a, float[]? b)
    {
        if (a is null || b is null)
        {
            return 0.0;
        }

        var length = Math.Min(a.Length, b.Length);
        if (length == 0)
        {
            return 0.0;
        }

        double dot = 0.0;
        double aNorm = 0.0;
        double bNorm = 0.0;

        for (var i = 0; i < length; i++)
        {
            var av = SanitizeScalar(a[i]);
            var bv = SanitizeScalar(b[i]);
            dot += av * bv;
            aNorm += av * av;
            bNorm += bv * bv;
        }

        var denominator = Math.Sqrt(aNorm) * Math.Sqrt(bNorm);
        if (denominator <= SimilarityEpsilon)
        {
            return 0.0;
        }

        return Math.Clamp(dot / denominator, 0.0, 1.0);
    }

    private static double SanitizeScalar(double value)
    {
        return double.IsFinite(value) ? value : 0.0;
    }
}
