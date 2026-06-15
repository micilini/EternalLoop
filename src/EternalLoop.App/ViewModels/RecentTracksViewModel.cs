using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using EternalLoop.App.Commands;
using EternalLoop.Core.Recent;

namespace EternalLoop.App.ViewModels;

public sealed class RecentTracksViewModel : INotifyPropertyChanged
{
    private readonly IRecentTracksService _recentTracksService;
    private readonly Action<string> _openTrack;
    private readonly Action _back;
    private string _statusMessage = string.Empty;

    public RecentTracksViewModel(
        IRecentTracksService recentTracksService,
        Action<string> openTrack,
        Action back)
    {
        _recentTracksService = recentTracksService
            ?? throw new ArgumentNullException(nameof(recentTracksService));
        _openTrack = openTrack
            ?? throw new ArgumentNullException(nameof(openTrack));
        _back = back ?? throw new ArgumentNullException(nameof(back));

        OpenTrackCommand = new ParameterRelayCommand(OpenTrack);
        BackCommand = new RelayCommand(_back);
        RemoveMissingCommand = new AsyncRelayCommand(RemoveMissingAsync, onError: HandleError);

        _ = LoadAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<RecentTrackItemViewModel> RecentTracks { get; } = [];

    public bool HasRecentTracks => RecentTracks.Count > 0;

    public string EmptyMessage => "No recent tracks yet.";

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public ICommand OpenTrackCommand { get; }

    public ICommand BackCommand { get; }

    public ICommand RemoveMissingCommand { get; }

    private async Task LoadAsync()
    {
        try
        {
            IReadOnlyList<RecentTrackEntry> entries = await _recentTracksService
                .LoadAsync()
                .ConfigureAwait(true);

            RecentTracks.Clear();
            foreach (RecentTrackEntry entry in entries)
            {
                RecentTracks.Add(new RecentTrackItemViewModel(entry));
            }

            StatusMessage = HasRecentTracks
                ? $"{RecentTracks.Count} recent track(s)."
                : EmptyMessage;
            OnPropertyChanged(nameof(HasRecentTracks));
        }
        catch (Exception)
        {
            RecentTracks.Clear();
            StatusMessage = "Recent tracks could not be loaded.";
            OnPropertyChanged(nameof(HasRecentTracks));
        }
    }

    private void OpenTrack(object? parameter)
    {
        if (parameter is not RecentTrackItemViewModel item || !item.Exists)
        {
            StatusMessage = "Original file was not found.";
            return;
        }

        _openTrack(item.FilePath);
    }

    private async Task RemoveMissingAsync()
    {
        IReadOnlyList<RecentTrackEntry> entries = await _recentTracksService
            .RemoveMissingAsync()
            .ConfigureAwait(true);

        RecentTracks.Clear();
        foreach (RecentTrackEntry entry in entries)
        {
            RecentTracks.Add(new RecentTrackItemViewModel(entry));
        }

        StatusMessage = "Missing tracks removed.";
        OnPropertyChanged(nameof(HasRecentTracks));
    }

    private void HandleError(Exception exception)
    {
        StatusMessage = "Recent tracks could not be updated.";
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
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private sealed class ParameterRelayCommand : ICommand
    {
        private readonly Action<object?> _execute;

        public ParameterRelayCommand(Action<object?> execute)
        {
            _execute = execute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter)
        {
            return true;
        }

        public void Execute(object? parameter)
        {
            _execute(parameter);
        }
    }
}

public sealed class RecentTrackItemViewModel
{
    public RecentTrackItemViewModel(RecentTrackEntry entry)
    {
        FilePath = entry.FilePath;
        FileName = string.IsNullOrWhiteSpace(entry.FileName)
            ? Path.GetFileName(entry.FilePath)
            : entry.FileName;
        Folder = entry.Folder;
        Exists = File.Exists(entry.FilePath);
        StatusText = Exists ? "Ready" : "Missing";
    }

    public string FilePath { get; }

    public string FileName { get; }

    public string Folder { get; }

    public bool Exists { get; }

    public string StatusText { get; }
}
