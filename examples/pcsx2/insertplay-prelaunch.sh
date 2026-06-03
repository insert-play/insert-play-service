#!/usr/bin/env bash
# InsertPlay - PCSX2 RetroAchievements Pre-Launch Script (Linux)
# Injects RA credentials into the PCSX2 portable config on the SD card.
#
# Environment variables provided by InsertPlay:
#   INSERTPLAY_CARD_PATH      - Root path of the SD card
#   INSERTPLAY_RA_USERNAME    - RetroAchievements username (empty if not logged in)
#   INSERTPLAY_RA_PASSWORD    - RetroAchievements account password

set -euo pipefail

USERNAME="${INSERTPLAY_RA_USERNAME:-}"
PASSWORD="${INSERTPLAY_RA_PASSWORD:-}"
TOKEN="${INSERTPLAY_RA_TOKEN:-}"
LOGIN_TIMESTAMP="${INSERTPLAY_RA_LOGIN_TIMESTAMP:-}"
CARD_PATH="${INSERTPLAY_CARD_PATH:-}"

if [[ -z "$USERNAME" || -z "$PASSWORD" ]]; then
    echo "No RetroAchievements credentials provided. Skipping RA configuration."
    exit 0
fi

INI_PATH="$CARD_PATH/pcsx2/inis/PCSX2.ini"
SECRETS_PATH="$CARD_PATH/pcsx2/inis/secrets.ini"

if [[ ! -f "$INI_PATH" ]]; then
    echo "PCSX2.ini not found at: $INI_PATH"
    exit 1
fi

in_achievements=0
in_autoupdater=0
has_achievements=0
has_autoupdater=0

enabled_set=0
username_set=0
timestamp_set=0
password_set=0

tmp_file=$(mktemp)

flush_achievements_missing() {
    if [[ $enabled_set -eq 0 ]]; then echo "Enabled = true" >> "$tmp_file"; fi
    if [[ $username_set -eq 0 ]]; then echo "Username = $USERNAME" >> "$tmp_file"; fi
    if [[ $timestamp_set -eq 0 ]]; then
        if [[ -n "$LOGIN_TIMESTAMP" ]]; then echo "LoginTimestamp = $LOGIN_TIMESTAMP" >> "$tmp_file"; else echo "LoginTimestamp = " >> "$tmp_file"; fi
    fi
}

flush_autoupdater_missing() {
    if [[ $password_set -eq 0 ]]; then echo "Password = $PASSWORD" >> "$tmp_file"; fi
}

while IFS= read -r line; do
    if [[ "$line" =~ ^\[(.+)\]$ ]]; then
        if [[ $in_achievements -eq 1 ]]; then flush_achievements_missing; fi
        if [[ $in_autoupdater -eq 1 ]]; then flush_autoupdater_missing; fi

        section_name="${BASH_REMATCH[1]}"
        in_achievements=0
        in_autoupdater=0

        if [[ "$section_name" == "Achievements" ]]; then
            in_achievements=1
            has_achievements=1
        elif [[ "$section_name" == "AutoUpdater" ]]; then
            in_autoupdater=1
            has_autoupdater=1
        fi

        echo "$line" >> "$tmp_file"
        continue
    fi

    if [[ $in_achievements -eq 1 ]]; then
        if [[ "$line" =~ ^[[:space:]]*Enabled[[:space:]]*= ]]; then
            enabled_set=1
            echo "Enabled = true" >> "$tmp_file"
        elif [[ "$line" =~ ^[[:space:]]*Username[[:space:]]*= ]]; then
            username_set=1
            echo "Username = $USERNAME" >> "$tmp_file"
        elif [[ "$line" =~ ^[[:space:]]*LoginTimestamp[[:space:]]*= ]]; then
            timestamp_set=1
            if [[ -n "$LOGIN_TIMESTAMP" ]]; then echo "LoginTimestamp = $LOGIN_TIMESTAMP" >> "$tmp_file"; else echo "LoginTimestamp = " >> "$tmp_file"; fi
        else
            echo "$line" >> "$tmp_file"
        fi
        continue
    fi

    if [[ $in_autoupdater -eq 1 ]]; then
        if [[ "$line" =~ ^[[:space:]]*Password[[:space:]]*= ]]; then
            password_set=1
            echo "Password = $PASSWORD" >> "$tmp_file"
        else
            echo "$line" >> "$tmp_file"
        fi
        continue
    fi

    echo "$line" >> "$tmp_file"
done < "$INI_PATH"

if [[ $in_achievements -eq 1 ]]; then flush_achievements_missing; fi
if [[ $in_autoupdater -eq 1 ]]; then flush_autoupdater_missing; fi

if [[ $has_achievements -eq 0 ]]; then
    echo >> "$tmp_file"
    echo "[Achievements]" >> "$tmp_file"
    echo "Enabled = true" >> "$tmp_file"
    echo "Username = $USERNAME" >> "$tmp_file"
    if [[ -n "$LOGIN_TIMESTAMP" ]]; then echo "LoginTimestamp = $LOGIN_TIMESTAMP" >> "$tmp_file"; else echo "LoginTimestamp = " >> "$tmp_file"; fi
fi

if [[ $has_autoupdater -eq 0 ]]; then
    echo >> "$tmp_file"
    echo "[AutoUpdater]" >> "$tmp_file"
    echo "Password = $PASSWORD" >> "$tmp_file"
fi

mv "$tmp_file" "$INI_PATH"

# Token is stored in secrets.ini for newer PCSX2 builds.
if [[ -f "$SECRETS_PATH" ]]; then
    in_achievements=0
    has_achievements=0
    token_set=0
    tmp_secrets=$(mktemp)

    while IFS= read -r line; do
        if [[ "$line" =~ ^\[(.+)\]$ ]]; then
            if [[ $in_achievements -eq 1 && $token_set -eq 0 ]]; then
                echo "Token = " >> "$tmp_secrets"
                token_set=1
            fi

            section_name="${BASH_REMATCH[1]}"
            in_achievements=0
            if [[ "$section_name" == "Achievements" ]]; then
                in_achievements=1
                has_achievements=1
            fi

            echo "$line" >> "$tmp_secrets"
            continue
        fi

        if [[ $in_achievements -eq 1 && "$line" =~ ^[[:space:]]*Token[[:space:]]*= ]]; then
            if [[ -n "$TOKEN" ]]; then echo "Token = $TOKEN" >> "$tmp_secrets"; else echo "Token = " >> "$tmp_secrets"; fi
            token_set=1
        else
            echo "$line" >> "$tmp_secrets"
        fi
    done < "$SECRETS_PATH"

    if [[ $in_achievements -eq 1 && $token_set -eq 0 ]]; then
        if [[ -n "$TOKEN" ]]; then echo "Token = $TOKEN" >> "$tmp_secrets"; else echo "Token = " >> "$tmp_secrets"; fi
        token_set=1
    fi

    if [[ $has_achievements -eq 0 ]]; then
        echo >> "$tmp_secrets"
        echo "[Achievements]" >> "$tmp_secrets"
        if [[ -n "$TOKEN" ]]; then echo "Token = $TOKEN" >> "$tmp_secrets"; else echo "Token = " >> "$tmp_secrets"; fi
    fi

    mv "$tmp_secrets" "$SECRETS_PATH"
else
    cat > "$SECRETS_PATH" <<EOF
[Achievements]
Token = $TOKEN
EOF
fi

echo "RetroAchievements credentials written for user: $USERNAME"
exit 0
