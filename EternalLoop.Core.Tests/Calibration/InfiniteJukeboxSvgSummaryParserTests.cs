using FluentAssertions;

namespace EternalLoop.Core.Tests.Calibration;

public sealed class InfiniteJukeboxSvgSummaryParserTests
{
    [Fact]
    public void Parser_counts_rects_and_paths()
    {
        var summary = InfiniteJukeboxSvgSummaryParser.Parse("<svg><rect/><rect x=\"1\"/><path d=\"M0 0\"/></svg>");

        summary.BeatTileCount.Should().Be(2);
        summary.BranchPathCount.Should().Be(1);
    }

    [Fact]
    public void Parser_reads_width_and_height()
    {
        var summary = InfiniteJukeboxSvgSummaryParser.Parse("<svg width=\"1200\" height=\"480\"><rect/><path/></svg>");

        summary.Width.Should().Be(1200);
        summary.Height.Should().Be(480);
    }

    [Fact]
    public void Parser_rejects_svg_without_tiles()
    {
        var act = () => InfiniteJukeboxSvgSummaryParser.Parse("<svg><path/></svg>");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*rect*");
    }

    [Fact]
    public void Parser_rejects_svg_without_paths()
    {
        var act = () => InfiniteJukeboxSvgSummaryParser.Parse("<svg><rect/></svg>");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*path*");
    }
}
