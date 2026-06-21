namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Ai.Advisor;

public sealed class BeatThisAdvisorPostprocessor
{
    private readonly BeatThisAdvisorPostprocessOptions _options;

    public BeatThisAdvisorPostprocessor(BeatThisAdvisorPostprocessOptions? options = null)
    {
        _options = options ?? new BeatThisAdvisorPostprocessOptions();
        ValidateOptions(_options);
    }

    public BeatThisAdvisorPostprocessResult Postprocess(BeatThisAdvisorOutput output)
    {
        ArgumentNullException.ThrowIfNull(output);

        if (output.FrameCount <= 0 || output.FrameRate <= 0.0)
        {
            throw new InvalidDataException("Beat This advisor output must contain positive frame count and frame rate.");
        }

        if (output.BeatLogits.Length < output.FrameCount || output.DownbeatLogits.Length < output.FrameCount)
        {
            throw new InvalidDataException("Beat This advisor logits length is smaller than frame count.");
        }

        var beatThreshold = CalculatePercentile(output.BeatLogits, output.FrameCount, _options.BeatThresholdPercentile);
        var downbeatThreshold = CalculatePercentile(output.DownbeatLogits, output.FrameCount, _options.DownbeatThresholdPercentile);
        var minBeatSpacingFrames = SecondsToFrames(_options.MinBeatSpacingSeconds, output.FrameRate);
        var minDownbeatSpacingFrames = SecondsToFrames(_options.MinDownbeatSpacingSeconds, output.FrameRate);
        var beatFrames = PickPeaks(
            output.BeatLogits,
            output.FrameCount,
            beatThreshold,
            _options.LocalMaximaWindowFrames,
            minBeatSpacingFrames);
        var downbeatFrames = PickPeaks(
            output.DownbeatLogits,
            output.FrameCount,
            downbeatThreshold,
            _options.LocalMaximaWindowFrames,
            minDownbeatSpacingFrames);
        var beatTimes = beatFrames
            .Select(frame => FrameToSeconds(frame, output.FrameRate))
            .ToArray();
        var downbeatTimes = downbeatFrames
            .Select(frame => FrameToSeconds(frame, output.FrameRate))
            .ToArray();
        var confidences = beatFrames
            .Select(frame => Sigmoid(output.BeatLogits[frame]))
            .ToArray();
        var estimatedBpm = EstimateBpm(beatTimes);
        var rejection = ValidateGuards(beatTimes, estimatedBpm, output.DurationSeconds);

        return new BeatThisAdvisorPostprocessResult
        {
            BeatTimes = beatTimes,
            DownbeatTimes = downbeatTimes,
            BeatConfidences = confidences,
            EstimatedBpm = estimatedBpm,
            IsDenseGrid = rejection is not null,
            RejectionReason = rejection,
            Transform = "raw",
            Algorithm = "local_maxima_percentile_min_spacing"
        };
    }

    private string? ValidateGuards(double[] beatTimes, double estimatedBpm, double durationSeconds)
    {
        if (beatTimes.Length < 2)
        {
            return "beat-count-too-low";
        }

        if (!double.IsFinite(estimatedBpm) || estimatedBpm < _options.MinBpm || estimatedBpm > _options.MaxBpm)
        {
            return $"bpm-out-of-range:{estimatedBpm:0.###}";
        }

        if (durationSeconds > 0.0)
        {
            var density = beatTimes.Length / durationSeconds;

            if (density > _options.MaxBeatDensityPerSecond)
            {
                return $"beat-density-too-high:{density:0.###}";
            }
        }

        if (_options.ReferenceBeatCount is > 0)
        {
            var countRatio = beatTimes.Length / (double)_options.ReferenceBeatCount.Value;

            if (countRatio > _options.MaxCountRatio)
            {
                return $"count-ratio-too-high:{countRatio:0.###}";
            }
        }

        var intervals = beatTimes
            .Zip(beatTimes.Skip(1), (left, right) => right - left)
            .Where(interval => interval > 0.0)
            .Order()
            .ToArray();

        if (intervals.Length == 0)
        {
            return "beat-intervals-missing";
        }

        var median = intervals[intervals.Length / 2];

        if (median < _options.MinMedianIntervalSeconds)
        {
            return $"median-interval-too-low:{median:0.###}";
        }

        return null;
    }

    private static int[] PickPeaks(
        float[] logits,
        int frameCount,
        double threshold,
        int localMaximaWindowFrames,
        int minSpacingFrames)
    {
        var candidates = new List<(int Frame, float Score)>();

        for (var frame = 0; frame < frameCount; frame++)
        {
            var value = logits[frame];

            if (!float.IsFinite(value) || value < threshold)
            {
                continue;
            }

            var isLocalMaximum = true;
            var left = Math.Max(0, frame - localMaximaWindowFrames);
            var right = Math.Min(frameCount - 1, frame + localMaximaWindowFrames);

            for (var other = left; other <= right; other++)
            {
                if (other != frame && logits[other] > value)
                {
                    isLocalMaximum = false;
                    break;
                }
            }

            if (isLocalMaximum)
            {
                candidates.Add((frame, value));
            }
        }

        var selected = new List<int>();

        foreach (var candidate in candidates.OrderByDescending(candidate => candidate.Score))
        {
            if (selected.All(frame => Math.Abs(frame - candidate.Frame) >= minSpacingFrames))
            {
                selected.Add(candidate.Frame);
            }
        }

        selected.Sort();

        return selected.ToArray();
    }

    private static double CalculatePercentile(float[] values, int count, double percentile)
    {
        percentile = percentile > 1.0 ? percentile / 100.0 : percentile;
        percentile = Math.Clamp(percentile, 0.0, 1.0);

        var finite = values
            .Take(count)
            .Where(float.IsFinite)
            .Order()
            .ToArray();

        if (finite.Length == 0)
        {
            return 0.0;
        }

        var index = (int)Math.Round((finite.Length - 1) * percentile);

        return finite[Math.Clamp(index, 0, finite.Length - 1)];
    }

    private static double EstimateBpm(double[] beatTimes)
    {
        if (beatTimes.Length < 2)
        {
            return 0.0;
        }

        var intervals = beatTimes
            .Zip(beatTimes.Skip(1), (left, right) => right - left)
            .Where(interval => interval > 0.0)
            .Order()
            .ToArray();

        if (intervals.Length == 0)
        {
            return 0.0;
        }

        var median = intervals[intervals.Length / 2];

        return median > 0.0 ? 60.0 / median : 0.0;
    }

    private static double FrameToSeconds(int frame, double frameRate)
    {
        return frame / frameRate;
    }

    private static int SecondsToFrames(double seconds, double frameRate)
    {
        return Math.Max(1, (int)Math.Round(seconds * frameRate));
    }

    private static double Sigmoid(float value)
    {
        if (value >= 0.0f)
        {
            var z = Math.Exp(-value);
            return 1.0 / (1.0 + z);
        }

        var exp = Math.Exp(value);
        return exp / (1.0 + exp);
    }

    private static void ValidateOptions(BeatThisAdvisorPostprocessOptions options)
    {
        if (options.LocalMaximaWindowFrames < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.LocalMaximaWindowFrames));
        }

        if (options.BeatThresholdPercentile is < 0.0 or > 100.0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.BeatThresholdPercentile));
        }

        if (options.DownbeatThresholdPercentile is < 0.0 or > 100.0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.DownbeatThresholdPercentile));
        }

        if (options.MinBeatSpacingSeconds <= 0.0 || options.MinDownbeatSpacingSeconds <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.MinBeatSpacingSeconds));
        }

        if (options.MinBpm <= 0.0 || options.MaxBpm <= options.MinBpm)
        {
            throw new ArgumentException("Advisor BPM bounds must be positive and ordered.");
        }

        if (options.MaxBeatDensityPerSecond <= 0.0 || options.MaxCountRatio <= 0.0)
        {
            throw new ArgumentException("Advisor density/count guardrails must be positive.");
        }
    }
}
