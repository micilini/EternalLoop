$ErrorActionPreference = "Stop"

function Fail([string] $Message) {
    Write-Error $Message
    exit 1
}

function Get-ActiveFiles {
    $roots = @(".\src", ".\modules", ".\tools") | Where-Object { Test-Path $_ }
    $extensions = @(".cs", ".xaml", ".xml", ".csproj", ".props", ".targets", ".ps1", ".slnx", ".slnLaunch")
    $files = @()

    foreach ($root in $roots) {
        $files += Get-ChildItem -Path $root -Recurse -File | Where-Object {
            $extensions -contains $_.Extension `
                -and $_.FullName -notmatch '[\\/](bin|obj|\.vs)[\\/]'
        }
    }

    foreach ($path in @(".\EternalLoop.slnx", ".\EternalLoop.slnLaunch")) {
        if (Test-Path $path) {
            $files += Get-Item $path
        }
    }

    $files | Sort-Object -Unique FullName
}

$blockedTerms = @(
    ("Ju" + "kebox"),
    ("ju" + "kebox"),
    ("Eternal" + "Ju" + "kebox"),
    ("eternal" + "ju" + "kebox"),
    ("FA" + "SE"),
    ("F" + "ase"),
    ("f" + "ase"),
    ("HOT" + "FIX"),
    ("Hot" + "fix"),
    ("hot" + "fix"),
    ("CO" + "DEX"),
    ("Co" + "dex"),
    ("co" + "dex"),
    ("ROAD" + "MAP"),
    ("Road" + "map"),
    ("road" + "map"),
    ("07" + "K")
)

$blockedCaseSensitiveTerms = @(
    ("15" + "A"),
    ("15" + "B"),
    ("15" + "C"),
    ("15" + "D"),
    ("15" + "E"),
    ("15" + "F")
)

$commentPatternsByExtension = @{
    ".cs" = @("^\s*//", "^\s*///", "/\*", "\*/", "(?<!:)//")
    ".xaml" = @("<!--", "-->")
    ".xml" = @("<!--", "-->")
    ".csproj" = @("<!--", "-->")
    ".props" = @("<!--", "-->")
    ".targets" = @("<!--", "-->")
    ".ps1" = @("^\s*#(?!requires\b)")
}

$failures = New-Object System.Collections.Generic.List[string]

foreach ($file in Get-ActiveFiles) {
    $relative = Resolve-Path -Relative $file.FullName
    $text = Get-Content -LiteralPath $file.FullName -Raw

    foreach ($term in $blockedTerms) {
        if ($text.IndexOf($term, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
            $failures.Add("$relative contains blocked term $term")
        }
    }

    foreach ($term in $blockedCaseSensitiveTerms) {
        if ($text.Contains($term)) {
            $failures.Add("$relative contains blocked term $term")
        }
    }

    if ($commentPatternsByExtension.ContainsKey($file.Extension)) {
        $lines = Get-Content -LiteralPath $file.FullName
        for ($index = 0; $index -lt $lines.Count; $index++) {
            foreach ($pattern in $commentPatternsByExtension[$file.Extension]) {
                if ($lines[$index] -match $pattern) {
                    $failures.Add("$relative line $($index + 1) contains comment pattern $pattern")
                }
            }
        }
    }
}

$blockedPathPatterns = @(
    ('verify-' + 'h' + '*-*.ps1'),
    ('verify-' + 'hard' + 'ening-release-ready.ps1')
)

foreach ($pattern in $blockedPathPatterns) {
    $blockedPaths = Get-ChildItem -Path .\tools -File -Filter $pattern
    foreach ($blockedPath in $blockedPaths) {
        $failures.Add("tools contains legacy script name $($blockedPath.Name)")
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host $_ }
    Fail "Release clean-code verification failed."
}

Write-Host "OK: release clean-code verification passed."
