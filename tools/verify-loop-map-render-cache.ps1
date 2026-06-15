$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$visualizationPath = Join-Path $repoRoot 'src\EternalLoop.App\Controls\LoopMapVisualization.cs'
$planPath = Join-Path $repoRoot 'src\EternalLoop.App\Controls\LoopMapRenderPlan.cs'
$testsPath = Join-Path $repoRoot 'src\EternalLoop.App.Tests\Controls\LoopMapRenderPlanTests.cs'
$playerViewPath = Join-Path $repoRoot 'src\EternalLoop.App\Views\PlayerView.xaml'

function Assert-FileExists([string] $path) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Missing file: $path"
    }
}

function Read-Text([string] $path) {
    return Get-Content -LiteralPath $path -Raw
}

function Assert-Contains([string] $content, [string] $value, [string] $path) {
    if (-not $content.Contains($value)) {
        throw "Expected $path to contain: $value"
    }
}

function Assert-NotContains([string] $content, [string] $value, [string] $path) {
    if ($content.Contains($value)) {
        throw "Expected $path not to contain: $value"
    }
}

Assert-FileExists $visualizationPath
Assert-FileExists $planPath
Assert-FileExists $testsPath
Assert-FileExists $playerViewPath

$visualization = Read-Text $visualizationPath
$plan = Read-Text $planPath
$tests = Read-Text $testsPath
$playerView = Read-Text $playerViewPath

Assert-Contains $plan 'public sealed class LoopMapRenderPlan' $planPath
Assert-Contains $plan 'MaxDisplayedEdges = 650' $planPath
Assert-Contains $plan 'HighlightedEdgeCount = 12' $planPath
Assert-Contains $plan 'CreateBeatOrdinals' $planPath
Assert-Contains $plan 'CreateDisplayEdges' $planPath
Assert-Contains $plan 'CreateHighlightedEdgesByBeat' $planPath
Assert-Contains $plan 'TryGetHighlightedEdges' $planPath
Assert-Contains $plan 'Math.Clamp(quality, 0.15, 1.0)' $planPath

Assert-Contains $visualization '_cachedGraph' $visualizationPath
Assert-Contains $visualization '_renderPlan' $visualizationPath
Assert-Contains $visualization 'GetRenderPlan' $visualizationPath
Assert-Contains $visualization 'LoopMapRenderPlan plan = GetRenderPlan(graph);' $visualizationPath
Assert-NotContains $visualization 'CreateBeatOrdinals(graph)' $visualizationPath
Assert-NotContains $visualization 'EnumerateDisplayEdges(graph)' $visualizationPath
Assert-Contains $visualization 'foreach (BranchGraphEdge edge in plan.DisplayEdges)' $visualizationPath
Assert-Contains $visualization 'plan.TryGetHighlightedEdges(CurrentBeatIndex' $visualizationPath
Assert-Contains $visualization 'private const byte MinimumBranchAlpha = 118' $visualizationPath
Assert-Contains $visualization 'private const byte MaximumBranchAlpha = 215' $visualizationPath
Assert-Contains $visualization 'private const byte HighlightBranchAlpha = 238' $visualizationPath
Assert-Contains $visualization 'private const double MinimumBranchThickness = 1.9' $visualizationPath
Assert-Contains $visualization 'private const double MaximumBranchThickness = 3.4' $visualizationPath
Assert-Contains $visualization 'private const double HighlightBranchThickness = 3.8' $visualizationPath
Assert-Contains $visualization 'private const double LastJumpBranchThickness = 4.4' $visualizationPath
Assert-Contains $visualization 'private const double OuterGlowThickness = 7.0' $visualizationPath
Assert-Contains $visualization 'private const double InnerGlowThickness = 3.0' $visualizationPath
Assert-Contains $visualization 'private const byte OuterGlowAlpha = 32' $visualizationPath
Assert-Contains $visualization 'private const byte InnerGlowAlpha = 76' $visualizationPath

Assert-Contains $tests 'CreateShouldBuildBeatOrdinalsOnceFromGraphNodes' $testsPath
Assert-Contains $tests 'CreateShouldIgnoreSelfEdgesInDisplayEdges' $testsPath
Assert-Contains $tests 'CreateShouldKeepOnlyMaxDisplayedEdges' $testsPath
Assert-Contains $tests 'CreateShouldPreserveBaseEdgeOrdering' $testsPath
Assert-Contains $tests 'CreateShouldPreserveHighlightedEdgeOrdering' $testsPath
Assert-Contains $tests 'TryGetHighlightedEdgesShouldReturnFalseWhenBeatHasNoEdges' $testsPath
Assert-Contains $tests 'TryGetHighlightedEdgesShouldReturnAtMostTwelveEdges' $testsPath
Assert-Contains $tests 'EmptyShouldHaveNoOrdinalsOrEdges' $testsPath
Assert-Contains $tests 'LoopMapVisualizationShouldUseRenderPlanCache' $testsPath

Assert-Contains $playerView 'controls:LoopMapVisualization' $playerViewPath
Assert-Contains $playerView 'Graph="{Binding Graph}"' $playerViewPath
Assert-Contains $playerView 'CurrentBeatIndex="{Binding CurrentBeatIndex}"' $playerViewPath
Assert-Contains $playerView 'LastJumpFromBeat="{Binding LastJumpFromBeat}"' $playerViewPath
Assert-Contains $playerView 'LastJumpToBeat="{Binding LastJumpToBeat}"' $playerViewPath

Push-Location $repoRoot
try {
    dotnet test .\src\EternalLoop.App.Tests\EternalLoop.App.Tests.csproj -c Release
}
finally {
    Pop-Location
}

Write-Host 'OK: loop map render cache verification passed.'
