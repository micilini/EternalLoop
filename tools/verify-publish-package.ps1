$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$profilePath = Join-Path $repoRoot 'src\EternalLoop.App\Properties\PublishProfiles\win-x64-self-contained.pubxml'
$appProjectPath = Join-Path $repoRoot 'src\EternalLoop.App\EternalLoop.App.csproj'
$publishScriptPath = Join-Path $repoRoot 'tools\publish-release-win-x64.ps1'
$readmePath = Join-Path $repoRoot 'README.md'
$gitignorePath = Join-Path $repoRoot '.gitignore'
$publishDirectory = Join-Path $repoRoot 'artifacts\publish\EternalLoop-1.2.0-win-x64'
$exePath = Join-Path $publishDirectory 'EternalLoop.App.exe'

function Assert-FileExists([string] $path) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Missing file: $path"
    }
}

function Assert-Contains([string] $path, [string] $value) {
    $content = Get-Content -LiteralPath $path -Raw
    if (-not $content.Contains($value)) {
        throw "Expected $path to contain: $value"
    }
}

function Assert-NotContains([string] $path, [string] $value) {
    $content = Get-Content -LiteralPath $path -Raw
    if ($content.Contains($value)) {
        throw "Expected $path not to contain: $value"
    }
}

Assert-FileExists $profilePath
Assert-FileExists $appProjectPath
Assert-FileExists $publishScriptPath
Assert-FileExists $readmePath
Assert-FileExists $gitignorePath

Assert-Contains $profilePath '<RuntimeIdentifier>win-x64</RuntimeIdentifier>'
Assert-Contains $profilePath '<SelfContained>true</SelfContained>'
Assert-Contains $profilePath '<PublishSingleFile>true</PublishSingleFile>'
Assert-Contains $profilePath '<PublishTrimmed>false</PublishTrimmed>'
Assert-Contains $profilePath 'artifacts\publish\EternalLoop-1.2.0-win-x64'
Assert-NotContains $profilePath 'C:\Users'
Assert-NotContains $profilePath 'sdanz'
Assert-NotContains $profilePath 'Desktop'
Assert-NotContains $profilePath 'v1.0.0'
Assert-NotContains $profilePath 'EternalLoop-v1.0.0'

Assert-Contains $appProjectPath '<TargetFramework>net8.0-windows</TargetFramework>'
Assert-Contains $appProjectPath '<UseWPF>true</UseWPF>'
Assert-Contains $appProjectPath '<Version>1.2.0</Version>'
Assert-Contains $appProjectPath '<FileVersion>1.2.0.0</FileVersion>'
Assert-Contains $appProjectPath '<RuntimeIdentifiers>win-x64</RuntimeIdentifiers>'

Assert-Contains $publishScriptPath 'dotnet publish .\src\EternalLoop.App\EternalLoop.App.csproj -c Release -p:PublishProfile=win-x64-self-contained'
Assert-Contains $readmePath 'powershell -ExecutionPolicy Bypass -File .\tools\publish-release-win-x64.ps1'
Assert-Contains $gitignorePath 'artifacts/'

$appFiles = Get-ChildItem -LiteralPath (Join-Path $repoRoot 'src\EternalLoop.App') -Recurse -File | Where-Object {
    $_.Extension -in @('.csproj', '.pubxml', '.props', '.targets')
}
foreach ($file in $appFiles) {
    if ((Get-Content -LiteralPath $file.FullName -Raw).Contains('<PublishTrimmed>true</PublishTrimmed>')) {
        throw "PublishTrimmed true is not allowed in App publish files: $($file.FullName)"
    }
}

$publishProfiles = Get-ChildItem -LiteralPath $repoRoot -Recurse -File -Filter *.pubxml
foreach ($publishProfile in $publishProfiles) {
    $content = Get-Content -LiteralPath $publishProfile.FullName -Raw
    foreach ($blocked in @('C:\Users', 'sdanz', 'Desktop', 'v1.0.0', 'EternalLoop-v1.0.0')) {
        if ($content.Contains($blocked)) {
            throw "Publish profile contains blocked release text '$blocked': $($publishProfile.FullName)"
        }
    }
}

Push-Location $repoRoot
try {
    dotnet publish .\src\EternalLoop.App\EternalLoop.App.csproj -c Release -p:PublishProfile=win-x64-self-contained
}
finally {
    Pop-Location
}

Assert-FileExists $exePath

if ($IsWindows -or $env:OS -eq 'Windows_NT') {
    $versionInfo = (Get-Item -LiteralPath $exePath).VersionInfo
    if ($versionInfo.FileVersion -ne '1.2.0.0') {
        throw "Unexpected FileVersion: $($versionInfo.FileVersion)"
    }

    if (-not $versionInfo.ProductVersion.StartsWith('1.2.0', [StringComparison]::OrdinalIgnoreCase)) {
        throw "Unexpected ProductVersion: $($versionInfo.ProductVersion)"
    }
}

Write-Host 'OK: publish package verification passed.'
