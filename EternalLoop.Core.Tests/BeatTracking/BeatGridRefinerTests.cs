using EternalLoop.Core.BeatTracking;
using FluentAssertions;

namespace EternalLoop.Core.Tests.BeatTracking;

public sealed class BeatGridRefinerTests
{
    [Fact]
    public void EnsureUsableBeatGrid_Densifies_Sparse_ThirtySecond_Track()
    {
        const double durationSeconds = 30.0;
        const double bpm = 136.0;
        const double framesPerSecond = 22050.0 / 512.0;

        var frameCount = (int)Math.Ceiling(durationSeconds * framesPerSecond);
        var periodFrames = framesPerSecond * 60.0 / bpm;
        var odf = new float[frameCount];

        for (var i = 0; i < odf.Length; i += (int)Math.Round(periodFrames))
        {
            odf[i] = 1f;
        }

        var sparse = new[] { 0, frameCount / 2, frameCount - 1 };

        var refined = BeatGridRefiner.EnsureUsableBeatGrid(
            odf,
            sparse,
            periodFrames,
            durationSeconds,
            framesPerSecond);

        refined.Length.Should().BeInRange(55, 75);
        refined.Should().BeInAscendingOrder();
    }

    [Fact]
    public void EnsureUsableBeatGrid_Keeps_Usable_Aligned_Frames()
    {
        var odf = new float[200];
        var aligned = Enumerable.Range(0, 40).Select(i => i * 5).ToArray();

        var refined = BeatGridRefiner.EnsureUsableBeatGrid(
            odf,
            aligned,
            targetPeriodFrames: 5,
            durationSeconds: 20,
            framesPerSecond: 10);

        refined.Should().Equal(aligned);
    }
}
