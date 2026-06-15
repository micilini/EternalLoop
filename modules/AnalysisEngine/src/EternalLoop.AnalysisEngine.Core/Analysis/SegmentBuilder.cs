using EternalLoop.AnalysisEngine.Core.Models;

namespace EternalLoop.AnalysisEngine.Core.Analysis;

public static class SegmentBuilder
{
    private const double DefaultConfidence = 1.0;
    private const double DefaultLoudnessMaxTime = 0.0;
    private const double MaximumUncompactedSegmentsPerSecond = 8.0;
    private const double TargetSegmentDurationSeconds = 0.22;
    private const double MinimumNoveltySegmentDurationSeconds = 0.09;
    private const double MaximumNoveltySegmentDurationSeconds = 0.70;
    private const double MinimumTargetSegmentsPerSecond = 4.0;
    private const double MaximumTargetSegmentsPerSecond = 6.0;
    private const double MinimumWideSegmentsPerSecond = 3.0;
    private const double MaximumWideSegmentsPerSecond = 7.5;

    public static IReadOnlyList<Segment> Build(FeatureMatrix features, int sampleRate)
    {
        ArgumentNullException.ThrowIfNull(features);

        if (sampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be greater than zero.");
        }

        if (features.FrameSizeSamples <= 0)
        {
            throw new ArgumentException("Feature frame size must be greater than zero.", nameof(features));
        }

        if (features.HopLengthSamples <= 0)
        {
            throw new ArgumentException("Feature hop length must be greater than zero.", nameof(features));
        }

        var frameCount = Math.Min(features.Mfcc.Length, features.Chroma.Length);
        frameCount = Math.Min(frameCount, features.Rms.Length);

        if (frameCount == 0)
        {
            return [];
        }

        return BuildTemporal(features, sampleRate);
    }

    public static IReadOnlyList<Segment> Build(
        FeatureMatrix features,
        int sampleRate,
        bool acousticSegmentation,
        bool evidenceConfidences)
    {
        if (!acousticSegmentation)
        {
            return Build(features, sampleRate);
        }

        return BuildDetailed(features, sampleRate, acousticSegmentation, evidenceConfidences).Segments;
    }

    public static SegmentBuildResult BuildDetailed(
        FeatureMatrix features,
        int sampleRate,
        bool acousticSegmentation,
        bool evidenceConfidences)
    {
        if (!acousticSegmentation)
        {
            return new SegmentBuildResult
            {
                Segments = Build(features, sampleRate),
                Mode = "temporal-fallback"
            };
        }

        ValidateInputs(features, sampleRate);

        var frameCount = GetFrameCount(features);
        if (frameCount < 3)
        {
            return new SegmentBuildResult
            {
                Segments = Build(features, sampleRate),
                Mode = "temporal-fallback"
            };
        }

        var novelty = FeatureNovelty.BuildFrameNovelty(features);
        if (FeatureNovelty.Variance(novelty) < 1e-8)
        {
            return new SegmentBuildResult
            {
                Segments = Build(features, sampleRate),
                Mode = "temporal-fallback"
            };
        }

        var boundaryResult = BuildNoveltyBoundaries(novelty, features, sampleRate);
        var boundaries = boundaryResult.Boundaries;
        if (boundaries.Count < 2)
        {
            return new SegmentBuildResult
            {
                Segments = Build(features, sampleRate),
                Mode = "temporal-fallback"
            };
        }

        var frameDuration = features.FrameSizeSamples / (double)sampleRate;
        var minNovelty = novelty.Min();
        var maxNovelty = novelty.Max();
        var segments = new List<Segment>(boundaries.Count - 1);

        for (var index = 0; index < boundaries.Count - 1; index++)
        {
            var segment = BuildSegment(features, sampleRate, boundaries[index], boundaries[index + 1], frameDuration);

            if (evidenceConfidences)
            {
                var confidence = index == 0 ? 0.5 : FeatureNovelty.Normalize(novelty[boundaries[index]], minNovelty, maxNovelty);
                segment = WithSegmentConfidence(segment, confidence);
            }

            segments.Add(segment);
        }

        return new SegmentBuildResult
        {
            Segments = segments,
            Mode = features.HpssApplied && boundaryResult.Mode == "novelty" ? "novelty-hpss-v8" : boundaryResult.Mode,
            NoveltyBoundaryRatio = CalculateNoveltyBoundaryRatio(novelty, boundaries),
            TargetDensity = boundaryResult.TargetDensity,
            ActualDensity = (segments.Count) / Math.Max(features.HopLengthSamples / (double)sampleRate, frameCount * features.HopLengthSamples / (double)sampleRate),
            CandidateCount = boundaryResult.CandidateCount,
            SelectedCount = Math.Max(0, boundaries.Count - 1)
        };
    }

    private static IReadOnlyList<Segment> BuildTemporal(FeatureMatrix features, int sampleRate)
    {
        ValidateInputs(features, sampleRate);

        var frameCount = GetFrameCount(features);

        if (frameCount == 0)
        {
            return [];
        }

        var frameDuration = features.FrameSizeSamples / (double)sampleRate;
        var hopDuration = features.HopLengthSamples / (double)sampleRate;
        var totalCoverage = (frameCount - 1) * hopDuration + frameDuration;
        var segmentsPerSecond = frameCount / Math.Max(frameDuration, totalCoverage);
        var framesPerSegment = segmentsPerSecond <= MaximumUncompactedSegmentsPerSecond
            ? 1
            : Math.Max(1, (int)Math.Round(TargetSegmentDurationSeconds / hopDuration));

        var segments = new List<Segment>((int)Math.Ceiling(frameCount / (double)framesPerSegment));

        for (var frame = 0; frame < frameCount; frame += framesPerSegment)
        {
            var exclusiveEnd = Math.Min(frameCount, frame + framesPerSegment);

            segments.Add(BuildSegment(features, sampleRate, frame, exclusiveEnd, frameDuration));
        }

        return segments;
    }

    private static BoundaryBuildResult BuildNoveltyBoundaries(double[] novelty, FeatureMatrix features, int sampleRate)
    {
        var hopDuration = features.HopLengthSamples / (double)sampleRate;
        var frameCount = novelty.Length;
        var targetDensity = EstimateTargetDensity(novelty, features, hopDuration);
        var minimumTargetCount = (int)Math.Ceiling(frameCount * hopDuration * Math.Max(MinimumWideSegmentsPerSecond, targetDensity - 1.2));
        var maximumTargetCount = (int)Math.Floor(frameCount * hopDuration * Math.Min(MaximumWideSegmentsPerSecond, targetDensity + 1.2));
        maximumTargetCount = Math.Max(minimumTargetCount, maximumTargetCount);
        var targetCount = Math.Clamp((int)Math.Round(frameCount * hopDuration * targetDensity), minimumTargetCount, maximumTargetCount);
        var minFrames = Math.Max(1, (int)Math.Round(MinimumNoveltySegmentDurationSeconds / hopDuration));
        var maxFrames = Math.Max(minFrames + 1, (int)Math.Round(MaximumNoveltySegmentDurationSeconds / hopDuration));
        var average = novelty.Average();
        var stdDev = Math.Sqrt(FeatureNovelty.Variance(novelty));
        var best = new List<int>();
        var bestDelta = int.MaxValue;

        for (var multiplier = 1.4; multiplier >= 0.2; multiplier -= 0.1)
        {
            var threshold = average + multiplier * stdDev;
            var candidates = FindNoveltyPeaks(novelty, threshold)
                .OrderByDescending(frame => novelty[frame])
                .ToList();
            var boundaries = new SortedSet<int> { 0, frameCount };

            foreach (var candidate in candidates)
            {
                var previous = boundaries.GetViewBetween(0, candidate).Max;
                var next = boundaries.GetViewBetween(candidate, frameCount).Min;

                if (candidate - previous >= minFrames && next - candidate >= minFrames)
                {
                    boundaries.Add(candidate);
                }
            }

            SplitLongSegments(boundaries, novelty, threshold, maxFrames, minFrames, frameCount);
            PruneDenseBoundaries(boundaries, novelty, minFrames, frameCount, maximumSegmentCount: (int)Math.Floor(frameCount * hopDuration * MaximumWideSegmentsPerSecond));
            var list = boundaries.ToList();
            var density = (list.Count - 1) / Math.Max(hopDuration, frameCount * hopDuration);
            var inWideRange = density >= Math.Max(MinimumWideSegmentsPerSecond, targetDensity - 0.35)
                && density <= Math.Min(MaximumWideSegmentsPerSecond, targetDensity + 0.75);
            var delta = Math.Abs(list.Count - 1 - targetCount);

            if (inWideRange)
            {
                return new BoundaryBuildResult(list, "novelty", targetDensity, candidates.Count);
            }

            if (delta < bestDelta)
            {
                best = list;
                bestDelta = delta;
            }
        }

        if (best.Count > 1)
        {
            FillToMinimumDensity(
                best,
                novelty,
                minFrames,
                frameCount,
                minimumSegmentCount: Math.Max(
                    (int)Math.Ceiling(frameCount * hopDuration * Math.Max(MinimumWideSegmentsPerSecond, targetDensity - 0.35)) + 1,
                    Math.Min(maximumTargetCount + 1, targetCount + 1)));

            return new BoundaryBuildResult(best, "novelty", targetDensity, CountPeaks(novelty));
        }

        return best.Count > 1
            ? new BoundaryBuildResult(best, "novelty", targetDensity, CountPeaks(novelty))
            : new BoundaryBuildResult(BuildSnappedTemporalBoundaries(frameCount, features, sampleRate, novelty), "snapped-temporal", targetDensity, CountPeaks(novelty));
    }

    private static double EstimateTargetDensity(double[] novelty, FeatureMatrix features, double hopDuration)
    {
        var onset = features.HpssApplied && features.PercussiveOnsetEnvelope.Length == novelty.Length
            ? features.PercussiveOnsetEnvelope
            : features.OnsetEnvelope.Length == novelty.Length ? features.OnsetEnvelope : features.SpectralFlux;
        var activeRatio = onset.Length == 0
            ? 0.0
            : onset.Count(value => value >= 0.25f) / (double)onset.Length;
        var noveltyStd = Math.Sqrt(FeatureNovelty.Variance(novelty));
        var density = 4.0 + activeRatio * 2.2 + Math.Clamp(noveltyStd, 0.0, 2.0) * 0.35;
        return Math.Clamp(density, MinimumWideSegmentsPerSecond, MaximumWideSegmentsPerSecond);
    }

    private static int CountPeaks(double[] novelty)
    {
        if (novelty.Length < 3)
        {
            return 0;
        }

        var average = novelty.Average();
        return FindNoveltyPeaks(novelty, average).Count;
    }

    private static IReadOnlyList<int> FindNoveltyPeaks(double[] novelty, double threshold)
    {
        var peaks = new List<int>();

        for (var frame = 1; frame < novelty.Length - 1; frame++)
        {
            if (novelty[frame] >= threshold &&
                novelty[frame] >= novelty[frame - 1] &&
                novelty[frame] > novelty[frame + 1])
            {
                peaks.Add(frame);
            }
        }

        return peaks;
    }

    private static void SplitLongSegments(
        SortedSet<int> boundaries,
        double[] novelty,
        double threshold,
        int maxFrames,
        int minFrames,
        int frameCount)
    {
        var changed = true;
        while (changed)
        {
            changed = false;
            var pairs = boundaries.Zip(boundaries.Skip(1)).ToArray();

            foreach (var (start, end) in pairs)
            {
                if (end - start <= maxFrames)
                {
                    continue;
                }

                var searchStart = start + minFrames;
                var searchEnd = Math.Min(end - minFrames, frameCount - 1);
                if (searchEnd <= searchStart)
                {
                    continue;
                }

                var best = searchStart;
                for (var frame = searchStart + 1; frame <= searchEnd; frame++)
                {
                    if (novelty[frame] > novelty[best])
                    {
                        best = frame;
                    }
                }

                if (novelty[best] >= threshold)
                {
                    boundaries.Add(best);
                    changed = true;
                    break;
                }
            }
        }
    }

    private static void PruneDenseBoundaries(
        SortedSet<int> boundaries,
        double[] novelty,
        int minFrames,
        int frameCount,
        int maximumSegmentCount)
    {
        while (boundaries.Count - 1 > maximumSegmentCount)
        {
            var removable = boundaries
                .Where(boundary => boundary > 0 && boundary < frameCount)
                .OrderBy(boundary => novelty[boundary])
                .ToArray();

            var removed = false;
            foreach (var boundary in removable)
            {
                var previous = boundaries.GetViewBetween(0, boundary).Max;
                var next = boundaries.GetViewBetween(boundary, frameCount).Min;

                if (next - previous >= minFrames)
                {
                    boundaries.Remove(boundary);
                    removed = true;
                    break;
                }
            }

            if (!removed)
            {
                break;
            }
        }
    }

    private static void FillToMinimumDensity(
        List<int> boundaries,
        double[] novelty,
        int minFrames,
        int frameCount,
        int minimumSegmentCount)
    {
        var set = new SortedSet<int>(boundaries);
        var candidates = Enumerable.Range(1, Math.Max(0, frameCount - 2))
            .Where(frame => !set.Contains(frame))
            .OrderByDescending(frame => novelty[frame])
            .ToArray();

        foreach (var candidate in candidates)
        {
            if (set.Count - 1 >= minimumSegmentCount)
            {
                break;
            }

            var previous = set.GetViewBetween(0, candidate).Max;
            var next = set.GetViewBetween(candidate, frameCount).Min;

            if (candidate - previous >= minFrames && next - candidate >= minFrames)
            {
                set.Add(candidate);
            }
        }

        boundaries.Clear();
        boundaries.AddRange(set);
    }

    private static List<int> BuildTemporalBoundaries(int frameCount, FeatureMatrix features, int sampleRate)
    {
        var hopDuration = features.HopLengthSamples / (double)sampleRate;
        var framesPerSegment = Math.Max(1, (int)Math.Round(TargetSegmentDurationSeconds / hopDuration));
        var boundaries = new List<int>();

        for (var frame = 0; frame < frameCount; frame += framesPerSegment)
        {
            boundaries.Add(frame);
        }

        if (boundaries[^1] != frameCount)
        {
            boundaries.Add(frameCount);
        }

        return boundaries;
    }

    private static List<int> BuildSnappedTemporalBoundaries(
        int frameCount,
        FeatureMatrix features,
        int sampleRate,
        double[] novelty)
    {
        var boundaries = BuildTemporalBoundaries(frameCount, features, sampleRate);
        var hopDuration = features.HopLengthSamples / (double)sampleRate;
        var window = Math.Max(1, (int)Math.Round(TargetSegmentDurationSeconds / hopDuration * 0.35));

        for (var index = 1; index < boundaries.Count - 1; index++)
        {
            var nominal = boundaries[index];
            var start = Math.Max(boundaries[index - 1] + 1, nominal - window);
            var end = Math.Min(boundaries[index + 1] - 1, nominal + window);

            if (end <= start)
            {
                continue;
            }

            var best = start;
            for (var frame = start + 1; frame <= end; frame++)
            {
                if (novelty[frame] > novelty[best])
                {
                    best = frame;
                }
            }

            boundaries[index] = best;
        }

        return boundaries.Distinct().Order().ToList();
    }

    private static int GetFrameCount(FeatureMatrix features)
    {
        var frameCount = Math.Min(features.Mfcc.Length, features.Chroma.Length);
        frameCount = Math.Min(frameCount, features.Rms.Length);
        return frameCount;
    }

    private static void ValidateInputs(FeatureMatrix features, int sampleRate)
    {
        ArgumentNullException.ThrowIfNull(features);

        if (sampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be greater than zero.");
        }

        if (features.FrameSizeSamples <= 0)
        {
            throw new ArgumentException("Feature frame size must be greater than zero.", nameof(features));
        }

        if (features.HopLengthSamples <= 0)
        {
            throw new ArgumentException("Feature hop length must be greater than zero.", nameof(features));
        }
    }

    private static Segment BuildSegment(
        FeatureMatrix features,
        int sampleRate,
        int firstFrame,
        int exclusiveEndFrame,
        double frameDuration)
    {
        var start = firstFrame * features.HopLengthSamples / (double)sampleRate;
        var lastFrame = exclusiveEndFrame - 1;
        var end = lastFrame * features.HopLengthSamples / (double)sampleRate + frameDuration;
        var loudnessMaxFrame = firstFrame;
        var loudnessMax = features.Rms[firstFrame];

        for (var frame = firstFrame + 1; frame < exclusiveEndFrame; frame++)
        {
            if (features.Rms[frame] > loudnessMax)
            {
                loudnessMax = features.Rms[frame];
                loudnessMaxFrame = frame;
            }
        }

        return new Segment
        {
            Start = start,
            Duration = Math.Max(0.0, end - start),
            Confidence = DefaultConfidence,
            LoudnessStart = features.Rms[firstFrame],
            LoudnessMax = loudnessMax,
            LoudnessMaxTime = Math.Max(DefaultLoudnessMaxTime, loudnessMaxFrame * features.HopLengthSamples / (double)sampleRate - start),
            Timbre = AverageVectors(features.Mfcc, firstFrame, exclusiveEndFrame, normalize: false),
            Pitches = AverageVectors(features.Chroma, firstFrame, exclusiveEndFrame, normalize: true)
        };
    }

    private static Segment WithSegmentConfidence(Segment segment, double confidence)
    {
        return new Segment
        {
            Start = segment.Start,
            Duration = segment.Duration,
            Confidence = Math.Clamp(confidence, 0.0, 1.0),
            LoudnessStart = segment.LoudnessStart,
            LoudnessMax = segment.LoudnessMax,
            LoudnessMaxTime = segment.LoudnessMaxTime,
            Timbre = segment.Timbre,
            Pitches = segment.Pitches
        };
    }

    private static double CalculateNoveltyBoundaryRatio(double[] novelty, IReadOnlyList<int> boundaries)
    {
        if (novelty.Length == 0 || boundaries.Count <= 2)
        {
            return 0.0;
        }

        var boundaryFrames = boundaries
            .Where(frame => frame > 0 && frame < novelty.Length)
            .ToHashSet();
        var boundaryAverage = boundaryFrames.Count == 0 ? 0.0 : boundaryFrames.Average(frame => novelty[frame]);
        var outside = Enumerable.Range(1, novelty.Length - 2)
            .Where(frame => !boundaryFrames.Contains(frame))
            .ToArray();
        var outsideAverage = outside.Length == 0 ? 0.0 : outside.Average(frame => novelty[frame]);

        return outsideAverage > 0.0 ? boundaryAverage / outsideAverage : 0.0;
    }

    private sealed record BoundaryBuildResult(List<int> Boundaries, string Mode, double TargetDensity, int CandidateCount);

    private static float[] AverageVectors(float[][] vectors, int firstFrame, int exclusiveEndFrame, bool normalize)
    {
        var length = vectors[firstFrame].Length;
        var average = new float[length];
        var count = Math.Max(1, exclusiveEndFrame - firstFrame);

        for (var frame = firstFrame; frame < exclusiveEndFrame; frame++)
        {
            for (var index = 0; index < length; index++)
            {
                var value = vectors[frame][index];
                if (float.IsFinite(value))
                {
                    average[index] += value;
                }
            }
        }

        var max = 0.0f;
        for (var index = 0; index < length; index++)
        {
            average[index] /= count;
            max = Math.Max(max, average[index]);
        }

        if (normalize && max > 0.0f)
        {
            for (var index = 0; index < length; index++)
            {
                average[index] /= max;
            }
        }

        return average;
    }
}
