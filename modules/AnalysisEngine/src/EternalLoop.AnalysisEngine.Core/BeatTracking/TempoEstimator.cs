namespace EternalLoop.AnalysisEngine.Core.BeatTracking;

public static class TempoEstimator
{
    private const double FallbackBpm = 120.0;

    private const float MinimumEnergy = 1e-9f;

    private const double HighBpmHalfTimeThreshold = 170.0;

    private const double SlowCandidateMinimumBpm = 75.0;

    private const double SlowCandidateMaximumBpm = 135.0;

    public static double EstimateBpm(
        float[] onsetDetectionFunction,
        int hopLengthSamples,
        int sampleRate,
        double minBpm,
        double maxBpm)
    {
        return EstimateBpm(
            onsetDetectionFunction,
            hopLengthSamples,
            sampleRate,
            minBpm,
            maxBpm,
            EternalLoop.AnalysisEngine.Core.Options.BeatTrackingOptions.DefaultTempoCenterBpm,
            EternalLoop.AnalysisEngine.Core.Options.BeatTrackingOptions.DefaultTempoPriorStdOctaves,
            EternalLoop.AnalysisEngine.Core.Options.BeatTrackingOptions.DefaultHalfTimeCompetitivenessThreshold);
    }

    public static double EstimateBpm(
        float[] onsetDetectionFunction,
        int hopLengthSamples,
        int sampleRate,
        double minBpm,
        double maxBpm,
        double tempoCenterBpm,
        double tempoPriorStdOctaves,
        double halfTimeCompetitivenessThreshold)
    {
        var candidates = EstimateCandidates(
            onsetDetectionFunction,
            hopLengthSamples,
            sampleRate,
            minBpm,
            maxBpm,
            tempoCenterBpm,
            tempoPriorStdOctaves,
            halfTimeCompetitivenessThreshold,
            20);

        return candidates.SelectedCandidate?.Bpm ?? ClampBpm(FallbackBpm, minBpm, maxBpm);
    }

    public static TempoCandidateSet EstimateCandidates(
        float[] onsetDetectionFunction,
        int hopLengthSamples,
        int sampleRate,
        double minBpm,
        double maxBpm,
        double tempoCenterBpm,
        double tempoPriorStdOctaves,
        double halfTimeCompetitivenessThreshold,
        int maxCandidates)
    {
        ArgumentNullException.ThrowIfNull(onsetDetectionFunction);

        if (hopLengthSamples <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(hopLengthSamples), "Hop length must be greater than zero.");
        }

        if (sampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be greater than zero.");
        }

        if (minBpm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minBpm), "Minimum BPM must be greater than zero.");
        }

        if (maxBpm <= minBpm)
        {
            throw new ArgumentOutOfRangeException(nameof(maxBpm), "Maximum BPM must be greater than minimum BPM.");
        }

        if (onsetDetectionFunction.Length < 2 || onsetDetectionFunction.Max() <= MinimumEnergy)
        {
            var fallback = new TempoCandidate(
                ClampBpm(FallbackBpm, minBpm, maxBpm),
                0,
                0.0,
                0.0,
                0.0,
                0.0,
                "fallback",
                "unknown");
            return new TempoCandidateSet([fallback], fallback);
        }

        var framesPerSecond = sampleRate / (double)hopLengthSamples;
        var minLag = Math.Max(1, (int)Math.Floor(framesPerSecond * 60.0 / maxBpm));
        var maxLag = Math.Max(minLag + 1, (int)Math.Ceiling(framesPerSecond * 60.0 / minBpm));
        maxLag = Math.Min(maxLag, onsetDetectionFunction.Length - 1);

        if (maxLag < minLag)
        {
            var fallback = new TempoCandidate(
                ClampBpm(FallbackBpm, minBpm, maxBpm),
                0,
                0.0,
                0.0,
                0.0,
                0.0,
                "fallback",
                "unknown");
            return new TempoCandidateSet([fallback], fallback);
        }

        var bestLag = minLag;
        var bestScore = double.NegativeInfinity;
        var scoresByLag = new Dictionary<int, double>();
        var candidates = new List<TempoCandidate>();

        for (var lag = minLag; lag <= maxLag; lag++)
        {
            double sum = 0.0;

            for (var index = 0; index < onsetDetectionFunction.Length - lag; index++)
            {
                sum += onsetDetectionFunction[index] * onsetDetectionFunction[index + lag];
            }

            var bpm = 60.0 * framesPerSecond / lag;
            var perceptualWeight = Math.Exp(-0.5 * Math.Pow(Math.Log2(bpm / tempoCenterBpm) / tempoPriorStdOctaves, 2.0));
            var rawScore = sum / Math.Max(1, onsetDetectionFunction.Length - lag);
            var tempoPenalty = ExtremeTempoPenalty(bpm);
            var score = rawScore * perceptualWeight * tempoPenalty;
            scoresByLag[lag] = score;
            candidates.Add(new TempoCandidate(
                bpm,
                lag,
                rawScore,
                perceptualWeight,
                tempoPenalty,
                score,
                "autocorrelation",
                "primary"));

            if (score > bestScore)
            {
                bestScore = score;
                bestLag = lag;
            }
        }

        bestLag = SelectMusicalTempoLag(bestLag, bestScore, scoresByLag, maxLag, framesPerSecond, halfTimeCompetitivenessThreshold);
        var periodFrames = RefinePeriodFromPeaks(onsetDetectionFunction, bestLag) ?? bestLag;
        var selectedBpm = ClampBpm(60.0 * framesPerSecond / periodFrames, minBpm, maxBpm);
        var selected = candidates.MinBy(candidate => Math.Abs(candidate.Bpm - selectedBpm));

        if (selected is null || Math.Abs(selected.Bpm - selectedBpm) > 0.001)
        {
            selected = new TempoCandidate(
                selectedBpm,
                bestLag,
                scoresByLag.TryGetValue(bestLag, out var score) ? score : 0.0,
                1.0,
                ExtremeTempoPenalty(selectedBpm),
                scoresByLag.TryGetValue(bestLag, out var finalScore) ? finalScore : 0.0,
                "refined-peak",
                "primary");
            candidates.Add(selected);
        }

        AddHarmonicCandidates(candidates, minBpm, maxBpm);

        var top = candidates
            .GroupBy(candidate => Math.Round(candidate.Bpm, 3))
            .Select(group => group.OrderByDescending(candidate => candidate.FinalScore).First())
            .OrderByDescending(candidate => candidate.FinalScore)
            .Take(Math.Max(1, maxCandidates))
            .ToArray();

        return new TempoCandidateSet(top, selected);
    }

    private static void AddHarmonicCandidates(List<TempoCandidate> candidates, double minBpm, double maxBpm)
    {
        foreach (var candidate in candidates.ToArray())
        {
            AddHarmonic(candidates, candidate, candidate.Bpm / 2.0, "half-time", "half", minBpm, maxBpm);
            AddHarmonic(candidates, candidate, candidate.Bpm * 2.0, "double-time", "double", minBpm, maxBpm);
            AddHarmonic(candidates, candidate, candidate.Bpm * 0.75, "three-quarter", "three-quarter", minBpm, maxBpm);
            AddHarmonic(candidates, candidate, candidate.Bpm * 4.0 / 3.0, "four-third", "four-third", minBpm, maxBpm);
        }
    }

    private static void AddHarmonic(
        List<TempoCandidate> candidates,
        TempoCandidate source,
        double bpm,
        string origin,
        string label,
        double minBpm,
        double maxBpm)
    {
        if (bpm < minBpm || bpm > maxBpm)
        {
            return;
        }

        candidates.Add(source with
        {
            Bpm = bpm,
            Lag = source.Lag > 0 ? Math.Max(1, (int)Math.Round(source.Lag * source.Bpm / bpm)) : 0,
            FinalScore = source.FinalScore * 0.92,
            Origin = origin,
            HarmonicLabel = label
        });
    }

    internal static int SelectMusicalTempoLag(
        int bestLag,
        double bestScore,
        IReadOnlyDictionary<int, double> scoresByLag,
        int maxLag,
        double framesPerSecond)
    {
        return SelectMusicalTempoLag(
            bestLag,
            bestScore,
            scoresByLag,
            maxLag,
            framesPerSecond,
            EternalLoop.AnalysisEngine.Core.Options.BeatTrackingOptions.DefaultHalfTimeCompetitivenessThreshold);
    }

    internal static int SelectMusicalTempoLag(
        int bestLag,
        double bestScore,
        IReadOnlyDictionary<int, double> scoresByLag,
        int maxLag,
        double framesPerSecond,
        double halfTimeCompetitivenessThreshold)
    {
        var selectedLag = bestLag;
        var selectedBpm = ConvertLagToBpm(selectedLag, framesPerSecond);

        if (selectedBpm < HighBpmHalfTimeThreshold)
        {
            return selectedLag;
        }

        var slowerLag = selectedLag * 2;
        if (slowerLag > maxLag || !scoresByLag.TryGetValue(slowerLag, out var slowerScore))
        {
            return selectedLag;
        }

        var slowerBpm = ConvertLagToBpm(slowerLag, framesPerSecond);
        if (slowerBpm < SlowCandidateMinimumBpm || slowerBpm > SlowCandidateMaximumBpm)
        {
            return selectedLag;
        }

        var acceptanceRatio = slowerBpm is >= 90.0 and <= 120.0
            ? Math.Min(0.45, halfTimeCompetitivenessThreshold)
            : halfTimeCompetitivenessThreshold;

        return slowerScore >= bestScore * acceptanceRatio
            ? slowerLag
            : selectedLag;
    }

    internal static double ConvertLagToBpm(int lag, double framesPerSecond)
    {
        return 60.0 * framesPerSecond / lag;
    }

    private static double ExtremeTempoPenalty(double bpm)
    {
        if (bpm is >= 85.0 and <= 150.0)
        {
            return 1.0;
        }

        if (bpm < 70.0 || bpm > 190.0)
        {
            return 0.85;
        }

        return 0.95;
    }

    private static double? RefinePeriodFromPeaks(float[] onsetDetectionFunction, int referenceLag)
    {
        var max = onsetDetectionFunction.Max();
        if (max <= MinimumEnergy)
        {
            return null;
        }

        var threshold = max * 0.5f;
        var peaks = new List<int>();

        for (var index = 0; index < onsetDetectionFunction.Length; index++)
        {
            var left = index == 0 ? float.MinValue : onsetDetectionFunction[index - 1];
            var right = index == onsetDetectionFunction.Length - 1 ? float.MinValue : onsetDetectionFunction[index + 1];

            if (onsetDetectionFunction[index] >= threshold &&
                onsetDetectionFunction[index] >= left &&
                onsetDetectionFunction[index] > right)
            {
                peaks.Add(index);
            }
        }

        if (peaks.Count < 3)
        {
            return null;
        }

        var intervals = new List<int>();

        for (var index = 1; index < peaks.Count; index++)
        {
            var interval = peaks[index] - peaks[index - 1];

            if (interval >= referenceLag * 0.75 && interval <= referenceLag * 1.5)
            {
                intervals.Add(interval);
            }
        }

        if (intervals.Count == 0)
        {
            return null;
        }

        return intervals.Average();
    }

    private static double ClampBpm(double bpm, double minBpm, double maxBpm)
    {
        return Math.Clamp(bpm, minBpm, maxBpm);
    }
}
