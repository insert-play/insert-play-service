# InsertPlay pre-launch script — Need for Speed: Underground (Windows)
# Adjusts resolution in NFSUnderground.WidescreenFix.ini before the game starts.
#
# Environment variables set by InsertPlay:
#   INSERTPLAY_RESOLUTION  — e.g. "1920x1080" or "native"
#   INSERTPLAY_CARD_PATH   — root of the SD card (e.g. F:\)

$ErrorActionPreference = "Stop"

$iniPath = Join-Path $env:INSERTPLAY_CARD_PATH "scripts\NFSUnderground.WidescreenFix.ini"

if (-not (Test-Path $iniPath)) {
    Write-Error "WidescreenFix INI not found at: $iniPath"
    exit 1
}

$resolution = $env:INSERTPLAY_RESOLUTION

if ($resolution -eq "native" -or [string]::IsNullOrEmpty($resolution)) {
    # Auto-detect primary screen resolution via .NET
    Add-Type -AssemblyName System.Windows.Forms
    $screen = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
    $resX   = $screen.Width
    $resY   = $screen.Height
    Write-Host "Using native resolution: ${resX}x${resY}"
} else {
    $parts = $resolution -split "x", 2
    if ($parts.Count -ne 2 -or -not ([int]::TryParse($parts[0], [ref]$null)) -or -not ([int]::TryParse($parts[1], [ref]$null))) {
        Write-Error "Invalid resolution format '$resolution'. Expected WIDTHxHEIGHT (e.g. 1920x1080)."
        exit 1
    }
    $resX = [int]$parts[0]
    $resY = [int]$parts[1]
    Write-Host "Using custom resolution: ${resX}x${resY}"
}

# Update ResX and ResY in the INI file, preserving comments and formatting
$content = Get-Content $iniPath
$content = $content -replace '^(ResX\s*=)\s*\d+', "`${1} $resX"
$content = $content -replace '^(ResY\s*=)\s*\d+', "`${1} $resY"
Set-Content -Path $iniPath -Value $content -Encoding UTF8

Write-Host "Resolution set to ${resX}x${resY} in $iniPath"
