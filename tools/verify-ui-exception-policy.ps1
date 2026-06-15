$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$policyPath = Join-Path $repoRoot 'src\EternalLoop.App\Diagnostics\UnhandledUiExceptionPolicy.cs'
$decisionPath = Join-Path $repoRoot 'src\EternalLoop.App\Diagnostics\UnhandledUiExceptionDecision.cs'
$actionPath = Join-Path $repoRoot 'src\EternalLoop.App\Diagnostics\UnhandledUiExceptionAction.cs'
$appPath = Join-Path $repoRoot 'src\EternalLoop.App\App.xaml.cs'
$policyTestsPath = Join-Path $repoRoot 'src\EternalLoop.App.Tests\Diagnostics\UnhandledUiExceptionPolicyTests.cs'
$appTestsPath = Join-Path $repoRoot 'src\EternalLoop.App.Tests\Diagnostics\AppExceptionHandlingTests.cs'

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

Assert-FileExists $policyPath
Assert-FileExists $decisionPath
Assert-FileExists $actionPath
Assert-FileExists $appPath
Assert-FileExists $policyTestsPath
Assert-FileExists $appTestsPath

$policy = Read-Text $policyPath
$decision = Read-Text $decisionPath
$action = Read-Text $actionPath
$app = Read-Text $appPath
$policyTests = Read-Text $policyTestsPath
$appTests = Read-Text $appTestsPath

Assert-Contains $action 'public enum UnhandledUiExceptionAction' $actionPath
Assert-Contains $action 'Continue' $actionPath
Assert-Contains $action 'Shutdown' $actionPath
Assert-Contains $decision 'UnhandledUiExceptionDecision' $decisionPath
Assert-Contains $decision 'AppLogLevel' $decisionPath
Assert-Contains $policy 'UnhandledUiExceptionPolicy' $policyPath
Assert-Contains $policy 'OperationCanceledException' $policyPath
Assert-Contains $policy 'IOException' $policyPath
Assert-Contains $policy 'UnauthorizedAccessException' $policyPath
Assert-Contains $policy 'AppLogLevel.Critical' $policyPath
Assert-Contains $policy 'EternalLoop found a serious problem and needs to close safely.' $policyPath

Assert-Contains $app 'UnhandledUiExceptionPolicy.Decide(e.Exception)' $appPath
Assert-Contains $app 'Current.Shutdown(1)' $appPath
Assert-Contains $app 'DisposeOpenWindowDataContexts()' $appPath
Assert-Contains $app 'window.DataContext is not IDisposable disposable' $appPath
Assert-Contains $app 'disposable.Dispose()' $appPath
Assert-Contains $app 'AppLogLevel.Critical' $appPath
Assert-Contains $app 'TaskScheduler.UnobservedTaskException' $appPath
Assert-Contains $app 'e.SetObserved()' $appPath
Assert-NotContains $app 'EternalLoop found an unexpected problem and logged the details. You can keep using the app, but if something looks wrong, restart it.' $appPath

Assert-Contains $policyTests 'DecideShouldContinueForRecoverableExceptions' $policyTestsPath
Assert-Contains $policyTests 'DecideShouldShutdownForFatalExceptions' $policyTestsPath
Assert-Contains $policyTests 'OperationCanceledException' $policyTestsPath
Assert-Contains $policyTests 'IOException' $policyTestsPath
Assert-Contains $policyTests 'UnauthorizedAccessException' $policyTestsPath
Assert-Contains $policyTests 'NullReferenceException' $policyTestsPath
Assert-Contains $policyTests 'InvalidOperationException' $policyTestsPath
Assert-Contains $policyTests 'OutOfMemoryException' $policyTestsPath
Assert-Contains $policyTests 'TypeInitializationException' $policyTestsPath
Assert-Contains $policyTests 'NotSupportedException' $policyTestsPath
Assert-Contains $policyTests 'XamlParseException' $policyTestsPath
Assert-Contains $policyTests 'AppLogLevel.Critical' $policyTestsPath
Assert-Contains $appTests 'AppShouldUseUnhandledUiExceptionPolicy' $appTestsPath
Assert-Contains $appTests 'AppShouldDisposeWindowDataContextsBeforeFatalShutdown' $appTestsPath

Push-Location $repoRoot
try {
    dotnet test .\src\EternalLoop.App.Tests\EternalLoop.App.Tests.csproj -c Release
}
finally {
    Pop-Location
}

Write-Host 'OK: UI exception policy verification passed.'
