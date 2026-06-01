<#
.SYNOPSIS
    Adds InsertPlay to the current user's Windows startup folder.

.DESCRIPTION
    Creates a shortcut for InsertPlay.Service.exe in %APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup.
    The application will start automatically on login, running silently in the system tray.
    No administrator rights required.

.EXAMPLE
    .\add-to-startup.ps1
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ScriptDir     = Split-Path -Parent $MyInvocation.MyCommand.Definition
$ExePath       = Join-Path $ScriptDir 'InsertPlay.Service.exe'
$StartupFolder = [Environment]::GetFolderPath('Startup')
$ShortcutPath  = Join-Path $StartupFolder 'InsertPlay.lnk'

if (-not (Test-Path $ExePath)) {
    Write-Error "InsertPlay.Service.exe not found at '$ExePath'.`nPlace this script in the same directory as the executable."
    exit 1
}

$wsh           = New-Object -ComObject WScript.Shell
$shortcut      = $wsh.CreateShortcut($ShortcutPath)
$shortcut.TargetPath       = $ExePath
$shortcut.WorkingDirectory = $ScriptDir
$shortcut.Description      = 'InsertPlay — SD game card auto-launcher'
$shortcut.Save()

Write-Host ""
Write-Host "InsertPlay adicionado ao inicializar do Windows."
Write-Host "Atalho criado em: $ShortcutPath"
Write-Host ""
Write-Host "Para remover do inicializar:"
Write-Host "  Remove-Item '$ShortcutPath'"
