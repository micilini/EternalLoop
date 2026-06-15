using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EternalLoop.App.Converters;

public sealed class ZeroToVisibilityConverter : IValueConverter
{
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        return value switch
        {
            int number => number == 0 ? Visibility.Collapsed : Visibility.Visible,
            double number => Math.Abs(number) < double.Epsilon ? Visibility.Collapsed : Visibility.Visible,
            _ => Visibility.Collapsed
        };
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}
