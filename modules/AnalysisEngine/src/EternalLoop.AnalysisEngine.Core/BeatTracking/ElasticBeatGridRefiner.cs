namespace EternalLoop.AnalysisEngine.Core.BeatTracking;

public sealed record ElasticBeatGridRefinementResult(
    int[] BeatFrames,
    bool Applied,
    double MedianShiftMs,
    double P90ShiftMs,
    double IntervalStdDevRatioBefore,
    double IntervalStdDevRatioAfter,
    double OnsetScoreBefore,
    double OnsetScoreAfter,
    string Mode);

public static class ElasticBeatGridRefiner
{
    public static ElasticBeatGridRefinementResult Refine(
        float[] onsetDetectionFunction,
        int[] beatFrames,
        double targetPeriodFrames,
        double framesPerSecond,
        double searchWindowRatio,
        double maxShiftMs)
    {
        ArgumentNullException.ThrowIfNull(onsetDetectionFunction);
        ArgumentNullException.ThrowIfNull(beatFrames);

        if (onsetDetectionFunction.Length == 0 || beatFrames.Length < 8 || targetPeriodFrames <= 0.0 || framesPerSecond <= 0.0)
        {
            return Noop(beatFrames, "not-enough-data");
        }

        var beforeQuality = BeatGridRefiner.BeatGridQuality.Measure(beatFrames, targetPeriodFrames);
        var beforeScore = MeanOnsetScore(onsetDetectionFunction, beatFrames);
        var maxShiftFrames = Math.Max(
            1,
            (int)Math.Round(Math.Min(targetPeriodFrames * searchWindowRatio, framesPerSecond * maxShiftMs / 1000.0)));
        var candidates = new int[beatFrames.Length][];
        for (var index = 0; index < beatFrames.Length; index++)
        {
            candidates[index] = BuildCandidates(onsetDetectionFunction, beatFrames[index], maxShiftFrames);
        }

        var selected = SelectMonotonicPath(
            onsetDetectionFunction,
            candidates,
            beatFrames,
            targetPeriodFrames);
        var afterQuality = BeatGridRefiner.BeatGridQuality.Measure(selected, targetPeriodFrames);
        var afterScore = MeanOnsetScore(onsetDetectionFunction, selected);
        var shiftsMs = beatFrames
            .Zip(selected, (before, after) => Math.Abs(after - before) / framesPerSecond * 1000.0)
            .Order()
            .ToArray();

        var acceptable =
            selected.Length == beatFrames.Length &&
            afterScore > beforeScore * 1.01 &&
            afterQuality.DuplicateOrTooCloseCount == 0 &&
            afterQuality.MissingBeatGapCount <= beforeQuality.MissingBeatGapCount &&
            afterQuality.BeatDurationStdDevRatio <= Math.Max(beforeQuality.BeatDurationStdDevRatio * 1.8, beforeQuality.BeatDurationStdDevRatio + 0.04);

        return new ElasticBeatGridRefinementResult(
            acceptable ? selected : beatFrames,
            acceptable,
            Median(shiftsMs),
            Percentile(shiftsMs, 0.90),
            beforeQuality.BeatDurationStdDevRatio,
            afterQuality.BeatDurationStdDevRatio,
            beforeScore,
            afterScore,
            acceptable ? "elastic-local-onset" : "rejected");
    }

    private static int[] BuildCandidates(float[] odf, int nominalFrame, int window)
    {
        var start = Math.Max(0, nominalFrame - window);
        var end = Math.Min(odf.Length - 1, nominalFrame + window);
        var candidates = new HashSet<int> { Math.Clamp(nominalFrame, 0, odf.Length - 1) };
        var best = candidates.First();
        var bestValue = odf[best];
        var weighted = 0.0;
        var weight = 0.0;

        for (var frame = start; frame <= end; frame++)
        {
            if (odf[frame] > bestValue)
            {
                bestValue = odf[frame];
                best = frame;
            }

            if (frame > start && frame < end && odf[frame] >= odf[frame - 1] && odf[frame] > odf[frame + 1])
            {
                candidates.Add(frame);
            }

            var positive = Math.Max(0.0, odf[frame]);
            weighted += frame * positive;
            weight += positive;
        }

        candidates.Add(best);
        if (weight > 0.0)
        {
            candidates.Add((int)Math.Round(weighted / weight));
        }

        return candidates
            .Select(frame => Math.Clamp(frame, 0, odf.Length - 1))
            .Distinct()
            .Order()
            .ToArray();
    }

    private static int[] SelectMonotonicPath(
        float[] odf,
        int[][] candidates,
        int[] original,
        double targetPeriodFrames)
    {
        var scores = candidates.Select(row => new double[row.Length]).ToArray();
        var previous = candidates.Select(row => new int[row.Length]).ToArray();
        for (var i = 0; i < previous.Length; i++)
        {
            Array.Fill(previous[i], -1);
        }

        for (var j = 0; j < candidates[0].Length; j++)
        {
            scores[0][j] = CandidateScore(odf, candidates[0][j], original[0], targetPeriodFrames);
        }

        for (var i = 1; i < candidates.Length; i++)
        {
            for (var j = 0; j < candidates[i].Length; j++)
            {
                var frame = candidates[i][j];
                var bestScore = double.NegativeInfinity;
                var bestPrevious = -1;
                for (var k = 0; k < candidates[i - 1].Length; k++)
                {
                    var prevFrame = candidates[i - 1][k];
                    var interval = frame - prevFrame;
                    if (interval <= targetPeriodFrames * 0.70 || interval >= targetPeriodFrames * 1.30)
                    {
                        continue;
                    }

                    var intervalPenalty = Math.Pow((interval - targetPeriodFrames) / targetPeriodFrames, 2.0) * 2.0;
                    var score = scores[i - 1][k] + CandidateScore(odf, frame, original[i], targetPeriodFrames) - intervalPenalty;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestPrevious = k;
                    }
                }

                if (bestPrevious < 0)
                {
                    scores[i][j] = CandidateScore(odf, frame, original[i], targetPeriodFrames) - 5.0;
                }
                else
                {
                    scores[i][j] = bestScore;
                    previous[i][j] = bestPrevious;
                }
            }
        }

        var last = Array.IndexOf(scores[^1], scores[^1].Max());
        var path = new int[candidates.Length];
        for (var i = candidates.Length - 1; i >= 0; i--)
        {
            path[i] = candidates[i][last];
            last = previous[i][last] >= 0 ? previous[i][last] : 0;
        }

        return path.Distinct().Order().ToArray();
    }

    private static double CandidateScore(float[] odf, int frame, int originalFrame, double targetPeriodFrames)
    {
        var onsetReward = odf[frame];
        var shiftPenalty = Math.Abs(frame - originalFrame) / Math.Max(1.0, targetPeriodFrames) * 0.20;
        return onsetReward - shiftPenalty;
    }

    private static double MeanOnsetScore(float[] odf, IReadOnlyList<int> frames)
    {
        if (frames.Count == 0)
        {
            return 0.0;
        }

        var max = Math.Max(1e-9f, odf.Max());
        return frames
            .Select(frame => odf[Math.Clamp(frame, 0, odf.Length - 1)] / max)
            .Average();
    }

    private static ElasticBeatGridRefinementResult Noop(int[] beatFrames, string mode)
    {
        return new ElasticBeatGridRefinementResult(beatFrames, false, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, mode);
    }

    private static double Median(IReadOnlyList<double> values)
    {
        return Percentile(values, 0.50);
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
