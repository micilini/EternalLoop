using EternalLoop.AnalysisEngine.Core.Models;

namespace EternalLoop.AnalysisEngine.Core.Analysis;

public static class TimeQuantumBuilder
{
    private const double DefaultConfidence = 1.0;

    private const string FullTrackSectionLabel = "Full Track";

    private const int DefaultBarsPerSection = 8;

    private const int LongTrackBarsPerSection = 16;

    private const double MinimumSectionDurationSeconds = 8.0;

    private const double LongTrackDurationSeconds = 240.0;

    private const int MaximumSectionCount = 16;

    public static IReadOnlyList<Bar> BuildBars(IReadOnlyList<Beat> beats, int timeSignature)
    {
        return BuildBars(beats, timeSignature, 0);
    }

    public static IReadOnlyList<Bar> BuildBars(IReadOnlyList<Beat> beats, int timeSignature, int barPhase)
    {
        ArgumentNullException.ThrowIfNull(beats);

        if (timeSignature <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeSignature), "Time signature must be greater than zero.");
        }

        if (beats.Count == 0)
        {
            return [];
        }

        var bars = new List<Bar>();

        var phase = Math.Clamp(barPhase, 0, timeSignature - 1);
        for (var startIndex = phase; startIndex < beats.Count; startIndex += timeSignature)
        {
            var firstBeat = beats[startIndex];
            var lastIndex = Math.Min(beats.Count - 1, startIndex + timeSignature - 1);
            var lastBeat = beats[lastIndex];
            var duration = Math.Max(0.0, lastBeat.Start + lastBeat.Duration - firstBeat.Start);

            bars.Add(new Bar
            {
                Index = bars.Count,
                Start = firstBeat.Start,
                Duration = duration,
                Confidence = AverageConfidence(beats, startIndex, lastIndex)
            });
        }

        return bars;
    }

    public static IReadOnlyList<Tatum> BuildTatums(IReadOnlyList<Beat> beats)
    {
        ArgumentNullException.ThrowIfNull(beats);

        var tatums = new List<Tatum>(beats.Count * 2);

        for (var index = 0; index < beats.Count; index++)
        {
            var beat = beats[index];
            var duration = beat.Duration;

            if (index < beats.Count - 1)
            {
                duration = Math.Max(0.0, beats[index + 1].Start - beat.Start);
            }

            if (duration <= 0.0)
            {
                duration = beat.Duration;
            }

            var halfDuration = Math.Max(0.0, duration / 2.0);

            tatums.Add(new Tatum
            {
                Index = tatums.Count,
                Start = beat.Start,
                Duration = halfDuration,
                Confidence = beat.Confidence
            });

            tatums.Add(new Tatum
            {
                Index = tatums.Count,
                Start = beat.Start + halfDuration,
                Duration = halfDuration,
                Confidence = beat.Confidence
            });
        }

        return tatums;
    }

    public static IReadOnlyList<Tatum> BuildFixedTwoPerBeatTatums(IReadOnlyList<Beat> beats)
    {
        ArgumentNullException.ThrowIfNull(beats);

        return BuildTatums(beats);
    }

    public static IReadOnlyList<Tatum> BuildTatums(
        IReadOnlyList<Beat> beats,
        float[] onsetDetectionFunction,
        double framesPerSecond,
        bool adaptiveTatums,
        bool evidenceConfidences)
    {
        if (!adaptiveTatums || onsetDetectionFunction.Length == 0 || framesPerSecond <= 0)
        {
            return BuildTatums(beats);
        }

        var tatums = new List<Tatum>(beats.Count * 2);
        var maxOdf = Math.Max(1e-9f, onsetDetectionFunction.Max());

        for (var index = 0; index < beats.Count; index++)
        {
            var beat = beats[index];
            var nextStart = index + 1 < beats.Count ? beats[index + 1].Start : beat.Start + beat.Duration;
            var duration = Math.Max(beat.Duration, nextStart - beat.Start);
            var startFrame = Math.Clamp((int)Math.Round(beat.Start * framesPerSecond), 0, onsetDetectionFunction.Length - 1);
            var endFrame = Math.Clamp((int)Math.Round((beat.Start + duration) * framesPerSecond), startFrame + 1, onsetDetectionFunction.Length);
            var internalPeaks = FindInternalOnsetPeaks(onsetDetectionFunction, startFrame, endFrame, maxOdf);
            var subdivisionCount = Math.Clamp(internalPeaks.Count + 1, 2, 4);

            if (subdivisionCount > 2 && tatums.Count + subdivisionCount > beats.Count * 2.6)
            {
                subdivisionCount = 2;
            }

            var starts = new List<double> { beat.Start };

            if (subdivisionCount == 2 && internalPeaks.Count > 0)
            {
                starts.Add(internalPeaks.OrderBy(frame => Math.Abs(frame - (startFrame + endFrame) / 2.0)).First() / framesPerSecond);
            }
            else if (subdivisionCount > 2)
            {
                starts.AddRange(internalPeaks.Take(subdivisionCount - 1).Select(frame => frame / framesPerSecond));
            }

            while (starts.Count < subdivisionCount)
            {
                starts.Add(beat.Start + duration * starts.Count / subdivisionCount);
            }

            starts = starts
                .Select(start => Math.Clamp(start, beat.Start, beat.Start + duration))
                .DistinctBy(start => Math.Round(start, 6))
                .Order()
                .ToList();

            while (starts.Count < 2)
            {
                starts.Add(beat.Start + duration / 2.0);
                starts.Sort();
            }

            for (var tatumIndex = 0; tatumIndex < starts.Count; tatumIndex++)
            {
                var start = starts[tatumIndex];
                var end = tatumIndex + 1 < starts.Count ? starts[tatumIndex + 1] : beat.Start + duration;
                var frame = Math.Clamp((int)Math.Round(start * framesPerSecond), 0, onsetDetectionFunction.Length - 1);

                tatums.Add(new Tatum
                {
                    Index = tatums.Count,
                    Start = start,
                    Duration = Math.Max(1e-6, end - start),
                    Confidence = evidenceConfidences
                        ? Math.Clamp(onsetDetectionFunction[frame] / maxOdf, 0.0, 1.0)
                        : beat.Confidence
                });
            }
        }

        return tatums.Count / (double)Math.Max(1, beats.Count) > 2.6 ? BuildTatums(beats) : tatums;
    }

    public static IReadOnlyList<Section> BuildSections(
        LoadedAudio audio,
        IReadOnlyList<Bar> bars,
        double tempo)
    {
        ArgumentNullException.ThrowIfNull(audio);
        ArgumentNullException.ThrowIfNull(bars);

        if (audio.DurationSeconds <= 0)
        {
            return [];
        }

        if (audio.DurationSeconds <= 60.0 || bars.Count < DefaultBarsPerSection * 2)
        {
            return [CreateSection(0, 0.0, audio.DurationSeconds, tempo, FullTrackSectionLabel)];
        }

        var barsPerSection = audio.DurationSeconds >= LongTrackDurationSeconds
            ? LongTrackBarsPerSection
            : DefaultBarsPerSection;

        var sections = new List<Section>();
        for (var startBar = 0; startBar < bars.Count && sections.Count < MaximumSectionCount; startBar += barsPerSection)
        {
            var firstBar = bars[startBar];
            var nextStartBar = Math.Min(bars.Count, startBar + barsPerSection);
            var sectionEnd = nextStartBar < bars.Count
                ? bars[nextStartBar].Start
                : audio.DurationSeconds;
            var duration = Math.Max(0.0, sectionEnd - firstBar.Start);

            if (duration < MinimumSectionDurationSeconds && nextStartBar < bars.Count)
            {
                continue;
            }

            sections.Add(CreateSection(sections.Count, firstBar.Start, duration, tempo, $"Section {sections.Count + 1}"));
        }

        if (sections.Count < 2)
        {
            return [CreateSection(0, 0.0, audio.DurationSeconds, tempo, FullTrackSectionLabel)];
        }

        if (sections[^1].Start + sections[^1].Duration < audio.DurationSeconds)
        {
            var last = sections[^1];
            sections[^1] = CreateSection(
                last.Index,
                last.Start,
                audio.DurationSeconds - last.Start,
                tempo,
                last.Label ?? $"Section {last.Index + 1}");
        }

        return sections;
    }

    public static IReadOnlyList<Section> BuildSections(
        LoadedAudio audio,
        IReadOnlyList<Bar> bars,
        IReadOnlyList<Beat> beats,
        double tempo,
        int timeSignature,
        bool structuralSections,
        bool evidenceConfidences)
    {
        if (!structuralSections || audio.DurationSeconds <= 60.0 || bars.Count < DefaultBarsPerSection * 2)
        {
            var fallback = BuildSections(audio, bars, tempo);
            return structuralSections && evidenceConfidences
                ? EnrichSectionsWithEvidence(fallback, beats, tempo)
                : fallback;
        }

        var novelty = BuildBarNovelty(beats, bars, timeSignature);
        if (novelty.Length == 0 || FeatureNovelty.Variance(novelty) < 1e-8)
        {
            return EnrichSectionsWithEvidence(BuildSections(audio, bars, tempo), beats, tempo);
        }

        var candidates = BuildSectionBoundaryCandidates(novelty, bars, audio.DurationSeconds)
            .OrderByDescending(candidate => candidate.Score)
            .ToArray();
        var boundaries = SelectSectionBoundaries(candidates, bars, audio.DurationSeconds);

        if (boundaries.Count < 3)
        {
            return EnrichSectionsWithEvidence(BuildSections(audio, bars, tempo), beats, tempo);
        }

        var sections = new List<Section>();
        var minNovelty = novelty.Min();
        var maxNovelty = novelty.Max();
        var ordered = boundaries.ToArray();

        for (var index = 0; index < ordered.Length - 1; index++)
        {
            var firstBar = bars[ordered[index]];
            var end = ordered[index + 1] < bars.Count ? bars[ordered[index + 1]].Start : audio.DurationSeconds;
            var startBeat = ordered[index] * timeSignature;
            var endBeat = Math.Min(beats.Count, ordered[index + 1] * timeSignature);
            var confidence = index == 0 || !evidenceConfidences
                ? DefaultConfidence
                : FeatureNovelty.Normalize(novelty[Math.Min(ordered[index], novelty.Length - 1)], minNovelty, maxNovelty);

            sections.Add(new Section
            {
                Index = sections.Count,
                Start = firstBar.Start,
                Duration = Math.Max(1e-6, end - firstBar.Start),
                Confidence = confidence,
                Loudness = AverageSectionLoudness(beats, startBeat, endBeat),
                Tempo = tempo,
                Label = $"Section {sections.Count + 1}"
            });
        }

        var minimumSectionCount = Math.Max(3, (int)Math.Ceiling(audio.DurationSeconds / 45.0));
        if (sections.Count < minimumSectionCount)
        {
            return EnrichSectionsWithEvidence(BuildSections(audio, bars, tempo), beats, tempo);
        }

        return sections.Count >= 2 ? sections : EnrichSectionsWithEvidence(BuildSections(audio, bars, tempo), beats, tempo);
    }

    internal static SectionNoveltySummary AnalyzeSectionNovelty(
        LoadedAudio audio,
        IReadOnlyList<Bar> bars,
        IReadOnlyList<Beat> beats,
        int timeSignature)
    {
        if (bars.Count == 0 || beats.Count == 0)
        {
            return new SectionNoveltySummary(0, 0, 0.0, 0.0);
        }

        var novelty = BuildBarNovelty(beats, bars, timeSignature);
        var candidates = BuildSectionBoundaryCandidates(novelty, bars, audio.DurationSeconds).ToArray();
        var selected = SelectSectionBoundaries(candidates, bars, audio.DurationSeconds);
        return new SectionNoveltySummary(
            candidates.Length,
            Math.Max(0, selected.Count - 2),
            novelty.Length == 0 ? 0.0 : novelty.Average(),
            novelty.Length == 0 ? 0.0 : novelty.Max());
    }

    private static double AverageConfidence(IReadOnlyList<Beat> beats, int firstIndex, int lastIndex)
    {
        var sum = 0.0;
        var count = 0;

        for (var index = firstIndex; index <= lastIndex; index++)
        {
            sum += beats[index].Confidence;
            count++;
        }

        return count > 0 ? sum / count : 0.0;
    }

    private static IReadOnlyList<int> FindInternalOnsetPeaks(float[] odf, int startFrame, int endFrame, float maxOdf)
    {
        var threshold = maxOdf * 0.35f;
        var peaks = new List<int>();

        for (var frame = startFrame + 1; frame < endFrame - 1; frame++)
        {
            if (odf[frame] >= threshold && odf[frame] >= odf[frame - 1] && odf[frame] > odf[frame + 1])
            {
                peaks.Add(frame);
            }
        }

        return peaks;
    }

    private static double[] BuildBarNovelty(IReadOnlyList<Beat> beats, IReadOnlyList<Bar> bars, int timeSignature)
    {
        var vectors = new float[bars.Count][];
        for (var barIndex = 0; barIndex < bars.Count; barIndex++)
        {
            vectors[barIndex] = AverageBeatVector(
                beats,
                barIndex * timeSignature,
                Math.Min(beats.Count, (barIndex + 1) * timeSignature));
        }

        vectors = ZScoreNormalize(vectors);
        var similarity = new double[bars.Count, bars.Count];

        for (var row = 0; row < bars.Count; row++)
        {
            for (var column = 0; column < bars.Count; column++)
            {
                similarity[row, column] = CosineSimilarity(vectors[row], vectors[column]);
            }
        }

        var values = new double[bars.Count];
        var radius = Math.Clamp(bars.Count / 12, 2, 4);
        var sigma = Math.Max(1.0, radius / 2.0);

        for (var center = radius; center < bars.Count - radius; center++)
        {
            var score = 0.0;
            for (var rowOffset = -radius; rowOffset <= radius; rowOffset++)
            {
                for (var columnOffset = -radius; columnOffset <= radius; columnOffset++)
                {
                    var sameSide = Math.Sign(rowOffset == 0 ? -1 : rowOffset) == Math.Sign(columnOffset == 0 ? -1 : columnOffset);
                    var sign = sameSide ? 1.0 : -1.0;
                    var gaussian = Math.Exp(-(rowOffset * rowOffset + columnOffset * columnOffset) / (2.0 * sigma * sigma));
                    score += sign * gaussian * similarity[center + rowOffset, center + columnOffset];
                }
            }

            values[center] = Math.Max(0.0, score);
        }

        return FeatureNovelty.MovingAverage(values, 3);
    }

    private static IReadOnlyList<SectionBoundaryCandidate> BuildSectionBoundaryCandidates(
        double[] novelty,
        IReadOnlyList<Bar> bars,
        double durationSeconds)
    {
        if (novelty.Length < 3 || bars.Count < 3)
        {
            return [];
        }

        var candidates = new List<SectionBoundaryCandidate>();
        var scales = new[] { 3, 5, 8 };
        var average = novelty.Average();
        var stdDev = Math.Sqrt(FeatureNovelty.Variance(novelty));
        var threshold = average + stdDev * 0.12;
        var minBoundaryTime = Math.Min(8.0, durationSeconds * 0.08);
        var maxBoundaryTime = Math.Max(minBoundaryTime, durationSeconds - 4.0);

        foreach (var scale in scales)
        {
            var smoothed = FeatureNovelty.MovingAverage(novelty, Math.Max(3, scale | 1));
            for (var bar = 1; bar < Math.Min(smoothed.Length - 1, bars.Count - 1); bar++)
            {
                var start = bars[bar].Start;
                if (start < minBoundaryTime || start > maxBoundaryTime)
                {
                    continue;
                }

                if (smoothed[bar] >= threshold &&
                    smoothed[bar] >= smoothed[bar - 1] &&
                    smoothed[bar] > smoothed[bar + 1])
                {
                    var phraseBonus = bar % 4 == 0 ? 0.10 : bar % 2 == 0 ? 0.05 : 0.0;
                    candidates.Add(new SectionBoundaryCandidate(bar, smoothed[bar] + phraseBonus, scale));
                }
            }
        }

        return candidates
            .GroupBy(candidate => candidate.BarIndex)
            .Select(group => group.OrderByDescending(candidate => candidate.Score).First())
            .ToArray();
    }

    private static SortedSet<int> SelectSectionBoundaries(
        IReadOnlyList<SectionBoundaryCandidate> candidates,
        IReadOnlyList<Bar> bars,
        double durationSeconds)
    {
        var boundaries = new SortedSet<int> { 0, bars.Count };
        var maxCount = Math.Clamp((int)Math.Round(durationSeconds / 28.0) + 2, 4, 10);
        var minGapSeconds = Math.Clamp(durationSeconds / 36.0, 6.0, 11.0);
        foreach (var candidate in candidates.OrderByDescending(candidate => candidate.Score))
        {
            if (boundaries.Count - 1 >= maxCount)
            {
                break;
            }

            var previous = boundaries.GetViewBetween(0, candidate.BarIndex).Max;
            var next = boundaries.GetViewBetween(candidate.BarIndex, bars.Count).Min;
            var currentTime = bars[candidate.BarIndex].Start;
            var previousTime = previous < bars.Count ? bars[previous].Start : durationSeconds;
            var nextTime = next < bars.Count ? bars[next].Start : durationSeconds;
            var allowsIntroOutro = currentTime <= 12.0 || durationSeconds - currentTime <= 12.0;

            if (!allowsIntroOutro && (currentTime - previousTime < minGapSeconds || nextTime - currentTime < minGapSeconds))
            {
                continue;
            }

            boundaries.Add(candidate.BarIndex);
        }

        return boundaries;
    }

    private static float[] AverageBeatVector(IReadOnlyList<Beat> beats, int start, int end)
    {
        if (beats.Count == 0 || start >= end)
        {
            return [];
        }

        var first = beats[Math.Clamp(start, 0, beats.Count - 1)];
        var length = first.Timbre.Length + first.Pitches.Length + first.Loudness.Length;
        var result = new float[length];
        var count = 0;

        for (var index = Math.Max(0, start); index < Math.Min(beats.Count, end); index++)
        {
            var cursor = 0;
            foreach (var value in beats[index].Timbre.Concat(beats[index].Pitches).Concat(beats[index].Loudness))
            {
                result[cursor++] += value;
            }
            count++;
        }

        if (count > 0)
        {
            for (var index = 0; index < result.Length; index++)
            {
                result[index] /= count;
            }
        }

        return result;
    }

    private static double VectorDistance(float[] left, float[] right)
    {
        var length = Math.Min(left.Length, right.Length);
        if (length == 0)
        {
            return 0.0;
        }

        var sum = 0.0;
        for (var index = 0; index < length; index++)
        {
            var delta = left[index] - right[index];
            sum += delta * delta;
        }

        return Math.Sqrt(sum / length);
    }

    private static double CosineSimilarity(float[] left, float[] right)
    {
        var length = Math.Min(left.Length, right.Length);
        if (length == 0)
        {
            return 0.0;
        }

        var dot = 0.0;
        var leftNorm = 0.0;
        var rightNorm = 0.0;

        for (var index = 0; index < length; index++)
        {
            dot += left[index] * right[index];
            leftNorm += left[index] * left[index];
            rightNorm += right[index] * right[index];
        }

        var denominator = Math.Sqrt(leftNorm) * Math.Sqrt(rightNorm);
        return denominator > 0.0 ? dot / denominator : 0.0;
    }

    private static float[][] ZScoreNormalize(float[][] vectors)
    {
        if (vectors.Length == 0)
        {
            return vectors;
        }

        var dimensions = vectors.Max(vector => vector.Length);
        var means = new double[dimensions];
        var deviations = new double[dimensions];

        for (var dimension = 0; dimension < dimensions; dimension++)
        {
            means[dimension] = vectors.Average(vector => dimension < vector.Length ? vector[dimension] : 0f);
            deviations[dimension] = Math.Sqrt(vectors.Average(vector =>
            {
                var value = dimension < vector.Length ? vector[dimension] : 0f;
                return Math.Pow(value - means[dimension], 2.0);
            }));
            deviations[dimension] = Math.Max(1e-4, deviations[dimension]);
        }

        return vectors
            .Select(vector =>
            {
                var result = new float[dimensions];
                for (var dimension = 0; dimension < dimensions; dimension++)
                {
                    var value = dimension < vector.Length ? vector[dimension] : 0f;
                    result[dimension] = (float)((value - means[dimension]) / deviations[dimension]);
                }

                return result;
            })
            .ToArray();
    }

    private static double AverageSectionLoudness(IReadOnlyList<Beat> beats, int start, int end)
    {
        var values = new List<double>();
        for (var index = Math.Max(0, start); index < Math.Min(beats.Count, end); index++)
        {
            if (beats[index].Loudness.Length > 0)
            {
                values.Add(beats[index].Loudness.Average());
            }
        }

        return values.Count > 0 ? values.Average() : 0.0;
    }

    private static Section CreateSection(int index, double start, double duration, double tempo, string label)
    {
        return new Section
        {
            Index = index,
            Start = start,
            Duration = duration,
            Confidence = DefaultConfidence,
            Loudness = 0.0,
            Tempo = tempo,
            Label = label
        };
    }

    private static IReadOnlyList<Section> EnrichSectionsWithEvidence(
        IReadOnlyList<Section> sections,
        IReadOnlyList<Beat> beats,
        double tempo)
    {
        if (sections.Count == 0 || beats.Count == 0)
        {
            return sections;
        }

        var strengths = new double[sections.Count];
        for (var index = 0; index < sections.Count; index++)
        {
            var section = sections[index];
            var sectionBeats = beats
                .Where(beat => beat.Start >= section.Start && beat.Start < section.Start + section.Duration)
                .ToArray();
            strengths[index] = sectionBeats.Length == 0 ? 0.0 : sectionBeats.Average(beat => beat.Confidence);
        }

        var min = strengths.Min();
        var max = strengths.Max();

        return sections
            .Select((section, index) => new Section
            {
                Index = section.Index,
                Start = section.Start,
                Duration = section.Duration,
                Confidence = FeatureNovelty.Normalize(strengths[index], min, max),
                Loudness = AverageSectionLoudness(
                    beats,
                    beats.TakeWhile(beat => beat.Start < section.Start).Count(),
                    beats.TakeWhile(beat => beat.Start < section.Start + section.Duration).Count()),
                Tempo = tempo,
                Label = section.Label
            })
            .ToArray();
    }
}

internal sealed record SectionNoveltySummary(
    int CandidateCount,
    int SelectedCount,
    double NoveltyMean,
    double NoveltyMax);

internal sealed record SectionBoundaryCandidate(int BarIndex, double Score, int Scale);
