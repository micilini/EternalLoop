using System.Globalization;
using System.Text;

namespace EternalLoop.Core.Tests.Calibration;

public static class GangnamBranchComparisonSvgWriter
{
    private const int Width = 1200;
    private const int PanelHeight = 220;
    private const int Margin = 32;
    private const int BeatY = 130;
    private const int TileHeight = 18;

    public static string Write(
        string outputDirectory,
        InfiniteJukeboxSvgSummary reference,
        EternalLoopBranchSummary balanced,
        EternalLoopBranchSummary wild,
        bool pass)
    {
        Directory.CreateDirectory(outputDirectory);
        var path = Path.Combine(outputDirectory, "gangnam-comparison.svg");
        var builder = new StringBuilder();
        builder.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{Width}\" height=\"{PanelHeight * 3}\">");
        builder.AppendLine("<rect width=\"100%\" height=\"100%\" fill=\"#111\"/>");
        AppendReferencePanel(builder, reference, pass);
        AppendEternalLoopPanel(builder, balanced, PanelHeight, "#4da3ff");
        AppendEternalLoopPanel(builder, wild, PanelHeight * 2, "#6ee77f");
        builder.AppendLine("</svg>");
        File.WriteAllText(path, builder.ToString());
        return path;
    }

    private static void AppendReferencePanel(StringBuilder builder, InfiniteJukeboxSvgSummary reference, bool pass)
    {
        builder.AppendLine($"<text x=\"{Margin}\" y=\"28\" fill=\"#fff\" font-family=\"Arial\" font-size=\"18\">Infinite Jukebox Reference - tiles {reference.BeatTileCount}, paths {reference.BranchPathCount}, status {(pass ? "PASS" : "FAIL")}</text>");
        AppendTiles(builder, reference.BeatTileCount, 0, "#555");
        AppendPathCounter(builder, reference.BranchPathCount, 0, "#d9b44a");
    }

    private static void AppendEternalLoopPanel(
        StringBuilder builder,
        EternalLoopBranchSummary summary,
        int yOffset,
        string color)
    {
        builder.AppendLine($"<text x=\"{Margin}\" y=\"{yOffset + 28}\" fill=\"#fff\" font-family=\"Arial\" font-size=\"18\">EternalLoop {summary.Preset} - beats {summary.BeatCount}, edges {summary.EdgeCount}, sources {summary.SourceCount}</text>");
        AppendTiles(builder, summary.BeatCount, yOffset, "#333");
        AppendEdges(builder, summary.CsvPath, summary.BeatCount, yOffset, color);
    }

    private static void AppendTiles(StringBuilder builder, int beatCount, int yOffset, string color)
    {
        var availableWidth = Width - (Margin * 2);
        var tileWidth = Math.Max(1.0, availableWidth / Math.Max(1, beatCount));

        for (var beat = 0; beat < beatCount; beat++)
        {
            var x = Margin + (beat * tileWidth);
            builder.AppendLine($"<rect x=\"{Format(x)}\" y=\"{yOffset + BeatY}\" width=\"{Format(tileWidth * 0.8)}\" height=\"{TileHeight}\" fill=\"{color}\"/>");
        }
    }

    private static void AppendPathCounter(StringBuilder builder, int pathCount, int yOffset, string color)
    {
        var availableWidth = Width - (Margin * 2);
        for (var index = 0; index < pathCount; index++)
        {
            var x = Margin + (availableWidth * index / Math.Max(1, pathCount));
            var y = yOffset + BeatY - 24 - (index % 5 * 12);
            builder.AppendLine($"<path d=\"M {Format(x)} {y} C {Format(x + 4)} {y - 18}, {Format(x + 12)} {y - 18}, {Format(x + 16)} {y}\" stroke=\"{color}\" stroke-width=\"1\" fill=\"none\" opacity=\"0.55\"/>");
        }
    }

    private static void AppendEdges(StringBuilder builder, string csvPath, int beatCount, int yOffset, string color)
    {
        if (string.IsNullOrWhiteSpace(csvPath) || !File.Exists(csvPath))
        {
            return;
        }

        var availableWidth = Width - (Margin * 2);
        foreach (var line in File.ReadLines(csvPath).Skip(1))
        {
            var parts = line.Split(',');
            if (parts.Length < 2 ||
                !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var fromBeat) ||
                !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var toBeat))
            {
                continue;
            }

            var x1 = Margin + (availableWidth * fromBeat / Math.Max(1, beatCount - 1));
            var x2 = Margin + (availableWidth * toBeat / Math.Max(1, beatCount - 1));
            var distance = Math.Abs(x2 - x1);
            var top = yOffset + BeatY - Math.Clamp(distance * 0.20, 18.0, 95.0);
            builder.AppendLine($"<path d=\"M {Format(x1)} {yOffset + BeatY} C {Format(x1)} {Format(top)}, {Format(x2)} {Format(top)}, {Format(x2)} {yOffset + BeatY}\" stroke=\"{color}\" stroke-width=\"1\" fill=\"none\" opacity=\"0.55\"/>");
        }
    }

    private static string Format(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
