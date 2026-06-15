using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace EternalLoop.App.ViewModels;

public sealed class SplashScreenViewModel : INotifyPropertyChanged
{
    private int _progressValue;
    private string _loadingMessage = "Starting audio engine...";

    public event PropertyChangedEventHandler? PropertyChanged;

    public event Action? LoadingComplete;

    public string ApplicationVersion => $"v{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0"}";

    public int ProgressValue
    {
        get => _progressValue;
        private set => SetProperty(ref _progressValue, value);
    }

    public string LoadingMessage
    {
        get => _loadingMessage;
        private set => SetProperty(ref _loadingMessage, value);
    }

    public async Task InitializeAsync()
    {
        string[] steps =
        [
            "Starting audio engine...",
            "Preparing analysis pipeline...",
            "Loading loop traversal...",
            "Warming up neon interface...",
            "Almost ready..."
        ];

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

    private bool SetProperty<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
