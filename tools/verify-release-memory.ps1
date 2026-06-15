$ErrorActionPreference = "Stop"

function Fail([string] $Message) {
    Write-Error $Message
    exit 1
}

function ReadText([string] $Path) {
    if (!(Test-Path $Path)) {
        Fail "Missing required file: $Path"
    }

    Get-Content -LiteralPath $Path -Raw
}

function AssertContains([string] $Path, [string] $Needle) {
    $text = ReadText $Path
    if (!$text.Contains($Needle)) {
        Fail "$Path does not contain required text: $Needle"
    }
}

function AssertNotContains([string] $Path, [string] $Needle) {
    $text = ReadText $Path
    if ($text.Contains($Needle)) {
        Fail "$Path contains blocked text: $Needle"
    }
}

$playerViewModel = ".\src\EternalLoop.App\ViewModels\PlayerViewModel.cs"
$mainWindowViewModel = ".\src\EternalLoop.App\ViewModels\MainWindowViewModel.cs"
$analysisViewModel = ".\src\EternalLoop.App\ViewModels\AnalysisViewModel.cs"
$miniSpectrum = ".\src\EternalLoop.App\Controls\MiniSpectrumControl.cs"
$loopMap = ".\src\EternalLoop.App\Controls\LoopMapVisualization.cs"
$audioPlayer = ".\src\EternalLoop.Playback\Audio\LoopingAudioPlayer.cs"

AssertContains $playerViewModel "public sealed class PlayerViewModel : INotifyPropertyChanged, IDisposable"
AssertContains $playerViewModel "_positionTimer.Stop();"
AssertContains $playerViewModel "_positionTimer.Tick -= OnPositionTimerTick;"
AssertContains $playerViewModel "_player.BeatChanged -= OnBeatChanged;"
AssertContains $playerViewModel "_player.BranchJumped -= OnBranchJumped;"
AssertContains $playerViewModel "_player.StateChanged -= OnStateChanged;"
AssertContains $playerViewModel "TryStopAndDisposePlayer();"
AssertContains $playerViewModel "TrackArtwork = null;"
AssertContains $playerViewModel "Graph = BranchGraph.Empty;"

AssertContains $mainWindowViewModel "object? previousViewModel = _currentViewModel;"
AssertContains $mainWindowViewModel "CurrentViewModel = null;"
AssertContains $mainWindowViewModel "disposable.Dispose();"

AssertContains $analysisViewModel "public sealed class AnalysisViewModel : INotifyPropertyChanged, IDisposable"
AssertContains $analysisViewModel "_cancellationTokenSource?.Cancel();"
AssertContains $analysisViewModel "_elapsedClockCts?.Cancel();"
AssertContains $analysisViewModel "_elapsedClockCts?.Dispose();"

AssertContains $miniSpectrum "Unloaded += OnUnloaded;"
AssertContains $miniSpectrum "_timer.Stop();"
AssertContains $miniSpectrum "_timer.Tick -= OnTimerTick;"

AssertContains $audioPlayer "_sampleProvider.BeatChanged -= OnSampleProviderBeatChanged;"
AssertContains $audioPlayer "_sampleProvider.BranchJumped -= OnSampleProviderBranchJumped;"
AssertContains $audioPlayer "StateChanged = null;"

if (!(Test-Path $loopMap)) {
    Fail "Loop map visualization file is missing."
}

$oldVisualName = "Ju" + "kebox"
AssertNotContains $loopMap $oldVisualName

$testFiles = @(
    ".\src\EternalLoop.App.Tests\ViewModels\MainWindowViewModelDisposalTests.cs",
    ".\src\EternalLoop.App.Tests\ViewModels\PlayerViewModelDisposalTests.cs",
    ".\src\EternalLoop.App.Tests\ViewModels\AnalysisViewModelDisposalTests.cs",
    ".\src\EternalLoop.App.Tests\Controls\MiniSpectrumControlDisposalTests.cs",
    ".\src\EternalLoop.Playback.Tests\Audio\LoopingAudioPlayerDisposalTests.cs"
)

foreach ($testFile in $testFiles) {
    if (!(Test-Path $testFile)) {
        Fail "Missing disposal test file: $testFile"
    }
}

Write-Host "OK: release memory verification passed."
