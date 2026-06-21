using EternalLoop.AnalysisEngine.Core.Analysis;
using EternalLoop.AnalysisEngine.Core.Models;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.Analysis;

public sealed class BarBuilderFromDownbeatsTests
{
    [Fact]
    public void Build_uses_provider_downbeats_as_bar_starts()
    {
        var result = BarBuilderFromDownbeats.Build(CreateBeats(10), [1.0, 3.0], timeSignature: 4);

        result.UsedProviderDownbeats.Should().BeTrue();
        result.BarPhaseSelection.Mode.Should().Be("provider-downbeats");
        result.MatchedDownbeatCount.Should().Be(2);
        result.Bars.Select(bar => bar.Start).Should().Equal(1.0, 3.0);
    }

    [Fact]
    public void Build_matches_downbeats_to_nearest_beats_with_tolerance()
    {
        var result = BarBuilderFromDownbeats.Build(CreateBeats(10), [1.06, 2.94], timeSignature: 4);

        result.UsedProviderDownbeats.Should().BeTrue();
        result.Bars.Select(bar => bar.Start).Should().Equal(1.0, 3.0);
    }

    [Fact]
    public void Build_deduplicates_downbeats_matching_same_beat()
    {
        var result = BarBuilderFromDownbeats.Build(CreateBeats(10), [1.0, 1.03, 3.0], timeSignature: 4);

        result.UsedProviderDownbeats.Should().BeTrue();
        result.MatchedDownbeatCount.Should().Be(2);
        result.Bars.Select(bar => bar.Start).Should().Equal(1.0, 3.0);
    }

    [Fact]
    public void Build_ignores_non_finite_downbeats()
    {
        var result = BarBuilderFromDownbeats.Build(
            CreateBeats(10),
            [double.NaN, double.PositiveInfinity, 1.0],
            timeSignature: 4);

        result.UsedProviderDownbeats.Should().BeTrue();
        result.MatchedDownbeatCount.Should().Be(1);
        result.Bars.Should().ContainSingle();
        result.Bars[0].Start.Should().Be(1.0);
    }

    [Fact]
    public void Build_ignores_downbeats_outside_beat_range()
    {
        var result = BarBuilderFromDownbeats.Build(CreateBeats(10), [-1.0, 1.0, 99.0], timeSignature: 4);

        result.UsedProviderDownbeats.Should().BeTrue();
        result.MatchedDownbeatCount.Should().Be(1);
        result.Bars.Should().ContainSingle();
        result.Bars[0].Start.Should().Be(1.0);
    }

    [Fact]
    public void Build_returns_fallback_when_downbeats_empty()
    {
        var result = BarBuilderFromDownbeats.Build(CreateBeats(10), [], timeSignature: 4);

        result.UsedProviderDownbeats.Should().BeFalse();
        result.Bars.Should().BeEmpty();
        result.FallbackReason.Should().Be("No provider downbeats available.");
    }

    [Fact]
    public void Build_returns_fallback_when_no_downbeat_matches()
    {
        var result = BarBuilderFromDownbeats.Build(CreateBeats(10), [0.3, 0.8], timeSignature: 4);

        result.UsedProviderDownbeats.Should().BeFalse();
        result.Bars.Should().BeEmpty();
        result.FallbackReason.Should().Be("No provider downbeats matched beat grid.");
    }

    [Fact]
    public void Build_rejects_invalid_time_signature()
    {
        var act = () => BarBuilderFromDownbeats.Build(CreateBeats(10), [1.0], timeSignature: 0);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .Where(exception => exception.ParamName == "timeSignature");
    }

    [Fact]
    public void Build_keeps_positive_bar_durations()
    {
        var result = BarBuilderFromDownbeats.Build(CreateBeats(10), [1.0, 3.0], timeSignature: 4);

        result.Bars.Should().OnlyContain(bar => bar.Duration > 0.0);
    }

    [Fact]
    public void Build_uses_average_beat_confidence()
    {
        var beats = CreateBeats(10, confidenceFactory: index => index switch
        {
            2 => 0.2,
            3 => 0.4,
            4 => 0.6,
            5 => 0.8,
            _ => 1.0
        });

        var result = BarBuilderFromDownbeats.Build(beats, [1.0, 3.0], timeSignature: 4);

        result.Bars[0].Confidence.Should().BeApproximately(0.5, 0.000001);
    }

    private static IReadOnlyList<Beat> CreateBeats(
        int count,
        double intervalSeconds = 0.5,
        Func<int, double>? confidenceFactory = null)
    {
        return Enumerable.Range(0, count)
            .Select(index => new Beat
            {
                Index = index,
                Start = index * intervalSeconds,
                Duration = intervalSeconds,
                Confidence = confidenceFactory?.Invoke(index) ?? 0.8,
                Timbre = new float[26],
                Pitches = new float[12],
                Loudness = [0.1f, 0.2f, 0.3f],
                BarPosition = [1.0f, 0.0f]
            })
            .ToArray();
    }
}
