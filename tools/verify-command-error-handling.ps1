$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$providerPath = Join-Path $repoRoot 'src\EternalLoop.Playback\Audio\BeatScheduledSampleProvider.cs'
$playerViewModelPath = Join-Path $repoRoot 'src\EternalLoop.App\ViewModels\PlayerViewModel.cs'
$mainWindowViewModelPath = Join-Path $repoRoot 'src\EternalLoop.App\ViewModels\MainWindowViewModel.cs'
$playbackTestsPath = Join-Path $repoRoot 'src\EternalLoop.Playback.Tests\Audio\BeatScheduledSampleProviderTests.cs'
$playerTestsPath = Join-Path $repoRoot 'src\EternalLoop.App.Tests\ViewModels\PlayerViewModelCommandErrorTests.cs'
$analysisCompletionTestsPath = Join-Path $repoRoot 'src\EternalLoop.App.Tests\ViewModels\MainWindowViewModelAnalysisCompletionTests.cs'

function Assert-FileExists([string] $path) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Missing file: $path"
    }
}

function Read-Text([string] $path) {
    return Get-Content -LiteralPath $path -Raw
}

function Assert-Contains([string] $content, [string] $value, [string] $path) {
    if (-not $content.Contains($value)) {
        throw "Expected $path to contain: $value"
    }
}

function Assert-NotContains([string] $content, [string] $value, [string] $path) {
    if ($content.Contains($value)) {
        throw "Expected $path not to contain: $value"
    }
}

Assert-FileExists $providerPath
Assert-FileExists $playerViewModelPath
Assert-FileExists $mainWindowViewModelPath
Assert-FileExists $playbackTestsPath
Assert-FileExists $playerTestsPath
Assert-FileExists $analysisCompletionTestsPath

$provider = Read-Text $providerPath
$playerViewModel = Read-Text $playerViewModelPath
$mainWindowViewModel = Read-Text $mainWindowViewModelPath
$playbackTests = Read-Text $playbackTestsPath
$playerTests = Read-Text $playerTestsPath
$analysisCompletionTests = Read-Text $analysisCompletionTestsPath

Assert-NotContains $provider 'changedEvent = SetBeat(targetBeatIndex)' $providerPath
Assert-Contains $provider 'SetBeat(targetBeatIndex);' $providerPath
Assert-Contains $playbackTests 'SeekShouldRaiseBeatChangedOnceForTargetBeat' $playbackTestsPath

Assert-Contains $playerViewModel 'IAppLogger' $playerViewModelPath
Assert-Contains $playerViewModel '_logger = logger ?? NullAppLogger.Instance;' $playerViewModelPath
Assert-Contains $playerViewModel 'PlayPauseCommand = new AsyncRelayCommand(PlayPauseAsync, onError: HandleCommandError);' $playerViewModelPath
Assert-Contains $playerViewModel 'private void HandleCommandError(Exception exception)' $playerViewModelPath
Assert-Contains $playerViewModel 'OperationCanceledException' $playerViewModelPath
Assert-Contains $playerViewModel '_logger.Log(AppLogLevel.Error, "Playback command failed.", exception);' $playerViewModelPath
Assert-Contains $playerViewModel 'AnalyzeAgainStatusText = "Playback action failed. Try stopping and starting again.";' $playerViewModelPath

Assert-Contains $mainWindowViewModel 'internal async Task CompleteAnalysisAsync(TrackWorkflowResult result)' $mainWindowViewModelPath
Assert-Contains $mainWindowViewModel 'try' $mainWindowViewModelPath
Assert-Contains $mainWindowViewModel 'catch (Exception exception)' $mainWindowViewModelPath
Assert-Contains $mainWindowViewModel '_logger.Log(AppLogLevel.Error, "Analysis completion failed.", exception);' $mainWindowViewModelPath
Assert-Contains $mainWindowViewModel 'NavigateHome();' $mainWindowViewModelPath
Assert-Contains $mainWindowViewModel 'await RegisterRecentTrackAsync(result).ConfigureAwait(true);' $mainWindowViewModelPath
Assert-Contains $mainWindowViewModel 'if (_disposed)' $mainWindowViewModelPath
Assert-Contains $mainWindowViewModel '_logger);' $mainWindowViewModelPath

Assert-Contains $playerTests 'PlayPauseCommandShouldReportUnexpectedCommandFailure' $playerTestsPath
Assert-Contains $playerTests 'Playback command failed.' $playerTestsPath
Assert-Contains $analysisCompletionTests 'AnalysisCompletionShouldLogAndReturnHomeWhenPlayerCreationFails' $analysisCompletionTestsPath
Assert-Contains $analysisCompletionTests 'Analysis completion failed.' $analysisCompletionTestsPath

Push-Location $repoRoot
try {
    dotnet test .\src\EternalLoop.Playback.Tests\EternalLoop.Playback.Tests.csproj -c Release
    dotnet test .\src\EternalLoop.App.Tests\EternalLoop.App.Tests.csproj -c Release
}
finally {
    Pop-Location
}

Write-Host 'OK: command error handling verification passed.'
