namespace EternalLoop.Core.BeatTracking;

public static class BeatAligner
{
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

        if (targetPeriodFrames <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetPeriodFrames), "Target period must be greater than zero.");
        }

        if (tightnessLambda < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tightnessLambda), "Tightness must be non-negative.");
        }

        var candidates = FindCandidatePeaks(onsetDetectionFunction);

        if (candidates.Length == 0)
        {
            candidates = CreateRegularGrid(onsetDetectionFunction.Length, targetPeriodFrames);
        }

        if (candidates.Length == 0)
        {
            return [];
        }

        var scores = new double[candidates.Length];
        var previous = new int[candidates.Length];
        Array.Fill(previous, -1);

        for (var i = 0; i < candidates.Length; i++)
        {
            scores[i] = OnsetScore(onsetDetectionFunction[candidates[i]]);

            for (var j = 0; j < i; j++)
            {
                var interval = candidates[i] - candidates[j];

                if (interval <= 0 ||
                    interval < targetPeriodFrames * 0.5 ||
                    interval > targetPeriodFrames * 2.0)
                {
                    continue;
                }

                var penalty = -tightnessLambda * Math.Pow(Math.Log(interval / targetPeriodFrames), 2);
                var score = scores[j] + OnsetScore(onsetDetectionFunction[candidates[i]]) + penalty;

                if (score > scores[i])
                {
                    scores[i] = score;
                    previous[i] = j;
                }
            }
        }

        var bestIndex = 0;
        var bestScore = scores[0];

        for (var i = 1; i < scores.Length; i++)
        {
            if (scores[i] > bestScore)
            {
                bestScore = scores[i];
                bestIndex = i;
            }
        }

        var path = new List<int>();
        for (var i = bestIndex; i >= 0; i = previous[i])
        {
            path.Add(candidates[i]);

            if (previous[i] < 0)
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

    internal static int[] FindCandidatePeaks(float[] odf)
    {
        ArgumentNullException.ThrowIfNull(odf);

        if (odf.Length == 0)
        {
            return [];
        }

        var max = odf.Max();
        if (max <= 1e-9f)
        {
            return [];
        }

        var threshold = max * 0.15f;
        var peaks = new List<int>();

        if (odf.Length == 1 && odf[0] >= threshold)
        {
            peaks.Add(0);
        }

        for (var i = 1; i < odf.Length - 1; i++)
        {
            if (odf[i] >= threshold && odf[i] >= odf[i - 1] && odf[i] > odf[i + 1])
            {
                peaks.Add(i);
            }
        }

        if (odf.Length > 1 && odf[^1] >= threshold && odf[^1] > odf[^2])
        {
            peaks.Add(odf.Length - 1);
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
        return Math.Log(1.0 + Math.Max(0f, value) * 100.0);
    }
}
