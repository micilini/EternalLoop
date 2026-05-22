using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace EternalLoop.App.Controls;

public sealed class MiniSpectrumControl : FrameworkElement
{
    private readonly DispatcherTimer _timer;
    private double _animationOffset;

    public static readonly DependencyProperty IsPlayingProperty =
        DependencyProperty.Register(
            nameof(IsPlaying),
            typeof(bool),
            typeof(MiniSpectrumControl),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ActivitySeedProperty =
        DependencyProperty.Register(
            nameof(ActivitySeed),
            typeof(int),
            typeof(MiniSpectrumControl),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

    public MiniSpectrumControl()
    {
        SnapsToDevicePixels = true;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(70) };
        _timer.Tick += (_, _) =>
        {
            if (IsPlaying)
            {
                _animationOffset += 0.22;
                InvalidateVisual();
            }
        };

        Loaded += (_, _) => _timer.Start();
        Unloaded += (_, _) => _timer.Stop();
    }

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

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(double.IsInfinity(availableSize.Width) ? 180 : availableSize.Width, 30);
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        var width = ActualWidth <= 0 ? 180 : ActualWidth;
        var height = ActualHeight <= 0 ? 30 : ActualHeight;
        const int bars = 24;
        const double gap = 3;
        var barWidth = Math.Max(3, (width - (bars - 1) * gap) / bars);

        var cyan = Color.FromRgb(124, 231, 255);
        var purple = Color.FromRgb(156, 108, 255);
        var pink = Color.FromRgb(255, 119, 200);

        for (var i = 0; i < bars; i++)
        {
            var normalized = IsPlaying
                ? 0.28 + 0.72 * Math.Abs(Math.Sin(_animationOffset + i * 0.58 + ActivitySeed * 0.03))
                : 0.18 + 0.22 * Math.Abs(Math.Sin(i * 0.7));
            var barHeight = Math.Max(5, normalized * height);
            var x = i * (barWidth + gap);
            var y = height - barHeight;
            var color = (i % 3) switch
            {
                0 => cyan,
                1 => purple,
                _ => pink
            };
            var brush = new LinearGradientBrush(
                Color.FromArgb(230, color.R, color.G, color.B),
                Color.FromArgb(150, purple.R, purple.G, purple.B),
                90);
            brush.Freeze();

            dc.DrawRoundedRectangle(brush, null, new Rect(x, y, barWidth, barHeight), barWidth / 2, barWidth / 2);
        }
    }
}
