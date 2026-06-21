param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [string]$TargetFramework = "net8.0",

    [string]$SourceRoot = ".\assets\models\beat-this",

    [string]$FallbackSourceRoot = ".\tools\beat-this-conversion\output",

    [string]$CliProjectRoot = ".\src\EternalLoop.AnalysisEngine.Cli"
)

$ErrorActionPreference = "Stop"

function Test-BeatThisModelSource {
    param([string]$Root)

    if (!(Test-Path $Root)) {
        return $false
    }

    $onnx = Join-Path $Root "beat-this-large.onnx"
    $modelJson = Join-Path $Root "model.json"

    return (Test-Path $onnx) -and (Test-Path $modelJson)
}

if (!(Test-BeatThisModelSource $SourceRoot)) {
    Write-Warning "Primary Beat This model source is missing or incomplete: $SourceRoot"

    if (Test-BeatThisModelSource $FallbackSourceRoot) {
        Write-Host "Using fallback source: $FallbackSourceRoot"
        $SourceRoot = $FallbackSourceRoot
    }
    else {
        throw "No valid Beat This model source found. Checked: $SourceRoot and $FallbackSourceRoot"
    }
}

$destinationRoot = Join-Path $CliProjectRoot "bin\$Configuration\$TargetFramework\assets\models\beat-this"

New-Item -ItemType Directory -Force $destinationRoot | Out-Null

Copy-Item (Join-Path $SourceRoot "beat-this-large.onnx") (Join-Path $destinationRoot "beat-this-large.onnx") -Force
Copy-Item (Join-Path $SourceRoot "model.json") (Join-Path $destinationRoot "model.json") -Force

$verification = Join-Path $SourceRoot "beat-this-large.verification.json"
if (Test-Path $verification) {
    Copy-Item $verification (Join-Path $destinationRoot "beat-this-large.verification.json") -Force
}

$runtimeOnnx = Join-Path $destinationRoot "beat-this-large.onnx"
$runtimeModelJson = Join-Path $destinationRoot "model.json"

if (!(Test-Path $runtimeOnnx)) {
    throw "Runtime ONNX was not copied: $runtimeOnnx"
}

if (!(Test-Path $runtimeModelJson)) {
    throw "Runtime model.json was not copied: $runtimeModelJson"
}

Write-Host "Beat This runtime model hydrated:"
Write-Host "  Source:      $SourceRoot"
Write-Host "  Destination: $destinationRoot"
Write-Host "  ONNX:        $((Get-Item $runtimeOnnx).Length) bytes"
Write-Host "  model.json:  $((Get-Item $runtimeModelJson).Length) bytes"
