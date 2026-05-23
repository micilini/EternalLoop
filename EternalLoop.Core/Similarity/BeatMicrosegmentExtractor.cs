using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;

namespace EternalLoop.Core.Similarity;

public static class BeatMicrosegmentExtractor
{
    private const int LoudnessDimensions = 3;
    private const float MinimumStandardDeviation = 1e-4f;

    public static IReadOnlyList<BeatMicroFingerprint> Extract(
        IReadOnlyList<Beat> beats,
        FeatureMatrix features,
        int sampleRate,
        int microsegmentCount)
    {
        ArgumentNullException.ThrowIfNull(beats);
        ArgumentNullException.ThrowIfNull(features);

        if (sampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be greater than zero.");
        }

        if (features.HopLengthSamples <= 0)
        {
            throw new ArgumentException("Feature hop length must be greater than zero.", nameof(features));
        }

        if (microsegmentCount < TuningDefaultValues.MinMicrosegmentCount ||
            microsegmentCount > TuningDefaultValues.MaxMicrosegmentCount)
        {
            throw new ArgumentOutOfRangeException(nameof(microsegmentCount));
        }

        if (beats.Count == 0 || GetFrameCount(features) == 0)
        {
            return [];
        }

        var framesPerSecond = sampleRate / (double)features.HopLengthSamples;
        var rawFingerprints = new List<RawFingerprint>(beats.Count);
        var rawLoudness = new List<float[]>();
        var rawSegments = new List<RawSegment>(beats.Count * microsegmentCount);

        foreach (var beat in beats)
        {
            var beatSegments = new RawSegment[microsegmentCount];
            var duration = double.IsFinite(beat.Duration) && beat.Duration > 0.0
                ? beat.Duration
                : 0.0;
            var subDuration = duration / microsegmentCount;

            for (var segmentIndex = 0; segmentIndex < microsegmentCount; segmentIndex++)
            {
                var start = beat.Start + (segmentIndex * subDuration);
                var end = start + subDuration;
                var startFrame = (int)Math.Floor(start * framesPerSecond);
                var endFrame = (int)Math.Ceiling(end * framesPerSecond);
                var loudness = AggregateLoudness(features.Rms, startFrame, endFrame);

                var segment = new RawSegment(
                    beat.Index,
                    segmentIndex,
                    SanitizeDouble(start),
                    SanitizeDouble(subDuration),
                    microsegmentCount <= 1 ? 0f : segmentIndex / (float)(microsegmentCount - 1),
                    AggregateMedian(features.Mfcc, startFrame, endFrame),
                    AggregateMedian(features.Chroma, startFrame, endFrame),
                    loudness,
                    AggregateFlux(features.SpectralFlux, startFrame, endFrame));

                beatSegments[segmentIndex] = segment;
                rawSegments.Add(segment);
                rawLoudness.Add(loudness);
            }

            rawFingerprints.Add(new RawFingerprint(beat.Index, beatSegments));
        }

        var normalizedLoudness = ZScoreNormalize(rawLoudness.ToArray());
        var normalizedSegments = new Dictionary<RawSegment, BeatMicrosegment>(rawSegments.Count);
        for (var i = 0; i < rawSegments.Count; i++)
        {
            var raw = rawSegments[i];
            normalizedSegments[raw] = new BeatMicrosegment
                {
                    BeatIndex = raw.BeatIndex,
                    SegmentIndex = raw.SegmentIndex,
                    Start = raw.Start,
                    Duration = raw.Duration,
                    RelativePosition = raw.RelativePosition,
                    Timbre = raw.Timbre,
                    Pitches = raw.Pitches,
                    Loudness = normalizedLoudness[i],
                    Flux = raw.Flux
                };
        }

        return rawFingerprints
            .Select(fingerprint => new BeatMicroFingerprint
            {
                BeatIndex = fingerprint.BeatIndex,
                Microsegments = fingerprint.Segments.Select(segment => normalizedSegments[segment]).ToArray()
            })
            .ToArray();
    }

    private static int GetFrameCount(FeatureMatrix features)
    {
        return Math.Max(
            Math.Max(features.Mfcc.Length, features.Chroma.Length),
            Math.Max(features.Rms.Length, features.SpectralFlux.Length));
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
                    scratch.Add(SanitizeFloat(vectors[frame][coefficient]));
                }
            }

            result[coefficient] = Median(scratch);
        }

        return result;
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

        var loudnessStart = SanitizeFloat(rms[firstFrame]);
        var loudnessMax = float.NegativeInfinity;
        var loudnessSum = 0f;
        var frameCount = 0;

        for (var frame = firstFrame; frame <= lastFrame; frame++)
        {
            var value = SanitizeFloat(rms[frame]);
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

    private static float AggregateFlux(float[] flux, int startFrame, int endFrame)
    {
        if (flux.Length == 0)
        {
            return 0f;
        }

        var firstFrame = Math.Clamp(startFrame, 0, flux.Length - 1);
        var lastFrame = Math.Clamp(Math.Max(startFrame, endFrame - 1), 0, flux.Length - 1);
        if (lastFrame < firstFrame)
        {
            lastFrame = firstFrame;
        }

        var sum = 0.0;
        var count = 0;
        for (var frame = firstFrame; frame <= lastFrame; frame++)
        {
            sum += Math.Max(0f, SanitizeFloat(flux[frame]));
            count++;
        }

        return count == 0 ? 0f : (float)(sum / count);
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

    private static double SanitizeDouble(double value)
    {
        return double.IsFinite(value) ? value : 0.0;
    }

    private static float SanitizeFloat(float value)
    {
        return float.IsFinite(value) ? value : 0f;
    }

    private sealed record RawFingerprint(int BeatIndex, IReadOnlyList<RawSegment> Segments);

    private sealed record RawSegment(
        int BeatIndex,
        int SegmentIndex,
        double Start,
        double Duration,
        float RelativePosition,
        float[] Timbre,
        float[] Pitches,
        float[] Loudness,
        float Flux);
}
