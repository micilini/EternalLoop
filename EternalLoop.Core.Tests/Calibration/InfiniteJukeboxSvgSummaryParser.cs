using System.Globalization;
using System.Text.RegularExpressions;

namespace EternalLoop.Core.Tests.Calibration;

public static partial class InfiniteJukeboxSvgSummaryParser
{
    public static InfiniteJukeboxSvgSummary ParseFile(string svgPath)
    {
        if (string.IsNullOrWhiteSpace(svgPath))
        {
            throw new ArgumentException("SVG path cannot be empty.", nameof(svgPath));
        }

        if (!File.Exists(svgPath))
        {
            throw new FileNotFoundException($"Infinite Jukebox SVG not found: {svgPath}", svgPath);
        }

        return Parse(File.ReadAllText(svgPath));
    }

    public static InfiniteJukeboxSvgSummary Parse(string svg)
    {
        if (string.IsNullOrWhiteSpace(svg))
        {
            throw new ArgumentException("SVG content cannot be empty.", nameof(svg));
        }

        var beatTileCount = RectRegex().Matches(svg).Count;
        var branchPathCount = PathRegex().Matches(svg).Count;

        if (beatTileCount <= 0)
        {
            throw new InvalidOperationException("Infinite Jukebox SVG does not contain beat tile rect elements.");
        }

        if (branchPathCount <= 0)
        {
            throw new InvalidOperationException("Infinite Jukebox SVG does not contain branch path elements.");
        }

        return new InfiniteJukeboxSvgSummary(
            beatTileCount,
            branchPathCount,
            ReadDimension(svg, "width"),
            ReadDimension(svg, "height"));
    }

    private static int ReadDimension(string svg, string name)
    {
        var match = Regex.Match(
            svg,
            $@"\b{name}\s*=\s*[""'](?<value>[0-9]+(?:\.[0-9]+)?)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        if (!match.Success)
        {
            return 0;
        }

        return double.TryParse(
            match.Groups["value"].Value,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out var value)
            ? (int)Math.Round(value)
            : 0;
    }

    [GeneratedRegex("<rect\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RectRegex();

    [GeneratedRegex("<path\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PathRegex();
}
