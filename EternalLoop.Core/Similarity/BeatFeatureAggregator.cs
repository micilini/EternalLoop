using EternalLoop.Contracts.Models;

namespace EternalLoop.Core.Similarity;

public static class BeatFeatureAggregator
{
    private const int LoudnessDimensions = 3;
    private const float MinimumStandardDeviation = 1e-4f;

    public static IReadOnlyList<Beat> AggregateFeatures(
        BeatTrackingResult beatTrackingResult,
        FeatureMatrix features,
        int sampleRate,
        int timeSignature = 4)
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

        for (var i = 0; i < beatCount; i++)
        {
            var start = beatTrackingResult.BeatTimes[i];
            var duration = GetDuration(beatTrackingResult.BeatTimes, i, fallbackDuration);
            var end = start + duration;
            var startFrame = (int)Math.Floor(start * framesPerSecond);
            var endFrame = (int)Math.Ceiling(end * framesPerSecond);

            startTimes[i] = start;
            durations[i] = duration;
            confidences[i] = i < beatTrackingResult.Confidences.Length ? beatTrackingResult.Confidences[i] : 0.0;
            timbre[i] = AggregateMedian(features.Mfcc, startFrame, endFrame);
            pitches[i] = AggregateMedian(features.Chroma, startFrame, endFrame);
            rawLoudness[i] = AggregateLoudness(features.Rms, startFrame, endFrame);
        }

        var normalizedLoudness = ZScoreNormalize(rawLoudness);
        var beats = new List<Beat>(beatCount);

        for (var i = 0; i < beatCount; i++)
        {
            beats.Add(new Beat
            {
                Index = i,
                Start = startTimes[i],
                Duration = durations[i],
                Confidence = confidences[i],
                Timbre = timbre[i],
                Pitches = pitches[i],
                Loudness = normalizedLoudness[i],
                BarPosition = ComputeBarPosition(i, timeSignature)
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
        var stds = new float[dimensions];

        for (var d = 0; d < dimensions; d++)
        {
            var sum = 0.0;
            for (var i = 0; i < rawVectors.Length; i++)
            {
                sum += rawVectors[i][d];
            }

            means[d] = (float)(sum / rawVectors.Length);
        }

        for (var d = 0; d < dimensions; d++)
        {
            var sumSquaredDeviations = 0.0;
            for (var i = 0; i < rawVectors.Length; i++)
            {
                var deviation = rawVectors[i][d] - means[d];
                sumSquaredDeviations += deviation * deviation;
            }

            var variance = sumSquaredDeviations / rawVectors.Length;
            stds[d] = (float)Math.Sqrt(variance);
            if (stds[d] < MinimumStandardDeviation)
            {
                stds[d] = MinimumStandardDeviation;
            }
        }

        var normalized = new float[rawVectors.Length][];
        for (var i = 0; i < rawVectors.Length; i++)
        {
            normalized[i] = new float[dimensions];
            for (var d = 0; d < dimensions; d++)
            {
                normalized[i][d] = (rawVectors[i][d] - means[d]) / stds[d];
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
