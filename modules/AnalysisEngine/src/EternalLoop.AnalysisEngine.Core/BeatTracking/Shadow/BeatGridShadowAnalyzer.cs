namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Shadow;

public sealed class BeatGridShadowAnalyzer
{
    private const double FiftyMilliseconds = 0.050;
    private const double SeventyMilliseconds = 0.070;
    private const double OneHundredMilliseconds = 0.100;

    public BeatGridShadowComparison Compare(BeatTrackingResult legacy, BeatTrackingResult advisor)
    {
        ArgumentNullException.ThrowIfNull(legacy);
        ArgumentNullException.ThrowIfNull(advisor);

        var metrics50 = CalculateAgreement(legacy.BeatTimes, advisor.BeatTimes, FiftyMilliseconds, offsetSeconds: 0.0);
        var metrics70 = CalculateAgreement(legacy.BeatTimes, advisor.BeatTimes, SeventyMilliseconds, offsetSeconds: 0.0);
        var metrics100 = CalculateAgreement(legacy.BeatTimes, advisor.BeatTimes, OneHundredMilliseconds, offsetSeconds: 0.0);
        var offset = FindBestOffset(legacy.BeatTimes, advisor.BeatTimes);

        return new BeatGridShadowComparison
        {
            CountRatio = legacy.BeatTimes.Length > 0
                ? advisor.BeatTimes.Length / (double)legacy.BeatTimes.Length
                : null,
            BpmDelta = double.IsFinite(advisor.EstimatedBpm) && double.IsFinite(legacy.EstimatedBpm)
                ? advisor.EstimatedBpm - legacy.EstimatedBpm
                : null,
            Precision50Ms = metrics50.Precision,
            Recall50Ms = metrics50.Recall,
            F1_50Ms = metrics50.F1,
            Precision70Ms = metrics70.Precision,
            Recall70Ms = metrics70.Recall,
            F1_70Ms = metrics70.F1,
            Precision100Ms = metrics100.Precision,
            Recall100Ms = metrics100.Recall,
            F1_100Ms = metrics100.F1,
            BestOffsetMs = offset.OffsetMs,
            BestOffsetF1_70Ms = offset.F1
        };
    }

    private static (double OffsetMs, double F1) FindBestOffset(double[] reference, double[] candidate)
    {
        if (reference.Length == 0 || candidate.Length == 0)
        {
            return (0.0, 0.0);
        }

        var bestOffsetMs = 0.0;
        var bestF1 = -1.0;
        var bestMeanDistance = double.PositiveInfinity;

        for (var offsetMs = -120; offsetMs <= 120; offsetMs += 10)
        {
            var metrics = CalculateAgreement(reference, candidate, SeventyMilliseconds, offsetMs / 1000.0);

            if (metrics.F1 > bestF1
                || (Math.Abs(metrics.F1 - bestF1) < 0.0000001 && metrics.MeanMatchedDistance < bestMeanDistance))
            {
                bestF1 = metrics.F1;
                bestOffsetMs = offsetMs;
                bestMeanDistance = metrics.MeanMatchedDistance;
            }
        }

        return (bestOffsetMs, Math.Max(bestF1, 0.0));
    }

    private static (double Precision, double Recall, double F1, double MeanMatchedDistance) CalculateAgreement(
        double[] reference,
        double[] candidate,
        double toleranceSeconds,
        double offsetSeconds)
    {
        if (reference.Length == 0 && candidate.Length == 0)
        {
            return (1.0, 1.0, 1.0, 0.0);
        }

        if (reference.Length == 0 || candidate.Length == 0)
        {
            return (0.0, 0.0, 0.0, double.PositiveInfinity);
        }

        var used = new bool[reference.Length];
        var matches = 0;
        var totalMatchedDistance = 0.0;

        foreach (var candidateBeat in candidate)
        {
            var adjustedCandidate = candidateBeat + offsetSeconds;
            var bestIndex = -1;
            var bestDistance = double.PositiveInfinity;

            for (var index = 0; index < reference.Length; index++)
            {
                if (used[index])
                {
                    continue;
                }

                var distance = Math.Abs(reference[index] - adjustedCandidate);

                if (distance <= toleranceSeconds && distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = index;
                }
            }

            if (bestIndex >= 0)
            {
                used[bestIndex] = true;
                matches++;
                totalMatchedDistance += bestDistance;
            }
        }

        var precision = matches / (double)candidate.Length;
        var recall = matches / (double)reference.Length;
        var f1 = precision + recall > 0.0
            ? 2.0 * precision * recall / (precision + recall)
            : 0.0;

        var meanMatchedDistance = matches > 0
            ? totalMatchedDistance / matches
            : double.PositiveInfinity;

        return (precision, recall, f1, meanMatchedDistance);
    }
}
