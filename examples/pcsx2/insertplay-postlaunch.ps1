# InsertPlay - PCSX2 RetroAchievements Post-Launch Script
# Clears RA credentials from PCSX2 config ONLY if they were set by InsertPlay
# (verified by comparing the stored username to INSERTPLAY_RA_USERNAME).
#
# Environment variables provided by InsertPlay:
#   INSERTPLAY_CARD_PATH      - Root path of the SD card
#   INSERTPLAY_RA_USERNAME    - RetroAchievements username that was injected

$username = $env:INSERTPLAY_RA_USERNAME
$cardPath = $env:INSERTPLAY_CARD_PATH

if (-not $username) {
    exit 0
}

$iniPath = Join-Path $cardPath "pcsx2\inis\PCSX2.ini"
$secretsPath = Join-Path $cardPath "pcsx2\inis\secrets.ini"

if (-not (Test-Path $iniPath)) {
    exit 0
}

$lines     = Get-Content $iniPath
$inSection = $false

# Read the username currently stored in the config
$storedUsername = ''
foreach ($line in $lines) {
    if ($line -match '^\[Achievements\]') { $inSection = $true; continue }
    if ($inSection -and $line -match '^\[') { $inSection = $false }
    if ($inSection -and $line -match '^\s*Username\s*=\s*(.*)$') { $storedUsername = $Matches[1].Trim() }
}

if ($storedUsername -ne $username) {
    Write-Host "Stored username '$storedUsername' does not match InsertPlay user '$username'. Skipping cleanup."
    exit 0
}

$inAchievements = $false
$inAutoUpdater = $false
$result = foreach ($line in $lines) {
    if ($line -match '^\[(.+)\]') {
        $inAchievements = $Matches[1] -eq 'Achievements'
        $inAutoUpdater = $Matches[1] -eq 'AutoUpdater'
        $line
        continue
    }

    if ($inAchievements) {
        if      ($line -match '^\s*Enabled\s*=')        { "Enabled = false" }
        elseif  ($line -match '^\s*Username\s*=')       { "Username = " }
        elseif  ($line -match '^\s*LoginTimestamp\s*=') { "LoginTimestamp = " }
        else                                              { $line }
    }
    elseif ($inAutoUpdater) {
        if ($line -match '^\s*Password\s*=') { "Password = " }
        else                               { $line }
    } else {
        $line
    }
}

Set-Content -Path $iniPath -Value $result -Encoding UTF8

if (Test-Path $secretsPath) {
    $secretLines = Get-Content $secretsPath
    $inAchievementsSecrets = $false
    $secretOut = foreach ($line in $secretLines) {
        if ($line -match '^\[(.+)\]') {
            $inAchievementsSecrets = $Matches[1] -eq 'Achievements'
            $line
            continue
        }

        if ($inAchievementsSecrets -and $line -match '^\s*Token\s*=') {
            "Token = "
        }
        else {
            $line
        }
    }

    Set-Content -Path $secretsPath -Value $secretOut -Encoding UTF8
}

Write-Host "RetroAchievements credentials cleared for user: $username"
exit 0
