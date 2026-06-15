namespace EternalLoop.AnalysisEngine.Core.BeatTracking;

public static class BeatAligner
{
    private const float MinimumEnergy = 1e-9f;

    private const float PeakThresholdRatio = 0.15f;

    private const double MinimumIntervalRatio = 0.5;

    private const double MaximumIntervalRatio = 2.0;

    private const double OnsetScoreMultiplier = 100.0;

    public static int[] AlignBeats(
        float[] onsetDetectionFunction,
        double targetPeriodFrames,
        double tightnessLambda)
    {
        ArgumentNullException.ThrowIfNull(onsetDetectionFunction);

        if (onsetDetectionFunction.Length == 0)
        {
            return [];
        }

        if (targetPeriodFrames <= 0 || !double.IsFinite(targetPeriodFrames))
        {
            return CreateRegularGrid(onsetDetectionFunction.Length, 1.0);
        }

        var candidates = FindCandidatePeaks(onsetDetectionFunction);

        if (candidates.Length == 0)
        {
            return CreateRegularGrid(onsetDetectionFunction.Length, targetPeriodFrames);
        }

        var scores = new double[candidates.Length];
        var previous = new int[candidates.Length];
        Array.Fill(previous, -1);

        for (var index = 0; index < candidates.Length; index++)
        {
            scores[index] = OnsetScore(onsetDetectionFunction[candidates[index]]);

            for (var previousIndex = 0; previousIndex < index; previousIndex++)
            {
                var interval = candidates[index] - candidates[previousIndex];

                if (interval <= 0 ||
                    interval < targetPeriodFrames * MinimumIntervalRatio ||
                    interval > targetPeriodFrames * MaximumIntervalRatio)
                {
                    continue;
                }

                var penalty = -tightnessLambda * Math.Pow(Math.Log(interval / targetPeriodFrames), 2);
                var score = scores[previousIndex] + OnsetScore(onsetDetectionFunction[candidates[index]]) + penalty;

                if (score > scores[index])
                {
                    scores[index] = score;
                    previous[index] = previousIndex;
                }
            }
        }

        var bestIndex = 0;
        var bestScore = scores[0];

        for (var index = 1; index < scores.Length; index++)
        {
            if (scores[index] > bestScore)
            {
                bestScore = scores[index];
                bestIndex = index;
            }
        }

        var path = new List<int>();

        for (var index = bestIndex; index >= 0; index = previous[index])
        {
            path.Add(candidates[index]);

            if (previous[index] < 0)
            {
                break;
            }
        }

        path.Reverse();

        return path
            .Distinct()
            .Order()
            .ToArray();
    }

    public static int[] AlignBeatsDynamicProgramming(
        float[] onsetDetectionFunction,
        double targetPeriodFrames,
        double tightnessLambda)
    {
        ArgumentNullException.ThrowIfNull(onsetDetectionFunction);

        if (onsetDetectionFunction.Length == 0)
        {
            return [];
        }

        if (targetPeriodFrames <= 0 || !double.IsFinite(targetPeriodFrames))
        {
            return CreateRegularGrid(onsetDetectionFunction.Length, 1.0);
        }

        var localscore = BuildLocalScore(onsetDetectionFunction, targetPeriodFrames);
        if (localscore.Length == 0 || localscore.Max() <= MinimumEnergy)
        {
            return CreateRegularGrid(onsetDetectionFunction.Length, targetPeriodFrames);
        }

        var cumulative = new double[localscore.Length];
        var backlink = new int[localscore.Length];
        Array.Fill(backlink, -1);

        var minInterval = Math.Max(1, (int)Math.Round(targetPeriodFrames * 0.5));
        var maxInterval = Math.Max(minInterval + 1, (int)Math.Round(targetPeriodFrames * 2.0));

        for (var frame = 0; frame < localscore.Length; frame++)
        {
            var bestScore = 0.0;
            var bestPrevious = -1;

            for (var previous = frame - minInterval; previous >= Math.Max(0, frame - maxInterval); previous--)
            {
                var interval = frame - previous;
                var penalty = -tightnessLambda * Math.Pow(Math.Log(interval / targetPeriodFrames), 2.0);
                var score = cumulative[previous] + penalty;

                if (score > bestScore || bestPrevious < 0)
                {
                    bestScore = score;
                    bestPrevious = previous;
                }
            }

            cumulative[frame] = localscore[frame] + Math.Max(0.0, bestScore);
            backlink[frame] = bestPrevious;
        }

        var localMaxima = FindLocalMaxima(cumulative);
        if (localMaxima.Length == 0)
        {
            return CreateRegularGrid(onsetDetectionFunction.Length, targetPeriodFrames);
        }

        var medianScore = Median(localMaxima.Select(frame => cumulative[frame]).ToArray());
        var startFrame = localMaxima
            .Where(frame => cumulative[frame] >= medianScore * 0.5)
            .DefaultIfEmpty(localMaxima[^1])
            .Last();

        var path = new List<int>();
        for (var frame = startFrame; frame >= 0; frame = backlink[frame])
        {
            path.Add(frame);
            if (backlink[frame] < 0)
            {
                break;
            }
        }

        path.Reverse();
        return TrimWeakBeats(path, localscore)
            .Distinct()
            .Order()
            .ToArray();
    }

    private static double[] BuildLocalScore(float[] onsetDetectionFunction, double targetPeriodFrames)
    {
        var stdDev = Math.Sqrt(onsetDetectionFunction.Average(value => Math.Pow(value - onsetDetectionFunction.Average(), 2.0)));
        var normalized = onsetDetectionFunction
            .Select(value => value / Math.Max(1e-9, stdDev))
            .ToArray();
        var radius = Math.Max(1, (int)Math.Round(targetPeriodFrames));
        var window = new double[(radius * 2) + 1];

        for (var offset = -radius; offset <= radius; offset++)
        {
            window[offset + radius] = Math.Exp(-0.5 * Math.Pow(offset * 32.0 / targetPeriodFrames, 2.0));
        }

        var localscore = new double[normalized.Length];
        for (var frame = 0; frame < normalized.Length; frame++)
        {
            var sum = 0.0;
            var weight = 0.0;

            for (var offset = -radius; offset <= radius; offset++)
            {
                var source = frame + offset;
                if (source < 0 || source >= normalized.Length)
                {
                    continue;
                }

                var w = window[offset + radius];
                sum += normalized[source] * w;
                weight += w;
            }

            localscore[frame] = weight > 0.0 ? sum / weight : 0.0;
        }

        return localscore;
    }

    private static int[] FindLocalMaxima(IReadOnlyList<double> values)
    {
        var maxima = new List<int>();

        for (var index = 1; index < values.Count - 1; index++)
        {
            if (values[index] >= values[index - 1] && values[index] > values[index + 1])
            {
                maxima.Add(index);
            }
        }

        return maxima.ToArray();
    }

    private static IReadOnlyList<int> TrimWeakBeats(IReadOnlyList<int> beats, IReadOnlyList<double> localscore)
    {
        if (beats.Count <= 2)
        {
            return beats;
        }

        var rms = Math.Sqrt(beats.Average(frame => localscore[frame] * localscore[frame]));
        var threshold = rms * 0.5;
        var start = 0;
        var end = beats.Count - 1;

        while (start < beats.Count && localscore[beats[start]] <= threshold)
        {
            start++;
        }

        while (end > start && localscore[beats[end]] <= threshold)
        {
            end--;
        }

        return beats.Skip(start).Take(end - start + 1).ToArray();
    }

    private static double Median(double[] values)
    {
        if (values.Length == 0)
        {
            return 0.0;
        }

        Array.Sort(values);
        return values.Length % 2 == 1
            ? values[values.Length / 2]
            : (values[values.Length / 2 - 1] + values[values.Length / 2]) / 2.0;
    }

    internal static int[] FindCandidatePeaks(float[] onsetDetectionFunction)
    {
        ArgumentNullException.ThrowIfNull(onsetDetectionFunction);

        if (onsetDetectionFunction.Length == 0)
        {
            return [];
        }

        var max = onsetDetectionFunction.Max();
        if (max <= MinimumEnergy)
        {
            return [];
        }

        var threshold = max * PeakThresholdRatio;
        var peaks = new List<int>();

        if (onsetDetectionFunction.Length == 1 && onsetDetectionFunction[0] >= threshold)
        {
            peaks.Add(0);
        }

        for (var index = 1; index < onsetDetectionFunction.Length - 1; index++)
        {
            if (onsetDetectionFunction[index] >= threshold &&
                onsetDetectionFunction[index] >= onsetDetectionFunction[index - 1] &&
                onsetDetectionFunction[index] > onsetDetectionFunction[index + 1])
            {
                peaks.Add(index);
            }
        }

        if (onsetDetectionFunction.Length > 1 &&
            onsetDetectionFunction[^1] >= threshold &&
            onsetDetectionFunction[^1] > onsetDetectionFunction[^2])
        {
            peaks.Add(onsetDetectionFunction.Length - 1);
        }

        return peaks.ToArray();
    }

    private static int[] CreateRegularGrid(int length, double targetPeriodFrames)
    {
        var step = Math.Max(1, (int)Math.Round(targetPeriodFrames));
        var frames = new List<int>();

        for (var frame = 0; frame < length; frame += step)
        {
            frames.Add(frame);
        }

        return frames.ToArray();
    }

    private static double OnsetScore(float value)
    {
        return Math.Log(1.0 + Math.Max(0f, value) * OnsetScoreMultiplier);
    }
}
