# InsertPlay - PCSX2 RetroAchievements Pre-Launch Script
# Injects RA credentials into the PCSX2 portable config on the SD card.
# Expects PCSX2 to be bundled at <card_root>/pcsx2/ in portable mode.
#
# Environment variables provided by InsertPlay:
#   INSERTPLAY_CARD_PATH      - Root path of the SD card
#   INSERTPLAY_RA_USERNAME    - RetroAchievements username (empty if not logged in)
#   INSERTPLAY_RA_PASSWORD    - RetroAchievements account password

$username = $env:INSERTPLAY_RA_USERNAME
$password = $env:INSERTPLAY_RA_PASSWORD
$token = $env:INSERTPLAY_RA_TOKEN
$loginTimestamp = $env:INSERTPLAY_RA_LOGIN_TIMESTAMP
$cardPath = $env:INSERTPLAY_CARD_PATH

if (-not $username -or -not $password) {
    Write-Host "No RetroAchievements credentials provided. Skipping RA configuration."
    exit 0
}

$iniPath = Join-Path $cardPath "pcsx2\inis\PCSX2.ini"
$secretsPath = Join-Path $cardPath "pcsx2\inis\secrets.ini"

if (-not (Test-Path $iniPath)) {
    Write-Host "PCSX2.ini not found at: $iniPath"
    exit 1
}

$lines = Get-Content $iniPath
$output = New-Object System.Collections.Generic.List[string]

$inAchievements = $false
$inAutoUpdater = $false
$hasAchievements = $false
$hasAutoUpdater = $false

$enabledSet = $false
$usernameSet = $false
$timestampSet = $false
$passwordSet = $false

function Flush-AchievementsMissing {
    if (-not $enabledSet) { $output.Add("Enabled = true") }
    if (-not $usernameSet) { $output.Add("Username = $username") }
    if (-not $timestampSet) {
        if ($loginTimestamp) { $output.Add("LoginTimestamp = $loginTimestamp") }
        else { $output.Add("LoginTimestamp = ") }
    }
}

function Flush-AutoUpdaterMissing {
    if (-not $passwordSet) { $output.Add("Password = $password") }
}

foreach ($line in $lines) {
    if ($line -match '^\[(.+)\]') {
        if ($inAchievements) { Flush-AchievementsMissing }
        if ($inAutoUpdater) { Flush-AutoUpdaterMissing }

        $sectionName = $Matches[1]
        $inAchievements = $sectionName -eq 'Achievements'
        $inAutoUpdater = $sectionName -eq 'AutoUpdater'

        if ($inAchievements) { $hasAchievements = $true }
        if ($inAutoUpdater) { $hasAutoUpdater = $true }

        $output.Add($line)
        continue
    }

    if ($inAchievements) {
        if ($line -match '^\s*Enabled\s*=') {
            $enabledSet = $true
            $output.Add("Enabled = true")
        }
        elseif ($line -match '^\s*Username\s*=') {
            $usernameSet = $true
            $output.Add("Username = $username")
        }
        elseif ($line -match '^\s*LoginTimestamp\s*=') {
            $timestampSet = $true
            if ($loginTimestamp) { $output.Add("LoginTimestamp = $loginTimestamp") }
            else { $output.Add("LoginTimestamp = ") }
        }
        else {
            $output.Add($line)
        }
        continue
    }

    if ($inAutoUpdater) {
        if ($line -match '^\s*Password\s*=') {
            $passwordSet = $true
            $output.Add("Password = $password")
        }
        else {
            $output.Add($line)
        }
        continue
    }

    $output.Add($line)
}

if ($inAchievements) { Flush-AchievementsMissing }
if ($inAutoUpdater) { Flush-AutoUpdaterMissing }

if (-not $hasAchievements) {
    $output.Add("")
    $output.Add("[Achievements]")
    $output.Add("Enabled = true")
    $output.Add("Username = $username")
    if ($loginTimestamp) { $output.Add("LoginTimestamp = $loginTimestamp") }
    else { $output.Add("LoginTimestamp = ") }
}

if (-not $hasAutoUpdater) {
    $output.Add("")
    $output.Add("[AutoUpdater]")
    $output.Add("Password = $password")
}

Set-Content -Path $iniPath -Value $output -Encoding UTF8

# Token is stored in secrets.ini for newer PCSX2 builds.
if (Test-Path $secretsPath) {
    $secretLines = Get-Content $secretsPath
    $secretOut = New-Object System.Collections.Generic.List[string]
    $inAchievementsSecrets = $false
    $hasAchievementsSecrets = $false
    $tokenSet = $false

    foreach ($line in $secretLines) {
        if ($line -match '^\[(.+)\]') {
            if ($inAchievementsSecrets -and -not $tokenSet) {
                $secretOut.Add("Token = ")
                $tokenSet = $true
            }

            $inAchievementsSecrets = $Matches[1] -eq 'Achievements'
            if ($inAchievementsSecrets) { $hasAchievementsSecrets = $true }

            $secretOut.Add($line)
            continue
        }

        if ($inAchievementsSecrets -and $line -match '^\s*Token\s*=') {
            if ($token) { $secretOut.Add("Token = $token") }
            else { $secretOut.Add("Token = ") }
            $tokenSet = $true
            continue
        }

        $secretOut.Add($line)
    }

    if ($inAchievementsSecrets -and -not $tokenSet) {
        if ($token) { $secretOut.Add("Token = $token") }
        else { $secretOut.Add("Token = ") }
        $tokenSet = $true
    }

    if (-not $hasAchievementsSecrets) {
        $secretOut.Add("")
        $secretOut.Add("[Achievements]")
        if ($token) { $secretOut.Add("Token = $token") }
        else { $secretOut.Add("Token = ") }
    }

    Set-Content -Path $secretsPath -Value $secretOut -Encoding UTF8
}
else {
    if ($token) {
        Set-Content -Path $secretsPath -Value @("[Achievements]", "Token = $token") -Encoding UTF8
    }
    else {
        Set-Content -Path $secretsPath -Value @("[Achievements]", "Token = ") -Encoding UTF8
    }
}

Write-Host "RetroAchievements credentials written for user: $username"
exit 0
