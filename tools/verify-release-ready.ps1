$ErrorActionPreference = "Stop"

function Fail([string] $Message) {
    Write-Error $Message
    exit 1
}

function Invoke-Step([scriptblock] $Command, [string] $Name) {
    Write-Host $Name
    & $Command
    if ($LASTEXITCODE -ne 0) {
        Fail "$Name failed with exit code $LASTEXITCODE."
    }
}

function AssertPathExists([string] $Path) {
    if (!(Test-Path -LiteralPath $Path)) {
        Fail "Missing required path: $Path"
    }
}

function ReadText([string] $Path) {
    AssertPathExists $Path
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

$verifiers = @(
    ".\tools\verify-playback-branch-index.ps1",
    ".\tools\verify-playback-read-events.ps1",
    ".\tools\verify-playback-graph-isolation.ps1",
    ".\tools\verify-audio-loader-memory.ps1",
    ".\tools\verify-audio-load-limits.ps1",
    ".\tools\verify-audio-format-support.ps1",
    ".\tools\verify-publish-package.ps1",
    ".\tools\verify-repository-hygiene.ps1",
    ".\tools\verify-ui-exception-policy.ps1",
    ".\tools\verify-loop-map-render-cache.ps1",
    ".\tools\verify-mini-spectrum-render-cache.ps1",
    ".\tools\verify-settings-save-concurrency.ps1",
    ".\tools\verify-command-error-handling.ps1",
    ".\tools\verify-app-regression-coverage.ps1",
    ".\tools\verify-release-memory.ps1",
    ".\tools\verify-release-clean-code.ps1"
)

foreach ($verifier in $verifiers) {
    AssertPathExists $verifier
    Invoke-Step { powershell -ExecutionPolicy Bypass -File $verifier } "Running $verifier"
}

Invoke-Step { dotnet restore .\EternalLoop.slnx } "Restoring solution"
Invoke-Step { dotnet build .\EternalLoop.slnx -c Release } "Building solution"
Invoke-Step { dotnet test .\EternalLoop.slnx -c Release } "Testing solution"
Invoke-Step { dotnet test .\modules\AnalysisEngine\EternalLoop.AnalysisEngine.slnx -c Release } "Testing analysis engine"
Invoke-Step { dotnet test .\modules\BranchAnalysis\EternalLoop.BranchAnalysis.slnx -c Release } "Testing branch analysis"
Invoke-Step { powershell -ExecutionPolicy Bypass -File .\tools\publish-release-win-x64.ps1 } "Publishing release package"

AssertPathExists ".\EternalLoop.slnLaunch"
AssertContains ".\EternalLoop.slnLaunch" '"Name": "EternalLoop.App"'
AssertContains ".\EternalLoop.slnLaunch" '"Path": "src\\EternalLoop.App\\EternalLoop.App.csproj"'
AssertContains ".\EternalLoop.slnLaunch" '"Action": "Start"'

$solution = ReadText ".\EternalLoop.slnx"
$firstProject = [regex]::Match($solution, '<Project Path="([^"]+)"')
if (!$firstProject.Success -or $firstProject.Groups[1].Value -ne "src/EternalLoop.App/EternalLoop.App.csproj") {
    Fail "EternalLoop.App is not the first project in EternalLoop.slnx."
}

AssertContains ".\src\EternalLoop.App\Views\PlayerView.xaml" "controls:LoopMapVisualization"
AssertContains ".\src\EternalLoop.App\Views\PlayerView.xaml" 'Graph="{Binding Graph}"'
AssertContains ".\src\EternalLoop.App\Views\PlayerView.xaml" 'CurrentBeatIndex="{Binding CurrentBeatIndex}"'
AssertContains ".\src\EternalLoop.App\Views\PlayerView.xaml" 'LastJumpFromBeat="{Binding LastJumpFromBeat}"'
AssertContains ".\src\EternalLoop.App\Views\PlayerView.xaml" 'LastJumpToBeat="{Binding LastJumpToBeat}"'
AssertContains ".\src\EternalLoop.App\Views\PlayerView.xaml" 'Margin="28"'
AssertPathExists ".\src\EternalLoop.App\Views\WelcomeView.xaml"
AssertPathExists ".\src\EternalLoop.App\Views\SettingsView.xaml"
AssertPathExists ".\src\EternalLoop.App\Views\SplashScreenWindow.xaml"

AssertNotContains ".\src\EternalLoop.App\EternalLoop.App.csproj" "AnalysisEngine"
AssertNotContains ".\src\EternalLoop.App\EternalLoop.App.csproj" "BranchAnalysis"
AssertContains ".\src\EternalLoop.App\EternalLoop.App.csproj" "EternalLoop.Playback.csproj"

$playbackProject = ReadText ".\src\EternalLoop.Playback\EternalLoop.Playback.csproj"
if ($playbackProject.Contains("EternalLoop.App") -or $playbackProject.Contains("EternalLoop.Core") -or $playbackProject.Contains("modules\")) {
    Fail "Playback project has a forbidden project dependency."
}

AssertContains ".\src\EternalLoop.Core\EternalLoop.Core.csproj" "EternalLoop.Playback.csproj"

$blockedModelTerms = @("ONNX", "DiscogsEffNet", "Assets\Models")
foreach ($term in $blockedModelTerms) {
    $matches = Get-ChildItem -Path .\src, .\modules -Recurse -File | Select-String -Pattern ([regex]::Escape($term)) -SimpleMatch
    if ($matches) {
        Fail "Blocked model dependency found: $term"
    }
}

$legacyVerifierPattern = 'verify-' + 'h' + '*.ps1'
$legacyVerifierFiles = Get-ChildItem -Path .\tools -File -Filter $legacyVerifierPattern
if ($legacyVerifierFiles) {
    Fail "Legacy internal verifier name(s) found: $($legacyVerifierFiles.Name -join ', ')"
}

$legacyReleaseRunner = ".\tools\verify-" + "hard" + "ening-release-ready.ps1"
if (Test-Path -LiteralPath $legacyReleaseRunner) {
    Fail "Legacy release runner should not exist."
}

Write-Host "OK: release ready verification passed."
