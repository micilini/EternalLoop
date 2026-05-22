using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EternalLoop.App.Navigation;
using EternalLoop.App.Services;
using EternalLoop.Contracts.Abstractions;
using EternalLoop.Core.Persistence;
using System.Collections.ObjectModel;
using System.IO;

namespace EternalLoop.App.ViewModels;

public partial class RecentTracksViewModel : ObservableObject
{
    private readonly AppSessionState _sessionState;
    private readonly INavigationService _navigationService;
    private readonly ISettingsRepository _settingsRepository;

    [ObservableProperty] private ObservableCollection<RecentTrackItemViewModel> recentTracks = new();
    [ObservableProperty] private bool hasRecentTracks;
    [ObservableProperty] private string emptyMessage = "No recent tracks yet.";

    public RecentTracksViewModel(
        AppSessionState sessionState,
        INavigationService navigationService,
        ISettingsRepository settingsRepository)
    {
        _sessionState = sessionState;
        _navigationService = navigationService;
        _settingsRepository = settingsRepository;

        LoadRecentTracks();
    }

    [RelayCommand]
    private void OpenTrack(RecentTrackItemViewModel? item)
    {
        if (item is null || !File.Exists(item.FilePath))
        {
            return;
        }

        _sessionState.SelectedFilePath = item.FilePath;
        _sessionState.CurrentResult = null;
        _sessionState.ForceReanalysis = false;
        _sessionState.Settings.LastOpenedFile = item.FilePath;
        _sessionState.Settings.RecentFiles = RecentFileList.Add(item.FilePath, _sessionState.Settings.RecentFiles);
        _ = _settingsRepository.SaveAsync(_sessionState.Settings, CancellationToken.None);
        _navigationService.NavigateTo<AnalysisViewModel>();
    }

    [RelayCommand]
    private void Back()
    {
        _navigationService.NavigateTo<WelcomeViewModel>();
    }

    [RelayCommand]
    private void RemoveMissing()
    {
        _sessionState.Settings.RecentFiles = _sessionState.Settings.RecentFiles
            .Where(File.Exists)
            .Take(RecentFileList.MaxItems)
            .ToList();

        _ = _settingsRepository.SaveAsync(_sessionState.Settings, CancellationToken.None);
        LoadRecentTracks();
    }

    private void LoadRecentTracks()
    {
        RecentTracks.Clear();

        foreach (var file in _sessionState.Settings.RecentFiles.Take(RecentFileList.MaxItems))
        {
            if (string.IsNullOrWhiteSpace(file))
            {
                continue;
            }

            RecentTracks.Add(new RecentTrackItemViewModel
            {
                FilePath = file,
                FileName = Path.GetFileName(file),
                Folder = Path.GetDirectoryName(file) ?? string.Empty,
                Exists = File.Exists(file)
            });
        }

        HasRecentTracks = RecentTracks.Count > 0;
        EmptyMessage = HasRecentTracks
            ? string.Empty
            : "No recent tracks yet.";
    }
}

public sealed partial class RecentTrackItemViewModel : ObservableObject
{
    public required string FilePath { get; init; }

    public required string FileName { get; init; }

    public required string Folder { get; init; }

    public required bool Exists { get; init; }

    public string StatusText => Exists ? "Ready" : "Missing";
}
