namespace EternalLoop.Core.BeatTracking;

public static class BeatGridRefiner
{
    public static int[] EnsureUsableBeatGrid(
        float[] onsetDetectionFunction,
        int[] alignedFrames,
        double targetPeriodFrames,
        double durationSeconds,
        double framesPerSecond)
    {
        ArgumentNullException.ThrowIfNull(onsetDetectionFunction);
        ArgumentNullException.ThrowIfNull(alignedFrames);

        if (onsetDetectionFunction.Length == 0)
        {
            return [];
        }

        if (targetPeriodFrames <= 0 || framesPerSecond <= 0 || durationSeconds <= 0)
        {
            return alignedFrames.Distinct().Order().ToArray();
        }

        var expectedFromOdf = Math.Max(1, (int)Math.Round(onsetDetectionFunction.Length / targetPeriodFrames));
        var expectedFromDuration = Math.Max(1, (int)Math.Round(durationSeconds * framesPerSecond / targetPeriodFrames));
        var expectedBeatCount = Math.Max(expectedFromOdf, expectedFromDuration);
        var minimumUsableBeatCount = Math.Max(8, (int)Math.Floor(expectedBeatCount * 0.65));

        var cleanAligned = alignedFrames
            .Where(frame => frame >= 0 && frame < onsetDetectionFunction.Length)
            .Distinct()
            .Order()
            .ToArray();

        if (cleanAligned.Length >= minimumUsableBeatCount)
        {
            return cleanAligned;
        }

        var snapped = BuildSnappedRegularGrid(
            onsetDetectionFunction,
            cleanAligned,
            targetPeriodFrames);

        if (snapped.Length >= minimumUsableBeatCount)
        {
            return snapped;
        }

        return BuildRegularGrid(onsetDetectionFunction.Length, targetPeriodFrames);
    }

    private static int[] BuildSnappedRegularGrid(
        float[] onsetDetectionFunction,
        int[] sparseFrames,
        double targetPeriodFrames)
    {
        var step = Math.Max(1, (int)Math.Round(targetPeriodFrames));
        var snapWindow = Math.Max(2, (int)Math.Round(step * 0.20));
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

        for (var i = 0; i <= searchEnd; i++)
        {
            if (onsetDetectionFunction[i] > bestValue)
            {
                bestValue = onsetDetectionFunction[i];
                bestFrame = i;
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

        for (var i = start; i <= end; i++)
        {
            if (onsetDetectionFunction[i] > bestValue)
            {
                bestValue = onsetDetectionFunction[i];
                bestFrame = i;
            }
        }

        return bestFrame;
    }
}
