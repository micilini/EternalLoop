$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

$repositoryRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$modelDirectory = Join-Path $repositoryRoot "EternalLoop.App\Assets\Models\DiscogsEffNet"

$onnxFileName = "discogs_track_embeddings-effnet-bs64-1.onnx"
$metadataFileName = "discogs_track_embeddings-effnet-bs64-1.json"

$onnxMinimumBytes = 10000000
$metadataMinimumBytes = 1000
$htmlProbeBytes = 512
$maximumRedirects = 5

$modelFiles = @(
    [pscustomobject]@{
        Url = "https://essentia.upf.edu/models/feature-extractors/discogs-effnet/discogs_track_embeddings-effnet-bs64-1.onnx"
        FileName = $onnxFileName
        MinimumBytes = $onnxMinimumBytes
        Kind = "onnx"
    },
    [pscustomobject]@{
        Url = "https://essentia.upf.edu/models/feature-extractors/discogs-effnet/discogs_track_embeddings-effnet-bs64-1.json"
        FileName = $metadataFileName
        MinimumBytes = $metadataMinimumBytes
        Kind = "json"
    }
)

function Test-DownloadedFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [long]$MinimumBytes,

        [Parameter(Mandatory = $true)]
        [string]$Kind
    )

    $fileInfo = Get-Item $Path

    if ($fileInfo.Length -lt $MinimumBytes) {
        throw "Downloaded file is too small: $($fileInfo.Name)"
    }

    $bytesToRead = [Math]::Min($htmlProbeBytes, [int]$fileInfo.Length)
    $buffer = New-Object byte[] $bytesToRead
    $stream = [System.IO.File]::OpenRead($Path)

    try {
        [void]$stream.Read($buffer, 0, $bytesToRead)
    }
    finally {
        $stream.Dispose()
    }

    $prefix = [System.Text.Encoding]::UTF8.GetString($buffer).TrimStart()
    $lowerPrefix = $prefix.ToLowerInvariant()

    if ($prefix.StartsWith("<") -or $lowerPrefix.Contains("<html")) {
        throw "Downloaded file looks like HTML instead of model data: $($fileInfo.Name)"
    }

    if ($Kind -eq "json") {
        Get-Content -Raw -Path $Path | ConvertFrom-Json | Out-Null
    }
}

New-Item -ItemType Directory -Force -Path $modelDirectory | Out-Null

foreach ($modelFile in $modelFiles) {
    $targetPath = Join-Path $modelDirectory $modelFile.FileName
    Write-Host "Downloading $($modelFile.FileName)"
    Invoke-WebRequest -Uri $modelFile.Url -OutFile $targetPath -MaximumRedirection $maximumRedirects
    Test-DownloadedFile -Path $targetPath -MinimumBytes $modelFile.MinimumBytes -Kind $modelFile.Kind
}

Write-Host "AI model files downloaded to $modelDirectory"
Write-Host "The app will package these files during publish, but they are intentionally not committed by this phase."
