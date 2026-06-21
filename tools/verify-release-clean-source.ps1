$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")

$extensions = @(
    ".cs",
    ".xaml",
    ".csproj",
    ".props",
    ".targets",
    ".ps1",
    ".json",
    ".md"
)

$excludedPathFragments = @(
    "\bin\",
    "\obj\",
    "\.git\",
    "\assets\models\beat-this\THIRD_PARTY_NOTICES.md",
    "\assets\models\beat-this\readme.md",
    "\LICENSE"
)

$forbiddenPatterns = @(
    "\b" + "ju" + "kebox" + "\b",
    "\b" + "in" + "finite" + "\b",
    "ec" + "ho\s+" + "ne" + "st",
    "\b" + "ec" + "ho" + "\b",
    "\b" + "ne" + "st" + "\b",
    "\b" + "mad" + "mom" + "\b",
    "\b" + "h" + "bg" + "\b",
    "\b" + "fa" + "se" + "\b",
    "\b" + "road" + "map" + "\b"
)

$commentPatterns = @(
    "^\s*" + [regex]::Escape("/" + "/"),
    "^\s*" + [regex]::Escape("/" + "/" + "/"),
    [regex]::Escape("/" + "*"),
    [regex]::Escape("*" + "/"),
    [regex]::Escape("<" + "!" + "-" + "-"),
    [regex]::Escape("-" + "-" + ">")
)

function Remove-QuotedText([string] $Line) {
    $result = New-Object System.Text.StringBuilder
    $inString = $false
    $verbatim = $false
    $escaped = $false

    for ($index = 0; $index -lt $Line.Length; $index++) {
        $current = $Line[$index]
        $previous = if ($index -gt 0) { $Line[$index - 1] } else { [char]0 }

        if (!$inString -and $current -eq '"') {
            $inString = $true
            $verbatim = $previous -eq '@'
            [void]$result.Append(' ')
            continue
        }

        if ($inString) {
            if ($verbatim -and $current -eq '"' -and $index + 1 -lt $Line.Length -and $Line[$index + 1] -eq '"') {
                $index++
            }
            elseif ($current -eq '"' -and ($verbatim -or !$escaped)) {
                $inString = $false
                $verbatim = $false
            }

            $escaped = !$verbatim -and $current -eq '\' -and !$escaped
            [void]$result.Append(' ')
            continue
        }

        $escaped = $false
        [void]$result.Append($current)
    }

    $result.ToString()
}

$files = Get-ChildItem -Path $root -Recurse -File | Where-Object {
    $extensions -contains $_.Extension
}

$files = $files | Where-Object {
    $fullName = $_.FullName
    -not ($excludedPathFragments | Where-Object { $fullName.Contains($_) })
}

$failures = New-Object System.Collections.Generic.List[string]

foreach ($file in $files) {
    $relative = Resolve-Path $file.FullName -Relative
    $lines = Get-Content $file.FullName

    for ($index = 0; $index -lt $lines.Count; $index++) {
        $line = $lines[$index]
        $scanLine = Remove-QuotedText $line
        $lineNumber = $index + 1

        foreach ($pattern in $commentPatterns) {
            if ($scanLine -match $pattern) {
                $failures.Add("${relative}:${lineNumber} comment pattern '${pattern}'")
            }
        }

        if ($file.Extension -eq ".ps1" -and $scanLine -match "^\s*#") {
            $failures.Add("${relative}:${lineNumber} PowerShell comment")
        }

        foreach ($pattern in $forbiddenPatterns) {
            if ($scanLine -match $pattern) {
                if ($line -match 'RepeatBehavior\s*=\s*"Forever"') {
                    continue
                }

                $failures.Add("${relative}:${lineNumber} forbidden term '${pattern}'")
            }
        }
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    throw "Release source cleanup verification failed."
}

& (Join-Path $PSScriptRoot 'verify-third-party-notices.ps1')

Write-Host "Release source cleanup verification passed."
