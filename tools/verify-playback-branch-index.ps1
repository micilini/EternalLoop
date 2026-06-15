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

function AssertContains([string] $Path, [string] $Needle) {
    $text = ReadText $Path
    if (!$text.Contains($Needle)) {
        Fail "$Path does not contain required text: $Needle"
    }
}

function AssertNotContains([string] $Path, [string] $Needle) {
    $text = ReadText $Path
    if ($text.Contains($Needle)) {
        Fail "$Path contains blocked text: $Needle"
    }
}

$linearIndex = ".\src\EternalLoop.Playback\Runtime\RuntimeLinearBeatIndex.cs"
$decisionEngine = ".\src\EternalLoop.Playback\Runtime\BranchDecisionEngine.cs"
$escapeGuard = ".\src\EternalLoop.Playback\Runtime\BranchEscapeGuard.cs"
$sampleProvider = ".\src\EternalLoop.Playback\Audio\BeatScheduledSampleProvider.cs"

AssertContains $linearIndex "public sealed class RuntimeLinearBeatIndex"
AssertContains $linearIndex "ReferenceEqualityComparer.Instance"
AssertContains $linearIndex "TryGetOrdinal"

AssertNotContains $decisionEngine "CollectLinearBeats"
AssertNotContains $decisionEngine "ContainsReference"
AssertNotContains $decisionEngine "List<RuntimeBeat>"
AssertNotContains $decisionEngine "HashSet<RuntimeBeat>"
AssertContains $decisionEngine "DecideNextBeat(RuntimeBeat currentBeat, RuntimeLinearBeatIndex linearBeatIndex)"
AssertContains $decisionEngine "_escapeGuard.Evaluate(currentBeat, seedBeat, branch, linearBeatIndex)"
AssertContains $decisionEngine "_escapeGuard.IsInEndZone(seedBeat, linearBeatIndex)"

AssertNotContains $escapeGuard "CollectLinearBeats"
AssertNotContains $escapeGuard "IndexOf("
AssertContains $escapeGuard "RuntimeLinearBeatIndex linearBeatIndex"
AssertContains $escapeGuard "public bool IsInEndZone(RuntimeBeat beat, RuntimeLinearBeatIndex linearBeatIndex)"
AssertContains $escapeGuard "linearBeatIndex.Contains(destination)"
AssertContains $escapeGuard "linearBeatIndex.GetOrdinalOrWhich(beat)"

AssertContains $sampleProvider "private readonly RuntimeLinearBeatIndex _linearBeatIndex;"
AssertContains $sampleProvider "_linearBeatIndex = RuntimeLinearBeatIndex.FromTrack(track);"
AssertContains $sampleProvider "_branchDecisionEngine.DecideNextBeat(currentBeat, _linearBeatIndex)"
AssertContains $sampleProvider "_linearBeatIndex.TryGetOrdinal(beat, out int ordinal)"

dotnet test .\src\EternalLoop.Playback.Tests\EternalLoop.Playback.Tests.csproj -c Release

Write-Host "OK: playback branch index verification passed."
