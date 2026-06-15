using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using EternalLoop.App.Commands;
using EternalLoop.Core.Workflow;

namespace EternalLoop.App.ViewModels;

public sealed class AnalysisViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly string _filePath;
    private readonly ITrackWorkflowService _workflowService;
    private readonly Action _back;
    private readonly Action<TrackWorkflowResult>? _completed;
    private readonly bool _forceReanalysis;
    private readonly Stopwatch _elapsedStopwatch = new();
    private CancellationTokenSource? _elapsedClockCts;
    private Task? _elapsedClockTask;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _hasStarted;
    private bool _disposed;
    private string _currentStage = "Queued";
    private string _currentMessage = "Waiting to start.";
    private double _overallProgress;
    private bool _isRunning;
    private string? _errorMessage;
    private string _friendlyProgressText = "Preparing analysis.";
    private string _elapsedTimeText = "0:00";

    public AnalysisViewModel(
        string filePath,
        ITrackWorkflowService workflowService,
        Action back,
        Action<TrackWorkflowResult>? completed = null,
        bool forceReanalysis = false)
    {
        _filePath = filePath;
        _workflowService = workflowService
            ?? throw new ArgumentNullException(nameof(workflowService));
        _back = back ?? throw new ArgumentNullException(nameof(back));
        _completed = completed;
        _forceReanalysis = forceReanalysis;

        BackCommand = new RelayCommand(Back);
        CancelCommand = new RelayCommand(Cancel);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string FileName => Path.GetFileName(_filePath);

    public string AnalysisModeText => "Classic DSP analysis with modular branch analysis.";

    public string ElapsedTimeText
    {
        get => _elapsedTimeText;
        private set => SetProperty(ref _elapsedTimeText, value);
    }

    public ICommand BackCommand { get; }

    public ICommand CancelCommand { get; }

    public ObservableCollection<string> Log { get; } = [];

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cancellationTokenSource?.Cancel();
        _elapsedClockCts?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        _elapsedClockCts?.Dispose();
        _elapsedClockCts = null;
        _elapsedClockTask = null;
    }

    public string CurrentStage
    {
        get => _currentStage;
        private set => SetProperty(ref _currentStage, value);
    }

    public string CurrentMessage
    {
        get => _currentMessage;
        private set => SetProperty(ref _currentMessage, value);
    }

    public double OverallProgress
    {
        get => _overallProgress;
        private set => SetProperty(ref _overallProgress, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set => SetProperty(ref _isRunning, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public string FriendlyProgressText
    {
        get => _friendlyProgressText;
        private set => SetProperty(ref _friendlyProgressText, value);
    }

    public async Task StartAsync()
    {
        if (_hasStarted)
        {
            return;
        }

        _hasStarted = true;
        _cancellationTokenSource = new CancellationTokenSource();
        IsRunning = true;
        ErrorMessage = null;
        OverallProgress = 0;
        Log.Clear();
        StartElapsedClock();

        try
        {
            var input = TrackInput.FromFilePath(_filePath);
            var request = new TrackWorkflowRequest(input, forceReanalysis: _forceReanalysis);
            CancellationToken workflowToken = _cancellationTokenSource.Token;
            var reporter = new UiProgressReporter(this);

            TrackWorkflowResult result = await Task.Run(
                    async () => await _workflowService
                        .RunAsync(request, reporter, workflowToken)
                        .ConfigureAwait(false),
                    workflowToken)
                .ConfigureAwait(true);

            ApplyResult(result);
        }
        catch (OperationCanceledException)
        {
            CurrentStage = TrackWorkflowStatus.Canceled.ToString();
            CurrentMessage = "Analysis canceled.";
            FriendlyProgressText = "Analysis stopped before playback preparation.";
            Log.Add("Workflow canceled.");
        }
        catch (Exception)
        {
            CurrentStage = TrackWorkflowStatus.Failed.ToString();
            CurrentMessage = "EternalLoop could not prepare this track.";
            FriendlyProgressText = "EternalLoop could not prepare this track. Check the file and try again.";
            ErrorMessage = "EternalLoop could not prepare this track. Check the file and try again.";
            Log.Add("Workflow failed unexpectedly.");
        }
        finally
        {
            await StopElapsedClockAsync().ConfigureAwait(true);
            IsRunning = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private void Cancel()
    {
        _cancellationTokenSource?.Cancel();
        CurrentStage = "Canceling";
        FriendlyProgressText = "Cancel requested.";
    }

    private void Back()
    {
        Cancel();
        _back();
    }

    private void ApplyResult(TrackWorkflowResult result)
    {
        CurrentStage = result.Status.ToString();

        if (result.IsSuccess)
        {
            OverallProgress = 100;
            CurrentMessage = "Your loop map is ready.";
            FriendlyProgressText = CreateSuccessText(result);
            Log.Add("Workflow completed successfully.");
            if (result.CacheHit)
            {
                Log.Add("Runtime package loaded from cache.");
            }
            if (result.RuntimePackage is not null)
            {
                _completed?.Invoke(result);
            }
            return;
        }

        if (result.Status == TrackWorkflowStatus.Canceled)
        {
            CurrentMessage = "Analysis canceled.";
            FriendlyProgressText = "Canceled before playback preparation.";
            Log.Add("Workflow canceled.");
            return;
        }

        ErrorMessage = result.Error?.Message ?? "Workflow failed.";
        CurrentMessage = ErrorMessage;
        FriendlyProgressText = "Review the selected file and try again.";
        Log.Add($"Error: {ErrorMessage}");
    }

    private static string CreateSuccessText(TrackWorkflowResult result)
    {
        TrackAnalysisSummary? analysis = result.AnalysisSummary;
        TrackBranchSummary? branches = result.BranchSummary;

        string summary =
            $"Analyzed {analysis?.BeatCount ?? 0} beats, " +
            $"{analysis?.SegmentCount ?? 0} segments and " +
            $"{branches?.ActiveBranchCount ?? 0} active branches.";

        TrackRuntimePreparationSummary? runtime = result.RuntimeSummary;

        if (runtime is null)
        {
            return summary;
        }

        string source = result.CacheHit ? " Cached runtime package." : " Fresh runtime package.";

        return summary +
            $" Runtime package prepared with {runtime.RuntimeBeatCount} beats and " +
            $"{runtime.RuntimeBranchCount} playable branches." +
            source;
    }

    private void ApplyProgress(TrackWorkflowProgress progress)
    {
        CurrentStage = progress.Status.ToString();
        CurrentMessage = progress.Message;
        FriendlyProgressText = progress.Percent.HasValue
            ? $"{progress.Message} {progress.Percent.Value:0}%"
            : progress.Message;

        if (progress.Percent.HasValue)
        {
            OverallProgress = progress.Percent.Value;
        }

        Log.Add($"{progress.Status}: {progress.Message}");
    }

    private void StartElapsedClock()
    {
        _elapsedStopwatch.Restart();
        ElapsedTimeText = FormatElapsedTime(_elapsedStopwatch.Elapsed);

        _elapsedClockCts?.Cancel();
        _elapsedClockCts?.Dispose();
        _elapsedClockCts = new CancellationTokenSource();
        _elapsedClockTask = RunElapsedClockAsync(_elapsedClockCts.Token);
    }

    private async Task StopElapsedClockAsync()
    {
        CancellationTokenSource? cts = _elapsedClockCts;
        Task? task = _elapsedClockTask;

        _elapsedClockCts = null;
        _elapsedClockTask = null;

        if (cts is not null)
        {
            try
            {
                cts.Cancel();
                if (task is not null)
                {
                    await task.ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                cts.Dispose();
            }
        }

        _elapsedStopwatch.Stop();
        await DispatchElapsedTimeUpdateAsync(CancellationToken.None).ConfigureAwait(false);
    }

    private async Task RunElapsedClockAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(250));

        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            await DispatchElapsedTimeUpdateAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task DispatchElapsedTimeUpdateAsync(CancellationToken cancellationToken)
    {
        string text = FormatElapsedTime(_elapsedStopwatch.Elapsed);
        var dispatcher = Application.Current?.Dispatcher;

        if (dispatcher is null || dispatcher.CheckAccess())
        {
            ElapsedTimeText = text;
            return;
        }

        await dispatcher
            .InvokeAsync(() => ElapsedTimeText = text, DispatcherPriority.Background, cancellationToken)
            .Task
            .ConfigureAwait(false);
    }

    private static string FormatElapsedTime(TimeSpan elapsed)
    {
        return $"{(int)elapsed.TotalMinutes}:{elapsed.Seconds:00}";
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

    private sealed class UiProgressReporter : ITrackWorkflowProgressReporter
    {
        private readonly AnalysisViewModel _viewModel;

        public UiProgressReporter(AnalysisViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public ValueTask ReportAsync(
            TrackWorkflowProgress progress,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var dispatcher = Application.Current?.Dispatcher;

            if (dispatcher is null || dispatcher.CheckAccess())
            {
                _viewModel.ApplyProgress(progress);
                return ValueTask.CompletedTask;
            }

            _ = dispatcher.InvokeAsync(
                () => _viewModel.ApplyProgress(progress),
                DispatcherPriority.Background);

            return ValueTask.CompletedTask;
        }
    }
}
