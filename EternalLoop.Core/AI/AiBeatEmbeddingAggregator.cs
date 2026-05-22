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

        var validFrames = frames
            .Where(frame => frame.Vector is { Length: > 0 })
            .ToArray();

        if (validFrames.Length == 0)
        {
            return [];
        }

        var beatEmbeddings = new List<AiBeatEmbedding>(beats.Count);

        for (var beatIndex = 0; beatIndex < beats.Count; beatIndex++)
        {
            var beat = beats[beatIndex];
            var selectedFrames = SelectFrames(beats, validFrames, options, beatIndex);

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
        var beatStart = SanitizeTime(beat.Start);
        var beatDuration = SanitizeDuration(beat.Duration);
        var previousDuration = beatIndex > 0 ? SanitizeDuration(beats[beatIndex - 1].Duration) : beatDuration;
        var nextDuration = beatIndex < beats.Count - 1 ? SanitizeDuration(beats[beatIndex + 1].Duration) : beatDuration;
        var contextBefore = Math.Max(0, options.BeatContextBefore);
        var contextAfter = Math.Max(0, options.BeatContextAfter);
        var windowStart = beatStart - previousDuration * contextBefore;
        var windowEnd = beatStart + beatDuration + nextDuration * contextAfter;
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

        var beatCenter = beatStart + beatDuration / 2.0;
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
        if (frames.Count == 0)
        {
            throw new ArgumentException("AI embedding frame collection must not be empty.", nameof(frames));
        }

        if (frames[0].Vector is null)
        {
            throw new ArgumentException("AI embedding frame vectors must not be null.", nameof(frames));
        }

        var dimension = frames[0].Vector.Length;

        if (dimension <= 0)
        {
            throw new ArgumentException("AI embedding frame vectors must have positive dimensions.", nameof(frames));
        }

        var aggregate = new float[dimension];

        foreach (var frame in frames)
        {
            if (frame.Vector is null)
            {
                throw new ArgumentException("AI embedding frame vectors must not be null.", nameof(frames));
            }

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
        return SanitizeTime(frame.Start) + SanitizeDuration(frame.Duration) / 2.0;
    }

    private static double SanitizeTime(double value)
    {
        return double.IsFinite(value) ? value : 0.0;
    }

    private static double SanitizeDuration(double value)
    {
        return double.IsFinite(value) && value > 0.0 ? value : 0.0;
    }

    private static float Sanitize(float value)
    {
        return float.IsFinite(value) ? value : EmptyValue;
    }
}
