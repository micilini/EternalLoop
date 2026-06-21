param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [string]$TargetFramework = "net8.0",

    [string]$CliProjectRoot = ".\src\EternalLoop.AnalysisEngine.Cli"
)

$ErrorActionPreference = "Stop"

$modelRoot = Join-Path $CliProjectRoot "bin\$Configuration\$TargetFramework\assets\models\beat-this"
$onnx = Join-Path $modelRoot "beat-this-large.onnx"
$modelJson = Join-Path $modelRoot "model.json"

if (!(Test-Path $modelRoot)) {
    throw "Beat This runtime model directory missing: $modelRoot"
}

if (!(Test-Path $onnx)) {
    throw "Beat This runtime ONNX missing: $onnx"
}

if (!(Test-Path $modelJson)) {
    throw "Beat This runtime model.json missing: $modelJson"
}

$onnxLength = (Get-Item $onnx).Length
$modelJsonLength = (Get-Item $modelJson).Length

if ($onnxLength -le 1024) {
    throw "Beat This runtime ONNX looks too small: $onnxLength bytes"
}

if ($modelJsonLength -le 10) {
    throw "Beat This runtime model.json looks too small: $modelJsonLength bytes"
}

Write-Host "Beat This runtime model is present:"
Write-Host "  $modelRoot"
Write-Host "  ONNX bytes: $onnxLength"
Write-Host "  model.json bytes: $modelJsonLength"
