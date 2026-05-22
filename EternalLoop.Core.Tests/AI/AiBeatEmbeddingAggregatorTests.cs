using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;
using EternalLoop.Core.AI;
using FluentAssertions;

namespace EternalLoop.Core.Tests.AI;

public sealed class AiBeatEmbeddingAggregatorTests
{
    private const double BeatDuration = 0.5;
    private const double FrameDuration = 0.25;
    private const double NormTolerance = 0.0001;
    private const int VectorDimensions = 2;

    [Fact]
    public void Aggregate_creates_one_embedding_per_beat_when_frames_cover_each_beat()
    {
        var aggregator = new AiBeatEmbeddingAggregator();

        var embeddings = aggregator.Aggregate(CreateBeats(), CreateFrames(), new AiAnalysisOptions());

        embeddings.Should().HaveCount(CreateBeats().Length);
        embeddings.Select(embedding => embedding.BeatIndex).Should().Equal(0, 1, 2);
    }

    [Fact]
    public void Aggregate_normalizes_each_beat_embedding()
    {
        var aggregator = new AiBeatEmbeddingAggregator();

        var embeddings = aggregator.Aggregate(CreateBeats(), CreateFrames(), new AiAnalysisOptions());

        foreach (var embedding in embeddings)
        {
            VectorNorm(embedding.Vector).Should().BeApproximately(1.0, NormTolerance);
        }
    }

    [Fact]
    public void Aggregate_uses_context_before_and_after()
    {
        var aggregator = new AiBeatEmbeddingAggregator();
        var options = new AiAnalysisOptions
        {
            BeatContextBefore = 0,
            BeatContextAfter = 0
        };
        var beats = CreateBeats();
        var frames =
            new[]
            {
                CreateFrame(0, beats[1].Start + beats[1].Duration / 2.0, [1.0f, 0.0f])
            };

        var embeddings = aggregator.Aggregate(beats, frames, options);

        embeddings.Should().ContainSingle(embedding => embedding.BeatIndex == beats[1].Index);
    }

    [Fact]
    public void Aggregate_uses_nearest_frame_when_interval_empty()
    {
        var aggregator = new AiBeatEmbeddingAggregator();
        var beats =
            new[]
            {
                CreateBeat(0, 10.0)
            };
        var frames =
            new[]
            {
                CreateFrame(0, 0.0, [0.0f, 1.0f])
            };

        var embeddings = aggregator.Aggregate(beats, frames, new AiAnalysisOptions());

        embeddings.Should().HaveCount(1);
        embeddings[0].Vector.Should().Equal(0.0f, 1.0f);
    }

    [Fact]
    public void Aggregate_returns_empty_when_beats_are_empty()
    {
        var aggregator = new AiBeatEmbeddingAggregator();

        var embeddings = aggregator.Aggregate([], CreateFrames(), new AiAnalysisOptions());

        embeddings.Should().BeEmpty();
    }

    [Fact]
    public void Aggregate_returns_empty_when_frames_are_empty()
    {
        var aggregator = new AiBeatEmbeddingAggregator();

        var embeddings = aggregator.Aggregate(CreateBeats(), [], new AiAnalysisOptions());

        embeddings.Should().BeEmpty();
    }

    [Fact]
    public void Aggregate_sanitizes_nan_and_infinity()
    {
        var aggregator = new AiBeatEmbeddingAggregator();
        var frames =
            new[]
            {
                CreateFrame(0, 0.0, [float.NaN, float.PositiveInfinity])
            };

        var embeddings = aggregator.Aggregate([CreateBeat(0, 0.0)], frames, new AiAnalysisOptions());

        embeddings[0].Vector.Should().OnlyContain(value => float.IsFinite(value));
    }

    [Fact]
    public void Aggregate_is_deterministic()
    {
        var aggregator = new AiBeatEmbeddingAggregator();
        var beats = CreateBeats();
        var frames = CreateFrames();

        var first = aggregator.Aggregate(beats, frames, new AiAnalysisOptions());
        var second = aggregator.Aggregate(beats, frames, new AiAnalysisOptions());

        second.SelectMany(embedding => embedding.Vector).Should().Equal(first.SelectMany(embedding => embedding.Vector));
    }

    [Fact]
    public void Aggregate_ignores_invalid_empty_vectors_without_index_error()
    {
        var aggregator = new AiBeatEmbeddingAggregator();
        var frames =
            new[]
            {
                CreateFrame(0, 0.0, [])
            };

        var embeddings = aggregator.Aggregate([CreateBeat(0, 0.0)], frames, new AiAnalysisOptions());

        embeddings.Should().BeEmpty();
    }

    [Fact]
    public void Aggregate_clamps_negative_context_values()
    {
        var aggregator = new AiBeatEmbeddingAggregator();
        var options = new AiAnalysisOptions
        {
            BeatContextBefore = -1,
            BeatContextAfter = -2
        };
        var beats = CreateBeats();
        var frames =
            new[]
            {
                CreateFrame(0, beats[1].Start + beats[1].Duration / 2.0, [1.0f, 0.0f])
            };

        var embeddings = aggregator.Aggregate(beats, frames, options);

        embeddings.Should().ContainSingle(embedding => embedding.BeatIndex == beats[1].Index);
    }

    [Fact]
    public void Aggregate_handles_non_finite_beat_timing()
    {
        var aggregator = new AiBeatEmbeddingAggregator();
        var beat = CreateBeat(0, double.NaN, double.PositiveInfinity);
        var frames =
            new[]
            {
                CreateFrame(0, 0.0, [1.0f, 0.0f])
            };

        var embeddings = aggregator.Aggregate([beat], frames, new AiAnalysisOptions());

        embeddings.Should().HaveCount(1);
        embeddings[0].Vector.Should().OnlyContain(value => float.IsFinite(value));
    }

    [Fact]
    public void Aggregate_uses_nearest_valid_frame_when_window_has_no_match()
    {
        var aggregator = new AiBeatEmbeddingAggregator();
        var beats =
            new[]
            {
                CreateBeat(0, 10.0)
            };
        var frames =
            new[]
            {
                CreateFrame(0, 9.0, []),
                CreateFrame(1, 0.0, [0.0f, 1.0f])
            };

        var embeddings = aggregator.Aggregate(beats, frames, new AiAnalysisOptions());

        embeddings.Should().HaveCount(1);
        embeddings[0].Vector.Should().Equal(0.0f, 1.0f);
    }

    private static Beat[] CreateBeats()
    {
        return Enumerable.Range(0, 3)
            .Select(index => CreateBeat(index, index * BeatDuration))
            .ToArray();
    }

    private static Beat CreateBeat(int index, double start)
    {
        return CreateBeat(index, start, BeatDuration);
    }

    private static Beat CreateBeat(int index, double start, double duration)
    {
        return new Beat
        {
            Index = index,
            Start = start,
            Duration = duration,
            Confidence = 1.0,
            Timbre = [1.0f],
            Pitches = [1.0f],
            Loudness = [0.0f, 0.0f, 0.0f],
            BarPosition = [1.0f, 0.0f]
        };
    }

    private static AiEmbeddingFrame[] CreateFrames()
    {
        return Enumerable.Range(0, 3)
            .Select(index => CreateFrame(index, index * BeatDuration, CreateVector(index)))
            .ToArray();
    }

    private static AiEmbeddingFrame CreateFrame(int index, double start, float[] vector)
    {
        return new AiEmbeddingFrame
        {
            Index = index,
            Start = start,
            Duration = FrameDuration,
            Vector = vector
        };
    }

    private static float[] CreateVector(int index)
    {
        var vector = new float[VectorDimensions];
        vector[0] = index + 1.0f;
        vector[1] = index + 2.0f;
        return vector;
    }

    private static double VectorNorm(float[] vector)
    {
        return Math.Sqrt(vector.Sum(value => (double)value * value));
    }
}
