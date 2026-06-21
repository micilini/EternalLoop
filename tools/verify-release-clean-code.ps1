$ErrorActionPreference = "Stop"

& "$PSScriptRoot\verify-release-clean-source.ps1"

Write-Host "OK: release clean-code verification passed."
