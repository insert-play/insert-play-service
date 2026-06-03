#!/usr/bin/env bash
# InsertPlay - RetroArch RetroAchievements Pre-Launch Script (Linux)
# Injects RA credentials into the RetroArch portable config on the SD card.

set -euo pipefail

USERNAME="${INSERTPLAY_RA_USERNAME:-}"
PASSWORD="${INSERTPLAY_RA_PASSWORD:-}"
CARD_PATH="${INSERTPLAY_CARD_PATH:-}"

if [[ -z "$USERNAME" || -z "$PASSWORD" ]]; then
    echo "No RetroAchievements credentials provided. Skipping RA configuration."
    exit 0
fi

CFG_PATH="$CARD_PATH/retroarch/retroarch.cfg"

if [[ ! -f "$CFG_PATH" ]]; then
    echo "retroarch.cfg not found at: $CFG_PATH"
    exit 1
fi

username_set=0
password_set=0
enable_set=0
tmp_file=$(mktemp)

while IFS= read -r line; do
    if   [[ "$line" =~ ^cheevos_username[[:space:]]*= ]]; then
        echo "cheevos_username = \"$USERNAME\""; username_set=1
    elif [[ "$line" =~ ^cheevos_password[[:space:]]*= ]]; then
        echo "cheevos_password = \"$PASSWORD\"";  password_set=1
    elif [[ "$line" =~ ^cheevos_token[[:space:]]*= ]]; then
        echo 'cheevos_token = ""'
    elif [[ "$line" =~ ^cheevos_enable[[:space:]]*= ]]; then
        echo 'cheevos_enable = "true"';           enable_set=1
    else
        echo "$line"
    fi
done < "$CFG_PATH" > "$tmp_file"

# Append any keys that were not already present in the file
[[ $username_set -eq 0 ]] && echo "cheevos_username = \"$USERNAME\""  >> "$tmp_file"
[[ $password_set -eq 0 ]] && echo "cheevos_password = \"$PASSWORD\""   >> "$tmp_file"
[[ $enable_set   -eq 0 ]] && echo 'cheevos_enable = "true"'           >> "$tmp_file"

mv "$tmp_file" "$CFG_PATH"
echo "RetroAchievements credentials written for user: $USERNAME"
exit 0
