namespace EternalLoop.Core.Similarity;

public static class AdaptiveThresholdSelector
{
    public static double Select(
        double[,] similarityMatrix,
        int minJumpDistance,
        int lookaheadDepth,
        int targetMinEdges = 20,
        int targetMaxEdges = 200,
        double fallbackThreshold = 0.85)
    {
        ArgumentNullException.ThrowIfNull(similarityMatrix);

        var n = similarityMatrix.GetLength(0);
        if (n == 0 || similarityMatrix.GetLength(1) == 0)
        {
            return Clamp01(fallbackThreshold);
        }

        var minDistance = Math.Max(1, minJumpDistance);
        var lookahead = Math.Max(0, lookaheadDepth);
        var lookaheadMinima = new List<double>();

        for (var i = 0; i < n - lookahead; i++)
        {
            for (var j = 0; j < n - lookahead; j++)
            {
                if (i == j)
                {
                    continue;
                }

                if (Math.Abs(i - j) < minDistance)
                {
                    continue;
                }

                var minSimilarity = double.PositiveInfinity;
                var valid = true;

                for (var k = 0; k <= lookahead; k++)
                {
                    var value = similarityMatrix[i + k, j + k];
                    if (!double.IsFinite(value))
                    {
                        valid = false;
                        break;
                    }

                    if (value < minSimilarity)
                    {
                        minSimilarity = value;
                    }
                }

                if (valid && double.IsFinite(minSimilarity))
                {
                    lookaheadMinima.Add(minSimilarity);
                }
            }
        }

        if (lookaheadMinima.Count == 0)
        {
            return Clamp01(fallbackThreshold);
        }

        lookaheadMinima.Sort();

        var desiredEdges = Math.Clamp(targetMaxEdges, 1, Math.Max(1, lookaheadMinima.Count));
        var thresholdIndex = Math.Clamp(lookaheadMinima.Count - desiredEdges, 0, lookaheadMinima.Count - 1);

        return Clamp01(lookaheadMinima[thresholdIndex]);
    }

    private static double Clamp01(double value)
    {
        if (double.IsNaN(value))
        {
            return 0.85;
        }

        return Math.Clamp(value, 0.0, 1.0);
    }
}
