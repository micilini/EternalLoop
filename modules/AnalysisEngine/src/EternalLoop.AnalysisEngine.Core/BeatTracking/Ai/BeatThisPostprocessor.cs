using EternalLoop.AnalysisEngine.Core.BeatTracking;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Ai;

public sealed class BeatThisPostprocessor
{
    private readonly BeatThisPostprocessorOptions _options;

    public BeatThisPostprocessor(BeatThisPostprocessorOptions? options = null)
    {
        _options = options ?? new BeatThisPostprocessorOptions();
        ValidateOptions(_options);
    }

    public BeatTrackingResult Postprocess(
        BeatThisInferenceResult inference,
        BeatThisAvailability availability)
    {
        ArgumentNullException.ThrowIfNull(inference);
        ArgumentNullException.ThrowIfNull(availability);

        if (!availability.IsAvailable)
        {
            throw new InvalidOperationException($"Beat This model is unavailable: {availability.ErrorMessage}");
        }

        if (inference.FrameRate <= 0.0)
        {
            throw new InvalidDataException("Beat This inference frame rate must be positive.");
        }

        if (inference.ValidFrameCount <= 0)
        {
            throw new InvalidDataException("Beat This inference did not produce valid frames.");
        }

        var normalizedBeatActivations = NormalizeActivations(inference.BeatActivations, inference.ValidFrameCount);
        var normalizedDownbeatActivations = NormalizeActivations(inference.DownbeatActivations, inference.ValidFrameCount);

        var beatFrames = PickPeaks(
            normalizedBeatActivations,
            inference.ValidFrameCount,
            _options.BeatThreshold,
            _options.AdaptivePeakPercentile,
            SecondsToFrames(_options.MinBeatSpacingSeconds, inference.FrameRate));

        var downbeatFrames = PickPeaks(
            normalizedDownbeatActivations,
            inference.ValidFrameCount,
            _options.DownbeatThreshold,
            _options.AdaptivePeakPercentile,
            SecondsToFrames(_options.MinDownbeatSpacingSeconds, inference.FrameRate));

        var beatTimes = beatFrames
            .Select(frame => FrameToSeconds(frame, inference.FrameRate))
            .ToArray();
        var rawDownbeatTimes = downbeatFrames
            .Select(frame => FrameToSeconds(frame, inference.FrameRate))
            .ToArray();
        var confidences = beatFrames
            .Select(frame => (double)normalizedBeatActivations[frame])
            .ToArray();

        if (beatTimes.Length < 2)
        {
            throw new InvalidDataException("Beat This postprocessor did not detect any beats.");
        }

        var downbeatSnap = SnapDownbeatsToNearestBeats(
            rawDownbeatTimes,
            beatTimes,
            _options.MaxDownbeatSnapDistanceSeconds);
        var estimatedMeter = EstimateMeterFromDownbeats(
            downbeatSnap.BeatIndexes,
            _options.DefaultMeter,
            _options.MeterCandidates);
        var beatNumbers = BuildBeatNumbers(
            beatTimes.Length,
            downbeatSnap.BeatIndexes,
            estimatedMeter);
        var metadata = availability.Metadata ?? new BeatThisModelMetadata();
        var estimatedBpm = EstimateBpm(beatTimes, _options);

        var beatActivationSummary = BeatThisActivationSummary.From(
            normalizedBeatActivations,
            inference.ValidFrameCount,
            _options.BeatThreshold);
        var downbeatActivationSummary = BeatThisActivationSummary.From(
            normalizedDownbeatActivations,
            inference.ValidFrameCount,
            _options.DownbeatThreshold);
        var coverageSeconds = inference.ValidFrameCount / inference.FrameRate;
        var coverageRatio = inference.AudioDurationSeconds <= 0.0
            ? 0.0
            : Math.Min(1.0, coverageSeconds / inference.AudioDurationSeconds);

        return new BeatTrackingResult
        {
            EstimatedBpm = estimatedBpm,
            BeatTimes = beatTimes,
            Confidences = confidences,
            DownbeatTimes = downbeatSnap.DownbeatTimes,
            BeatNumbers = beatNumbers,
            EstimatedMeter = estimatedMeter,
            ProviderName = "beat-this",
            ProviderVersion = metadata.Version,
            ProviderLicense = metadata.License,
            ModelName = metadata.Name,
            ModelSha256 = availability.ModelSha256 ?? metadata.ModelSha256 ?? "none",
            UsedAiProvider = true,
            UsedBuiltInProvider = false,
            UsedFallbackProvider = false,
            BeatGridMode = "beat-this-onnx-musical-v2-full-track",
            BeatProviderOutputMode = inference.OutputMode,
            BeatProviderChunkCount = inference.ChunkCount,
            BeatProviderValidFrameCount = inference.ValidFrameCount,
            BeatProviderCoverageSeconds = coverageSeconds,
            BeatProviderCoverageRatio = coverageRatio,
            BeatActivationSummary = beatActivationSummary,
            DownbeatActivationSummary = downbeatActivationSummary
        };
    }

    private static float[] NormalizeActivations(float[] activations, int validFrameCount)
    {
        if (activations.Length < validFrameCount)
        {
            throw new InvalidDataException("Beat This activation length is smaller than valid frame count.");
        }

        var normalized = new float[validFrameCount];
        var requiresSigmoid = false;

        for (var index = 0; index < validFrameCount; index++)
        {
            var value = activations[index];

            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                value = 0.0f;
            }

            if (value is < 0.0f or > 1.0f)
            {
                requiresSigmoid = true;
            }

            normalized[index] = value;
        }

        if (requiresSigmoid)
        {
            for (var index = 0; index < validFrameCount; index++)
            {
                normalized[index] = Sigmoid(normalized[index]);
            }
        }
        else
        {
            for (var index = 0; index < validFrameCount; index++)
            {
                normalized[index] = Math.Clamp(normalized[index], 0.0f, 1.0f);
            }
        }

        var min = float.PositiveInfinity;
        var max = float.NegativeInfinity;

        for (var index = 0; index < validFrameCount; index++)
        {
            var value = normalized[index];

            if (value < min)
            {
                min = value;
            }

            if (value > max)
            {
                max = value;
            }
        }

        var range = max - min;

        if (range <= 1e-6f)
        {
            return normalized;
        }

        for (var index = 0; index < validFrameCount; index++)
        {
            normalized[index] = (normalized[index] - min) / range;
        }

        return normalized;
    }

    private static float Sigmoid(float value)
    {
        if (value >= 0.0f)
        {
            var z = Math.Exp(-value);
            return (float)(1.0 / (1.0 + z));
        }

        var exp = Math.Exp(value);
        return (float)(exp / (1.0 + exp));
    }

    private static int[] PickPeaks(
        float[] activations,
        int validFrameCount,
        double threshold,
        double adaptivePeakPercentile,
        int minSpacingFrames)
    {
        var candidates = BuildPeakCandidates(
            activations,
            validFrameCount,
            threshold,
            requireThreshold: true);

        var selected = SelectSpacedPeaks(candidates, minSpacingFrames);

        if (selected.Length >= 2)
        {
            return selected;
        }

        var adaptiveThreshold = CalculatePercentile(
            activations,
            validFrameCount,
            adaptivePeakPercentile);

        candidates = BuildPeakCandidates(
            activations,
            validFrameCount,
            Math.Max(threshold, adaptiveThreshold),
            requireThreshold: true);

        return SelectSpacedPeaks(candidates, minSpacingFrames);
    }

    private static List<(int Frame, float Score)> BuildPeakCandidates(
        float[] activations,
        int validFrameCount,
        double threshold,
        bool requireThreshold)
    {
        var candidates = new List<(int Frame, float Score)>();

        for (var frame = 0; frame < validFrameCount; frame++)
        {
            var current = activations[frame];

            if (requireThreshold && current < threshold)
            {
                continue;
            }

            var previous = frame > 0 ? activations[frame - 1] : float.NegativeInfinity;
            var next = frame + 1 < validFrameCount ? activations[frame + 1] : float.NegativeInfinity;

            if (current >= previous && current >= next)
            {
                candidates.Add((frame, current));
            }
        }

        return candidates;
    }

    private static int[] SelectSpacedPeaks(
        IReadOnlyList<(int Frame, float Score)> candidates,
        int minSpacingFrames)
    {
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

    private static double CalculatePercentile(
        float[] activations,
        int validFrameCount,
        double percentile)
    {
        if (validFrameCount <= 0)
        {
            return 0.0;
        }

        percentile = Math.Clamp(percentile, 0.0, 1.0);

        var values = new float[validFrameCount];

        Array.Copy(activations, values, validFrameCount);
        Array.Sort(values);

        var index = (int)Math.Round((values.Length - 1) * percentile);

        return values[Math.Clamp(index, 0, values.Length - 1)];
    }

    private static DownbeatSnapResult SnapDownbeatsToNearestBeats(
        IReadOnlyList<double> rawDownbeatTimes,
        IReadOnlyList<double> beatTimes,
        double maxDistanceSeconds)
    {
        var selected = new List<(double Time, int BeatIndex)>();
        var usedBeatIndexes = new HashSet<int>();

        foreach (var downbeatTime in rawDownbeatTimes)
        {
            var nearest = FindNearestBeatIndex(downbeatTime, beatTimes);
            var distance = Math.Abs(beatTimes[nearest] - downbeatTime);

            if (distance > maxDistanceSeconds || !usedBeatIndexes.Add(nearest))
            {
                continue;
            }

            selected.Add((beatTimes[nearest], nearest));
        }

        selected.Sort((left, right) => left.BeatIndex.CompareTo(right.BeatIndex));

        return new DownbeatSnapResult
        {
            DownbeatTimes = selected.Select(item => item.Time).ToArray(),
            BeatIndexes = selected.Select(item => item.BeatIndex).ToArray()
        };
    }

    private static int FindNearestBeatIndex(double targetTime, IReadOnlyList<double> beatTimes)
    {
        var nearestIndex = 0;
        var nearestDistance = double.MaxValue;

        for (var index = 0; index < beatTimes.Count; index++)
        {
            var distance = Math.Abs(beatTimes[index] - targetTime);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = index;
            }
        }

        return nearestIndex;
    }

    private static int EstimateMeterFromDownbeats(
        IReadOnlyList<int> downbeatIndexes,
        int defaultMeter,
        IReadOnlyList<int> meterCandidates)
    {
        if (downbeatIndexes.Count < 2)
        {
            return defaultMeter;
        }

        var gaps = downbeatIndexes
            .Zip(downbeatIndexes.Skip(1), (left, right) => right - left)
            .Where(gap => gap > 1)
            .ToArray();

        if (gaps.Length == 0)
        {
            return defaultMeter;
        }

        var allowed = meterCandidates
            .Where(candidate => candidate > 1)
            .Distinct()
            .Order()
            .ToArray();

        if (allowed.Length == 0)
        {
            return defaultMeter;
        }

        var directMatch = gaps
            .Where(gap => allowed.Contains(gap))
            .GroupBy(gap => gap)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => Math.Abs(group.Key - defaultMeter))
            .FirstOrDefault();

        if (directMatch is not null)
        {
            return directMatch.Key;
        }

        var medianGap = gaps.Order().ElementAt(gaps.Length / 2);

        return allowed
            .OrderBy(candidate => Math.Abs(candidate - medianGap))
            .ThenBy(candidate => Math.Abs(candidate - defaultMeter))
            .First();
    }

    private static int[] BuildBeatNumbers(
        int beatCount,
        IReadOnlyList<int> downbeatIndexes,
        int meter)
    {
        var beatNumbers = new int[beatCount];

        if (beatCount == 0)
        {
            return beatNumbers;
        }

        if (meter <= 1)
        {
            meter = 4;
        }

        var downbeatSet = downbeatIndexes
            .Where(index => index >= 0 && index < beatCount)
            .Distinct()
            .Order()
            .ToArray();

        if (downbeatSet.Length == 0)
        {
            for (var index = 0; index < beatCount; index++)
            {
                beatNumbers[index] = (index % meter) + 1;
            }

            return beatNumbers;
        }

        var currentDownbeatPointer = 0;

        for (var index = 0; index < beatCount; index++)
        {
            while (currentDownbeatPointer + 1 < downbeatSet.Length
                && downbeatSet[currentDownbeatPointer + 1] <= index)
            {
                currentDownbeatPointer++;
            }

            if (index < downbeatSet[0])
            {
                beatNumbers[index] = (index % meter) + 1;
                continue;
            }

            var currentDownbeatIndex = downbeatSet[currentDownbeatPointer];
            beatNumbers[index] = ((index - currentDownbeatIndex) % meter) + 1;
        }

        return beatNumbers;
    }

    private static int SecondsToFrames(double seconds, double frameRate)
    {
        return Math.Max(1, (int)Math.Round(seconds * frameRate));
    }

    private static double FrameToSeconds(int frame, double frameRate)
    {
        return frame / frameRate;
    }

    private static double EstimateBpm(double[] beatTimes, BeatThisPostprocessorOptions options)
    {
        if (beatTimes.Length < 2)
        {
            return options.FallbackBpm;
        }

        var intervals = beatTimes
            .Zip(beatTimes.Skip(1), (left, right) => right - left)
            .Where(interval => interval > 0.0)
            .ToArray();

        var plausibleIntervals = intervals
            .Where(interval =>
            {
                var bpm = 60.0 / interval;

                return bpm >= options.MinBpm && bpm <= options.MaxBpm;
            })
            .Order()
            .ToArray();

        var source = plausibleIntervals.Length > 0
            ? plausibleIntervals
            : intervals.Order().ToArray();

        if (source.Length == 0)
        {
            return options.FallbackBpm;
        }

        var median = source[source.Length / 2];

        return median <= 0.0 ? options.FallbackBpm : 60.0 / median;
    }

    private static void ValidateOptions(BeatThisPostprocessorOptions options)
    {
        if (options.BeatThreshold is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.BeatThreshold));
        }

        if (options.DownbeatThreshold is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.DownbeatThreshold));
        }

        if (options.AdaptivePeakPercentile is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.AdaptivePeakPercentile));
        }

        if (options.MinBeatSpacingSeconds <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.MinBeatSpacingSeconds));
        }

        if (options.MinDownbeatSpacingSeconds <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.MinDownbeatSpacingSeconds));
        }

        if (options.MaxDownbeatSnapDistanceSeconds < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.MaxDownbeatSnapDistanceSeconds));
        }

        if (options.MinBpm <= 0.0 || options.MaxBpm <= options.MinBpm)
        {
            throw new ArgumentException("Beat This BPM bounds must be positive and ordered.");
        }
    }

    private sealed class DownbeatSnapResult
    {
        public required double[] DownbeatTimes { get; init; }

        public required int[] BeatIndexes { get; init; }
    }
}
