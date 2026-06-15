using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace EternalLoop.App.Controls;

public sealed class MiniSpectrumControl : FrameworkElement
{
    private static readonly Brush PlayingBarBrush = CreateBarBrush(0.92);
    private static readonly Brush IdleBarBrush = CreateBarBrush(0.42);

    private readonly DispatcherTimer _timer;
    private double _animationOffset;

    public static readonly DependencyProperty IsPlayingProperty = DependencyProperty.Register(
        nameof(IsPlaying),
        typeof(bool),
        typeof(MiniSpectrumControl),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ActivitySeedProperty = DependencyProperty.Register(
        nameof(ActivitySeed),
        typeof(int),
        typeof(MiniSpectrumControl),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

    public bool IsPlaying
    {
        get => (bool)GetValue(IsPlayingProperty);
        set => SetValue(IsPlayingProperty, value);
    }

    public int ActivitySeed
    {
        get => (int)GetValue(ActivitySeedProperty);
        set => SetValue(ActivitySeedProperty, value);
    }

    public MiniSpectrumControl()
    {
        SnapsToDevicePixels = true;

        _timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(70)
        };

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(
            double.IsInfinity(availableSize.Width) ? 180 : availableSize.Width,
            double.IsInfinity(availableSize.Height) ? 30 : availableSize.Height);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        const int bars = 24;
        const double gap = 3;

        double renderWidth = Math.Max(1, ActualWidth);
        double renderHeight = Math.Max(1, ActualHeight);
        double barWidth = Math.Max(3, (renderWidth - (gap * (bars - 1))) / bars);
        double center = renderHeight / 2;
        Brush barBrush = IsPlaying ? PlayingBarBrush : IdleBarBrush;

        for (int index = 0; index < bars; index++)
        {
            double normalized = IsPlaying
                ? 0.28 + (0.72 * Math.Abs(Math.Sin(_animationOffset + (index * 0.58) + (ActivitySeed * 0.03))))
                : 0.18 + (0.22 * Math.Abs(Math.Sin(index * 0.7)));
            double height = Math.Max(4, renderHeight * normalized);
            double x = index * (barWidth + gap);
            double y = center - (height / 2);

            drawingContext.DrawRoundedRectangle(barBrush, null, new Rect(x, y, barWidth, height), 2, 2);
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _timer.Tick -= OnTimerTick;
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (!IsLoaded || !IsPlaying)
        {
            return;
        }

        _animationOffset += 0.22;
        InvalidateVisual();
    }

    private static LinearGradientBrush CreateBarBrush(double opacity)
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 1),
            EndPoint = new Point(0, 0),
            Opacity = opacity,
            GradientStops =
            [
                new GradientStop(Color.FromRgb(124, 231, 255), 0),
                new GradientStop(Color.FromRgb(156, 108, 255), 0.55),
                new GradientStop(Color.FromRgb(255, 119, 200), 1)
            ]
        };

        brush.Freeze();
        return brush;
    }
}
