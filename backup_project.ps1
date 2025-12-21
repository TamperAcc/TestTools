$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupDir = "Backups\Backup_$timestamp"
$sourceDir = "."
$exclude = @(".git", ".vs", "bin", "obj", "Backups")

New-Item -ItemType Directory -Force -Path $backupDir | Out-Null

Get-ChildItem -Path $sourceDir -Exclude $exclude | ForEach-Object {
    if ($_.Name -notin $exclude) {
        Copy-Item -Path $_.FullName -Destination $backupDir -Recurse -Force
    }
}

Write-Host "Project backed up to: $backupDir"
