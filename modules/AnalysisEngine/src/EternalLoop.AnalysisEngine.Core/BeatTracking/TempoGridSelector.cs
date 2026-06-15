namespace EternalLoop.AnalysisEngine.Core.BeatTracking;

public static class TempoGridSelector
{
    private const float MinimumEnergy = 1e-9f;

    public static TempoCandidate Select(
        float[] onsetDetectionFunction,
        IReadOnlyList<TempoCandidate> candidates,
        double framesPerSecond,
        double tightnessLambda)
    {
        ArgumentNullException.ThrowIfNull(onsetDetectionFunction);
        ArgumentNullException.ThrowIfNull(candidates);

        if (candidates.Count == 0 || onsetDetectionFunction.Length == 0 || framesPerSecond <= 0.0)
        {
            return candidates.FirstOrDefault() ?? new TempoCandidate(120.0, 0, 0.0, 0.0, 0.0, 0.0, "fallback", "unknown");
        }

        var maxCandidateScore = Math.Max(MinimumEnergy, (float)candidates.Max(candidate => candidate.FinalScore));
        var maxOdf = Math.Max(MinimumEnergy, onsetDetectionFunction.Max());
        var peaks = FindPeaks(onsetDetectionFunction, maxOdf * 0.20f);
        var best = candidates[0];
        var bestScore = double.NegativeInfinity;

        foreach (var candidate in candidates.Where(candidate => candidate.Bpm >= 85.0).Take(30))
        {
            var periodFrames = framesPerSecond * 60.0 / candidate.Bpm;
            if (periodFrames <= 0.0 || !double.IsFinite(periodFrames))
            {
                continue;
            }

            var beatFrames = BeatAligner.AlignBeatsDynamicProgramming(onsetDetectionFunction, periodFrames, tightnessLambda);
            if (beatFrames.Length < 4)
            {
                beatFrames = BuildRegularGrid(onsetDetectionFunction.Length, periodFrames);
            }

            var normalizedCandidateScore = candidate.FinalScore / maxCandidateScore;
            var onsetHitRate = CalculateOnsetHitRate(beatFrames, peaks, periodFrames);
            var confidenceMean = beatFrames
                .Where(frame => frame >= 0 && frame < onsetDetectionFunction.Length)
                .Select(frame => (double)onsetDetectionFunction[frame] / maxOdf)
                .DefaultIfEmpty(0.0)
                .Average();
            var coverageScore = CalculateCoverageScore(beatFrames, onsetDetectionFunction.Length);
            var regularityScore = CalculateRegularityScore(beatFrames, periodFrames);
            var barPhaseStability = CalculateBarPhaseStability(beatFrames, onsetDetectionFunction, periodFrames, maxOdf);
            var originPenalty = candidate.Origin == "refined-peak" ? 0.04 : 0.0;

            var score =
                0.30 * normalizedCandidateScore
                + 0.25 * onsetHitRate
                + 0.15 * confidenceMean
                + 0.12 * coverageScore
                + 0.10 * regularityScore
                + 0.08 * barPhaseStability
                - originPenalty;

            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        if (best.Bpm < 100.0)
        {
            var doubled = candidates
                .Where(candidate => candidate.Bpm >= 150.0 && Math.Abs(candidate.Bpm - (best.Bpm * 2.0)) / Math.Max(1.0, best.Bpm * 2.0) <= 0.03)
                .OrderByDescending(candidate => candidate.FinalScore)
                .FirstOrDefault();
            if (doubled is not null)
            {
                return doubled;
            }
        }

        var topPrimary = candidates
            .Where(candidate => candidate.Bpm >= 85.0 && candidate.Origin == "autocorrelation")
            .OrderByDescending(candidate => candidate.FinalScore)
            .FirstOrDefault();
        if (topPrimary is not null && topPrimary.FinalScore >= best.FinalScore * 0.98)
        {
            return topPrimary;
        }

        return best;
    }

    private static int[] FindPeaks(float[] values, float threshold)
    {
        var peaks = new List<int>();
        for (var index = 1; index < values.Length - 1; index++)
        {
            if (values[index] >= threshold && values[index] >= values[index - 1] && values[index] > values[index + 1])
            {
                peaks.Add(index);
            }
        }

        return peaks.ToArray();
    }

    private static double CalculateOnsetHitRate(IReadOnlyList<int> beatFrames, IReadOnlyList<int> peaks, double periodFrames)
    {
        if (beatFrames.Count == 0 || peaks.Count == 0)
        {
            return 0.0;
        }

        var window = Math.Max(1, (int)Math.Round(periodFrames * 0.12));
        var hits = beatFrames.Count(frame => peaks.Any(peak => Math.Abs(peak - frame) <= window));
        return hits / (double)beatFrames.Count;
    }

    private static double CalculateCoverageScore(IReadOnlyList<int> beatFrames, int length)
    {
        if (beatFrames.Count < 2 || length <= 0)
        {
            return 0.0;
        }

        var span = beatFrames[^1] - beatFrames[0];
        return Math.Clamp(span / (double)length, 0.0, 1.0);
    }

    private static double CalculateRegularityScore(IReadOnlyList<int> beatFrames, double periodFrames)
    {
        var quality = BeatGridRefiner.BeatGridQuality.Measure(beatFrames, periodFrames);
        return Math.Clamp(1.0 - quality.BeatDurationStdDevRatio / 0.25, 0.0, 1.0);
    }

    private static double CalculateBarPhaseStability(
        IReadOnlyList<int> beatFrames,
        IReadOnlyList<float> onsetDetectionFunction,
        double periodFrames,
        float maxOdf)
    {
        if (beatFrames.Count < 8)
        {
            return 0.0;
        }

        var downbeatScores = new List<double>();
        var otherScores = new List<double>();
        for (var index = 0; index < beatFrames.Count; index++)
        {
            var frame = Math.Clamp(beatFrames[index], 0, onsetDetectionFunction.Count - 1);
            var score = onsetDetectionFunction[frame] / maxOdf;
            if (index % 4 == 0)
            {
                downbeatScores.Add(score);
            }
            else
            {
                otherScores.Add(score);
            }
        }

        var downbeat = downbeatScores.DefaultIfEmpty(0.0).Average();
        var other = otherScores.DefaultIfEmpty(0.0).Average();
        return Math.Clamp(0.5 + (downbeat - other), 0.0, 1.0);
    }

    private static int[] BuildRegularGrid(int length, double periodFrames)
    {
        var frames = new List<int>();
        var step = Math.Max(1, (int)Math.Round(periodFrames));
        for (var frame = 0; frame < length; frame += step)
        {
            frames.Add(frame);
        }

        return frames.ToArray();
    }
}
