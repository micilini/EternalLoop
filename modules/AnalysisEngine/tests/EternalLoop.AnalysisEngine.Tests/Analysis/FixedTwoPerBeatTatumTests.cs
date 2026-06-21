using EternalLoop.AnalysisEngine.Core.Analysis;
using EternalLoop.AnalysisEngine.Core.Models;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.Analysis;

public sealed class FixedTwoPerBeatTatumTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(6)]
    [InlineData(37)]
    public void BuildFixedTwoPerBeatTatums_creates_exactly_two_tatums_per_beat(int beatCount)
    {
        var beats = CreateBeats(beatCount);

        var tatums = TimeQuantumBuilder.BuildFixedTwoPerBeatTatums(beats);

        tatums.Should().HaveCount(beats.Count * 2);
    }

    [Fact]
    public void BuildFixedTwoPerBeatTatums_with_no_beats_returns_empty()
    {
        var tatums = TimeQuantumBuilder.BuildFixedTwoPerBeatTatums([]);

        tatums.Should().BeEmpty();
    }

    [Fact]
    public void BuildFixedTwoPerBeatTatums_aligns_first_tatum_of_each_pair_with_beat()
    {
        var beats = CreateBeats(8);

        var tatums = TimeQuantumBuilder.BuildFixedTwoPerBeatTatums(beats);

        tatums
            .Where((_, index) => index % 2 == 0)
            .Select(tatum => tatum.Start)
            .Should()
            .Equal(beats.Select(beat => beat.Start));
    }

    [Fact]
    public void BuildFixedTwoPerBeatTatums_keeps_positive_ordered_durations()
    {
        var beats = CreateBeats(10);

        var tatums = TimeQuantumBuilder.BuildFixedTwoPerBeatTatums(beats);

        tatums.Should().OnlyContain(tatum => tatum.Duration > 0.0);
        tatums.Select(tatum => tatum.Start).Should().BeInAscendingOrder();
    }

    [Fact]
    public void BuildFixedTwoPerBeatTatums_splits_each_beat_into_two_equal_halves()
    {
        var beats = CreateBeats(4);

        var tatums = TimeQuantumBuilder.BuildFixedTwoPerBeatTatums(beats);

        for (var beatIndex = 0; beatIndex < beats.Count; beatIndex++)
        {
            var first = tatums[beatIndex * 2];
            var second = tatums[(beatIndex * 2) + 1];

            first.Duration.Should().BeApproximately(second.Duration, 1e-9);
            second.Start.Should().BeApproximately(first.Start + first.Duration, 1e-9);
        }
    }

    [Fact]
    public void BuildFixedTwoPerBeatTatums_reindexes_tatums_sequentially()
    {
        var beats = CreateBeats(5);

        var tatums = TimeQuantumBuilder.BuildFixedTwoPerBeatTatums(beats);

        tatums.Select(tatum => tatum.Index).Should().Equal(Enumerable.Range(0, tatums.Count));
    }

    private static IReadOnlyList<Beat> CreateBeats(int count)
    {
        var beats = new List<Beat>();

        for (var index = 0; index < count; index++)
        {
            beats.Add(new Beat
            {
                Index = index,
                Start = index * 0.5,
                Duration = 0.5,
                Confidence = 0.8,
                Timbre = new float[26],
                Pitches = new float[12],
                Loudness = new float[3],
                BarPosition = [0.0f, 1.0f]
            });
        }

        return beats;
    }
}