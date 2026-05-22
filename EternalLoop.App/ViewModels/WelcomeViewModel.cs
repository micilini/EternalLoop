using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EternalLoop.App.Navigation;
using EternalLoop.App.Services;
using EternalLoop.Contracts.Abstractions;
using EternalLoop.Core.Persistence;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;

namespace EternalLoop.App.ViewModels;

public partial class WelcomeViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly AppSessionState _sessionState;
    private readonly ISettingsRepository _settingsRepository;

    [ObservableProperty]
    private string? selectedFilePath;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private ObservableCollection<string> recentFiles = new();

    [ObservableProperty]
    private bool hasRecentFiles;

    public WelcomeViewModel(
        INavigationService navigationService,
        AppSessionState sessionState,
        ISettingsRepository settingsRepository)
    {
        _navigationService = navigationService;
        _sessionState = sessionState;
        _settingsRepository = settingsRepository;

        ReloadRecentFiles();
    }

    [RelayCommand]
    private void OpenFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Audio Files|*.mp3;*.wav;*.flac;*.m4a;*.aac|All Files|*.*",
            Title = "Open audio file"
        };

        if (dialog.ShowDialog() == true)
        {
            StartAnalysis(dialog.FileName);
        }
    }

    [RelayCommand]
    private void FileDropped(string? filePath)
    {
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            StartAnalysis(filePath);
        }
    }

    private void StartAnalysis(string filePath)
    {
        if (!File.Exists(filePath))
        {
            ErrorMessage = "File not found.";
            return;
        }

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (extension is not (".mp3" or ".wav" or ".flac" or ".m4a" or ".aac"))
        {
            ErrorMessage = "Unsupported file format. Use MP3, WAV, FLAC, M4A or AAC.";
            return;
        }

        SelectedFilePath = filePath;
        ErrorMessage = null;
        _sessionState.SelectedFilePath = filePath;
        _sessionState.CurrentResult = null;
        _sessionState.Settings.LastOpenedFile = filePath;
        _sessionState.Settings.RecentFiles = RecentFileList.Add(filePath, _sessionState.Settings.RecentFiles);
        _ = _settingsRepository.SaveAsync(_sessionState.Settings, CancellationToken.None);
        _navigationService.NavigateTo<AnalysisViewModel>();
    }

    [RelayCommand]
    private void OpenRecent(string? filePath)
    {
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            StartAnalysis(filePath);
        }
    }

    private void ReloadRecentFiles()
    {
        RecentFiles.Clear();

        foreach (var file in _sessionState.Settings.RecentFiles.Where(File.Exists))
        {
            RecentFiles.Add(file);
        }

        HasRecentFiles = RecentFiles.Count > 0;
    }
}
