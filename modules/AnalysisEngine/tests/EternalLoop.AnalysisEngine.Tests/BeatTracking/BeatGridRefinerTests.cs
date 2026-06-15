using EternalLoop.AnalysisEngine.Core.BeatTracking;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking;

public sealed class BeatGridRefinerTests
{
    [Fact]
    public void ApplyPerBeatMicroSnap_moves_beats_to_nearby_peaks_without_changing_count()
    {
        var odf = new float[120];
        var grid = new[] { 10, 30, 50, 70, 90 };

        foreach (var frame in new[] { 11, 29, 52, 69, 91 })
        {
            odf[frame] = 1.0f;
        }

        var snapped = BeatGridRefiner.ApplyPerBeatMicroSnap(odf, grid, targetPeriodFrames: 20);

        snapped.Should().HaveCount(grid.Length);
        snapped.Should().Equal(11, 29, 52, 69, 91);
        snapped.Should().BeInAscendingOrder();
    }

    [Fact]
    public void ApplyPerBeatMicroSnap_is_noop_for_metronomic_peaks()
    {
        var odf = new float[120];
        var grid = new[] { 10, 30, 50, 70, 90 };

        foreach (var frame in grid)
        {
            odf[frame] = 1.0f;
        }

        var snapped = BeatGridRefiner.ApplyPerBeatMicroSnap(odf, grid, targetPeriodFrames: 20);

        snapped.Should().Equal(grid);
    }

    [Fact]
    public void ApplyPerBeatMicroSnap_handles_first_index_without_overflow()
    {
        var odf = new float[20];
        odf[1] = 1.0f;
        odf[10] = 1.0f;

        var act = () => BeatGridRefiner.ApplyPerBeatMicroSnap(odf, [0, 10], targetPeriodFrames: 10);

        act.Should().NotThrow();
    }
}
