param(
    [Parameter(Mandatory = $true)]
    [string] $ExporterOutputDir,

    [Parameter(Mandatory = $true)]
    [string] $LoopLabRoot,

    [Parameter(Mandatory = $true)]
    [string] $TrackId,

    [switch] $CopyRaw
)

$analysisDir = Join-Path $LoopLabRoot "analysis"
$referenceDir = Join-Path $LoopLabRoot "reference"

New-Item -ItemType Directory -Force -Path $analysisDir | Out-Null
New-Item -ItemType Directory -Force -Path $referenceDir | Out-Null

$eternalLoopAnalysis = Join-Path $ExporterOutputDir "eternalloop-analysis.json"
$summary = Join-Path $ExporterOutputDir "summary.json"

if (Test-Path $eternalLoopAnalysis) {
    Copy-Item -Path $eternalLoopAnalysis -Destination (Join-Path $analysisDir "$TrackId.json") -Force
}

if ($CopyRaw -and (Test-Path $eternalLoopAnalysis)) {
    Copy-Item -Path $eternalLoopAnalysis -Destination (Join-Path $referenceDir "$TrackId-eternalloop-analysis.json") -Force
}

if ($CopyRaw -and (Test-Path $summary)) {
    Copy-Item -Path $summary -Destination (Join-Path $referenceDir "$TrackId-summary.json") -Force
}

Write-Host "loop-map.html?id=$TrackId"
Write-Host "loop-side-by-side.html?id=$TrackId"
