$ErrorActionPreference = "Stop"

function Fail([string] $Message) {
    Write-Error $Message
    exit 1
}

function ReadText([string] $Path) {
    if (!(Test-Path $Path)) {
        Fail "Missing required file: $Path"
    }

    Get-Content -LiteralPath $Path -Raw
}

function AssertContains([string] $Text, [string] $Needle, [string] $Context) {
    if (!$Text.Contains($Needle)) {
        Fail "$Context does not contain required text: $Needle"
    }
}

function AssertNotContains([string] $Text, [string] $Needle, [string] $Context) {
    if ($Text.Contains($Needle)) {
        Fail "$Context contains blocked text: $Needle"
    }
}

$loaderPath = ".\src\EternalLoop.Playback\Audio\AudioLoader.cs"
$testsPath = ".\src\EternalLoop.Playback.Tests\Audio\AudioLoaderTests.cs"

$loader = ReadText $loaderPath
$tests = ReadText $testsPath

AssertNotContains $loader "List<float> samples" $loaderPath
AssertNotContains $loader "samples.Add(" $loaderPath
AssertNotContains $loader "samples.ToArray()" $loaderPath
AssertContains $loader "reader.Read(buffer, 0, buffer.Length)" $loaderPath
AssertContains $loader "Array.Copy(buffer, 0, samples, sampleCount, read)" $loaderPath
AssertContains $loader "EnsureCapacity" $loaderPath
AssertContains $loader "EstimateOutputSampleCapacity" $loaderPath
AssertContains $loader "TotalSampleFrames = totalSampleFrames" $loaderPath
AssertContains $loader "Samples = samples" $loaderPath

AssertContains $tests "AudioLoaderTests" $testsPath
AssertContains $tests "LoadAsyncShouldLoadWaveFileWithExpectedFormat" $testsPath
AssertContains $tests "LoadAsyncShouldPreserveSampleCountAndDuration" $testsPath
AssertContains $tests "LoadAsyncShouldRejectEmptyPath" $testsPath
AssertContains $tests "LoadAsyncShouldRejectUnsupportedExtension" $testsPath
AssertContains $tests "LoadAsyncShouldRejectMissingFile" $testsPath
AssertContains $tests "LoadAsyncShouldHonorCancellation" $testsPath
dotnet test .\src\EternalLoop.Playback.Tests\EternalLoop.Playback.Tests.csproj -c Release

Write-Host "OK: audio loader memory verification passed."
