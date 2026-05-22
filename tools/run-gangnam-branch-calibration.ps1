param(
    [string]$AudioPath = ".\audio.mp3",
    [string]$SvgPath = ".\infinite-jukebox-gangnam.svg",
    [string]$OutputDirectory = ".\artifacts\gangnam-calibration",
    [switch]$AllowReferenceMismatch
)

$resolvedAudio = Resolve-Path -LiteralPath $AudioPath -ErrorAction SilentlyContinue
if (-not $resolvedAudio) {
    throw "Audio file not found: $AudioPath"
}

$resolvedSvg = Resolve-Path -LiteralPath $SvgPath -ErrorAction SilentlyContinue
if (-not $resolvedSvg) {
    throw "SVG file not found: $SvgPath"
}

$resolvedOutput = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
New-Item -ItemType Directory -Force -Path $resolvedOutput | Out-Null

$env:ETERNALLOOP_RUN_GANGNAM_CALIBRATION = "1"
$env:ETERNALLOOP_GANGNAM_AUDIO_PATH = $resolvedAudio.Path
$env:ETERNALLOOP_GANGNAM_SVG_PATH = $resolvedSvg.Path
$env:ETERNALLOOP_GANGNAM_OUTPUT_DIR = $resolvedOutput
$env:ETERNALLOOP_GANGNAM_ALLOW_REFERENCE_MISMATCH = if ($AllowReferenceMismatch) { "1" } else { "0" }

dotnet test .\EternalLoop.slnx --configuration Debug --filter "FullyQualifiedName~GangnamStyleReferenceCalibrationTests"

$reportPath = Join-Path $resolvedOutput "comparison-report.md"
"Report: $reportPath"
