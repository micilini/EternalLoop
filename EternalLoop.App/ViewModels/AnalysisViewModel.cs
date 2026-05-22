using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EternalLoop.App.Navigation;
using EternalLoop.App.Services;
using EternalLoop.Contracts.Abstractions;
using EternalLoop.Contracts.Enums;
using EternalLoop.Contracts.Options;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace EternalLoop.App.ViewModels;

public partial class AnalysisViewModel : ObservableObject, IAnalysisProgressReporter
{
    private readonly AppSessionState _sessionState;
    private readonly IJukeboxAnalysisPipeline _pipeline;
    private readonly IJukeboxEngine _engine;
    private readonly IAudioPlayer _player;
    private readonly INavigationService _navigationService;
    private CancellationTokenSource? _cts;
    private bool _started;

    [ObservableProperty] private string fileName = "No file selected";
    [ObservableProperty] private string currentStage = "Waiting";
    [ObservableProperty] private string currentMessage = "Preparing analysis...";
    [ObservableProperty] private double overallProgress;
    [ObservableProperty] private bool isRunning;
    [ObservableProperty] private string? errorMessage;
    [ObservableProperty] private string friendlyProgressText = "Preparing track...";
    [ObservableProperty] private ObservableCollection<string> log = new();

    public AnalysisViewModel(
        AppSessionState sessionState,
        IJukeboxAnalysisPipeline pipeline,
        IJukeboxEngine engine,
        IAudioPlayer player,
        INavigationService navigationService)
    {
        _sessionState = sessionState;
        _pipeline = pipeline;
        _engine = engine;
        _player = player;
        _navigationService = navigationService;

        if (!string.IsNullOrWhiteSpace(_sessionState.SelectedFilePath))
        {
            FileName = Path.GetFileName(_sessionState.SelectedFilePath);
        }
    }

    public async Task StartAsync()
    {
        if (_started)
        {
            return;
        }

        _started = true;
        _cts = new CancellationTokenSource();
        IsRunning = true;
        ErrorMessage = null;
        OverallProgress = 0;
        Log.Clear();

        try
        {
            var filePath = _sessionState.SelectedFilePath;
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new InvalidOperationException("No audio file selected.");
            }

            var forceReanalysis = _sessionState.ForceReanalysis;
            var branchOptions = TuningOptionsMapper.ToBranchFindingOptions(_sessionState.Settings);
            if (forceReanalysis)
            {
                FriendlyProgressText = "Refreshing your loop map...";
                ReportOnUi("Refreshing", "Rebuilding the track analysis");
            }

            var result = await Task.Run(
                async () => await _pipeline.AnalyzeAsync(
                    filePath,
                    this,
                    _cts.Token,
                    forceReanalysis,
                    branchOptions).ConfigureAwait(false),
                _cts.Token).ConfigureAwait(true);

            if (result.LoadedFromCache)
            {
                ReportOnUi("Cached", "Loaded saved analysis");
            }

            Report(AnalysisStage.Done, 1.0, "Preparing player");

            _engine.Load(result.Analysis, result.Graph);
            _engine.UpdateOptions(TuningOptionsMapper.ToJukeboxEngineOptions(_sessionState.Settings));
            await _player.LoadAsync(result.Audio, _engine, _cts.Token).ConfigureAwait(true);

            Report(AnalysisStage.Done, 1.0, "Ready to loop");
            await Task.Delay(300, _cts.Token).ConfigureAwait(true);

            _sessionState.CurrentResult = result;
            _navigationService.NavigateTo<PlayerViewModel>();
        }
        catch (OperationCanceledException)
        {
            ReportOnUi("Canceled", "Analysis canceled by user.");
            _navigationService.NavigateTo<WelcomeViewModel>();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            ReportOnUi("Error", ex.Message);
        }
        finally
        {
            IsRunning = false;
            _sessionState.ForceReanalysis = false;
        }
    }

    public void Report(AnalysisStage stage, double progress01, string? message = null)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            CurrentStage = stage.ToString();
            CurrentMessage = message ?? stage.ToString();
            FriendlyProgressText = stage switch
            {
                _ when message == "Loaded from cache" => "Your saved loop map is ready.",
                _ when message == "Refreshing saved analysis" => "Refreshing your loop map...",
                _ when string.Equals(message, "AI similarity failed. Using classic analysis.", StringComparison.Ordinal) => "AI unavailable. Continuing with classic analysis...",
                _ when string.Equals(message, "AI similarity unavailable. Using classic analysis.", StringComparison.Ordinal) => "AI unavailable. Continuing with classic analysis...",
                _ when string.Equals(message, "AI similarity is off. Using classic analysis.", StringComparison.Ordinal) => "Using classic analysis...",
                AnalysisStage.Loading => "Preparing the track...",
                AnalysisStage.ExtractingFeatures => "Listening for musical patterns...",
                AnalysisStage.TrackingBeats => "Finding the rhythm...",
                AnalysisStage.RunningAi => "Running local AI similarity...",
                AnalysisStage.BuildingGraph => "Drawing loop paths...",
                AnalysisStage.Done => "Almost ready...",
                _ => "Preparing..."
            };
            OverallProgress = CalculateOverallProgress(stage, progress01) * 100.0;
            Log.Add($"[{DateTime.Now:HH:mm:ss}] {CurrentStage}: {CurrentMessage}");
        });
    }

    [RelayCommand]
    private void Cancel()
    {
        _cts?.Cancel();
    }

    [RelayCommand]
    private void Back()
    {
        _cts?.Cancel();
        _navigationService.NavigateTo<WelcomeViewModel>();
    }

    private static double CalculateOverallProgress(AnalysisStage stage, double progress01)
    {
        var stageIndex = stage switch
        {
            AnalysisStage.Loading => 0,
            AnalysisStage.ExtractingFeatures => 1,
            AnalysisStage.TrackingBeats => 2,
            AnalysisStage.RunningAi => 3,
            AnalysisStage.BuildingGraph => 4,
            AnalysisStage.Done => 5,
            _ => 0
        };

        return Math.Clamp((stageIndex + Math.Clamp(progress01, 0, 1)) / 6.0, 0, 1);
    }

    private void ReportOnUi(string stage, string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            CurrentStage = stage;
            CurrentMessage = message;
            Log.Add($"[{DateTime.Now:HH:mm:ss}] {stage}: {message}");
        });
    }
}
