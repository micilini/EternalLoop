using EternalLoop.Core.BeatTracking;
using FluentAssertions;

namespace EternalLoop.Core.Tests.BeatTracking;

public sealed class BeatAlignerTests
{
    [Fact]
    public void AlignBeats_Should_ReturnEmpty_WhenOdfIsEmpty()
    {
        var beats = BeatAligner.AlignBeats([], 10, 100);

        beats.Should().BeEmpty();
    }

    [Fact]
    public void AlignBeats_Should_ReturnPeaksNearExpectedGrid()
    {
        var odf = new float[100];

        for (var i = 10; i < odf.Length; i += 10)
        {
            odf[i] = 1.0f;
        }

        var beats = BeatAligner.AlignBeats(odf, 10, 100);

        beats.Should().NotBeEmpty();
        beats.Should().OnlyContain(frame => frame % 10 <= 1 || frame % 10 >= 9);
    }

    [Fact]
    public void AlignBeats_Should_NotReturnEveryFrame()
    {
        var odf = new float[100];

        for (var i = 10; i < odf.Length; i += 10)
        {
            odf[i] = 1.0f;
        }

        var beats = BeatAligner.AlignBeats(odf, 10, 100);

        beats.Length.Should().BeLessThan(20);
    }

    [Fact]
    public void FindCandidatePeaks_Should_FindLocalMaxima()
    {
        var peaks = BeatAligner.FindCandidatePeaks([0f, 0.2f, 1f, 0.3f, 0f, 0.9f, 0.1f]);

        peaks.Should().Equal(2, 5);
    }
}
