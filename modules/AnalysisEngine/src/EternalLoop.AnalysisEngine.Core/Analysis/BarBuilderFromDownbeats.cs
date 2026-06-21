using EternalLoop.AnalysisEngine.Core.Models;

namespace EternalLoop.AnalysisEngine.Core.Analysis;

public sealed record BarBuilderFromDownbeatsResult(
    IReadOnlyList<Bar> Bars,
    BarPhaseSelectionResult BarPhaseSelection,
    bool UsedProviderDownbeats,
    string? FallbackReason,
    int MatchedDownbeatCount);

public static class BarBuilderFromDownbeats
{
    private const double MinimumMatchToleranceSeconds = 0.080;

    private const double MaximumMatchToleranceSeconds = 0.180;

    private const double MinimumDurationSeconds = 1e-6;

    public static BarBuilderFromDownbeatsResult Build(
        IReadOnlyList<Beat> beats,
        IReadOnlyList<double> downbeatTimes,
        int timeSignature)
    {
        ArgumentNullException.ThrowIfNull(beats);
        ArgumentNullException.ThrowIfNull(downbeatTimes);

        if (timeSignature <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeSignature), "Time signature must be greater than zero.");
        }

        if (beats.Count == 0)
        {
            return Fallback("No beats available.");
        }

        if (downbeatTimes.Count == 0)
        {
            return Fallback("No provider downbeats available.");
        }

        var tolerance = ResolveMatchTolerance(beats);
        var firstBeatStart = beats[0].Start;
        var trackEnd = beats[^1].Start + Math.Max(beats[^1].Duration, ResolveMedianBeatInterval(beats));
        var matchedBeatIndexes = downbeatTimes
            .Where(double.IsFinite)
            .Where(time => time >= firstBeatStart && time <= trackEnd)
            .Select(time => MatchNearestBeat(beats, time, tolerance))
            .Where(index => index >= 0)
            .Distinct()
            .Order()
            .ToArray();

        if (matchedBeatIndexes.Length == 0)
        {
            return Fallback("No provider downbeats matched beat grid.");
        }

        var bars = new List<Bar>(matchedBeatIndexes.Length);
        for (var index = 0; index < matchedBeatIndexes.Length; index++)
        {
            var startIndex = matchedBeatIndexes[index];
            var endExclusive = index + 1 < matchedBeatIndexes.Length
                ? matchedBeatIndexes[index + 1]
                : Math.Min(beats.Count, startIndex + timeSignature);
            var start = beats[startIndex].Start;
            var duration = ResolveDuration(beats, startIndex, endExclusive, timeSignature);

            if (duration <= 0.0 || !double.IsFinite(duration))
            {
                continue;
            }

            bars.Add(new Bar
            {
                Index = bars.Count,
                Start = start,
                Duration = duration,
                Confidence = AverageConfidence(beats, startIndex, endExclusive)
            });
        }

        return bars.Count == 0
            ? Fallback("Provider downbeats did not produce valid bars.")
            : new BarBuilderFromDownbeatsResult(
                bars,
                new BarPhaseSelectionResult(
                    0,
                    [new BarPhaseCandidate(0, 1.0, 1.0, 1.0, 1.0, 1.0)],
                    "provider-downbeats"),
                true,
                null,
                matchedBeatIndexes.Length);
    }

    private static BarBuilderFromDownbeatsResult Fallback(string reason)
    {
        return new BarBuilderFromDownbeatsResult(
            [],
            new BarPhaseSelectionResult(
                0,
                [new BarPhaseCandidate(0, 1.0, 1.0, 1.0, 1.0, 1.0)],
                "phase-zero"),
            false,
            reason,
            0);
    }

    private static int MatchNearestBeat(IReadOnlyList<Beat> beats, double downbeatTime, double tolerance)
    {
        var bestIndex = -1;
        var bestDistance = double.MaxValue;

        for (var index = 0; index < beats.Count; index++)
        {
            var distance = Math.Abs(beats[index].Start - downbeatTime);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = index;
            }
        }

        return bestDistance <= tolerance ? bestIndex : -1;
    }

    private static double ResolveMatchTolerance(IReadOnlyList<Beat> beats)
    {
        return Math.Max(
            MinimumMatchToleranceSeconds,
            Math.Min(MaximumMatchToleranceSeconds, 0.30 * ResolveMedianBeatInterval(beats)));
    }

    private static double ResolveMedianBeatInterval(IReadOnlyList<Beat> beats)
    {
        var intervals = beats
            .Zip(beats.Skip(1), (left, right) => right.Start - left.Start)
            .Where(interval => interval > 0.0 && double.IsFinite(interval))
            .Order()
            .ToArray();

        if (intervals.Length == 0)
        {
            return beats[0].Duration > 0.0 && double.IsFinite(beats[0].Duration) ? beats[0].Duration : 0.5;
        }

        return intervals.Length % 2 == 1
            ? intervals[intervals.Length / 2]
            : (intervals[intervals.Length / 2 - 1] + intervals[intervals.Length / 2]) / 2.0;
    }

    private static double ResolveDuration(
        IReadOnlyList<Beat> beats,
        int startIndex,
        int endExclusive,
        int timeSignature)
    {
        if (endExclusive > startIndex && endExclusive < beats.Count)
        {
            return Math.Max(MinimumDurationSeconds, beats[endExclusive].Start - beats[startIndex].Start);
        }

        var lastIndex = Math.Min(beats.Count - 1, startIndex + timeSignature - 1);
        var duration = beats[lastIndex].Start + beats[lastIndex].Duration - beats[startIndex].Start;

        if (duration <= 0.0 || !double.IsFinite(duration))
        {
            duration = beats[startIndex].Duration;
        }

        return Math.Max(MinimumDurationSeconds, duration);
    }

    private static double AverageConfidence(IReadOnlyList<Beat> beats, int startIndex, int endExclusive)
    {
        var count = 0;
        var sum = 0.0;

        for (var index = startIndex; index < Math.Min(beats.Count, Math.Max(startIndex + 1, endExclusive)); index++)
        {
            sum += Math.Clamp(beats[index].Confidence, 0.0, 1.0);
            count++;
        }

        return count == 0 ? 0.0 : Math.Clamp(sum / count, 0.0, 1.0);
    }
}
