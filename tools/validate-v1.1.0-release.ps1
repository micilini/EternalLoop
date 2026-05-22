$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$solution = Join-Path $root "EternalLoop.slnx"
$appProject = Join-Path $root "EternalLoop.App\EternalLoop.App.csproj"
$modelDir = Join-Path $root "EternalLoop.App\Assets\Models\DiscogsEffNet"
$publishDir = Join-Path $root "publish\EternalLoop-win-x64"
$minimumOnnxBytes = 10000000
$minimumMetadataBytes = 1000
$expectedInputName = "melspectrogram"
$expectedOutputName = "embeddings"
$expectedEmbeddingDimensions = 512
$expectedBatchSize = 64
$expectedMelBands = 128
$expectedPatchFrames = 96
$expectedSampleRate = 16000

function Assert-FileExists {
    param([string]$Path, [string]$Message)

    if (-not (Test-Path $Path)) {
        throw $Message
    }
}

function Assert-FileMinimumSize {
    param([string]$Path, [long]$MinimumBytes, [string]$Message)

    Assert-FileExists $Path $Message
    $item = Get-Item $Path
    if ($item.Length -lt $MinimumBytes) {
        throw "$Message Current size: $($item.Length) bytes."
    }
}

Write-Host "Validating EternalLoop V1.1.0 release from $root"

$onnxPath = Join-Path $modelDir "discogs_track_embeddings-effnet-bs64-1.onnx"
$metadataPath = Join-Path $modelDir "discogs_track_embeddings-effnet-bs64-1.json"
$manifestPath = Join-Path $modelDir "model-manifest.json"
$noticePath = Join-Path $modelDir "MODEL-LICENSE-NOTICE.txt"

Assert-FileMinimumSize $onnxPath $minimumOnnxBytes "AI ONNX model file is missing or too small. Run tools/download-ai-models.ps1."
Assert-FileMinimumSize $metadataPath $minimumMetadataBytes "AI metadata file is missing or too small. Run tools/download-ai-models.ps1."
Assert-FileExists $manifestPath "AI model manifest is missing."
Assert-FileExists $noticePath "AI model license notice is missing."

$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
if ($manifest.inputName -ne $expectedInputName) { throw "Unexpected ONNX runtime input name: $($manifest.inputName)" }
if ($manifest.embeddingOutputName -ne $expectedOutputName) { throw "Unexpected ONNX runtime output name: $($manifest.embeddingOutputName)" }
if ([int]$manifest.embeddingDimensions -ne $expectedEmbeddingDimensions) { throw "Unexpected embedding dimensions: $($manifest.embeddingDimensions)" }
if ([int]$manifest.batchSize -ne $expectedBatchSize) { throw "Unexpected ONNX batch size: $($manifest.batchSize)" }
if ([int]$manifest.melBands -ne $expectedMelBands) { throw "Unexpected mel bands: $($manifest.melBands)" }
if ([int]$manifest.patchFrames -ne $expectedPatchFrames) { throw "Unexpected patch frames: $($manifest.patchFrames)" }
if ([int]$manifest.sampleRate -ne $expectedSampleRate) { throw "Unexpected AI sample rate: $($manifest.sampleRate)" }

if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

& dotnet restore $solution
& dotnet build $solution --configuration Debug
& dotnet test $solution --configuration Debug
& dotnet test $solution --configuration Debug --filter "Category=AI"

& dotnet publish $appProject `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishReadyToRun=true `
    -p:EnableCompressionInSingleFile=true `
    -o $publishDir

Assert-FileExists (Join-Path $publishDir "EternalLoop.exe") "Published EternalLoop.exe is missing."

$publishedModelDir = Join-Path $publishDir "Assets\Models\DiscogsEffNet"
Assert-FileMinimumSize (Join-Path $publishedModelDir "discogs_track_embeddings-effnet-bs64-1.onnx") $minimumOnnxBytes "Published ONNX model is missing or too small."
Assert-FileMinimumSize (Join-Path $publishedModelDir "discogs_track_embeddings-effnet-bs64-1.json") $minimumMetadataBytes "Published model metadata is missing or too small."
Assert-FileExists (Join-Path $publishedModelDir "model-manifest.json") "Published model manifest is missing."
Assert-FileExists (Join-Path $publishedModelDir "MODEL-LICENSE-NOTICE.txt") "Published model license notice is missing."

$publishSize = (Get-ChildItem $publishDir -Recurse -File | Measure-Object -Property Length -Sum).Sum
Write-Host "Publish output: $publishDir"
Write-Host "Publish size bytes: $publishSize"
Write-Host "EternalLoop V1.1.0 release validation passed."
