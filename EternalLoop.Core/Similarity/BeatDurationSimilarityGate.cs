namespace EternalLoop.Core.Similarity;

public static class BeatDurationSimilarityGate
{
    private const double RatioEpsilon = 0.000001;

    public static bool TryApply(
        double baseScore,
        double sourceDuration,
        double destinationDuration,
        double penaltyStartRatio,
        double rejectionRatio,
        double penaltyStrength,
        out double adjustedScore)
    {
        adjustedScore = Math.Clamp(baseScore, 0.0, 1.0);

        if (!double.IsFinite(sourceDuration) ||
            !double.IsFinite(destinationDuration) ||
            sourceDuration <= RatioEpsilon ||
            destinationDuration <= RatioEpsilon)
        {
            return true;
        }

        var ratio = Math.Min(sourceDuration, destinationDuration) / Math.Max(sourceDuration, destinationDuration);
        ratio = Math.Clamp(ratio, 0.0, 1.0);

        var penaltyStart = Math.Clamp(penaltyStartRatio, 0.0, 1.0);
        var rejection = Math.Clamp(rejectionRatio, 0.0, penaltyStart);
        var strength = Math.Clamp(penaltyStrength, 0.0, 1.0);

        if (ratio < rejection)
        {
            adjustedScore = 0.0;
            return false;
        }

        if (ratio < penaltyStart && strength > 0.0)
        {
            var penaltyRange = Math.Max(RatioEpsilon, penaltyStart - rejection);
            var severity = (penaltyStart - ratio) / penaltyRange;
            var multiplier = 1.0 - (Math.Clamp(severity, 0.0, 1.0) * strength);
            adjustedScore = Math.Clamp(adjustedScore * multiplier, 0.0, 1.0);
        }

        return true;
    }
}
