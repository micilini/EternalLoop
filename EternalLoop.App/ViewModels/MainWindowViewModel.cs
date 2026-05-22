using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EternalLoop.App.Navigation;
using EternalLoop.App.Services;
using EternalLoop.Contracts.Abstractions;
using EternalLoop.Core.Persistence;
using Microsoft.Win32;
using System.IO;

namespace EternalLoop.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IAudioPlayer _player;
    private readonly AppSessionState _sessionState;
    private readonly ISettingsRepository _settingsRepository;

    [ObservableProperty]
    private ObservableObject? currentViewModel;

    public MainWindowViewModel(
        INavigationService navigationService,
        IAudioPlayer player,
        AppSessionState sessionState,
        ISettingsRepository settingsRepository)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _player = player ?? throw new ArgumentNullException(nameof(player));
        _sessionState = sessionState ?? throw new ArgumentNullException(nameof(sessionState));
        _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
        _navigationService.CurrentChanged += (_, _) => CurrentViewModel = _navigationService.CurrentViewModel;
    }

    public void Initialize()
    {
        if (CurrentViewModel is null)
        {
            _navigationService.NavigateTo<WelcomeViewModel>();
        }
    }

    [RelayCommand]
    private void NavigateHome()
    {
        StopPlayerIfLoaded();
        _navigationService.NavigateTo<WelcomeViewModel>();
    }

    [RelayCommand]
    private void OpenAnotherTrack()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Audio Files|*.mp3;*.wav;*.flac;*.m4a;*.aac|All Files|*.*",
            Title = "Open audio file"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var filePath = dialog.FileName;
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (!File.Exists(filePath) || extension is not (".mp3" or ".wav" or ".flac" or ".m4a" or ".aac"))
        {
            _navigationService.NavigateTo<WelcomeViewModel>();
            return;
        }

        StopPlayerIfLoaded();
        _sessionState.ClearCurrentTrack();
        _sessionState.SelectedFilePath = filePath;
        _sessionState.CurrentResult = null;
        _sessionState.ForceReanalysis = false;
        _sessionState.Settings.LastOpenedFile = filePath;
        _sessionState.Settings.RecentFiles = RecentFileList.Add(filePath, _sessionState.Settings.RecentFiles);
        _ = _settingsRepository.SaveAsync(_sessionState.Settings, CancellationToken.None);

        _navigationService.NavigateTo<AnalysisViewModel>();
    }

    [RelayCommand]
    private void NavigateRecentTracks()
    {
        StopPlayerIfLoaded();
        _navigationService.NavigateTo<RecentTracksViewModel>();
    }

    [RelayCommand]
    private void NavigateSettings()
    {
        _navigationService.NavigateTo<SettingsViewModel>();
    }

    private void StopPlayerIfLoaded()
    {
        try
        {
            _player.Stop();
        }
        catch (InvalidOperationException)
        {
        }
    }
}
