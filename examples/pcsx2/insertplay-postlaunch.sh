#!/usr/bin/env bash
# InsertPlay - PCSX2 RetroAchievements Post-Launch Script (Linux)
# Clears RA credentials from PCSX2 config ONLY if they were set by InsertPlay.

set -euo pipefail

USERNAME="${INSERTPLAY_RA_USERNAME:-}"
CARD_PATH="${INSERTPLAY_CARD_PATH:-}"

if [[ -z "$USERNAME" ]]; then
    exit 0
fi

INI_PATH="$CARD_PATH/pcsx2/inis/PCSX2.ini"
SECRETS_PATH="$CARD_PATH/pcsx2/inis/secrets.ini"

if [[ ! -f "$INI_PATH" ]]; then
    exit 0
fi

# Read the username currently stored in the config
in_section=0
stored_username=""
while IFS= read -r line; do
    if [[ "$line" =~ ^\[Achievements\] ]]; then
        in_section=1
    elif [[ "$line" =~ ^\[ ]]; then
        in_section=0
    fi
    if [[ $in_section -eq 1 && "$line" =~ ^[[:space:]]*Username[[:space:]]*=[[:space:]]*(.*)$ ]]; then
        stored_username="${BASH_REMATCH[1]}"
    fi
done < "$INI_PATH"

if [[ "$stored_username" != "$USERNAME" ]]; then
    echo "Stored username '$stored_username' does not match InsertPlay user '$USERNAME'. Skipping cleanup."
    exit 0
fi

in_achievements=0
in_autoupdater=0
tmp_file=$(mktemp)

while IFS= read -r line; do
    if [[ "$line" =~ ^\[(.+)\]$ ]]; then
        section_name="${BASH_REMATCH[1]}"
        in_achievements=0
        in_autoupdater=0
        if [[ "$section_name" == "Achievements" ]]; then
            in_achievements=1
        elif [[ "$section_name" == "AutoUpdater" ]]; then
            in_autoupdater=1
        fi
        echo "$line" >> "$tmp_file"
        continue
    fi

    if [[ $in_achievements -eq 1 ]]; then
        if   [[ "$line" =~ ^[[:space:]]*Enabled[[:space:]]*= ]]; then echo "Enabled = false" >> "$tmp_file"
        elif [[ "$line" =~ ^[[:space:]]*Username[[:space:]]*= ]]; then echo "Username = " >> "$tmp_file"
        elif [[ "$line" =~ ^[[:space:]]*LoginTimestamp[[:space:]]*= ]]; then echo "LoginTimestamp = " >> "$tmp_file"
        else echo "$line" >> "$tmp_file"
        fi
        continue
    fi

    if [[ $in_autoupdater -eq 1 ]]; then
        if [[ "$line" =~ ^[[:space:]]*Password[[:space:]]*= ]]; then
            echo "Password = " >> "$tmp_file"
        else
            echo "$line" >> "$tmp_file"
        fi
        continue
    fi

    echo "$line" >> "$tmp_file"
done < "$INI_PATH"

mv "$tmp_file" "$INI_PATH"

if [[ -f "$SECRETS_PATH" ]]; then
    in_achievements=0
    tmp_secrets=$(mktemp)
    while IFS= read -r line; do
        if [[ "$line" =~ ^\[(.+)\]$ ]]; then
            section_name="${BASH_REMATCH[1]}"
            in_achievements=0
            if [[ "$section_name" == "Achievements" ]]; then
                in_achievements=1
            fi
            echo "$line" >> "$tmp_secrets"
            continue
        fi

        if [[ $in_achievements -eq 1 && "$line" =~ ^[[:space:]]*Token[[:space:]]*= ]]; then
            echo "Token = " >> "$tmp_secrets"
        else
            echo "$line" >> "$tmp_secrets"
        fi
    done < "$SECRETS_PATH"
    mv "$tmp_secrets" "$SECRETS_PATH"
fi

echo "RetroAchievements credentials cleared for user: $USERNAME"
exit 0
