# UnlockOpenFile Example PowerShell Script
# This script demonstrates how to open files using UnlockOpenFile from PowerShell

# Set the path to UnlockOpenFile.exe
$UnlockerPath = "C:\Program Files\UnlockOpenFile\UnlockOpenFile.exe"

# Check if UnlockOpenFile.exe exists
if (-not (Test-Path $UnlockerPath)) {
    Write-Error "UnlockOpenFile.exe not found at $UnlockerPath"
    Write-Host "Please update the `$UnlockerPath variable in this script"
    exit 1
}

# Example 1: Open a specific file
Write-Host "Opening example.xlsx..."
Start-Process $UnlockerPath -ArgumentList "C:\Data\example.xlsx"

# Example 2: Open file with parameter
param(
    [Parameter(Position=0)]
    [string]$FilePath
)

if ($FilePath) {
    Write-Host "Opening $FilePath..."
    Start-Process $UnlockerPath -ArgumentList $FilePath
}

# Example 3: Open all Excel files in current directory
# Get-ChildItem -Filter "*.xlsx" | ForEach-Object {
#     Write-Host "Opening $($_.Name)..."
#     Start-Process $UnlockerPath -ArgumentList $_.FullName
# }

# Example 4: Open file and wait for completion
# Write-Host "Opening file and waiting..."
# Start-Process $UnlockerPath -ArgumentList "C:\Data\example.xlsx" -Wait
# Write-Host "File closed"
