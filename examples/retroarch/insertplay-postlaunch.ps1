# InsertPlay - RetroArch RetroAchievements Post-Launch Script
# Clears RA credentials from RetroArch config ONLY if they were set by InsertPlay
# (verified by comparing the stored cheevos_username to INSERTPLAY_RA_USERNAME).
#
# Environment variables provided by InsertPlay:
#   INSERTPLAY_CARD_PATH      - Root path of the SD card
#   INSERTPLAY_RA_USERNAME    - RetroAchievements username that was injected

$username = $env:INSERTPLAY_RA_USERNAME
$cardPath = $env:INSERTPLAY_CARD_PATH

if (-not $username) {
    exit 0
}

$cfgPath = Join-Path $cardPath "retroarch\retroarch.cfg"

if (-not (Test-Path $cfgPath)) {
    exit 0
}

$lines = Get-Content $cfgPath

# Read the username currently stored in the config
$storedUsername = ''
foreach ($line in $lines) {
    if ($line -match '^cheevos_username\s*=\s*"(.*)"') {
        $storedUsername = $Matches[1]
    }
}

if ($storedUsername -ne $username) {
    Write-Host "Stored username '$storedUsername' does not match InsertPlay user '$username'. Skipping cleanup."
    exit 0
}

$result = foreach ($line in $lines) {
    if      ($line -match '^cheevos_username\s*=') { 'cheevos_username = ""' }
    elseif  ($line -match '^cheevos_password\s*=') { 'cheevos_password = ""' }
    elseif  ($line -match '^cheevos_token\s*=')    { 'cheevos_token = ""' }
    elseif  ($line -match '^cheevos_enable\s*=')   { 'cheevos_enable = "false"' }
    else    { $line }
}

Set-Content -Path $cfgPath -Value $result -Encoding UTF8
Write-Host "RetroAchievements credentials cleared for user: $username"
exit 0
