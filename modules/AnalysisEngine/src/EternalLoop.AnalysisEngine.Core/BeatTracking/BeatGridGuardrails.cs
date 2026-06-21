using EternalLoop.AnalysisEngine.Core.Models;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking;

public sealed class BeatGridGuardrails
{
    private readonly BeatGridGuardrailOptions _options;

    public BeatGridGuardrails(BeatGridGuardrailOptions? options = null)
    {
        _options = options ?? new BeatGridGuardrailOptions();
        ValidateOptions(_options);
    }

    public BeatGridGuardrailResult Validate(BeatTrackingResult result, LoadedAudio audio)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(audio);

        if (result.BeatTimes.Length < _options.MinBeatCount)
        {
            return BeatGridGuardrailResult.Invalid(
                $"beat-count-too-low:{result.BeatTimes.Length}");
        }

        if (result.Confidences.Length != result.BeatTimes.Length)
        {
            return BeatGridGuardrailResult.Invalid(
                $"confidence-count-mismatch:beats={result.BeatTimes.Length},confidences={result.Confidences.Length}");
        }

        if (!double.IsFinite(result.EstimatedBpm)
            || result.EstimatedBpm < _options.MinBpm
            || result.EstimatedBpm > _options.MaxBpm)
        {
            return BeatGridGuardrailResult.Invalid(
                $"bpm-out-of-range:{result.EstimatedBpm:0.###}");
        }

        if (!IsStrictlyIncreasingFinite(result.BeatTimes))
        {
            return BeatGridGuardrailResult.Invalid("beat-times-not-strictly-increasing");
        }

        if (!AreFinite(result.Confidences))
        {
            return BeatGridGuardrailResult.Invalid("confidences-not-finite");
        }

        var meanConfidence = result.Confidences.Average();

        if (meanConfidence < _options.MinMeanConfidence)
        {
            return BeatGridGuardrailResult.Invalid(
                $"confidence-too-low:{meanConfidence:0.###}");
        }

        if (audio.DurationSeconds > 0.0)
        {
            var beatsPerSecond = result.BeatTimes.Length / audio.DurationSeconds;

            if (beatsPerSecond > _options.MaxBeatsPerSecond)
            {
                return BeatGridGuardrailResult.Invalid(
                    $"beat-density-too-high:{beatsPerSecond:0.###}");
            }
        }

        var intervalResult = ValidateIntervals(result.BeatTimes);

        if (!intervalResult.IsValid)
        {
            return intervalResult;
        }

        var downbeatResult = ValidateDownbeats(result.DownbeatTimes, result.BeatTimes);

        if (!downbeatResult.IsValid)
        {
            return downbeatResult;
        }

        if (ShouldValidateAiCoverage(result, audio))
        {
            var coverageRatio = result.BeatProviderCoverageRatio;

            if (!double.IsFinite(coverageRatio) || coverageRatio < _options.MinAiCoverageRatio)
            {
                return BeatGridGuardrailResult.Invalid(
                    $"ai-coverage-too-low:{coverageRatio:0.###}");
            }
        }

        return BeatGridGuardrailResult.Valid();
    }

    private BeatGridGuardrailResult ValidateIntervals(double[] beatTimes)
    {
        if (beatTimes.Length < 3)
        {
            return BeatGridGuardrailResult.Valid();
        }

        var intervals = new double[beatTimes.Length - 1];

        for (var index = 0; index < intervals.Length; index++)
        {
            intervals[index] = beatTimes[index + 1] - beatTimes[index];

            if (!double.IsFinite(intervals[index]) || intervals[index] <= 0.0)
            {
                return BeatGridGuardrailResult.Invalid("beat-interval-not-positive");
            }
        }

        var mean = intervals.Average();

        if (mean <= 0.0)
        {
            return BeatGridGuardrailResult.Invalid("beat-interval-mean-not-positive");
        }

        var variance = intervals
            .Select(interval => Math.Pow(interval - mean, 2.0))
            .Average();
        var stdDevRatio = Math.Sqrt(variance) / mean;

        if (stdDevRatio > _options.MaxBeatIntervalStdDevRatio)
        {
            return BeatGridGuardrailResult.Invalid(
                $"beat-interval-stddev-too-high:{stdDevRatio:0.###}");
        }

        return BeatGridGuardrailResult.Valid();
    }

    private BeatGridGuardrailResult ValidateDownbeats(
        double[] downbeatTimes,
        double[] beatTimes)
    {
        if (downbeatTimes.Length == 0)
        {
            return BeatGridGuardrailResult.Valid();
        }

        if (!IsStrictlyIncreasingFinite(downbeatTimes))
        {
            return BeatGridGuardrailResult.Invalid("downbeat-times-not-strictly-increasing");
        }

        foreach (var downbeatTime in downbeatTimes)
        {
            var nearestDistance = beatTimes
                .Select(beatTime => Math.Abs(beatTime - downbeatTime))
                .Min();

            if (nearestDistance > _options.MaxDownbeatToBeatDistanceSeconds)
            {
                return BeatGridGuardrailResult.Invalid(
                    $"downbeat-not-aligned-to-beat:{nearestDistance:0.###}");
            }
        }

        return BeatGridGuardrailResult.Valid();
    }

    private static bool ShouldValidateAiCoverage(BeatTrackingResult result, LoadedAudio audio)
    {
        if (!result.UsedAiProvider || audio.DurationSeconds <= 0.0)
        {
            return false;
        }

        return result.BeatProviderChunkCount > 0
            || result.BeatProviderValidFrameCount > 0
            || result.BeatProviderCoverageSeconds > 0.0
            || result.BeatProviderCoverageRatio > 0.0;
    }

    private static bool IsStrictlyIncreasingFinite(double[] values)
    {
        if (values.Length == 0)
        {
            return true;
        }

        if (!double.IsFinite(values[0]))
        {
            return false;
        }

        for (var index = 1; index < values.Length; index++)
        {
            if (!double.IsFinite(values[index]) || values[index] <= values[index - 1])
            {
                return false;
            }
        }

        return true;
    }

    private static bool AreFinite(double[] values)
    {
        return values.All(double.IsFinite);
    }

    private static void ValidateOptions(BeatGridGuardrailOptions options)
    {
        if (options.MinBeatCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "MinBeatCount must be greater than zero.");
        }

        if (options.MinBpm <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "MinBpm must be positive.");
        }

        if (options.MaxBpm <= options.MinBpm)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "MaxBpm must be greater than MinBpm.");
        }

        if (options.MaxBeatsPerSecond <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "MaxBeatsPerSecond must be positive.");
        }

        if (options.MaxBeatIntervalStdDevRatio < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "MaxBeatIntervalStdDevRatio must be non-negative.");
        }

        if (options.MaxDownbeatToBeatDistanceSeconds < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "MaxDownbeatToBeatDistanceSeconds must be non-negative.");
        }

        if (options.MinMeanConfidence < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "MinMeanConfidence must be non-negative.");
        }

        if (options.MinAiCoverageRatio is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "MinAiCoverageRatio must be between 0 and 1.");
        }
    }
}
