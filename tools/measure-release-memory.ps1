$ErrorActionPreference = "Stop"

param(
    [int] $IntervalSeconds = 5,
    [int] $DurationSeconds = 120,
    [string] $OutputPath = ""
)

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $logRoot = Join-Path $env:LOCALAPPDATA "EternalLoop\Logs"
    New-Item -ItemType Directory -Force -Path $logRoot | Out-Null
    $OutputPath = Join-Path $logRoot ("release-memory-" + (Get-Date -Format "yyyyMMdd-HHmmss") + ".csv")
}

$project = ".\src\EternalLoop.App\EternalLoop.App.csproj"
$process = Start-Process -FilePath "dotnet" -ArgumentList @("run", "-c", "Release", "--project", $project) -PassThru -WindowStyle Hidden

"timestamp,workingSetMb,privateMemoryMb,processId" | Set-Content -LiteralPath $OutputPath -Encoding UTF8

$deadline = (Get-Date).AddSeconds($DurationSeconds)

try {
    while ((Get-Date) -lt $deadline -and !$process.HasExited) {
        $sample = Get-Process -Id $process.Id -ErrorAction SilentlyContinue
        if ($sample) {
            $working = [Math]::Round($sample.WorkingSet64 / 1MB, 2)
            $private = [Math]::Round($sample.PrivateMemorySize64 / 1MB, 2)
            $line = "{0},{1},{2},{3}" -f (Get-Date).ToString("O"), $working, $private, $process.Id
            Add-Content -LiteralPath $OutputPath -Value $line -Encoding UTF8
        }

        Start-Sleep -Seconds $IntervalSeconds
    }
}
finally {
    if (!$process.HasExited) {
        Stop-Process -Id $process.Id -Force
    }
}

Write-Host "Memory samples written to $OutputPath"
