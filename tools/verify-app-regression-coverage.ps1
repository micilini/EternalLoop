$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$appTestsProject = Join-Path $repoRoot 'src\EternalLoop.App.Tests\EternalLoop.App.Tests.csproj'
$supportedFormatsPath = Join-Path $repoRoot 'src\EternalLoop.Playback\Audio\SupportedAudioFormats.cs'
$publishProfilePath = Join-Path $repoRoot 'src\EternalLoop.App\Properties\PublishProfiles\win-x64-self-contained.pubxml'

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

function Invoke-CheckedCommand([scriptblock] $command, [string] $description) {
    Write-Host $description
    & $command
    if ($LASTEXITCODE -ne 0) {
        throw "$description failed with exit code $LASTEXITCODE."
    }
}

function Invoke-DotNetTestAndGetCount([string] $projectPath) {
    $output = dotnet test $projectPath -c Release 2>&1
    $exitCode = $LASTEXITCODE
    $output | ForEach-Object { Write-Host $_ }

    if ($exitCode -ne 0) {
        throw "dotnet test failed for $projectPath with exit code $exitCode."
    }

    $text = $output -join "`n"
    $matches = [regex]::Matches($text, 'Total:\s+(\d+)')
    if ($matches.Count -eq 0) {
        throw "Could not read total test count from dotnet test output for $projectPath."
    }

    return [int] $matches[$matches.Count - 1].Groups[1].Value
}

$requiredFiles = @(
    'src\EternalLoop.App.Tests\ViewModels\PlayerViewModelPlaybackTests.cs',
    'src\EternalLoop.App.Tests\ViewModels\PlayerViewModelSeekTests.cs',
    'src\EternalLoop.App.Tests\ViewModels\PlayerViewModelEventTests.cs',
    'src\EternalLoop.App.Tests\ViewModels\AnalysisViewModelFlowTests.cs',
    'src\EternalLoop.App.Tests\ViewModels\MainWindowViewModelNavigationTests.cs',
    'src\EternalLoop.App.Tests\ViewModels\WelcomeViewModelTests.cs',
    'src\EternalLoop.App.Tests\ViewModels\RecentTracksViewModelTests.cs',
    'src\EternalLoop.App.Tests\ViewModels\SettingsViewModelTests.cs',
    'src\EternalLoop.App.Tests\TestDoubles\AppTestDoubles.cs'
)

Assert-FileExists $appTestsProject
Assert-FileExists $supportedFormatsPath
Assert-FileExists $publishProfilePath

foreach ($relativePath in $requiredFiles) {
    Assert-FileExists (Join-Path $repoRoot $relativePath)
}

$legacyVerifierPattern = 'verify-' + 'h' + '*.ps1'
$legacyVerifierFiles = Get-ChildItem -Path (Join-Path $repoRoot 'tools') -File -Filter $legacyVerifierPattern
if ($legacyVerifierFiles) {
    throw "Legacy internal verifier name(s) found: $($legacyVerifierFiles.Name -join ', ')"
}

$supportedFormats = Read-Text $supportedFormatsPath
Assert-Contains $supportedFormats '".mp3"' $supportedFormatsPath
Assert-Contains $supportedFormats '".wav"' $supportedFormatsPath
Assert-Contains $supportedFormats '".m4a"' $supportedFormatsPath
Assert-Contains $supportedFormats '".aac"' $supportedFormatsPath
Assert-NotContains $supportedFormats '".ogg"' $supportedFormatsPath
Assert-NotContains $supportedFormats '".flac"' $supportedFormatsPath

$publishProfile = Read-Text $publishProfilePath
Assert-Contains $publishProfile '<PublishTrimmed>false</PublishTrimmed>' $publishProfilePath
Assert-Contains $publishProfile '<SelfContained>true</SelfContained>' $publishProfilePath
Assert-Contains $publishProfile '<PublishSingleFile>true</PublishSingleFile>' $publishProfilePath

Push-Location $repoRoot
try {
    $changedFiles = git diff --name-only
    $forbiddenVisualFiles = @(
        'src/EternalLoop.App/Views/PlayerView.xaml',
        'src/EternalLoop.App/Views/WelcomeView.xaml',
        'src/EternalLoop.App/Views/SettingsView.xaml',
        'src/EternalLoop.App/SplashScreenWindow.xaml'
    )

    foreach ($visualFile in $forbiddenVisualFiles) {
        if ($changedFiles -contains $visualFile) {
            throw "App regression coverage must not change visual XAML: $visualFile"
        }
    }

    $appTestCount = Invoke-DotNetTestAndGetCount '.\src\EternalLoop.App.Tests\EternalLoop.App.Tests.csproj'
    if ($appTestCount -lt 60) {
        throw "EternalLoop.App.Tests must contain at least 60 tests. Observed: $appTestCount."
    }

    Invoke-CheckedCommand { dotnet test .\EternalLoop.slnx -c Release } 'Running solution tests'
}
finally {
    Pop-Location
}

Write-Host "OK: app regression coverage verification passed. App.Tests total: $appTestCount."
