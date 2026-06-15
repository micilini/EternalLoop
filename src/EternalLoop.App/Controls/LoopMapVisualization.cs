using System.Globalization;
using System.Windows;
using System.Windows.Media;
using EternalLoop.Playback.Visualization;

namespace EternalLoop.App.Controls;

public sealed class LoopMapVisualization : FrameworkElement
{
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

    private static readonly Color PrimaryColor = Color.FromRgb(156, 108, 255);
    private static readonly Color SecondaryColor = Color.FromRgb(255, 119, 200);
    private static readonly Color CyanColor = Color.FromRgb(124, 231, 255);
    private static readonly Color EmptyColor = Color.FromRgb(139, 92, 246);
    private static readonly Color CenterDarkColor = Color.FromRgb(9, 7, 17);

    private static readonly Color[] BranchPalette =
    [
        Color.FromRgb(226, 78, 214),
        Color.FromRgb(190, 113, 255),
        Color.FromRgb(135, 111, 245),
        Color.FromRgb(98, 205, 220),
        Color.FromRgb(241, 92, 174)
    ];

    private BranchGraph? _cachedGraph;
    private LoopMapRenderPlan _renderPlan = LoopMapRenderPlan.Empty;

    public static readonly DependencyProperty GraphProperty = DependencyProperty.Register(
        nameof(Graph),
        typeof(BranchGraph),
        typeof(LoopMapVisualization),
        new FrameworkPropertyMetadata(BranchGraph.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty CurrentBeatIndexProperty = DependencyProperty.Register(
        nameof(CurrentBeatIndex),
        typeof(int),
        typeof(LoopMapVisualization),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty LastJumpFromBeatProperty = DependencyProperty.Register(
        nameof(LastJumpFromBeat),
        typeof(int),
        typeof(LoopMapVisualization),
        new FrameworkPropertyMetadata(-1, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty LastJumpToBeatProperty = DependencyProperty.Register(
        nameof(LastJumpToBeat),
        typeof(int),
        typeof(LoopMapVisualization),
        new FrameworkPropertyMetadata(-1, FrameworkPropertyMetadataOptions.AffectsRender));

    public BranchGraph Graph
    {
        get => (BranchGraph)GetValue(GraphProperty);
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

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        if (ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        BranchGraph graph = Graph ?? BranchGraph.Empty;
        Point center = new(ActualWidth / 2, ActualHeight / 2);
        double radius = Math.Max(60, Math.Min(ActualWidth, ActualHeight) / 2 - 38);

        if (graph.Nodes.Count == 0)
        {
            DrawEmptyState(drawingContext, center, radius);
            DrawCenterOrb(drawingContext, center, radius);
            return;
        }

        LoopMapRenderPlan plan = GetRenderPlan(graph);

        DrawRings(drawingContext, center, radius);
        DrawBaseEdges(drawingContext, graph, plan, center, radius);
        DrawHighlightedEdges(drawingContext, graph, plan, center, radius);
        DrawLastJumpEdge(drawingContext, plan.BeatOrdinals, center, radius);
        DrawNodes(drawingContext, graph, plan.BeatOrdinals, center, radius);
        DrawCenterOrb(drawingContext, center, radius);
    }

    private LoopMapRenderPlan GetRenderPlan(BranchGraph graph)
    {
        if (ReferenceEquals(_cachedGraph, graph))
        {
            return _renderPlan;
        }

        _cachedGraph = graph;
        _renderPlan = LoopMapRenderPlan.Create(graph);
        return _renderPlan;
    }

    private static void DrawRings(DrawingContext drawingContext, Point center, double radius)
    {
        Brush subtle = CreateFrozenBrush(Color.FromArgb(36, PrimaryColor.R, PrimaryColor.G, PrimaryColor.B));

        drawingContext.DrawEllipse(
            CreateFrozenBrush(Color.FromArgb(28, PrimaryColor.R, PrimaryColor.G, PrimaryColor.B)),
            null,
            center,
            radius + 24,
            radius + 24);
        drawingContext.DrawEllipse(
            CreateFrozenBrush(Color.FromArgb(20, SecondaryColor.R, SecondaryColor.G, SecondaryColor.B)),
            null,
            center,
            radius + 14,
            radius + 14);
        drawingContext.DrawEllipse(
            subtle,
            CreateSolidPen(Color.FromArgb(185, PrimaryColor.R, PrimaryColor.G, PrimaryColor.B), 1.8),
            center,
            radius,
            radius);
        drawingContext.DrawEllipse(
            null,
            CreateSolidPen(Color.FromArgb(120, SecondaryColor.R, SecondaryColor.G, SecondaryColor.B), 1.2),
            center,
            radius * 0.78,
            radius * 0.78);
        drawingContext.DrawEllipse(
            null,
            CreateSolidPen(Color.FromArgb(100, CyanColor.R, CyanColor.G, CyanColor.B), 0.9),
            center,
            radius * 0.52,
            radius * 0.52);
    }

    private static void DrawEmptyState(DrawingContext drawingContext, Point center, double radius)
    {
        drawingContext.DrawEllipse(
            CreateFrozenBrush(Color.FromArgb(28, EmptyColor.R, EmptyColor.G, EmptyColor.B)),
            null,
            center,
            radius + 18,
            radius + 18);
        drawingContext.DrawEllipse(
            null,
            CreateSolidPen(Color.FromArgb(150, EmptyColor.R, EmptyColor.G, EmptyColor.B), 1.8),
            center,
            radius,
            radius);
        drawingContext.DrawEllipse(
            null,
            CreateSolidPen(Color.FromArgb(84, CyanColor.R, CyanColor.G, CyanColor.B), 1.0),
            center,
            radius * 0.58,
            radius * 0.58);
    }

    private void DrawBaseEdges(
        DrawingContext drawingContext,
        BranchGraph graph,
        LoopMapRenderPlan plan,
        Point center,
        double radius)
    {
        int count = graph.Nodes.Count;

        foreach (BranchGraphEdge edge in plan.DisplayEdges)
        {
            if (!TryPointForBeat(plan.BeatOrdinals, center, radius, count, edge.FromBeat, out Point from)
                || !TryPointForBeat(plan.BeatOrdinals, center, radius, count, edge.ToBeat, out Point to))
            {
                continue;
            }

            double quality = LoopMapRenderPlan.QualityFromDistance(edge.Distance);

            DrawEdge(drawingContext, center, from, to, CreateBranchPen(edge, quality, highlight: false));
        }
    }

    private void DrawHighlightedEdges(
        DrawingContext drawingContext,
        BranchGraph graph,
        LoopMapRenderPlan plan,
        Point center,
        double radius)
    {
        int count = graph.Nodes.Count;

        if (!plan.TryGetHighlightedEdges(CurrentBeatIndex, out IReadOnlyList<BranchGraphEdge> highlightedEdges))
        {
            return;
        }

        foreach (BranchGraphEdge edge in highlightedEdges)
        {
            if (!TryPointForBeat(plan.BeatOrdinals, center, radius, count, edge.FromBeat, out Point from)
                || !TryPointForBeat(plan.BeatOrdinals, center, radius, count, edge.ToBeat, out Point to))
            {
                continue;
            }

            DrawEdge(drawingContext, center, from, to, CreateBranchPen(edge, LoopMapRenderPlan.QualityFromDistance(edge.Distance), highlight: true));
        }
    }

    private void DrawLastJumpEdge(
        DrawingContext drawingContext,
        IReadOnlyDictionary<int, int> ordinals,
        Point center,
        double radius)
    {
        if (LastJumpFromBeat < 0 || LastJumpToBeat < 0)
        {
            return;
        }

        int count = ordinals.Count;
        if (!TryPointForBeat(ordinals, center, radius, count, LastJumpFromBeat, out Point from)
            || !TryPointForBeat(ordinals, center, radius, count, LastJumpToBeat, out Point to))
        {
            return;
        }

        DrawEdge(
            drawingContext,
            center,
            from,
            to,
            CreateSolidPen(
                Color.FromArgb(HighlightBranchAlpha, SecondaryColor.R, SecondaryColor.G, SecondaryColor.B),
                LastJumpBranchThickness));
    }

    private void DrawNodes(
        DrawingContext drawingContext,
        BranchGraph graph,
        IReadOnlyDictionary<int, int> ordinals,
        Point center,
        double radius)
    {
        int count = graph.Nodes.Count;
        Brush muted = CreateFrozenBrush(Color.FromArgb(135, CyanColor.R, CyanColor.G, CyanColor.B));
        Brush cyan = CreateFrozenBrush(CyanColor);

        foreach (BranchGraphNode node in graph.Nodes)
        {
            if (!TryPointForBeat(ordinals, center, radius, count, node.BeatIndex, out Point point))
            {
                continue;
            }

            bool isCurrent = node.BeatIndex == CurrentBeatIndex;
            double nodeRadius = isCurrent ? 6.0 : 2.8;
            Brush brush = isCurrent ? cyan : muted;

            if (isCurrent)
            {
                drawingContext.DrawEllipse(
                    CreateFrozenBrush(Color.FromArgb(96, 255, 255, 255)),
                    null,
                    point,
                    nodeRadius + 12,
                    nodeRadius + 12);
                drawingContext.DrawEllipse(
                    CreateFrozenBrush(Color.FromArgb(135, 124, 231, 255)),
                    null,
                    point,
                    nodeRadius + 6,
                    nodeRadius + 6);
            }

            drawingContext.DrawEllipse(brush, null, point, nodeRadius, nodeRadius);
        }
    }

    private void DrawCenterOrb(DrawingContext drawingContext, Point center, double radius)
    {
        RadialGradientBrush centerGlow = new();
        centerGlow.GradientStops.Add(new GradientStop(Color.FromArgb(125, PrimaryColor.R, PrimaryColor.G, PrimaryColor.B), 0));
        centerGlow.GradientStops.Add(new GradientStop(Color.FromArgb(80, SecondaryColor.R, SecondaryColor.G, SecondaryColor.B), 0.48));
        centerGlow.GradientStops.Add(new GradientStop(Color.FromArgb(10, CenterDarkColor.R, CenterDarkColor.G, CenterDarkColor.B), 1));
        centerGlow.Freeze();

        Brush borderBrush = CreateFrozenBrush(Color.FromArgb(70, 255, 255, 255));
        double orbRadius = Math.Max(58, radius * 0.22);
        drawingContext.DrawEllipse(centerGlow, new Pen(borderBrush, 1.2), center, orbRadius, orbRadius);

        FormattedText text = new(
            "∞",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            orbRadius * 0.62,
            Brushes.White,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

        drawingContext.DrawText(text, new Point(center.X - (text.Width / 2), center.Y - (text.Height / 2)));
    }

    private static bool TryPointForBeat(
        IReadOnlyDictionary<int, int> ordinals,
        Point center,
        double radius,
        int count,
        int beatIndex,
        out Point point)
    {
        point = default;

        if (count <= 0 || !ordinals.TryGetValue(beatIndex, out int ordinal))
        {
            return false;
        }

        double angle = (-Math.PI / 2) + ((Math.Tau * ordinal) / count);
        point = new Point(
            center.X + (Math.Cos(angle) * radius),
            center.Y + (Math.Sin(angle) * radius));
        return true;
    }

    private static void DrawEdge(DrawingContext drawingContext, Point center, Point from, Point to, Pen pen)
    {
        StreamGeometry geometry = new();

        using (StreamGeometryContext context = geometry.Open())
        {
            context.BeginFigure(from, false, false);
            context.QuadraticBezierTo(center, to, true, false);
        }

        geometry.Freeze();
        drawingContext.DrawGeometry(null, CreateGlowPen(pen, OuterGlowThickness, OuterGlowAlpha), geometry);
        drawingContext.DrawGeometry(null, CreateGlowPen(pen, InnerGlowThickness, InnerGlowAlpha), geometry);
        drawingContext.DrawGeometry(null, pen, geometry);
    }

    private static Color GetBranchColor(BranchGraphEdge edge)
    {
        return BranchPalette[Math.Abs(edge.Id) % BranchPalette.Length];
    }

    private static Pen CreateBranchPen(BranchGraphEdge edge, double quality, bool highlight)
    {
        Color color = GetBranchColor(edge);

        byte alpha = highlight
            ? HighlightBranchAlpha
            : (byte)Math.Clamp(
                MinimumBranchAlpha + (quality * (MaximumBranchAlpha - MinimumBranchAlpha)),
                MinimumBranchAlpha,
                MaximumBranchAlpha);

        double thickness = highlight
            ? HighlightBranchThickness
            : Math.Clamp(
                MinimumBranchThickness + (quality * (MaximumBranchThickness - MinimumBranchThickness)),
                MinimumBranchThickness,
                MaximumBranchThickness);

        return CreateSolidPen(Color.FromArgb(alpha, color.R, color.G, color.B), thickness);
    }

    private static Pen CreateGlowPen(Pen source, double extraThickness, byte alpha)
    {
        if (source.Brush is not SolidColorBrush solid)
        {
            return source;
        }

        Color color = solid.Color;
        return CreateSolidPen(
            Color.FromArgb(alpha, color.R, color.G, color.B),
            source.Thickness + extraThickness);
    }

    private static Pen CreateSolidPen(Color color, double thickness)
    {
        Pen pen = new(CreateFrozenBrush(color), thickness)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round
        };
        pen.Freeze();
        return pen;
    }

    private static Brush CreateFrozenBrush(Color color)
    {
        SolidColorBrush brush = new(color);
        brush.Freeze();
        return brush;
    }

}
