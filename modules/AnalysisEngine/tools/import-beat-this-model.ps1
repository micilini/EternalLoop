param(
    [Parameter(Mandatory = $true)]
    [string]$SourceModelPath,

    [string]$ModelDirectory = "assets/models/beat-this",

    [string]$ModelFileName = "beat-this-large.onnx",

    [string]$Name = "beat-this-large",

    [string]$Version = "unknown",

    [string]$License = "unknown",

    [int]$SampleRate = 22050,

    [double]$FrameRate = 100.0,

    [string]$InputName = "spectrogram",

    [string[]]$OutputNames = @("beat_logits", "downbeat_logits"),

    [string]$OnnxKind = "spectrogram-to-frame-logits",

    [int]$ChunkFrames = 1500,

    [int]$MelBins = 128,

    [int]$FrameSize = 1024,

    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-AnalysisEngineRoot {
    if ([string]::IsNullOrWhiteSpace($PSScriptRoot)) {
        throw "PSScriptRoot is not available. Run this script from disk."
    }

    $root = Split-Path -Parent $PSScriptRoot
    $solutionPath = Join-Path $root "EternalLoop.AnalysisEngine.slnx"

    if (-not (Test-Path $solutionPath)) {
        throw "Could not find EternalLoop.AnalysisEngine.slnx near tools directory: $solutionPath"
    }

    return $root
}

function Resolve-PathFromRoot {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Root,

        [Parameter(Mandatory = $true)]
        [string]$PathValue
    )

    if ([System.IO.Path]::IsPathRooted($PathValue)) {
        return [System.IO.Path]::GetFullPath($PathValue)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $Root $PathValue))
}

function Get-FileSha256 {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PathValue
    )

    $hash = Get-FileHash -Path $PathValue -Algorithm SHA256
    return $hash.Hash.ToLowerInvariant()
}

if ($SampleRate -le 0) {
    throw "SampleRate must be greater than zero."
}

if ($FrameRate -le 0) {
    throw "FrameRate must be greater than zero."
}

if ($ChunkFrames -le 0) {
    throw "ChunkFrames must be greater than zero."
}

if ($MelBins -le 0) {
    throw "MelBins must be greater than zero."
}

if ($FrameSize -le 0) {
    throw "FrameSize must be greater than zero."
}

$analysisEngineRoot = Resolve-AnalysisEngineRoot
$sourceFullPath = [System.IO.Path]::GetFullPath($SourceModelPath)
$modelDirectoryFullPath = Resolve-PathFromRoot -Root $analysisEngineRoot -PathValue $ModelDirectory
$destinationModelPath = Join-Path $modelDirectoryFullPath $ModelFileName
$metadataPath = Join-Path $modelDirectoryFullPath "model.json"

if (-not (Test-Path $sourceFullPath)) {
    throw "Source ONNX model was not found: $sourceFullPath"
}

if ([System.IO.Path]::GetExtension($sourceFullPath).ToLowerInvariant() -ne ".onnx") {
    throw "Source model must be an .onnx file: $sourceFullPath"
}

if ([string]::IsNullOrWhiteSpace($ModelFileName)) {
    throw "ModelFileName cannot be empty."
}

if ([System.IO.Path]::GetExtension($ModelFileName).ToLowerInvariant() -ne ".onnx") {
    throw "ModelFileName must end with .onnx: $ModelFileName"
}

New-Item -ItemType Directory -Force -Path $modelDirectoryFullPath | Out-Null

if ((Test-Path $destinationModelPath) -and -not $Force) {
    throw "Destination model already exists: $destinationModelPath. Use -Force to overwrite."
}

Copy-Item -Path $sourceFullPath -Destination $destinationModelPath -Force:$Force

$modelSha256 = Get-FileSha256 -PathValue $destinationModelPath

$metadata = [ordered]@{
    name = $Name
    version = $Version
    license = $License
    model_file = $ModelFileName
    model_sha256 = $modelSha256
    sample_rate = $SampleRate
    frame_rate = $FrameRate
    input_name = $InputName
    output_names = $OutputNames
    onnx_kind = $OnnxKind
    chunk_frames = $ChunkFrames
    mel_bins = $MelBins
    frame_size = $FrameSize
}

$metadataJson = $metadata | ConvertTo-Json -Depth 8
Set-Content -Path $metadataPath -Value $metadataJson -Encoding UTF8

Write-Host "Beat This model imported locally."
Write-Host "AnalysisEngine root: $analysisEngineRoot"
Write-Host "Model path: $destinationModelPath"
Write-Host "Metadata path: $metadataPath"
Write-Host "SHA256: $modelSha256"
Write-Host ""
Write-Host "Reminder: model payloads are ignored by git in this phase."
Write-Host "Run this to confirm:"
Write-Host "  git status --ignored --short modules/AnalysisEngine/assets/models/beat-this"