#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Installs InsertPlay as a Windows Service.

.DESCRIPTION
    Registers InsertPlay.Service.exe as an automatic-start Windows Service and
    starts it immediately.  Must be run from an elevated (Administrator) session.

    The script installs the service to run in-place from the directory where this
    script lives, so place it alongside InsertPlay.Service.exe before running.

.EXAMPLE
    .\install-service.ps1
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ServiceName    = 'InsertPlay'
$DisplayName    = 'InsertPlay'
$Description    = 'InsertPlay — SD game card auto-launcher'
$InstallDir     = Split-Path -Parent $MyInvocation.MyCommand.Definition
$BinaryPath     = Join-Path $InstallDir 'InsertPlay.Service.exe'

# ---------------------------------------------------------------------------
# Prerequisite checks
# ---------------------------------------------------------------------------

if (-not (Test-Path $BinaryPath)) {
    Write-Error "Executable not found: '$BinaryPath'.`nMake sure this script is in the same directory as InsertPlay.Service.exe."
    exit 1
}

# ---------------------------------------------------------------------------
# Remove existing service if present
# ---------------------------------------------------------------------------

$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existing) {
    Write-Host "Service '$ServiceName' already exists — stopping and removing it..."
    if ($existing.Status -ne 'Stopped') {
        Stop-Service -Name $ServiceName -Force
    }
    & sc.exe delete $ServiceName | Out-Null
    # Brief pause so SCM releases the entry before re-creating it
    Start-Sleep -Seconds 2
}

# ---------------------------------------------------------------------------
# Register the service
# ---------------------------------------------------------------------------

Write-Host "Installing InsertPlay service..."
Write-Host "  Install dir : $InstallDir"

New-Service `
    -Name           $ServiceName `
    -BinaryPathName $BinaryPath `
    -DisplayName    $DisplayName `
    -Description    $Description `
    -StartupType    Automatic

# ---------------------------------------------------------------------------
# Start the service
# ---------------------------------------------------------------------------

Start-Service -Name $ServiceName

Write-Host ""
Write-Host "InsertPlay service installed and started successfully."
Write-Host ""
Write-Host "Useful commands:"
Write-Host "  Status   : Get-Service -Name $ServiceName"
Write-Host "  Logs     : Get-EventLog -LogName Application -Source $ServiceName"
Write-Host "  Stop     : Stop-Service -Name $ServiceName"
Write-Host "  Remove   : sc.exe delete $ServiceName"
