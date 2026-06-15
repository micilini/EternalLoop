using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using EternalLoop.App.Commands;
using EternalLoop.App.Services;
using EternalLoop.Playback.Audio;

namespace EternalLoop.App.ViewModels;

public sealed class WelcomeViewModel : INotifyPropertyChanged
{
    private readonly IFilePickerService _filePickerService;
    private readonly Action<string> _startAnalysis;
    private string? _errorMessage;

    public WelcomeViewModel(
        IFilePickerService filePickerService,
        Action<string> startAnalysis)
    {
        _filePickerService = filePickerService
            ?? throw new ArgumentNullException(nameof(filePickerService));
        _startAnalysis = startAnalysis
            ?? throw new ArgumentNullException(nameof(startAnalysis));

        OpenFileCommand = new RelayCommand(OpenFile);
        FileDroppedCommand = new DroppedFileCommand(StartDroppedFile);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand OpenFileCommand { get; }

    public ICommand FileDroppedCommand { get; }

    public ObservableCollection<string> RecentFiles { get; } = [];

    public bool HasRecentFiles => RecentFiles.Count > 0;

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    private void OpenFile()
    {
        string? filePath = _filePickerService.PickAudioFile();

        if (!string.IsNullOrWhiteSpace(filePath))
        {
            StartFile(filePath);
        }
    }

    private void StartDroppedFile(string? filePath)
    {
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            StartFile(filePath);
        }
    }

    private void StartFile(string filePath)
    {
        if (!SupportedAudioFormats.IsSupportedExtension(filePath))
        {
            ErrorMessage = $"Choose an {SupportedAudioFormats.DisplayName} file.";
            return;
        }

        ErrorMessage = null;
        _startAnalysis(filePath);
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

    private sealed class DroppedFileCommand : ICommand
    {
        private readonly Action<string?> _execute;

        public DroppedFileCommand(Action<string?> execute)
        {
            _execute = execute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return parameter is string;
        }

        public void Execute(object? parameter)
        {
            _execute(parameter as string);
        }

        public void NotifyCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
