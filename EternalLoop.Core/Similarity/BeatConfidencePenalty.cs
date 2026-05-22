namespace EternalLoop.Core.Similarity;

public static class BeatConfidencePenalty
{
    private const double ConfidenceEpsilon = 0.000001;

    public static double Apply(
        double baseScore,
        double sourceConfidence,
        double destinationConfidence,
        double penaltyStart,
        double rejectionThreshold,
        double penaltyStrength)
    {
        var adjustedScore = Math.Clamp(baseScore, 0.0, 1.0);
        var source = SanitizeConfidence(sourceConfidence);
        var destination = SanitizeConfidence(destinationConfidence);
        var pairConfidence = Math.Min(source, destination);

        var start = Math.Clamp(penaltyStart, 0.0, 1.0);
        var rejection = Math.Clamp(rejectionThreshold, 0.0, start);
        var strength = Math.Clamp(penaltyStrength, 0.0, 1.0);

        if (pairConfidence >= start || strength <= 0.0)
        {
            return adjustedScore;
        }

        var severity = pairConfidence <= rejection
            ? 1.0
            : (start - pairConfidence) / Math.Max(ConfidenceEpsilon, start - rejection);

        var multiplier = 1.0 - (Math.Clamp(severity, 0.0, 1.0) * strength);
        return Math.Clamp(adjustedScore * multiplier, 0.0, 1.0);
    }

    private static double SanitizeConfidence(double confidence)
    {
        if (!double.IsFinite(confidence))
        {
            return 0.0;
        }

        return Math.Clamp(confidence, 0.0, 1.0);
    }
}
