using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;

namespace EternalLoop.Core.AI;

public sealed class AiBeatEmbeddingAggregator
{
    private const float EmptyValue = 0.0f;

    public IReadOnlyList<AiBeatEmbedding> Aggregate(
        IReadOnlyList<Beat> beats,
        IReadOnlyList<AiEmbeddingFrame> frames,
        AiAnalysisOptions options)
    {
        ArgumentNullException.ThrowIfNull(beats);
        ArgumentNullException.ThrowIfNull(frames);
        ArgumentNullException.ThrowIfNull(options);

        if (beats.Count == 0 || frames.Count == 0)
        {
            return [];
        }

        var beatEmbeddings = new List<AiBeatEmbedding>(beats.Count);

        for (var beatIndex = 0; beatIndex < beats.Count; beatIndex++)
        {
            var beat = beats[beatIndex];
            var selectedFrames = SelectFrames(beats, frames, options, beatIndex);

            if (selectedFrames.Count == 0)
            {
                continue;
            }

            beatEmbeddings.Add(new AiBeatEmbedding
            {
                BeatIndex = beat.Index,
                Vector = Normalize(Average(selectedFrames))
            });
        }

        return beatEmbeddings;
    }

    private static IReadOnlyList<AiEmbeddingFrame> SelectFrames(
        IReadOnlyList<Beat> beats,
        IReadOnlyList<AiEmbeddingFrame> frames,
        AiAnalysisOptions options,
        int beatIndex)
    {
        var beat = beats[beatIndex];
        var previousDuration = beatIndex > 0 ? beats[beatIndex - 1].Duration : beat.Duration;
        var nextDuration = beatIndex < beats.Count - 1 ? beats[beatIndex + 1].Duration : beat.Duration;
        var windowStart = beat.Start - previousDuration * options.BeatContextBefore;
        var windowEnd = beat.Start + beat.Duration + nextDuration * options.BeatContextAfter;
        var selected = frames
            .Where(frame =>
            {
                var center = GetFrameCenter(frame);
                return center >= windowStart && center <= windowEnd;
            })
            .ToArray();

        if (selected.Length > 0)
        {
            return selected;
        }

        var beatCenter = beat.Start + beat.Duration / 2.0;
        return
        [
            frames
                .OrderBy(frame => Math.Abs(GetFrameCenter(frame) - beatCenter))
                .ThenBy(frame => frame.Index)
                .First()
        ];
    }

    private static float[] Average(IReadOnlyList<AiEmbeddingFrame> frames)
    {
        var dimension = frames[0].Vector.Length;

        if (dimension <= 0)
        {
            throw new ArgumentException("AI embedding frame vectors must have positive dimensions.", nameof(frames));
        }

        var aggregate = new float[dimension];

        foreach (var frame in frames)
        {
            if (frame.Vector.Length != dimension)
            {
                throw new ArgumentException("AI embedding frame vectors must have compatible dimensions.", nameof(frames));
            }

            for (var index = 0; index < dimension; index++)
            {
                aggregate[index] += Sanitize(frame.Vector[index]);
            }
        }

        for (var index = 0; index < aggregate.Length; index++)
        {
            aggregate[index] /= frames.Count;
        }

        return aggregate;
    }

    private static float[] Normalize(float[] vector)
    {
        var sanitized = vector.Select(Sanitize).ToArray();
        var norm = Math.Sqrt(sanitized.Sum(value => (double)value * value));

        if (norm <= AiPreprocessingDefaultValues.NormalizationEpsilon)
        {
            return sanitized;
        }

        return sanitized.Select(value => Sanitize((float)(value / norm))).ToArray();
    }

    private static double GetFrameCenter(AiEmbeddingFrame frame)
    {
        return frame.Start + frame.Duration / 2.0;
    }

    private static float Sanitize(float value)
    {
        return float.IsFinite(value) ? value : EmptyValue;
    }
}
