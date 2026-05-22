using CommunityToolkit.Mvvm.ComponentModel;
using EternalLoop.Contracts;

namespace EternalLoop.App.ViewModels;

public partial class SplashScreenViewModel : ObservableObject
{
    public event Action? LoadingComplete;

    public string ApplicationVersion => ProductInfo.DisplayVersion;

    [ObservableProperty]
    private int progressValue;

    [ObservableProperty]
    private string loadingMessage = "Starting audio engine...";

    public async Task InitializeAsync()
    {
        var steps = new[]
        {
            "Starting audio engine...",
            "Preparing analysis pipeline...",
            "Loading jukebox traversal...",
            "Warming up neon interface...",
            "Almost ready..."
        };

        const int totalSteps = 100;
        const int delayMs = 55;

        for (var i = 0; i <= totalSteps; i++)
        {
            ProgressValue = i;
            LoadingMessage = steps[Math.Min(steps.Length - 1, i / 22)];
            await Task.Delay(delayMs).ConfigureAwait(true);
        }

        LoadingComplete?.Invoke();
    }
}
