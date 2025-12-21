# PowerShell script to find IOptionsMonitor.OnChange usage and check for corresponding disposal
# Exits with non-zero code if suspicious patterns found

Write-Host "Searching for IOptionsMonitor.OnChange usages..."

# Collect C# files recursively (skip bin/obj) to avoid '**/*.cs' glob issues in older PowerShell
$files = Get-ChildItem -Path . -Recurse -Filter *.cs -File | Where-Object { $_.FullName -notmatch "\\(bin|obj)\\" }
# Find files containing OnChange(
$matches = $files | ForEach-Object { Select-String -Path $_.FullName -Pattern "OnChange\s*\(" } | Select-Object Path, LineNumber, Line
if (-not $matches) {
    Write-Host "No OnChange usages found."; exit 0
}

$failures = @()

foreach ($m in $matches) {
    $file = $m.Path
    Write-Host "Found OnChange in $file at line $($m.LineNumber)"

    # Heuristic: check same file for disposal of token variable named optionsChangeToken or optionsChangeToken?.Dispose
    $content = Get-Content -Raw -Path $file

    # Skip method declarations (e.g., IOptionsMonitor implementations) to avoid false positives
    if ($m.Line -match '^\s*(public|private|protected|internal)\s+.*OnChange\s*\(') {
        continue
    }

    # If OnChange assigned to a variable, capture variable name
    if ($m.Line -match "=(.*)OnChange") {
        # attempt to extract variable name left of '='
        $left = $m.Line.Split('=')[0].Trim()
        $varName = ($left -split '\\s+')[-1]
        Write-Host "Detected assignment to variable '$varName'"

        # search for disposal of that variable
        if ($content -notmatch ([regex]::Escape("$varName?.Dispose")) -and $content -notmatch ([regex]::Escape("$varName.Dispose"))) {
            Write-Host "No Dispose call found for $varName in $file"
            $failures += "No Dispose for $varName in $file"
        }
    }
    else {
        # Not assigned, assume OnChange used inline - warn
        Write-Host "OnChange usage not assigned to a token in $file - please ensure subscription is cancelled"
        $failures += "OnChange not assigned in $file"
    }
}

if ($failures.Count -gt 0) {
    Write-Host "Found issues:" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host $_ }
    exit 1
}
else {
    Write-Host "All OnChange usages have disposal (heuristic check)." -ForegroundColor Green
    exit 0
}
