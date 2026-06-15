using EternalLoop.AnalysisEngine.Core.Models;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking;

public static class BeatFeatureAggregator
{
    private const int LoudnessDimensions = 3;

    private const float MinimumStandardDeviation = 1e-4f;

    public static IReadOnlyList<Beat> AggregateFeatures(
        BeatTrackingResult beatTrackingResult,
        FeatureMatrix features,
        int sampleRate,
        int timeSignature)
    {
        ArgumentNullException.ThrowIfNull(beatTrackingResult);
        ArgumentNullException.ThrowIfNull(features);

        if (sampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be greater than zero.");
        }

        if (timeSignature <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeSignature), "Time signature must be greater than zero.");
        }

        if (features.HopLengthSamples <= 0)
        {
            throw new ArgumentException("Feature hop length must be greater than zero.", nameof(features));
        }

        if (beatTrackingResult.BeatTimes.Length == 0)
        {
            return [];
        }

        var framesPerSecond = sampleRate / (double)features.HopLengthSamples;
        var fallbackDuration = GetFallbackDuration(beatTrackingResult.EstimatedBpm);
        var beatCount = beatTrackingResult.BeatTimes.Length;
        var timbre = new float[beatCount][];
        var pitches = new float[beatCount][];
        var rawLoudness = new float[beatCount][];
        var startTimes = new double[beatCount];
        var durations = new double[beatCount];
        var confidences = new double[beatCount];

        for (var index = 0; index < beatCount; index++)
        {
            var start = beatTrackingResult.BeatTimes[index];
            var duration = GetDuration(beatTrackingResult.BeatTimes, index, fallbackDuration);
            var end = start + duration;
            var startFrame = (int)Math.Floor(start * framesPerSecond);
            var endFrame = (int)Math.Ceiling(end * framesPerSecond);

            startTimes[index] = start;
            durations[index] = duration;
            confidences[index] = index < beatTrackingResult.Confidences.Length ? beatTrackingResult.Confidences[index] : 0.0;
            timbre[index] = AggregateMedian(features.Mfcc, startFrame, endFrame);
            pitches[index] = AggregateMedian(features.Chroma, startFrame, endFrame);
            rawLoudness[index] = AggregateLoudness(features.Rms, startFrame, endFrame);
        }

        var normalizedLoudness = ZScoreNormalize(rawLoudness);
        var beats = new List<Beat>(beatCount);

        for (var index = 0; index < beatCount; index++)
        {
            beats.Add(new Beat
            {
                Index = index,
                Start = startTimes[index],
                Duration = durations[index],
                Confidence = confidences[index],
                Timbre = timbre[index],
                Pitches = pitches[index],
                Loudness = normalizedLoudness[index],
                BarPosition = ComputeBarPosition(index, timeSignature)
            });
        }

        return beats;
    }

    private static float[] AggregateLoudness(float[] rms, int startFrame, int endFrame)
    {
        if (rms.Length == 0)
        {
            return new float[LoudnessDimensions];
        }

        var firstFrame = Math.Clamp(startFrame, 0, rms.Length - 1);
        var lastFrame = Math.Clamp(Math.Max(startFrame, endFrame - 1), 0, rms.Length - 1);

        if (lastFrame < firstFrame)
        {
            lastFrame = firstFrame;
        }

        var loudnessStart = rms[firstFrame];
        var loudnessMax = float.NegativeInfinity;
        var loudnessSum = 0f;
        var frameCount = 0;

        for (var frame = firstFrame; frame <= lastFrame; frame++)
        {
            var value = rms[frame];

            if (value > loudnessMax)
            {
                loudnessMax = value;
            }

            loudnessSum += value;
            frameCount++;
        }

        var loudnessMean = frameCount > 0 ? loudnessSum / frameCount : 0f;

        if (float.IsNegativeInfinity(loudnessMax))
        {
            loudnessMax = 0f;
        }

        return [loudnessStart, loudnessMax, loudnessMean];
    }

    private static float[][] ZScoreNormalize(float[][] rawVectors)
    {
        if (rawVectors.Length == 0)
        {
            return rawVectors;
        }

        var dimensions = rawVectors[0].Length;
        var means = new float[dimensions];
        var standardDeviations = new float[dimensions];

        for (var dimension = 0; dimension < dimensions; dimension++)
        {
            var sum = 0.0;

            for (var index = 0; index < rawVectors.Length; index++)
            {
                sum += rawVectors[index][dimension];
            }

            means[dimension] = (float)(sum / rawVectors.Length);
        }

        for (var dimension = 0; dimension < dimensions; dimension++)
        {
            var sumSquaredDeviations = 0.0;

            for (var index = 0; index < rawVectors.Length; index++)
            {
                var deviation = rawVectors[index][dimension] - means[dimension];
                sumSquaredDeviations += deviation * deviation;
            }

            var variance = sumSquaredDeviations / rawVectors.Length;
            standardDeviations[dimension] = (float)Math.Sqrt(variance);

            if (standardDeviations[dimension] < MinimumStandardDeviation)
            {
                standardDeviations[dimension] = MinimumStandardDeviation;
            }
        }

        var normalized = new float[rawVectors.Length][];

        for (var index = 0; index < rawVectors.Length; index++)
        {
            normalized[index] = new float[dimensions];

            for (var dimension = 0; dimension < dimensions; dimension++)
            {
                normalized[index][dimension] = (rawVectors[index][dimension] - means[dimension]) / standardDeviations[dimension];
            }
        }

        return normalized;
    }

    private static double GetDuration(double[] beatTimes, int index, double fallbackDuration)
    {
        if (index + 1 < beatTimes.Length)
        {
            var nextDuration = beatTimes[index + 1] - beatTimes[index];

            if (nextDuration > 0)
            {
                return nextDuration;
            }
        }

        if (index > 0)
        {
            var previousDuration = beatTimes[index] - beatTimes[index - 1];

            if (previousDuration > 0)
            {
                return previousDuration;
            }
        }

        return fallbackDuration;
    }

    private static double GetFallbackDuration(double estimatedBpm)
    {
        if (estimatedBpm > 0 && double.IsFinite(estimatedBpm))
        {
            return 60.0 / estimatedBpm;
        }

        return 0.5;
    }

    private static float[] AggregateMedian(float[][] vectors, int startFrame, int endFrame)
    {
        if (vectors.Length == 0)
        {
            return [];
        }

        var firstFrame = Math.Clamp(startFrame, 0, vectors.Length - 1);
        var lastFrame = Math.Clamp(Math.Max(startFrame, endFrame - 1), 0, vectors.Length - 1);

        if (lastFrame < firstFrame)
        {
            lastFrame = firstFrame;
        }

        var dimension = vectors[firstFrame].Length;
        var result = new float[dimension];
        var scratch = new List<float>(lastFrame - firstFrame + 1);

        for (var coefficient = 0; coefficient < dimension; coefficient++)
        {
            scratch.Clear();

            for (var frame = firstFrame; frame <= lastFrame; frame++)
            {
                if (coefficient < vectors[frame].Length)
                {
                    scratch.Add(vectors[frame][coefficient]);
                }
            }

            result[coefficient] = Median(scratch);
        }

        return result;
    }

    private static float Median(List<float> values)
    {
        if (values.Count == 0)
        {
            return 0f;
        }

        values.Sort();
        var middle = values.Count / 2;

        if (values.Count % 2 == 1)
        {
            return values[middle];
        }

        return (values[middle - 1] + values[middle]) * 0.5f;
    }

    private static float[] ComputeBarPosition(int beatIndex, int timeSignature)
    {
        var positionInBar = beatIndex % timeSignature;
        var angle = 2.0 * Math.PI * positionInBar / timeSignature;

        return [(float)Math.Sin(angle), (float)Math.Cos(angle)];
    }
}
