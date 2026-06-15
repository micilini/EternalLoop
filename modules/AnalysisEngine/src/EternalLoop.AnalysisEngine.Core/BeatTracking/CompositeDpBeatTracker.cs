namespace EternalLoop.AnalysisEngine.Core.BeatTracking;

public sealed record CompositeDpBeatTrackingResult(
    int[] BeatFrames,
    bool Applied,
    double EvidenceMeanBefore,
    double EvidenceMeanAfter,
    double IntervalStdDevRatioBefore,
    double IntervalStdDevRatioAfter,
    string Mode);

public static class CompositeDpBeatTracker
{
    public static CompositeDpBeatTrackingResult Track(
        float[] evidence,
        int[] currentBeatFrames,
        double targetPeriodFrames,
        double tightnessLambda)
    {
        if (evidence.Length == 0 || currentBeatFrames.Length < 8 || targetPeriodFrames <= 0)
        {
            return Noop(currentBeatFrames, "not-enough-data");
        }

        var candidate = BeatAligner.AlignBeatsDynamicProgramming(evidence, targetPeriodFrames, tightnessLambda * 0.65);
        if (candidate.Length == 0)
        {
            return Noop(currentBeatFrames, "no-candidate");
        }

        var before = MeanEvidence(evidence, currentBeatFrames);
        var after = MeanEvidence(evidence, candidate);
        var beforeQ = BeatGridRefiner.BeatGridQuality.Measure(currentBeatFrames, targetPeriodFrames);
        var afterQ = BeatGridRefiner.BeatGridQuality.Measure(candidate, targetPeriodFrames);
        var countRatio = Math.Abs(candidate.Length - currentBeatFrames.Length) / (double)Math.Max(1, currentBeatFrames.Length);
        var accepted =
            after >= before * 1.03
            && countRatio <= 0.02
            && afterQ.DuplicateOrTooCloseCount == 0
            && afterQ.MissingBeatGapCount <= beforeQ.MissingBeatGapCount
            && afterQ.BeatDurationStdDevRatio <= Math.Max(beforeQ.BeatDurationStdDevRatio * 1.35, beforeQ.BeatDurationStdDevRatio + 0.025);

        return new CompositeDpBeatTrackingResult(
            accepted ? candidate : currentBeatFrames,
            accepted,
            before,
            after,
            beforeQ.BeatDurationStdDevRatio,
            afterQ.BeatDurationStdDevRatio,
            accepted ? "composite-dp" : "rejected");
    }

    private static double MeanEvidence(float[] evidence, IReadOnlyList<int> frames)
    {
        return frames.Count == 0
            ? 0.0
            : frames.Select(frame => evidence[Math.Clamp(frame, 0, evidence.Length - 1)]).Average();
    }

    private static CompositeDpBeatTrackingResult Noop(int[] frames, string mode)
    {
        return new CompositeDpBeatTrackingResult(frames, false, 0.0, 0.0, 0.0, 0.0, mode);
    }
}
