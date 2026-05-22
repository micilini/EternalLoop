using EternalLoop.Core.BeatTracking;
using FluentAssertions;

namespace EternalLoop.Core.Tests.BeatTracking;

public sealed class TempoEstimatorTests
{
    [Theory]
    [InlineData(120.0)]
    [InlineData(90.0)]
    [InlineData(140.0)]
    public void EstimateBpm_Should_DetectSyntheticClickTrack(double expected)
    {
        var odf = CreateSyntheticOdf(expected);

        var bpm = TempoEstimator.EstimateBpm(odf, 512, 22_050, 60, 200);

        bpm.Should().BeApproximately(expected, 3.0);
    }

    [Fact]
    public void EstimateBpm_Should_ReturnFallback_WhenOdfIsSilent()
    {
        var bpm = TempoEstimator.EstimateBpm(new float[100], 512, 22_050, 60, 200);

        bpm.Should().Be(120.0);
    }

    [Fact]
    public void EstimateBpm_Should_Throw_WhenInvalidRange()
    {
        var act = () => TempoEstimator.EstimateBpm(new float[100], 512, 22_050, 200, 60);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static float[] CreateSyntheticOdf(
        double bpm,
        int sampleRate = 22_050,
        int hopLength = 512,
        double durationSeconds = 12.0)
    {
        var frameCount = (int)(durationSeconds * sampleRate / hopLength);
        var odf = new float[frameCount];
        var framesPerSecond = sampleRate / (double)hopLength;
        var periodFrames = framesPerSecond * 60.0 / bpm;

        for (var frame = 0.0; frame < frameCount; frame += periodFrames)
        {
            var index = (int)Math.Round(frame);

            if (index >= 0 && index < odf.Length)
            {
                odf[index] = 1.0f;
            }
        }

        return odf;
    }
}
