param(
    [string]$SourceRoot = ".\tools\beat-this-conversion\output",
    [string]$DestinationRoot = ".\assets\models\beat-this"
)

$ErrorActionPreference = "Stop"

$onnx = Join-Path $SourceRoot "beat-this-large.onnx"
$modelJson = Join-Path $SourceRoot "model.json"

if (!(Test-Path $onnx)) {
    throw "Missing ONNX model: $onnx"
}

if (!(Test-Path $modelJson)) {
    throw "Missing model.json: $modelJson"
}

New-Item -ItemType Directory -Force $DestinationRoot | Out-Null

Copy-Item $onnx (Join-Path $DestinationRoot "beat-this-large.onnx") -Force
Copy-Item $modelJson (Join-Path $DestinationRoot "model.json") -Force

$verification = Join-Path $SourceRoot "beat-this-large.verification.json"
if (Test-Path $verification) {
    Copy-Item $verification (Join-Path $DestinationRoot "beat-this-large.verification.json") -Force
}

Write-Host "Beat This model assets synchronized:"
Write-Host "  Source:      $SourceRoot"
Write-Host "  Destination: $DestinationRoot"
Write-Host "  ONNX:        $((Get-Item (Join-Path $DestinationRoot 'beat-this-large.onnx')).Length) bytes"
Write-Host "  model.json:  $((Get-Item (Join-Path $DestinationRoot 'model.json')).Length) bytes"
