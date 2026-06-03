#!/usr/bin/env bash
# InsertPlay - RetroArch RetroAchievements Post-Launch Script (Linux)
# Clears RA credentials from RetroArch config ONLY if they were set by InsertPlay.

set -euo pipefail

USERNAME="${INSERTPLAY_RA_USERNAME:-}"
CARD_PATH="${INSERTPLAY_CARD_PATH:-}"

if [[ -z "$USERNAME" ]]; then
    exit 0
fi

CFG_PATH="$CARD_PATH/retroarch/retroarch.cfg"

if [[ ! -f "$CFG_PATH" ]]; then
    exit 0
fi

# Read the username currently stored in the config
stored_username=""
while IFS= read -r line; do
    if [[ "$line" =~ ^cheevos_username[[:space:]]*=[[:space:]]*\"(.*)\"$ ]]; then
        stored_username="${BASH_REMATCH[1]}"
    fi
done < "$CFG_PATH"

if [[ "$stored_username" != "$USERNAME" ]]; then
    echo "Stored username '$stored_username' does not match InsertPlay user '$USERNAME'. Skipping cleanup."
    exit 0
fi

tmp_file=$(mktemp)
while IFS= read -r line; do
    if   [[ "$line" =~ ^cheevos_username[[:space:]]*= ]]; then echo 'cheevos_username = ""'
    elif [[ "$line" =~ ^cheevos_password[[:space:]]*= ]]; then echo 'cheevos_password = ""'
    elif [[ "$line" =~ ^cheevos_token[[:space:]]*=    ]]; then echo 'cheevos_token = ""'
    elif [[ "$line" =~ ^cheevos_enable[[:space:]]*=   ]]; then echo 'cheevos_enable = "false"'
    else echo "$line"
    fi
done < "$CFG_PATH" > "$tmp_file"

mv "$tmp_file" "$CFG_PATH"
echo "RetroAchievements credentials cleared for user: $USERNAME"
exit 0
