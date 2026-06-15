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

$providerPath = ".\src\EternalLoop.Playback\Audio\BeatScheduledSampleProvider.cs"
$provider = ReadText $providerPath

$readStart = $provider.IndexOf("public int Read", [StringComparison]::Ordinal)
$resetStart = $provider.IndexOf("public void Reset", [StringComparison]::Ordinal)
if ($readStart -lt 0 -or $resetStart -le $readStart) {
    Fail "Could not isolate BeatScheduledSampleProvider.Read()."
}

$readBody = $provider.Substring($readStart, $resetStart - $readStart)

AssertNotContains $readBody "List<BeatChangedEventArgs> changedEvents = []" "Read()"
AssertNotContains $readBody "List<BranchJumpEventArgs> branchEvents = []" "Read()"
AssertNotContains $readBody "new List<BeatChangedEventArgs>" "Read()"
AssertNotContains $readBody "new List<BranchJumpEventArgs>" "Read()"
AssertNotContains $readBody "BeatChanged?.Invoke" "Read()"
AssertNotContains $readBody "BranchJumped?.Invoke" "Read()"

AssertContains $provider "private struct PendingPlaybackEvents" $providerPath
AssertContains $provider "pendingEvents.Dispatch(this);" "Read()"
AssertContains $provider "_branchDecisionEngine.DecideNextBeat(currentBeat, _linearBeatIndex)" "MoveToNextBeat"
AssertContains $provider "private readonly RuntimeLinearBeatIndex _linearBeatIndex;" $providerPath
AssertContains $provider "_linearBeatIndex = RuntimeLinearBeatIndex.FromTrack(track);" $providerPath

dotnet test .\src\EternalLoop.Playback.Tests\EternalLoop.Playback.Tests.csproj -c Release

Write-Host "OK: playback read event verification passed."
