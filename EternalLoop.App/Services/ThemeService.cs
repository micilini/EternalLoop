using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using System.Windows;

namespace EternalLoop.App.Services;

public sealed class ThemeService : IThemeService
{
    public void Apply(string? themePreference)
    {
        var effective = ResolveEffectiveTheme(themePreference);

        var paletteHelper = new PaletteHelper();
        var theme = paletteHelper.GetTheme();
        theme.SetBaseTheme(effective == "Dark" ? BaseTheme.Dark : BaseTheme.Light);
        paletteHelper.SetTheme(theme);

        if (Application.Current is App app)
        {
            app.Resources["EL.CurrentTheme"] = effective;
        }
    }

    public string ResolveEffectiveTheme(string? themePreference)
    {
        var normalized = Normalize(themePreference);
        return normalized == "System"
            ? IsWindowsDarkModeEnabled() ? "Dark" : "Light"
            : normalized;
    }

    private static string Normalize(string? themePreference)
    {
        return themePreference switch
        {
            "Light" => "Light",
            "System" => "System",
            _ => "Dark"
        };
    }

    private static bool IsWindowsDarkModeEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");

            var value = key?.GetValue("AppsUseLightTheme");
            return value is int intValue && intValue == 0;
        }
        catch
        {
            return false;
        }
    }
}
