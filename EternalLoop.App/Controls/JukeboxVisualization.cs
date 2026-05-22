using EternalLoop.Contracts.Models;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace EternalLoop.App.Controls;

public sealed class JukeboxVisualization : FrameworkElement
{
    private const int MaxDisplayedEdges = 650;
    private const int HighlightedEdgeCount = 12;
    private const byte MinimumBranchAlpha = 118;
    private const byte MaximumBranchAlpha = 215;
    private const byte HighlightBranchAlpha = 238;
    private const double MinimumBranchThickness = 1.9;
    private const double MaximumBranchThickness = 3.4;
    private const double HighlightBranchThickness = 3.8;
    private const double LastJumpBranchThickness = 4.4;
    private const double OuterGlowThickness = 7.0;
    private const double InnerGlowThickness = 3.0;
    private const byte OuterGlowAlpha = 32;
    private const byte InnerGlowAlpha = 76;

    private static readonly Color[] BranchPalette =
    [
        Color.FromRgb(226, 78, 214),
        Color.FromRgb(190, 113, 255),
        Color.FromRgb(135, 111, 245),
        Color.FromRgb(98, 205, 220),
        Color.FromRgb(241, 92, 174)
    ];

    public static readonly DependencyProperty GraphProperty =
        DependencyProperty.Register(
            nameof(Graph),
            typeof(JukeboxGraph),
            typeof(JukeboxVisualization),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty CurrentBeatIndexProperty =
        DependencyProperty.Register(
            nameof(CurrentBeatIndex),
            typeof(int),
            typeof(JukeboxVisualization),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty LastJumpFromBeatProperty =
        DependencyProperty.Register(
            nameof(LastJumpFromBeat),
            typeof(int),
            typeof(JukeboxVisualization),
            new FrameworkPropertyMetadata(-1, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty LastJumpToBeatProperty =
        DependencyProperty.Register(
            nameof(LastJumpToBeat),
            typeof(int),
            typeof(JukeboxVisualization),
            new FrameworkPropertyMetadata(-1, FrameworkPropertyMetadataOptions.AffectsRender));

    public JukeboxGraph? Graph
    {
        get => (JukeboxGraph?)GetValue(GraphProperty);
        set => SetValue(GraphProperty, value);
    }

    public int CurrentBeatIndex
    {
        get => (int)GetValue(CurrentBeatIndexProperty);
        set => SetValue(CurrentBeatIndexProperty, value);
    }

    public int LastJumpFromBeat
    {
        get => (int)GetValue(LastJumpFromBeatProperty);
        set => SetValue(LastJumpFromBeatProperty, value);
    }

    public int LastJumpToBeat
    {
        get => (int)GetValue(LastJumpToBeatProperty);
        set => SetValue(LastJumpToBeatProperty, value);
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        var graph = Graph;
        if (graph is null || graph.Nodes.Count == 0 || ActualWidth <= 0 || ActualHeight <= 0)
        {
            DrawEmptyState(dc);
            return;
        }

        var center = new Point(ActualWidth / 2, ActualHeight / 2);
        var radius = Math.Max(60, Math.Min(ActualWidth, ActualHeight) / 2 - 38);
        var nodeCount = graph.Nodes.Count;

        var primary = new SolidColorBrush(Color.FromRgb(156, 108, 255));
        var secondary = new SolidColorBrush(Color.FromRgb(255, 119, 200));
        var cyan = new SolidColorBrush(Color.FromRgb(124, 231, 255));
        var muted = new SolidColorBrush(Color.FromArgb(135, 124, 231, 255));
        var subtle = new SolidColorBrush(Color.FromArgb(36, 156, 108, 255));

        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(28, 156, 108, 255)), null, center, radius + 24, radius + 24);
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(20, 255, 119, 200)), null, center, radius + 14, radius + 14);
        dc.DrawEllipse(subtle, new Pen(new SolidColorBrush(Color.FromArgb(185, 156, 108, 255)), 1.8), center, radius, radius);
        dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(120, 255, 119, 200)), 1.2), center, radius * 0.78, radius * 0.78);
        dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(100, 124, 231, 255)), 0.9), center, radius * 0.52, radius * 0.52);

        var displayEdges = EnumerateDisplayEdges(graph).ToArray();
        for (var i = 0; i < displayEdges.Length; i++)
        {
            var edge = displayEdges[i];
            DrawEdge(dc, center, radius, nodeCount, edge.FromBeat, edge.ToBeat, CreateBranchPen(edge, i, highlight: false));
        }

        if (graph.JumpEdges.TryGetValue(CurrentBeatIndex, out var currentEdges))
        {
            var highlighted = currentEdges
                .OrderByDescending(edge => edge.Similarity)
                .Take(HighlightedEdgeCount)
                .ToArray();

            for (var i = 0; i < highlighted.Length; i++)
            {
                var edge = highlighted[i];
                DrawEdge(dc, center, radius, nodeCount, CurrentBeatIndex, edge.ToBeat, CreateBranchPen(edge, i, highlight: true));
            }
        }

        if (LastJumpFromBeat >= 0 && LastJumpToBeat >= 0)
        {
            DrawEdge(
                dc,
                center,
                radius,
                nodeCount,
                LastJumpFromBeat,
                LastJumpToBeat,
                CreateSolidPen(secondary, LastJumpBranchThickness, HighlightBranchAlpha));
        }

        for (var i = 0; i < nodeCount; i++)
        {
            var p = PointForIndex(center, radius, nodeCount, i);
            var isCurrent = i == CurrentBeatIndex;
            var nodeRadius = isCurrent ? 6.0 : 2.8;
            var brush = isCurrent ? cyan : muted;

            if (isCurrent)
            {
                dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(96, 255, 255, 255)), null, p, nodeRadius + 12, nodeRadius + 12);
                dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(135, 124, 231, 255)), null, p, nodeRadius + 6, nodeRadius + 6);
            }

            dc.DrawEllipse(brush, null, p, nodeRadius, nodeRadius);
        }

        DrawCenterOrb(dc, center, radius);
    }

    private void DrawEmptyState(DrawingContext dc)
    {
        var center = new Point(ActualWidth / 2, ActualHeight / 2);
        var brush = new SolidColorBrush(Color.FromArgb(150, 139, 92, 246));
        dc.DrawEllipse(null, new Pen(brush, 1.5), center, 90, 90);
    }

    private static void DrawEdge(DrawingContext dc, Point center, double radius, int count, int from, int to, Pen pen)
    {
        if (count <= 0)
        {
            return;
        }

        var p1 = PointForIndex(center, radius, count, from);
        var p2 = PointForIndex(center, radius, count, to);
        var geometry = new StreamGeometry();

        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(p1, false, false);
            ctx.QuadraticBezierTo(center, p2, true, false);
        }

        geometry.Freeze();
        dc.DrawGeometry(null, CreateGlowPen(pen, OuterGlowThickness, OuterGlowAlpha), geometry);
        dc.DrawGeometry(null, CreateGlowPen(pen, InnerGlowThickness, InnerGlowAlpha), geometry);
        dc.DrawGeometry(null, pen, geometry);
    }

    private static IEnumerable<JukeboxEdge> EnumerateDisplayEdges(JukeboxGraph graph)
    {
        return graph.JumpEdges
            .SelectMany(pair => pair.Value)
            .OrderByDescending(edge => edge.Similarity)
            .Take(MaxDisplayedEdges)
            .OrderBy(edge => edge.Similarity);
    }

    private static Pen CreateBranchPen(JukeboxEdge edge, int index, bool highlight)
    {
        var color = BranchPalette[index % BranchPalette.Length];
        var alpha = highlight
            ? HighlightBranchAlpha
            : (byte)Math.Clamp(
                MinimumBranchAlpha + edge.Similarity * (MaximumBranchAlpha - MinimumBranchAlpha),
                MinimumBranchAlpha,
                MaximumBranchAlpha);
        var thickness = highlight
            ? HighlightBranchThickness
            : Math.Clamp(
                MinimumBranchThickness + edge.Similarity * (MaximumBranchThickness - MinimumBranchThickness),
                MinimumBranchThickness,
                MaximumBranchThickness);
        var brush = new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
        brush.Freeze();

        var pen = new Pen(brush, thickness)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round
        };
        pen.Freeze();
        return pen;
    }

    private static Pen CreateSolidPen(Brush brush, double thickness, byte alpha)
    {
        if (brush is SolidColorBrush solid)
        {
            brush = new SolidColorBrush(Color.FromArgb(alpha, solid.Color.R, solid.Color.G, solid.Color.B));
            brush.Freeze();
        }

        var pen = new Pen(brush, thickness)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round
        };

        pen.Freeze();
        return pen;
    }

    private static Pen CreateGlowPen(Pen source, double extraThickness, byte alpha)
    {
        if (source.Brush is not SolidColorBrush solid)
        {
            return source;
        }

        var color = solid.Color;
        var brush = new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
        brush.Freeze();

        var pen = new Pen(brush, source.Thickness + extraThickness)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round
        };
        pen.Freeze();
        return pen;
    }

    private static void DrawCenterOrb(DrawingContext dc, Point center, double radius)
    {
        var centerGlow = new RadialGradientBrush();
        centerGlow.GradientStops.Add(new GradientStop(Color.FromArgb(125, 156, 108, 255), 0));
        centerGlow.GradientStops.Add(new GradientStop(Color.FromArgb(80, 255, 119, 200), 0.48));
        centerGlow.GradientStops.Add(new GradientStop(Color.FromArgb(10, 9, 7, 17), 1));
        centerGlow.Freeze();

        var borderBrush = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255));
        borderBrush.Freeze();
        var orbRadius = Math.Max(58, radius * 0.22);
        dc.DrawEllipse(centerGlow, new Pen(borderBrush, 1.2), center, orbRadius, orbRadius);

        var text = new FormattedText(
            "∞",
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            orbRadius * 0.62,
            Brushes.White,
            1.0);

        dc.DrawText(text, new Point(center.X - text.Width / 2, center.Y - text.Height / 2 - 4));
    }

    private static Point PointForIndex(Point center, double radius, int count, int index)
    {
        var angle = (index / (double)Math.Max(1, count)) * Math.PI * 2 - Math.PI / 2;
        return new Point(
            center.X + Math.Cos(angle) * radius,
            center.Y + Math.Sin(angle) * radius);
    }
}
