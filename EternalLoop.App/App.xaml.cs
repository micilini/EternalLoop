using EternalLoop.App.Composition;
using EternalLoop.App.Services;
using EternalLoop.App.Views;
using EternalLoop.Contracts.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Windows;

namespace EternalLoop.App;

public partial class App : Application
{
    private const string AppMutexName = @"Local\EternalLoop";
    private static Mutex? _appMutex;
    private IHost? _host;

    public IServiceProvider Services =>
        _host?.Services ?? throw new InvalidOperationException("Application host has not been initialized.");

    protected override async void OnStartup(StartupEventArgs e)
    {
        _appMutex = new Mutex(true, AppMutexName, out var createdNew);

        if (!createdNew)
        {
            _appMutex.Dispose();
            _appMutex = null;
            Shutdown();
            return;
        }

        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(ServiceRegistration.ConfigureServices)
            .Build();

        await _host.StartAsync();

        var settingsRepository = Services.GetRequiredService<ISettingsRepository>();
        var sessionState = Services.GetRequiredService<AppSessionState>();
        var themeService = Services.GetRequiredService<IThemeService>();
        var player = Services.GetRequiredService<IAudioPlayer>();

        sessionState.Settings = await settingsRepository.LoadAsync(CancellationToken.None);
        sessionState.Settings.Theme = "Dark";
        sessionState.EffectiveTheme = "Dark";
        themeService.Apply("Dark");
        sessionState.Settings.Volume = 1.0f;
        player.Volume = 1.0f;

        var splash = Services.GetRequiredService<SplashScreenWindow>();
        splash.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            try
            {
                var settingsRepository = _host.Services.GetRequiredService<ISettingsRepository>();
                var sessionState = _host.Services.GetRequiredService<AppSessionState>();
                await settingsRepository.SaveAsync(sessionState.Settings, CancellationToken.None);
            }
            catch
            {
            }

            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
        }

        _appMutex?.Dispose();
        _appMutex = null;

        base.OnExit(e);
    }
}
