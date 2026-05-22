using EternalLoop.Contracts.Enums;

namespace EternalLoop.Core.Similarity;

public static class MetricPositionGate
{
    private const double SimilarityEpsilon = 0.000001;

    public static bool TryApply(
        double baseScore,
        double metricSimilarity,
        MetricPositionMode mode,
        double penaltyStrength,
        double rejectionThreshold,
        out double adjustedScore)
    {
        adjustedScore = Math.Clamp(baseScore, 0.0, 1.0);

        if (mode == MetricPositionMode.Disabled)
        {
            return true;
        }

        var similarity = SanitizeSimilarity(metricSimilarity);
        var strength = Math.Clamp(penaltyStrength, 0.0, 1.0);
        var rejection = Math.Clamp(rejectionThreshold, 0.0, 1.0);

        if (mode == MetricPositionMode.StrictGate && similarity < rejection)
        {
            adjustedScore = 0.0;
            return false;
        }

        if (strength <= 0.0 || similarity >= 1.0)
        {
            return true;
        }

        var severity = mode switch
        {
            MetricPositionMode.StrictGate => ComputeStrictSeverity(similarity, rejection),
            MetricPositionMode.StrongPenalty => ComputePenaltySeverity(similarity, rejection),
            MetricPositionMode.SoftPenalty => ComputePenaltySeverity(similarity, rejection),
            _ => 0.0
        };

        var multiplier = 1.0 - (Math.Clamp(severity, 0.0, 1.0) * strength);
        adjustedScore = Math.Clamp(adjustedScore * multiplier, 0.0, 1.0);
        return true;
    }

    private static double ComputeStrictSeverity(double similarity, double rejectionThreshold)
    {
        if (similarity >= 1.0)
        {
            return 0.0;
        }

        var safeThreshold = Math.Clamp(rejectionThreshold, 0.0, 1.0 - SimilarityEpsilon);
        return (1.0 - similarity) / Math.Max(SimilarityEpsilon, 1.0 - safeThreshold);
    }

    private static double ComputePenaltySeverity(double similarity, double rejectionThreshold)
    {
        var safeThreshold = Math.Clamp(rejectionThreshold, 0.0, 1.0 - SimilarityEpsilon);
        return (1.0 - similarity) / Math.Max(SimilarityEpsilon, 1.0 - safeThreshold);
    }

    private static double SanitizeSimilarity(double similarity)
    {
        if (!double.IsFinite(similarity))
        {
            return 1.0;
        }

        return Math.Clamp(similarity, 0.0, 1.0);
    }
}
