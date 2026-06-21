$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$licensePath = Join-Path $repoRoot 'LICENSE'
$gitAttributesPath = Join-Path $repoRoot '.gitattributes'
$gitignorePath = Join-Path $repoRoot '.gitignore'
$readmePath = Join-Path $repoRoot 'README.md'
$legacyPlanningArtifactPattern = '*HARD' + 'ENING_1.2.0.md'
$analysisSolutionPath = Join-Path $repoRoot 'modules\AnalysisEngine\EternalLoop.AnalysisEngine.slnx'

function Assert-FileExists([string] $path) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Missing file: $path"
    }
}

function Assert-PathMissing([string] $path) {
    if (Test-Path -LiteralPath $path) {
        throw "Path should not exist: $path"
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

Assert-FileExists $licensePath
Assert-FileExists $gitAttributesPath
Assert-FileExists $gitignorePath
Assert-FileExists $readmePath
Assert-FileExists $analysisSolutionPath

$license = Read-Text $licensePath
$gitAttributes = Read-Text $gitAttributesPath
$gitignore = Read-Text $gitignorePath
$readme = Read-Text $readmePath
$analysisSolution = Read-Text $analysisSolutionPath

Assert-Contains $license 'MIT License' $licensePath
Assert-Contains $license 'Copyright (c) 2026 William Lima' $licensePath
Assert-NotContains $license 'TODO' $licensePath
Assert-NotContains $license 'confirm before release' $licensePath

Assert-Contains $gitAttributes '* text=auto' $gitAttributesPath
Assert-Contains $gitAttributes '*.png binary' $gitAttributesPath
Assert-Contains $gitAttributes '*.jpg binary' $gitAttributesPath
Assert-Contains $gitAttributes '*.webp binary' $gitAttributesPath
Assert-Contains $gitAttributes '*.ico binary' $gitAttributesPath
Assert-Contains $gitAttributes '*.wav binary' $gitAttributesPath
Assert-Contains $gitAttributes '*.mp3 binary' $gitAttributesPath
Assert-Contains $gitAttributes '*.m4a binary' $gitAttributesPath
Assert-Contains $gitAttributes '*.aac binary' $gitAttributesPath
Assert-Contains $gitAttributes '*.zip binary' $gitAttributesPath
Assert-Contains $gitAttributes '*.7z binary' $gitAttributesPath
Assert-Contains $gitAttributes '*.rar binary' $gitAttributesPath

Assert-Contains $readme 'EternalLoop 1.3.0' $readmePath
Assert-Contains $readme 'MP3' $readmePath
Assert-Contains $readme 'WAV' $readmePath
Assert-Contains $readme 'M4A' $readmePath
Assert-Contains $readme 'AAC' $readmePath
Assert-Contains $readme 'powershell -ExecutionPolicy Bypass -File .\tools\publish-release-win-x64.ps1' $readmePath
Assert-NotContains $readme 'docs/' $readmePath
Assert-NotContains $readme 'docs\' $readmePath
Assert-NotContains $readme 'V1 monorepo structure is complete' $readmePath
Assert-NotContains $readme 'does not yet integrate the WPF app' $readmePath
Assert-NotContains $readme 'V2_STARTING_POINT' $readmePath
Assert-NotContains $readme 'MONOREPO_STRUCTURE' $readmePath
Assert-NotContains $readme 'V1_COMPLETION_CHECKLIST' $readmePath
Assert-NotContains $readme 'OGG' $readmePath
Assert-NotContains $readme 'FLAC' $readmePath
Assert-NotContains $readme '.ogg' $readmePath
Assert-NotContains $readme '.flac' $readmePath

if (Get-ChildItem -LiteralPath $repoRoot -File -Filter $legacyPlanningArtifactPattern) {
    throw 'Legacy planning artifact should not exist.'
}
Assert-PathMissing (Join-Path $repoRoot 'docs')
Assert-PathMissing (Join-Path $repoRoot 'older-files')
Assert-NotContains $analysisSolution '<Folder Name="/docs/" />' $analysisSolutionPath

Assert-Contains $gitignore '.vs/' $gitignorePath
Assert-Contains $gitignore '*.user' $gitignorePath
Assert-Contains $gitignore '*.suo' $gitignorePath
Assert-Contains $gitignore '*.rsuser' $gitignorePath
Assert-Contains $gitignore '*.userosscache' $gitignorePath
Assert-Contains $gitignore '*.sln.docstates' $gitignorePath
Assert-Contains $gitignore 'bin/' $gitignorePath
Assert-Contains $gitignore 'obj/' $gitignorePath
Assert-Contains $gitignore 'TestResults/' $gitignorePath
Assert-Contains $gitignore 'artifacts/' $gitignorePath
Assert-Contains $gitignore '*.zip' $gitignorePath
Assert-Contains $gitignore '*.7z' $gitignorePath
Assert-Contains $gitignore '*.rar' $gitignorePath
Assert-Contains $gitignore 'older-files/' $gitignorePath
Assert-Contains $gitignore 'docs/' $gitignorePath

$status = git -C $repoRoot status --short --untracked-files=all
foreach ($line in $status) {
    if ($line -match '(^|\s)(\.vs/|bin/|obj/|artifacts/)' -or $line -match '/(bin|obj)/') {
        throw "Local generated artifact is visible to Git: $line"
    }
}

$excludedRelativePaths = @(
    'src\EternalLoop.Tests\Release\ReleasePackagingTests.cs',
    'src\EternalLoop.Tests\Release\RepositoryHygieneTests.cs',
    'tools\verify-publish-package.ps1',
    'tools\verify-repository-hygiene.ps1'
)

$blockedTexts = @(
    'C:\Users',
    'sdanz',
    'Desktop',
    'EternalLoop-v1.0.0',
    'v1.0.0-x64',
    'PUBLISHING_CLEANUP',
    'V2_STARTING_POINT',
    'MONOREPO_STRUCTURE',
    'V1_COMPLETION_CHECKLIST',
    'confirm before the public release',
    'License information should be confirmed',
    'TODO license'
)

$scanRoots = @(
    'README.md',
    'LICENSE',
    '.gitattributes',
    '.gitignore',
    'EternalLoop.slnx',
    'src',
    'modules',
    'tools'
)

$textExtensions = @('.slnx', '.csproj', '.props', '.targets', '.pubxml', '.ps1', '.cs', '.xaml', '.json', '.xml', '.md', '.txt', '')
$scanFiles = foreach ($scanRoot in $scanRoots) {
    $path = Join-Path $repoRoot $scanRoot
    if (Test-Path -LiteralPath $path -PathType Leaf) {
        Get-Item -LiteralPath $path
    } elseif (Test-Path -LiteralPath $path -PathType Container) {
        Get-ChildItem -LiteralPath $path -Recurse -File | Where-Object {
            $_.FullName -notmatch '\\(bin|obj)\\' -and
            $_.FullName -notmatch '\\artifacts\\' -and
            $textExtensions -contains $_.Extension
        }
    }
}

foreach ($file in $scanFiles) {
    $relativePath = $file.FullName.Substring($repoRoot.Length).TrimStart('\', '/')
    if ($excludedRelativePaths -contains $relativePath) {
        continue
    }

    $content = Read-Text $file.FullName
    foreach ($blockedText in $blockedTexts) {
        if ($content.Contains($blockedText)) {
            throw "Blocked repository hygiene text '$blockedText' found in $relativePath"
        }
    }
}

Write-Host 'OK: repository hygiene verification passed.'
