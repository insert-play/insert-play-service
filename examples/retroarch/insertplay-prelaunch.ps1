# InsertPlay - RetroArch RetroAchievements Pre-Launch Script
# Injects RA credentials into the RetroArch portable config on the SD card.
# Expects RetroArch to be bundled at <card_root>/retroarch/ in portable mode.
#
# Environment variables provided by InsertPlay:
#   INSERTPLAY_CARD_PATH      - Root path of the SD card
#   INSERTPLAY_RA_USERNAME    - RetroAchievements username (empty if not logged in)
#   INSERTPLAY_RA_PASSWORD    - RetroAchievements account password (empty if not logged in)

$username = $env:INSERTPLAY_RA_USERNAME
$password = $env:INSERTPLAY_RA_PASSWORD
$cardPath = $env:INSERTPLAY_CARD_PATH

if (-not $username -or -not $password) {
    Write-Host "No RetroAchievements credentials provided. Skipping RA configuration."
    exit 0
}

$cfgPath = Join-Path $cardPath "retroarch\retroarch.cfg"

if (-not (Test-Path $cfgPath)) {
    Write-Host "retroarch.cfg not found at: $cfgPath"
    exit 1
}

$lines                  = Get-Content $cfgPath
$usernameSet            = $false
$passwordSet            = $false
$enableSet              = $false

$result = foreach ($line in $lines) {
    if      ($line -match '^cheevos_username\s*=') { "cheevos_username = `"$username`""; $usernameSet = $true }
    elseif  ($line -match '^cheevos_password\s*=') { "cheevos_password = `"$password`"";  $passwordSet = $true }
    elseif  ($line -match '^cheevos_token\s*=')    { 'cheevos_token = ""' }
    elseif  ($line -match '^cheevos_enable\s*=')   { 'cheevos_enable = "true"';          $enableSet   = $true }
    else    { $line }
}

# Append any keys that were not already present in the file
$output = [System.Collections.Generic.List[string]]$result
if (-not $usernameSet) { $output.Add("cheevos_username = `"$username`"") }
if (-not $passwordSet) { $output.Add("cheevos_password = `"$password`"") }
if (-not $enableSet)   { $output.Add('cheevos_enable = "true"') }

Set-Content -Path $cfgPath -Value $output -Encoding UTF8
Write-Host "RetroAchievements credentials written for user: $username"
exit 0
