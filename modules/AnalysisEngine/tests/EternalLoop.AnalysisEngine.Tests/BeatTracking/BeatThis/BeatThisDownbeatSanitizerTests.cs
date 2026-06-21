using EternalLoop.AnalysisEngine.Core.BeatTracking.BeatThis;
using FluentAssertions;
using Xunit;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.BeatThis;

public sealed class BeatThisDownbeatSanitizerTests
{
    [Fact]
    public void Sanitize_keeps_aligned_downbeats()
    {
        var sanitizer = new BeatThisDownbeatSanitizer();
        double[] beats = [0.0, 0.5, 1.0, 1.5];
        double[] downbeats = [0.0, 1.0];

        var result = sanitizer.Sanitize(beats, downbeats, maxDistanceToNearestBeatSeconds: 0.03);

        result.Sanitized.Should().BeFalse();
        result.Downbeats.Should().Equal(downbeats);
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void Sanitize_discards_downbeats_when_far_from_beats()
    {
        var sanitizer = new BeatThisDownbeatSanitizer();
        double[] beats = [0.0, 0.5, 1.0, 1.5];
        double[] downbeats = [0.0, 0.9];

        var result = sanitizer.Sanitize(beats, downbeats, maxDistanceToNearestBeatSeconds: 0.03);

        result.Sanitized.Should().BeTrue();
        result.Downbeats.Should().BeEmpty();
        result.Warnings.Should().ContainSingle().Which.Should().StartWith("beat-this-warning:downbeats-discarded:not-aligned-to-beat");
    }

    [Fact]
    public void Sanitize_returns_empty_when_no_downbeats()
    {
        var sanitizer = new BeatThisDownbeatSanitizer();
        double[] beats = [0.0, 0.5, 1.0, 1.5];
        double[] downbeats = [];

        var result = sanitizer.Sanitize(beats, downbeats, maxDistanceToNearestBeatSeconds: 0.03);

        result.Sanitized.Should().BeFalse();
        result.Downbeats.Should().BeEmpty();
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void Sanitize_discards_downbeats_when_no_beats()
    {
        var sanitizer = new BeatThisDownbeatSanitizer();
        double[] beats = [];
        double[] downbeats = [0.0, 1.0];

        var result = sanitizer.Sanitize(beats, downbeats, maxDistanceToNearestBeatSeconds: 0.03);

        result.Sanitized.Should().BeTrue();
        result.Downbeats.Should().BeEmpty();
        result.Warnings.Should().ContainSingle().Which.Should().Be("beat-this-warning:downbeats-discarded:beats-missing");
    }

    [Fact]
    public void Sanitize_reports_max_distance()
    {
        var sanitizer = new BeatThisDownbeatSanitizer();
        double[] beats = [0.0, 1.0];
        double[] downbeats = [0.05, 1.15];

        var result = sanitizer.Sanitize(beats, downbeats, maxDistanceToNearestBeatSeconds: 0.03);

        result.Sanitized.Should().BeTrue();
        result.MaxDistanceToNearestBeatSeconds.Should().BeApproximately(0.15, 0.000001);
        result.Warnings.Should().ContainSingle().Which.Should().EndWith("0.15");
    }

    [Fact]
    public void Sanitize_discards_nan_downbeats()
    {
        var sanitizer = new BeatThisDownbeatSanitizer();

        var result = sanitizer.Sanitize([0.0, 0.5, 1.0], [0.0, double.NaN], maxDistanceToNearestBeatSeconds: 0.03);

        result.Sanitized.Should().BeTrue();
        result.Downbeats.Should().BeEmpty();
        result.Reason.Should().Be("downbeats-not-finite");
        result.Warnings.Should().ContainSingle().Which.Should().Be("beat-this-warning:downbeats-discarded:not-finite");
    }

    [Fact]
    public void Sanitize_discards_infinite_downbeats()
    {
        var sanitizer = new BeatThisDownbeatSanitizer();

        var result = sanitizer.Sanitize([0.0, 0.5, 1.0], [0.0, double.PositiveInfinity], maxDistanceToNearestBeatSeconds: 0.03);

        result.Sanitized.Should().BeTrue();
        result.Downbeats.Should().BeEmpty();
        result.Reason.Should().Be("downbeats-not-finite");
        result.Warnings.Should().ContainSingle().Which.Should().Be("beat-this-warning:downbeats-discarded:not-finite");
    }

    [Fact]
    public void Sanitize_discards_non_increasing_downbeats()
    {
        var sanitizer = new BeatThisDownbeatSanitizer();

        var result = sanitizer.Sanitize([0.0, 0.5, 1.0], [0.5, 0.0], maxDistanceToNearestBeatSeconds: 0.03);

        result.Sanitized.Should().BeTrue();
        result.Downbeats.Should().BeEmpty();
        result.Reason.Should().Be("downbeats-not-strictly-increasing");
        result.Warnings.Should().ContainSingle().Which.Should().Be("beat-this-warning:downbeats-discarded:not-strictly-increasing");
    }

    [Fact]
    public void Sanitize_discards_duplicate_downbeats()
    {
        var sanitizer = new BeatThisDownbeatSanitizer();

        var result = sanitizer.Sanitize([0.0, 0.5, 1.0], [0.0, 0.0], maxDistanceToNearestBeatSeconds: 0.03);

        result.Sanitized.Should().BeTrue();
        result.Downbeats.Should().BeEmpty();
        result.Reason.Should().Be("downbeats-not-strictly-increasing");
        result.Warnings.Should().ContainSingle().Which.Should().Be("beat-this-warning:downbeats-discarded:not-strictly-increasing");
    }

    [Fact]
    public void Sanitize_discards_downbeats_when_beats_are_not_finite()
    {
        var sanitizer = new BeatThisDownbeatSanitizer();

        var result = sanitizer.Sanitize([0.0, double.PositiveInfinity, 1.0], [0.0, 1.0], maxDistanceToNearestBeatSeconds: 0.03);

        result.Sanitized.Should().BeTrue();
        result.Downbeats.Should().BeEmpty();
        result.Reason.Should().Be("beats-not-finite");
        result.Warnings.Should().ContainSingle().Which.Should().Be("beat-this-warning:downbeats-discarded:beats-not-finite");
    }

    [Fact]
    public void Sanitize_throws_when_max_distance_is_negative()
    {
        var sanitizer = new BeatThisDownbeatSanitizer();

        var act = () => sanitizer.Sanitize([0.0], [0.0], maxDistanceToNearestBeatSeconds: -0.01);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Sanitize_throws_when_max_distance_is_nan()
    {
        var sanitizer = new BeatThisDownbeatSanitizer();

        var act = () => sanitizer.Sanitize([0.0], [0.0], maxDistanceToNearestBeatSeconds: double.NaN);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
