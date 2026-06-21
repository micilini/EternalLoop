$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$solutionPath = Join-Path $repoRoot 'EternalLoop.slnx'

if (-not (Test-Path -LiteralPath $solutionPath)) {
    throw 'Run this script from the EternalLoop repository.'
}

$publishDirectory = Join-Path $repoRoot 'artifacts\publish\EternalLoop-1.3.0-win-x64'

if (-not $publishDirectory.StartsWith((Join-Path $repoRoot 'artifacts'), [StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to remove publish directory outside artifacts: $publishDirectory"
}

if (Test-Path -LiteralPath $publishDirectory) {
    Remove-Item -LiteralPath $publishDirectory -Recurse -Force
}

Push-Location $repoRoot
try {
    dotnet restore .\EternalLoop.slnx
    dotnet publish .\src\EternalLoop.App\EternalLoop.App.csproj -c Release -p:PublishProfile=win-x64-self-contained
}
finally {
    Pop-Location
}

$exePath = Join-Path $publishDirectory 'EternalLoop.App.exe'

if (-not (Test-Path -LiteralPath $exePath)) {
    throw "Publish did not create the expected executable: $exePath"
}

& (Join-Path $PSScriptRoot 'verify-third-party-notices.ps1')

Write-Host 'Publish completed.'
Write-Host "Executable: artifacts\publish\EternalLoop-1.3.0-win-x64\EternalLoop.App.exe"

if ($IsWindows -or $env:OS -eq 'Windows_NT') {
    $versionInfo = (Get-Item -LiteralPath $exePath).VersionInfo
    Write-Host "FileVersion: $($versionInfo.FileVersion)"
    Write-Host "ProductVersion: $($versionInfo.ProductVersion)"
}
