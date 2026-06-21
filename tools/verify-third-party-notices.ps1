$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$modelDirectory = Join-Path $repoRoot 'modules\AnalysisEngine\assets\models\beat-this'
$modelPath = Join-Path $modelDirectory 'beat-this-large.onnx'
$modelJsonPath = Join-Path $modelDirectory 'model.json'
$noticePath = Join-Path $modelDirectory 'THIRD_PARTY_NOTICES.md'
$licensePath = Join-Path $modelDirectory 'BEAT_THIS_LICENSE.txt'
$verificationPath = Join-Path $modelDirectory 'beat-this-large.verification.json'

function Assert-FileExists([string] $path) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Missing file: $path"
    }
}

function Assert-FileDoesNotExist([string] $path) {
    if (Test-Path -LiteralPath $path) {
        throw "File should not exist in tracked release source location: $path"
    }
}

function Assert-Contains([string] $path, [string] $value) {
    $content = Get-Content -LiteralPath $path -Raw
    if (-not $content.Contains($value)) {
        throw "Expected $path to contain: $value"
    }
}

function Assert-NotContains([string] $path, [string] $value) {
    $content = Get-Content -LiteralPath $path -Raw
    if ($content.Contains($value)) {
        throw "Expected $path not to contain: $value"
    }
}

Assert-FileExists $modelPath
Assert-FileExists $modelJsonPath
Assert-FileExists $noticePath
Assert-FileExists $licensePath

Assert-NotContains $noticePath 'REPLACE_WITH'
Assert-NotContains $noticePath 'Do not ship'
Assert-NotContains $noticePath 'template only'
Assert-NotContains $noticePath 'yes/no'
Assert-Contains $noticePath 'CPJKU/beat_this'
Assert-Contains $noticePath 'MIT'
Assert-Contains $noticePath 'final0'
Assert-Contains $noticePath 'eb2b205f4f49f8393daf7504d5822da34e23b3ec7da703a7700c2aa70a8c6c8b'
Assert-Contains $licensePath 'MIT License'
Assert-Contains $licensePath 'Institute of Computational Perception, JKU Linz, Austria'

$modelJson = Get-Content -LiteralPath $modelJsonPath -Raw | ConvertFrom-Json
if ($modelJson.license -ne 'MIT') {
    throw "Expected model.json license MIT."
}

if ($modelJson.model_file -ne 'beat-this-large.onnx') {
    throw "Expected model_file beat-this-large.onnx."
}

$expectedSha = $modelJson.model_sha256
$actualSha = (Get-FileHash -LiteralPath $modelPath -Algorithm SHA256).Hash.ToLowerInvariant()

if ($actualSha -ne $expectedSha) {
    throw "Model SHA256 mismatch. Expected $expectedSha but found $actualSha."
}

Assert-FileDoesNotExist $verificationPath

Write-Host 'Third-party notice verification passed.'
