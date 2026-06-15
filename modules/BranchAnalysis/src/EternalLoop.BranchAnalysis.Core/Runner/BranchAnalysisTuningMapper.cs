namespace EternalLoop.BranchAnalysis.Core.Runner;

public static class BranchAnalysisTuningMapper
{
    public const double MinSimilarityThreshold = 0.65;
    public const double WildSimilarityThreshold = 0.78;
    public const double BalancedSimilarityThreshold = 0.86;
    public const double ConservativeSimilarityThreshold = 0.92;
    public const double MaxSimilarityThreshold = 0.95;

    public const int WildMaxThreshold = 95;
    public const int BalancedMaxThreshold = 80;
    public const int ConservativeMaxThreshold = 70;

    public static int MapSimilarityToMaxThreshold(double similarityThreshold)
    {
        double similarity = Clamp(
            similarityThreshold,
            MinSimilarityThreshold,
            MaxSimilarityThreshold);

        double threshold = similarity >= BalancedSimilarityThreshold
            ? Interpolate(
                similarity,
                BalancedSimilarityThreshold,
                BalancedMaxThreshold,
                ConservativeSimilarityThreshold,
                ConservativeMaxThreshold)
            : Interpolate(
                similarity,
                WildSimilarityThreshold,
                WildMaxThreshold,
                BalancedSimilarityThreshold,
                BalancedMaxThreshold);

        return Clamp((int)Math.Round(threshold), 1, 100);
    }

    private static double Interpolate(
        double value,
        double leftValue,
        double leftResult,
        double rightValue,
        double rightResult)
    {
        if (Math.Abs(rightValue - leftValue) < double.Epsilon)
        {
            return leftResult;
        }

        double t = (value - leftValue) / (rightValue - leftValue);
        return leftResult + ((rightResult - leftResult) * t);
    }

    private static int Clamp(int value, int minimum, int maximum)
    {
        return Math.Min(Math.Max(value, minimum), maximum);
    }

    private static double Clamp(double value, double minimum, double maximum)
    {
        if (!double.IsFinite(value))
        {
            return minimum;
        }

        return Math.Min(Math.Max(value, minimum), maximum);
    }
}
