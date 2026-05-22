using EternalLoop.Core.BeatTracking;
using FluentAssertions;

namespace EternalLoop.Core.Tests.BeatTracking;

public sealed class BeatDensitySanityCheckTests
{
    [Fact]
    public void IsSuspicious_ReturnsTrue_ForGangnamLikeSparseAnalysis()
    {
        var result = BeatDensitySanityCheck.IsSuspicious(
            durationSeconds: 252.34,
            estimatedBpm: 123.0,
            beatCount: 63);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsSuspicious_ReturnsFalse_ForReasonableDanceTrackDensity()
    {
        var result = BeatDensitySanityCheck.IsSuspicious(
            durationSeconds: 252.34,
            estimatedBpm: 123.0,
            beatCount: 500);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsSuspicious_ReturnsFalse_ForShortTrackWithFewButReasonableBeats()
    {
        var result = BeatDensitySanityCheck.IsSuspicious(
            durationSeconds: 12.0,
            estimatedBpm: 90.0,
            beatCount: 12);

        result.Should().BeFalse();
    }
}
