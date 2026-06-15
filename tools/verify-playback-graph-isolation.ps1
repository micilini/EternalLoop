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

$branchOrderPath = ".\src\EternalLoop.Playback\Runtime\RuntimeBranchOrder.cs"
$decisionEnginePath = ".\src\EternalLoop.Playback\Runtime\BranchDecisionEngine.cs"
$branchOrderTestsPath = ".\src\EternalLoop.Playback.Tests\Runtime\RuntimeBranchOrderTests.cs"
$decisionEngineTestsPath = ".\src\EternalLoop.Playback.Tests\Runtime\BranchDecisionEngineTests.cs"

$branchOrder = ReadText $branchOrderPath
$decisionEngine = ReadText $decisionEnginePath
$branchOrderTests = ReadText $branchOrderTestsPath
$decisionEngineTests = ReadText $decisionEngineTestsPath

AssertContains $branchOrder "public sealed class RuntimeBranchOrder" $branchOrderPath
AssertContains $branchOrder "ReferenceEqualityComparer.Instance" $branchOrderPath
AssertContains $branchOrder "new RuntimeBranchEdge[" $branchOrderPath
AssertContains $branchOrder "MoveToEnd" $branchOrderPath
AssertContains $branchOrder "ReferenceEquals" $branchOrderPath

AssertNotContains $decisionEngine "seedBeat.Neighbors.Remove" $decisionEnginePath
AssertNotContains $decisionEngine "seedBeat.Neighbors.Add" $decisionEnginePath
AssertNotContains $decisionEngine ".Neighbors.Remove(" $decisionEnginePath
AssertNotContains $decisionEngine ".Neighbors.Add(" $decisionEnginePath
AssertContains $decisionEngine "RuntimeBranchOrder" $decisionEnginePath
AssertContains $decisionEngine "GetBranchOrder" $decisionEnginePath
AssertContains $decisionEngine "MoveToEnd" $decisionEnginePath
AssertContains $decisionEngine "SelectSafeBranch(currentBeat, seedBeat, linearBeatIndex, branchOrder)" $decisionEnginePath

AssertContains $branchOrderTests "RuntimeBranchOrderTests" $branchOrderTestsPath
AssertContains $branchOrderTests "MoveToEndShouldNotMutateRuntimeBeatNeighbors" $branchOrderTestsPath
AssertContains $branchOrderTests "MoveToEndShouldUseReferenceIdentity" $branchOrderTestsPath
AssertContains $decisionEngineTests "DecideNextBeatShouldNotMutateSeedBeatNeighborsWhenRotating" $decisionEngineTestsPath
AssertContains $decisionEngineTests "ForcedEndGuardJumpShouldNotMutateSeedBeatNeighbors" $decisionEngineTestsPath
AssertContains $decisionEngineTests "EnumeratingSeedBeatNeighborsWhileDecisionsRotateShouldNotThrow" $decisionEngineTestsPath
dotnet test .\src\EternalLoop.Playback.Tests\EternalLoop.Playback.Tests.csproj -c Release

Write-Host "OK: playback graph isolation verification passed."
