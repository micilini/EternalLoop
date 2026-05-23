param(
    [Parameter(Mandatory = $true)]
    [string]$SvgPath
)

if (-not (Test-Path -LiteralPath $SvgPath)) {
    throw "SVG file not found: $SvgPath"
}

[xml]$svg = Get-Content -LiteralPath $SvgPath -Raw
$rectCount = $svg.GetElementsByTagName("rect").Count
$pathCount = $svg.GetElementsByTagName("path").Count

"RectCount: $rectCount"
"PathCount: $pathCount"
"LikelyBeatCount: $rectCount"
"LikelyBranchCount: $pathCount"
