<#
.SYNOPSIS
    Downloads the SDL2 native runtime DLL for Windows x64 and places it where
    the build system will copy it to the service output directory.

.PARAMETER Version
    SDL2 release version to download. Defaults to 2.30.12.

.EXAMPLE
    .\tools\get-sdl2-native.ps1
    .\tools\get-sdl2-native.ps1 -Version 2.30.12
#>
param(
    [string]$Version = "2.30.12"
)

$ErrorActionPreference = "Stop"

$downloadUrl = "https://github.com/libsdl-org/SDL/releases/download/release-$Version/SDL2-$Version-win32-x64.zip"
$destDir     = Join-Path $PSScriptRoot "..\src\InsertPlay.Service\native\win-x64"
$destFile    = Join-Path $destDir "SDL2.dll"

if (Test-Path $destFile) {
    Write-Host "SDL2.dll already present at $destFile. Delete it first to re-download."
    exit 0
}

New-Item -ItemType Directory -Force -Path $destDir | Out-Null

$tempZip = Join-Path $env:TEMP "sdl2-$Version-win64.zip"

Write-Host "Downloading SDL2 $Version for Windows x64..."
Invoke-WebRequest -Uri $downloadUrl -OutFile $tempZip -UseBasicParsing

Write-Host "Extracting SDL2.dll..."
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip   = [System.IO.Compression.ZipFile]::OpenRead($tempZip)
$entry = $zip.Entries | Where-Object { $_.Name -eq "SDL2.dll" } | Select-Object -First 1

if ($null -eq $entry) {
    $zip.Dispose()
    Remove-Item $tempZip -ErrorAction SilentlyContinue
    Write-Error "SDL2.dll not found inside the downloaded archive."
}

[System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $destFile, $true)
$zip.Dispose()
Remove-Item $tempZip -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "Done! SDL2.dll placed at:"
Write-Host "  $destFile"
Write-Host ""
Write-Host "Build the project to copy SDL2.dll to the output directory:"
Write-Host "  dotnet build src/InsertPlay.Service"
