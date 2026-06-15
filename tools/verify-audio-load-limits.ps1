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

$limitsPath = ".\src\EternalLoop.Playback\Audio\AudioLoadLimits.cs"
$loaderPath = ".\src\EternalLoop.Playback\Audio\AudioLoader.cs"
$testsPath = ".\src\EternalLoop.Playback.Tests\Audio\AudioLoaderTests.cs"

$limits = ReadText $limitsPath
$loader = ReadText $loaderPath
$tests = ReadText $testsPath

AssertContains $limits "public sealed class AudioLoadLimits" $limitsPath
AssertContains $limits "public static AudioLoadLimits Default" $limitsPath
AssertContains $limits "MaxDuration" $limitsPath
AssertContains $limits "MaxDecodedSamples" $limitsPath
AssertContains $limits "MaxDecodedBytes" $limitsPath

AssertContains $loader "private readonly AudioLoadLimits _limits;" $loaderPath
AssertContains $loader "public AudioLoader(AudioLoadLimits limits)" $loaderPath
AssertContains $loader "ValidateDuration(reader.TotalTime, _limits)" $loaderPath
AssertContains $loader "EstimateOutputSampleCapacity(reader, sampleRate, channels, _limits)" $loaderPath
AssertContains $loader "ValidateDecodedSampleCount(requiredCapacity, _limits)" $loaderPath
AssertContains $loader "EnsureCapacity(ref samples, requiredCapacity, _limits)" $loaderPath
AssertContains $loader "Audio file is too long for loop mode" $loaderPath
AssertContains $loader "Audio file is too large for loop mode" $loaderPath
AssertContains $loader "catch (OutOfMemoryException exception)" $loaderPath
AssertNotContains $loader "List<float> samples" $loaderPath
AssertNotContains $loader "samples.Add(" $loaderPath
AssertNotContains $loader "samples.ToArray()" $loaderPath
AssertContains $loader "Array.Copy(buffer, 0, samples, sampleCount, read)" $loaderPath

AssertContains $tests "LoadAsyncShouldRejectAudioLongerThanConfiguredLimit" $testsPath
AssertContains $tests "LoadAsyncShouldRejectEstimatedDecodedSamplesAboveLimit" $testsPath
AssertContains $tests "LoadAsyncShouldRejectDecodedSamplesAboveLimitWhileReading" $testsPath
AssertContains $tests "LoadAsyncShouldStillLoadValidWaveUnderLimit" $testsPath
dotnet test .\src\EternalLoop.Playback.Tests\EternalLoop.Playback.Tests.csproj -c Release

Write-Host "OK: audio load limit verification passed."
