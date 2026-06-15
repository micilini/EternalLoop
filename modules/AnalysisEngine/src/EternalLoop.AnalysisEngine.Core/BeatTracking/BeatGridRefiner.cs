namespace EternalLoop.AnalysisEngine.Core.BeatTracking;

public static class BeatGridRefiner
{
    private const int MinimumAbsoluteBeatCount = 8;

    private const double MinimumBeatCountRatio = 0.65;

    private const double SnapWindowRatio = 0.15;

    private const double MaximumStdDevRatio = 0.22;

    public static int[] EnsureUsableBeatGrid(
        float[] onsetDetectionFunction,
        int[] alignedFrames,
        double targetPeriodFrames,
        double durationSeconds,
        double framesPerSecond)
    {
        return EnsureUsableBeatGrid(
            onsetDetectionFunction,
            alignedFrames,
            targetPeriodFrames,
            durationSeconds,
            framesPerSecond,
            beatMicroSnap: false,
            out _);
    }

    public static int[] EnsureUsableBeatGrid(
        float[] onsetDetectionFunction,
        int[] alignedFrames,
        double targetPeriodFrames,
        double durationSeconds,
        double framesPerSecond,
        bool beatMicroSnap)
    {
        return EnsureUsableBeatGrid(
            onsetDetectionFunction,
            alignedFrames,
            targetPeriodFrames,
            durationSeconds,
            framesPerSecond,
            beatMicroSnap,
            out _);
    }

    public static int[] EnsureUsableBeatGrid(
        float[] onsetDetectionFunction,
        int[] alignedFrames,
        double targetPeriodFrames,
        double durationSeconds,
        double framesPerSecond,
        bool beatMicroSnap,
        out string beatGridMode)
    {
        return EnsureUsableBeatGrid(
            onsetDetectionFunction,
            alignedFrames,
            targetPeriodFrames,
            durationSeconds,
            framesPerSecond,
            beatMicroSnap,
            EternalLoop.AnalysisEngine.Core.Options.BeatTrackingOptions.DefaultBeatSnapWindowRatio,
            out beatGridMode);
    }

    public static int[] EnsureUsableBeatGrid(
        float[] onsetDetectionFunction,
        int[] alignedFrames,
        double targetPeriodFrames,
        double durationSeconds,
        double framesPerSecond,
        bool beatMicroSnap,
        double beatSnapWindowRatio,
        out string beatGridMode)
    {
        ArgumentNullException.ThrowIfNull(onsetDetectionFunction);
        ArgumentNullException.ThrowIfNull(alignedFrames);
        beatGridMode = "regular";

        if (onsetDetectionFunction.Length == 0)
        {
            beatGridMode = "regular";
            return [];
        }

        if (targetPeriodFrames <= 0 || framesPerSecond <= 0 || durationSeconds <= 0)
        {
            beatGridMode = "aligned";
            return alignedFrames.Distinct().Order().ToArray();
        }

        var expectedFromOdf = Math.Max(1, (int)Math.Round(onsetDetectionFunction.Length / targetPeriodFrames));
        var expectedFromDuration = Math.Max(1, (int)Math.Round(durationSeconds * framesPerSecond / targetPeriodFrames));
        var expectedBeatCount = Math.Max(expectedFromOdf, expectedFromDuration);
        var minimumUsableBeatCount = Math.Max(MinimumAbsoluteBeatCount, (int)Math.Floor(expectedBeatCount * MinimumBeatCountRatio));

        var cleanAligned = alignedFrames
            .Where(frame => frame >= 0 && frame < onsetDetectionFunction.Length)
            .Distinct()
            .Order()
            .ToArray();

        var regularGrid = BuildRegularGrid(onsetDetectionFunction.Length, targetPeriodFrames);

        if (cleanAligned.Length >= minimumUsableBeatCount &&
            BeatGridQuality.Measure(cleanAligned, targetPeriodFrames).IsUsable)
        {
            beatGridMode = "aligned";
            return cleanAligned;
        }

        var snapped = BuildSnappedRegularGrid(
            onsetDetectionFunction,
            cleanAligned,
            targetPeriodFrames);

        if (snapped.Length >= minimumUsableBeatCount &&
            BeatGridQuality.Measure(snapped, targetPeriodFrames).IsNoWorseThan(BeatGridQuality.Measure(regularGrid, targetPeriodFrames)))
        {
            beatGridMode = "snapped-regular";
            return snapped;
        }

        if (!beatMicroSnap)
        {
            beatGridMode = "regular";
            return regularGrid;
        }

        var microSnapped = ApplyPerBeatMicroSnap(onsetDetectionFunction, regularGrid, targetPeriodFrames, beatSnapWindowRatio);
        if (BeatGridQuality.Measure(microSnapped, targetPeriodFrames).IsNoWorseThan(BeatGridQuality.Measure(regularGrid, targetPeriodFrames)))
        {
            beatGridMode = "regular+micro-snap";
            return microSnapped;
        }

        beatGridMode = "regular";
        return regularGrid;
    }

    private static int[] BuildSnappedRegularGrid(
        float[] onsetDetectionFunction,
        int[] sparseFrames,
        double targetPeriodFrames)
    {
        var step = Math.Max(1, (int)Math.Round(targetPeriodFrames));
        var snapWindow = Math.Max(2, (int)Math.Round(step * SnapWindowRatio));
        var startFrame = ChooseStartFrame(onsetDetectionFunction, sparseFrames, step);
        var frames = new List<int>();

        for (var frame = startFrame; frame < onsetDetectionFunction.Length; frame += step)
        {
            frames.Add(SnapToLocalMaximum(onsetDetectionFunction, frame, snapWindow));
        }

        return frames
            .Where(frame => frame >= 0 && frame < onsetDetectionFunction.Length)
            .Distinct()
            .Order()
            .ToArray();
    }

    private static int[] BuildRegularGrid(int length, double targetPeriodFrames)
    {
        var step = Math.Max(1, (int)Math.Round(targetPeriodFrames));
        var frames = new List<int>();

        for (var frame = 0; frame < length; frame += step)
        {
            frames.Add(frame);
        }

        return frames.ToArray();
    }

    private static int ChooseStartFrame(float[] onsetDetectionFunction, int[] sparseFrames, int step)
    {
        if (sparseFrames.Length > 0)
        {
            return Math.Clamp(sparseFrames[0], 0, onsetDetectionFunction.Length - 1);
        }

        var searchEnd = Math.Min(onsetDetectionFunction.Length - 1, Math.Max(1, step * 2));
        var bestFrame = 0;
        var bestValue = float.NegativeInfinity;

        for (var index = 0; index <= searchEnd; index++)
        {
            if (onsetDetectionFunction[index] > bestValue)
            {
                bestValue = onsetDetectionFunction[index];
                bestFrame = index;
            }
        }

        return bestFrame;
    }

    private static int SnapToLocalMaximum(float[] onsetDetectionFunction, int nominalFrame, int window)
    {
        var start = Math.Max(0, nominalFrame - window);
        var end = Math.Min(onsetDetectionFunction.Length - 1, nominalFrame + window);
        var bestFrame = nominalFrame;
        var bestValue = float.NegativeInfinity;

        for (var index = start; index <= end; index++)
        {
            if (onsetDetectionFunction[index] > bestValue)
            {
                bestValue = onsetDetectionFunction[index];
                bestFrame = index;
            }
        }

        return bestFrame;
    }

    internal static int[] ApplyPerBeatMicroSnap(
        float[] onsetDetectionFunction,
        int[] beatFrames,
        double targetPeriodFrames)
    {
        return ApplyPerBeatMicroSnap(
            onsetDetectionFunction,
            beatFrames,
            targetPeriodFrames,
            EternalLoop.AnalysisEngine.Core.Options.BeatTrackingOptions.DefaultBeatSnapWindowRatio);
    }

    internal static int[] ApplyPerBeatMicroSnap(
        float[] onsetDetectionFunction,
        int[] beatFrames,
        double targetPeriodFrames,
        double beatSnapWindowRatio)
    {
        ArgumentNullException.ThrowIfNull(onsetDetectionFunction);
        ArgumentNullException.ThrowIfNull(beatFrames);

        if (onsetDetectionFunction.Length == 0 || beatFrames.Length == 0 || targetPeriodFrames <= 0)
        {
            return beatFrames;
        }

        var window = Math.Max(1, (int)Math.Round(targetPeriodFrames * beatSnapWindowRatio));
        var snapped = new int[beatFrames.Length];

        for (var index = 0; index < beatFrames.Length; index++)
        {
            var nominal = Math.Clamp(beatFrames[index], 0, onsetDetectionFunction.Length - 1);
            var start = Math.Max(0, nominal - window);
            var end = Math.Min(onsetDetectionFunction.Length - 1, nominal + window);
            var localAverage = 0.0;

            for (var frame = start; frame <= end; frame++)
            {
                localAverage += onsetDetectionFunction[frame];
            }

            localAverage /= end - start + 1;
            var bestFrame = nominal;
            var bestValue = onsetDetectionFunction[nominal];

            for (var frame = start; frame <= end; frame++)
            {
                if (onsetDetectionFunction[frame] > bestValue)
                {
                    bestValue = onsetDetectionFunction[frame];
                    bestFrame = frame;
                }
            }

            var hasPrevious = index > 0;
            var previous = hasPrevious ? snapped[index - 1] : 0;
            var nextNominal = index + 1 < beatFrames.Length ? beatFrames[index + 1] : int.MaxValue;
            var previousInterval = hasPrevious ? bestFrame - previous : targetPeriodFrames;
            var nextInterval = nextNominal - bestFrame;
            var strongEnough = bestValue > localAverage * 1.05 || bestValue > onsetDetectionFunction[nominal];
            var keepsOrder = bestFrame > previous && bestFrame < nextNominal;
            var keepsPeriod =
                (!hasPrevious || previousInterval >= targetPeriodFrames * 0.85) &&
                (index + 1 == beatFrames.Length || nextInterval >= targetPeriodFrames * 0.85) &&
                (!hasPrevious || previousInterval <= targetPeriodFrames * 1.15) &&
                (index + 1 == beatFrames.Length || nextInterval <= targetPeriodFrames * 1.15);

            snapped[index] = strongEnough && keepsOrder && keepsPeriod ? bestFrame : nominal;
        }

        return snapped.Distinct().Order().ToArray();
    }

    internal readonly record struct BeatGridQuality(
        double MedianBeatDuration,
        double BeatDurationStdDevRatio,
        int MissingBeatGapCount,
        int DuplicateOrTooCloseCount)
    {
        public bool IsUsable =>
            DuplicateOrTooCloseCount == 0 &&
            MissingBeatGapCount == 0 &&
            BeatDurationStdDevRatio <= MaximumStdDevRatio;

        public bool IsNoWorseThan(BeatGridQuality other) =>
            DuplicateOrTooCloseCount <= other.DuplicateOrTooCloseCount &&
            MissingBeatGapCount <= other.MissingBeatGapCount &&
            BeatDurationStdDevRatio <= Math.Max(MaximumStdDevRatio, other.BeatDurationStdDevRatio + 0.02);

        public static BeatGridQuality Measure(IReadOnlyList<int> frames, double targetPeriodFrames)
        {
            if (frames.Count < 2 || targetPeriodFrames <= 0 || !double.IsFinite(targetPeriodFrames))
            {
                return new BeatGridQuality(0.0, 0.0, 0, 0);
            }

            var intervals = new double[frames.Count - 1];
            for (var index = 1; index < frames.Count; index++)
            {
                intervals[index - 1] = frames[index] - frames[index - 1];
            }

            Array.Sort(intervals);
            var median = intervals.Length % 2 == 1
                ? intervals[intervals.Length / 2]
                : (intervals[intervals.Length / 2 - 1] + intervals[intervals.Length / 2]) / 2.0;

            var average = intervals.Average();
            var variance = intervals.Sum(interval => Math.Pow(interval - average, 2.0)) / intervals.Length;
            var stdDevRatio = Math.Sqrt(variance) / Math.Max(1.0, median);
            var missingBeatGaps = intervals.Count(interval => interval > targetPeriodFrames * 1.65);
            var duplicates = intervals.Count(interval => interval < targetPeriodFrames * 0.45);

            return new BeatGridQuality(median, stdDevRatio, missingBeatGaps, duplicates);
        }
    }
}
