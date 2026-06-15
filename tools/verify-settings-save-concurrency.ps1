$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$repositoryPath = Join-Path $repoRoot 'src\EternalLoop.Core\Settings\JsonUserSettingsRepository.cs'
$testsPath = Join-Path $repoRoot 'src\EternalLoop.Tests\Core\Settings\JsonUserSettingsRepositoryTests.cs'

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

function Get-Section([string] $content, [string] $startMarker, [string] $endMarker, [string] $path) {
    $start = $content.IndexOf($startMarker, [StringComparison]::Ordinal)
    $end = $content.IndexOf($endMarker, [StringComparison]::Ordinal)

    if ($start -lt 0 -or $end -le $start) {
        throw "Could not extract section in $path from '$startMarker' to '$endMarker'."
    }

    return $content.Substring($start, $end - $start)
}

Assert-FileExists $repositoryPath
Assert-FileExists $testsPath

$repository = Read-Text $repositoryPath
$tests = Read-Text $testsPath
$saveAsync = Get-Section $repository 'public async Task SaveAsync' 'private static SemaphoreSlim GetSaveLock' $repositoryPath

Assert-Contains $repository 'ConcurrentDictionary<string, SemaphoreSlim>' $repositoryPath
Assert-Contains $repository 'StringComparer.OrdinalIgnoreCase' $repositoryPath
Assert-Contains $repository 'Path.GetFullPath(_pathProvider.SettingsFilePath)' $repositoryPath
Assert-Contains $repository 'GetSaveLock(settingsPath)' $repositoryPath
Assert-Contains $repository 'SaveLocks.GetOrAdd(settingsPath' $repositoryPath
Assert-Contains $repository 'await saveLock.WaitAsync(cancellationToken).ConfigureAwait(false);' $repositoryPath
Assert-Contains $saveAsync 'finally' $repositoryPath
Assert-Contains $saveAsync 'TryDeleteTempFile(tempPath);' $repositoryPath
Assert-Contains $saveAsync 'saveLock.Release();' $repositoryPath
Assert-Contains $repository 'Guid.NewGuid():N' $repositoryPath
Assert-Contains $repository 'FileMode.CreateNew' $repositoryPath
Assert-Contains $repository 'File.Move(tempPath, settingsPath, overwrite: true);' $repositoryPath
Assert-Contains $repository 'private static void TryDeleteTempFile(string? tempPath)' $repositoryPath
Assert-Contains $repository 'File.Delete(tempPath);' $repositoryPath
Assert-NotContains $repository '_pathProvider.SettingsFilePath + ".tmp"' $repositoryPath

Assert-Contains $tests 'SaveAsyncShouldHandleConcurrentSavesOnSameRepository' $testsPath
Assert-Contains $tests 'SaveAsyncShouldHandleConcurrentSavesAcrossRepositoryInstances' $testsPath
Assert-Contains $tests 'SaveAsyncShouldNotLeaveTemporarySettingsFilesAfterSuccessfulConcurrentSaves' $testsPath
Assert-Contains $tests 'SaveAsyncShouldRemoveTemporaryFileWhenMoveFails' $testsPath
Assert-Contains $tests 'JsonDocument.Parse' $testsPath
Assert-Contains $tests 'settings.json.*.tmp' $testsPath

Push-Location $repoRoot
try {
    dotnet test .\src\EternalLoop.Tests\EternalLoop.Tests.csproj -c Release --filter JsonUserSettingsRepositoryTests
}
finally {
    Pop-Location
}

Write-Host 'OK: settings save concurrency verification passed.'
