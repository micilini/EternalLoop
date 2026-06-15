$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$controlPath = Join-Path $repoRoot 'src\EternalLoop.App\Controls\MiniSpectrumControl.cs'
$testsPath = Join-Path $repoRoot 'src\EternalLoop.App.Tests\Controls\MiniSpectrumControlTests.cs'

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

function Get-Section([string] $content, [string] $startMarker, [string] $endMarker, [string] $path) {
    $start = $content.IndexOf($startMarker, [StringComparison]::Ordinal)
    $end = $content.IndexOf($endMarker, [StringComparison]::Ordinal)

    if ($start -lt 0 -or $end -le $start) {
        throw "Could not extract section in $path from '$startMarker' to '$endMarker'."
    }

    return $content.Substring($start, $end - $start)
}

Assert-FileExists $controlPath
Assert-FileExists $testsPath

$control = Read-Text $controlPath
$tests = Read-Text $testsPath
$onRender = Get-Section $control 'protected override void OnRender' 'private void OnLoaded' $controlPath
$onUnloaded = Get-Section $control 'private void OnUnloaded' 'private void OnTimerTick' $controlPath

Assert-Contains $control 'private static readonly Brush PlayingBarBrush = CreateBarBrush(0.92);' $controlPath
Assert-Contains $control 'private static readonly Brush IdleBarBrush = CreateBarBrush(0.42);' $controlPath
Assert-Contains $control 'private static LinearGradientBrush CreateBarBrush(double opacity)' $controlPath
Assert-Contains $control 'StartPoint = new Point(0, 1)' $controlPath
Assert-Contains $control 'EndPoint = new Point(0, 0)' $controlPath
Assert-Contains $control 'Opacity = opacity' $controlPath
Assert-Contains $control 'new GradientStop(Color.FromRgb(124, 231, 255), 0)' $controlPath
Assert-Contains $control 'new GradientStop(Color.FromRgb(156, 108, 255), 0.55)' $controlPath
Assert-Contains $control 'new GradientStop(Color.FromRgb(255, 119, 200), 1)' $controlPath
Assert-Contains $control 'brush.Freeze();' $controlPath

Assert-Contains $onRender 'const int bars = 24;' $controlPath
Assert-Contains $onRender 'const double gap = 3;' $controlPath
Assert-Contains $onRender 'Brush barBrush = IsPlaying ? PlayingBarBrush : IdleBarBrush;' $controlPath
Assert-Contains $onRender 'drawingContext.DrawRoundedRectangle(barBrush, null' $controlPath
Assert-NotContains $onRender 'new LinearGradientBrush' $controlPath
Assert-NotContains $onRender 'new GradientStop' $controlPath
Assert-NotContains $onRender '.Freeze()' $controlPath

Assert-Contains $onUnloaded '_timer.Stop();' $controlPath
Assert-Contains $onUnloaded '_timer.Tick -= OnTimerTick;' $controlPath

Assert-Contains $tests 'MiniSpectrumControlShouldExposeFrozenReusableBrushes' $testsPath
Assert-Contains $tests 'MiniSpectrumControlShouldNotCreateGradientBrushInsideOnRender' $testsPath
Assert-Contains $tests 'MiniSpectrumControlShouldStopTimerOnUnloaded' $testsPath
Assert-Contains $tests 'MiniSpectrumControlShouldPreservePlayingAndIdleOpacity' $testsPath

Push-Location $repoRoot
try {
    dotnet test .\src\EternalLoop.App.Tests\EternalLoop.App.Tests.csproj -c Release
}
finally {
    Pop-Location
}

Write-Host 'OK: mini spectrum render cache verification passed.'
