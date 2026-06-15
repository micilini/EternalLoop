namespace EternalLoop.AnalysisEngine.Core.BeatTracking;

public sealed record PiecewiseBeatGridRefinementResult(
    int[] BeatFrames,
    bool Applied,
    int WindowCount,
    int AcceptedWindows,
    double MeanShiftMs,
    double MaxShiftMs,
    double OnsetScoreBefore,
    double OnsetScoreAfter,
    double RegularityBefore,
    double RegularityAfter,
    string Mode,
    IReadOnlyList<double> WindowOffsetBeatRatios);

public static class PiecewiseBeatGridRefiner
{
    public static PiecewiseBeatGridRefinementResult Refine(
        float[] onsetDetectionFunction,
        int[] beatFrames,
        double targetPeriodFrames,
        double framesPerSecond,
        Options.BeatTrackingOptions options)
    {
        ArgumentNullException.ThrowIfNull(onsetDetectionFunction);
        ArgumentNullException.ThrowIfNull(beatFrames);
        ArgumentNullException.ThrowIfNull(options);

        if (onsetDetectionFunction.Length == 0 || beatFrames.Length < options.PiecewiseWindowBeats || targetPeriodFrames <= 0.0 || framesPerSecond <= 0.0)
        {
            return Noop(beatFrames, "not-enough-data");
        }

        var windows = BuildWindows(beatFrames.Length, options.PiecewiseWindowBeats, options.PiecewiseWindowHopBeats);
        if (windows.Length == 0)
        {
            return Noop(beatFrames, "no-windows");
        }

        var offsets = BuildOffsetCandidates(options.PiecewiseMaxOffsetBeatRatio, options.PiecewiseOffsetStepBeatRatio);
        var localScores = new double[windows.Length][];
        for (var window = 0; window < windows.Length; window++)
        {
            localScores[window] = offsets
                .Select(offset => ScoreWindow(onsetDetectionFunction, beatFrames, windows[window], offset, targetPeriodFrames))
                .ToArray();
        }

        var selectedOffsets = SelectOffsets(localScores, offsets, options.PiecewiseTransitionPenalty);
        var adjusted = ApplyWindowOffsets(beatFrames, windows, selectedOffsets, targetPeriodFrames);
        if (adjusted.Length != beatFrames.Length)
        {
            return Noop(beatFrames, "duplicate-reject");
        }

        var beforeScore = MeanOnsetScore(onsetDetectionFunction, beatFrames);
        var afterScore = MeanOnsetScore(onsetDetectionFunction, adjusted);
        var beforeQuality = BeatGridRefiner.BeatGridQuality.Measure(beatFrames, targetPeriodFrames);
        var afterQuality = BeatGridRefiner.BeatGridQuality.Measure(adjusted, targetPeriodFrames);
        var shifts = beatFrames
            .Zip(adjusted, (before, after) => Math.Abs(after - before) / framesPerSecond * 1000.0)
            .Order()
            .ToArray();
        var meanShift = shifts.DefaultIfEmpty(0.0).Average();
        var maxShift = shifts.DefaultIfEmpty(0.0).Max();
        var medianShift = Percentile(shifts, 0.50);
        var acceptedWindows = selectedOffsets.Count(offset => Math.Abs(offset) > 0.0001);

        var accepted =
            afterScore >= beforeScore + options.PiecewiseMinOnsetGain &&
            medianShift <= options.PiecewiseMaxMedianShiftMs &&
            maxShift <= options.PiecewiseMaxSingleShiftMs &&
            afterQuality.DuplicateOrTooCloseCount == 0 &&
            afterQuality.MissingBeatGapCount <= beforeQuality.MissingBeatGapCount + 1 &&
            afterQuality.BeatDurationStdDevRatio <= Math.Max(beforeQuality.BeatDurationStdDevRatio * 1.6, beforeQuality.BeatDurationStdDevRatio + 0.035);

        return new PiecewiseBeatGridRefinementResult(
            accepted ? adjusted : beatFrames,
            accepted,
            windows.Length,
            accepted ? acceptedWindows : 0,
            meanShift,
            maxShift,
            beforeScore,
            afterScore,
            beforeQuality.BeatDurationStdDevRatio,
            afterQuality.BeatDurationStdDevRatio,
            accepted ? "piecewise-window-offset" : "rejected",
            selectedOffsets);
    }

    private static (int Start, int End)[] BuildWindows(int beatCount, int windowBeats, int hopBeats)
    {
        var windows = new List<(int Start, int End)>();
        for (var start = 0; start < beatCount; start += Math.Max(1, hopBeats))
        {
            var end = Math.Min(beatCount, start + windowBeats);
            if (end - start >= Math.Min(8, windowBeats))
            {
                windows.Add((start, end));
            }

            if (end == beatCount)
            {
                break;
            }
        }

        return windows.ToArray();
    }

    private static double[] BuildOffsetCandidates(double maxOffset, double step)
    {
        var values = new List<double>();
        for (var value = -maxOffset; value <= maxOffset + 1e-9; value += Math.Max(0.01, step))
        {
            values.Add(Math.Round(value, 4));
        }

        if (!values.Any(value => Math.Abs(value) < 1e-9))
        {
            values.Add(0.0);
        }

        return values.Order().ToArray();
    }

    private static double ScoreWindow(
        float[] odf,
        int[] beatFrames,
        (int Start, int End) window,
        double offsetBeatRatio,
        double targetPeriodFrames)
    {
        var offsetFrames = offsetBeatRatio * targetPeriodFrames;
        var hitWindow = Math.Max(1, (int)Math.Round(targetPeriodFrames * 0.12));
        var max = Math.Max(1e-9f, odf.Max());
        var hits = 0;
        var proximity = 0.0;
        var strength = 0.0;
        var count = 0;

        for (var index = window.Start; index < window.End; index++)
        {
            var frame = Math.Clamp((int)Math.Round(beatFrames[index] + offsetFrames), 0, odf.Length - 1);
            var bestDistance = hitWindow + 1;
            var best = odf[frame];
            for (var candidate = Math.Max(0, frame - hitWindow); candidate <= Math.Min(odf.Length - 1, frame + hitWindow); candidate++)
            {
                if (odf[candidate] > best)
                {
                    best = odf[candidate];
                    bestDistance = Math.Abs(candidate - frame);
                }
            }

            if (bestDistance <= hitWindow)
            {
                hits++;
            }

            proximity += 1.0 - Math.Clamp(bestDistance / (double)(hitWindow + 1), 0.0, 1.0);
            strength += best / max;
            count++;
        }

        if (count == 0)
        {
            return double.NegativeInfinity;
        }

        var onsetHitRate = hits / (double)count;
        var meanStrength = strength / count;
        var peakProximity = proximity / count;
        var coverage = Math.Clamp(count / 32.0, 0.0, 1.0);
        var barPhaseStability = EstimateBarPhaseStability(odf, beatFrames, window, offsetFrames, max);
        return
            0.45 * onsetHitRate
            + 0.25 * meanStrength
            + 0.15 * peakProximity
            + 0.10 * barPhaseStability
            + 0.05 * coverage
            - 0.20 * Math.Abs(offsetBeatRatio);
    }

    private static double EstimateBarPhaseStability(
        float[] odf,
        int[] beatFrames,
        (int Start, int End) window,
        double offsetFrames,
        float max)
    {
        var downbeats = new List<double>();
        var others = new List<double>();
        for (var index = window.Start; index < window.End; index++)
        {
            var frame = Math.Clamp((int)Math.Round(beatFrames[index] + offsetFrames), 0, odf.Length - 1);
            if ((index - window.Start) % 4 == 0)
            {
                downbeats.Add(odf[frame] / max);
            }
            else
            {
                others.Add(odf[frame] / max);
            }
        }

        return Math.Clamp(0.5 + downbeats.DefaultIfEmpty(0.0).Average() - others.DefaultIfEmpty(0.0).Average(), 0.0, 1.0);
    }

    private static IReadOnlyList<double> SelectOffsets(double[][] localScores, double[] offsets, double transitionPenalty)
    {
        var dp = localScores.Select(row => new double[row.Length]).ToArray();
        var prev = localScores.Select(row => new int[row.Length]).ToArray();
        for (var i = 0; i < prev.Length; i++)
        {
            Array.Fill(prev[i], -1);
        }

        for (var j = 0; j < offsets.Length; j++)
        {
            dp[0][j] = localScores[0][j];
        }

        for (var i = 1; i < localScores.Length; i++)
        {
            for (var j = 0; j < offsets.Length; j++)
            {
                var best = double.NegativeInfinity;
                var bestPrev = 0;
                for (var k = 0; k < offsets.Length; k++)
                {
                    var transition = transitionPenalty * Math.Pow(offsets[j] - offsets[k], 2.0);
                    var score = dp[i - 1][k] + localScores[i][j] - transition;
                    if (score > best)
                    {
                        best = score;
                        bestPrev = k;
                    }
                }

                dp[i][j] = best;
                prev[i][j] = bestPrev;
            }
        }

        var selected = new double[localScores.Length];
        var current = Array.IndexOf(dp[^1], dp[^1].Max());
        for (var i = localScores.Length - 1; i >= 0; i--)
        {
            selected[i] = offsets[current];
            current = prev[i][current] >= 0 ? prev[i][current] : current;
        }

        return selected;
    }

    private static int[] ApplyWindowOffsets(
        int[] beatFrames,
        (int Start, int End)[] windows,
        IReadOnlyList<double> offsets,
        double targetPeriodFrames)
    {
        var weightedOffsets = new double[beatFrames.Length];
        var weights = new double[beatFrames.Length];
        for (var windowIndex = 0; windowIndex < windows.Length; windowIndex++)
        {
            var window = windows[windowIndex];
            var center = (window.Start + window.End - 1) / 2.0;
            var radius = Math.Max(1.0, (window.End - window.Start) / 2.0);
            for (var beat = window.Start; beat < window.End; beat++)
            {
                var distance = Math.Abs(beat - center) / radius;
                var weight = Math.Max(0.05, 1.0 - distance);
                weightedOffsets[beat] += offsets[windowIndex] * targetPeriodFrames * weight;
                weights[beat] += weight;
            }
        }

        var adjusted = new int[beatFrames.Length];
        for (var i = 0; i < beatFrames.Length; i++)
        {
            var offset = weights[i] > 0.0 ? weightedOffsets[i] / weights[i] : 0.0;
            adjusted[i] = (int)Math.Round(beatFrames[i] + offset);
        }

        for (var i = 1; i < adjusted.Length; i++)
        {
            if (adjusted[i] <= adjusted[i - 1])
            {
                adjusted[i] = adjusted[i - 1] + 1;
            }
        }

        return adjusted.Distinct().Order().ToArray();
    }

    private static double MeanOnsetScore(float[] odf, IReadOnlyList<int> frames)
    {
        if (frames.Count == 0)
        {
            return 0.0;
        }

        var max = Math.Max(1e-9f, odf.Max());
        return frames.Select(frame => odf[Math.Clamp(frame, 0, odf.Length - 1)] / max).Average();
    }

    private static PiecewiseBeatGridRefinementResult Noop(int[] beatFrames, string mode)
    {
        return new PiecewiseBeatGridRefinementResult(beatFrames, false, 0, 0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, mode, []);
    }

    private static double Percentile(IReadOnlyList<double> values, double percentile)
    {
        if (values.Count == 0)
        {
            return 0.0;
        }

        var index = (int)Math.Round((values.Count - 1) * percentile);
        return values[Math.Clamp(index, 0, values.Count - 1)];
    }
}
