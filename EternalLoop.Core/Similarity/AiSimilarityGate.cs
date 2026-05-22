namespace EternalLoop.Core.Similarity;

public static class AiSimilarityGate
{
    private const double SimilarityEpsilon = 0.000001;

    public static bool TryApply(
        double baseScore,
        float[]? sourceEmbedding,
        float[]? destinationEmbedding,
        double rejectionThreshold,
        double penaltyStartThreshold,
        double penaltyStrength,
        out double adjustedScore)
    {
        adjustedScore = Math.Clamp(baseScore, 0.0, 1.0);

        if (!TryCosineSimilarity(sourceEmbedding, destinationEmbedding, out var aiSimilarity))
        {
            return true;
        }

        var rejection = Math.Clamp(rejectionThreshold, 0.0, 1.0);
        var penaltyStart = Math.Clamp(penaltyStartThreshold, rejection, 1.0);
        var strength = Math.Clamp(penaltyStrength, 0.0, 1.0);

        if (aiSimilarity < rejection)
        {
            adjustedScore = 0.0;
            return false;
        }

        if (aiSimilarity < penaltyStart && strength > 0.0)
        {
            var penaltyRange = Math.Max(SimilarityEpsilon, penaltyStart - rejection);
            var severity = (penaltyStart - aiSimilarity) / penaltyRange;
            var multiplier = 1.0 - (Math.Clamp(severity, 0.0, 1.0) * strength);
            adjustedScore = Math.Clamp(adjustedScore * multiplier, 0.0, 1.0);
        }

        return true;
    }

    private static bool TryCosineSimilarity(float[]? a, float[]? b, out double similarity)
    {
        similarity = 0.0;

        if (a is null || b is null || a.Length == 0 || b.Length == 0 || a.Length != b.Length)
        {
            return false;
        }

        var dot = 0.0;
        var aNorm = 0.0;
        var bNorm = 0.0;

        for (var index = 0; index < a.Length; index++)
        {
            var left = float.IsFinite(a[index]) ? a[index] : 0.0f;
            var right = float.IsFinite(b[index]) ? b[index] : 0.0f;
            dot += left * right;
            aNorm += left * left;
            bNorm += right * right;
        }

        var denominator = Math.Sqrt(aNorm) * Math.Sqrt(bNorm);
        if (denominator <= SimilarityEpsilon)
        {
            return false;
        }

        similarity = Math.Clamp(dot / denominator, 0.0, 1.0);
        return true;
    }
}
