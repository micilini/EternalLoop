using EternalLoop.AnalysisEngine.Core.BeatTracking;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking;

public sealed class TempoEstimatorTests
{
    [Fact]
    public void EstimateBpm_does_not_prefer_double_time_without_evidence()
    {
        var odf = CreatePulseOdf(frameCount: 1200, primaryPeriod: 60, secondaryOffset: 30, secondaryValue: 0.15f);

        var bpm = TempoEstimator.EstimateBpm(odf, hopLengthSamples: 10, sampleRate: 1000, minBpm: 60.0, maxBpm: 220.0);

        bpm.Should().BeInRange(95.0, 105.0);
    }

    [Fact]
    public void SelectMusicalTempoLag_accepts_competitive_half_time_for_high_bpm()
    {
        var scores = new Dictionary<int, double>
        {
            [30] = 1.0,
            [60] = 0.5
        };

        var lag = TempoEstimator.SelectMusicalTempoLag(30, 1.0, scores, maxLag: 100, framesPerSecond: 100.0);

        lag.Should().Be(60);
    }

    [Fact]
    public void SelectMusicalTempoLag_preserves_high_bpm_when_slower_candidate_is_weak()
    {
        var scores = new Dictionary<int, double>
        {
            [30] = 1.0,
            [60] = 0.2
        };

        var lag = TempoEstimator.SelectMusicalTempoLag(30, 1.0, scores, maxLag: 100, framesPerSecond: 100.0);

        lag.Should().Be(30);
    }

    [Fact]
    public void EstimateBpm_falls_back_for_empty_or_silent_odf()
    {
        TempoEstimator.EstimateBpm([], 10, 1000, 60.0, 220.0).Should().Be(120.0);
        TempoEstimator.EstimateBpm(new float[32], 10, 1000, 60.0, 220.0).Should().Be(120.0);
    }

    private static float[] CreatePulseOdf(
        int frameCount,
        int primaryPeriod,
        int secondaryOffset,
        float secondaryValue)
    {
        var odf = new float[frameCount];

        for (var frame = 0; frame < frameCount; frame += primaryPeriod)
        {
            odf[frame] = 1.0f;

            var secondary = frame + secondaryOffset;
            if (secondary < frameCount)
            {
                odf[secondary] = secondaryValue;
            }
        }

        return odf;
    }
}
