#!/usr/bin/env bash
# InsertPlay pre-launch script — Need for Speed: Underground (Linux/SteamOS)
# Adjusts resolution in NFSUnderground.WidescreenFix.ini before the game starts.
#
# Environment variables set by InsertPlay:
#   INSERTPLAY_RESOLUTION  — e.g. "1920x1080" or "native"
#   INSERTPLAY_CARD_PATH   — root of the SD card (e.g. /run/media/user/NFSU)

set -euo pipefail

INI_PATH="${INSERTPLAY_CARD_PATH}/scripts/NFSUnderground.WidescreenFix.ini"

if [[ ! -f "$INI_PATH" ]]; then
    echo "ERROR: WidescreenFix INI not found at: $INI_PATH" >&2
    exit 1
fi

RESOLUTION="${INSERTPLAY_RESOLUTION:-native}"

if [[ "$RESOLUTION" == "native" ]]; then
    # Try to detect screen resolution using xrandr (X11) or wlr-randr (Wayland)
    if command -v xrandr &>/dev/null; then
        NATIVE=$(xrandr --current 2>/dev/null \
            | awk '/\*/{match($0, /([0-9]+)x([0-9]+)/, a); if (a[1]) {print a[1] "x" a[2]; exit}}')
    elif command -v wlr-randr &>/dev/null; then
        NATIVE=$(wlr-randr 2>/dev/null \
            | awk '/current/{match($0, /([0-9]+)x([0-9]+)/, a); if (a[1]) {print a[1] "x" a[2]; exit}}')
    fi

    if [[ -z "${NATIVE:-}" ]]; then
        echo "WARNING: Could not detect native resolution. Leaving INI unchanged." >&2
        exit 0
    fi

    RESOLUTION="$NATIVE"
    echo "Using native resolution: $RESOLUTION"
fi

# Validate WIDTHxHEIGHT format
if [[ ! "$RESOLUTION" =~ ^([0-9]+)x([0-9]+)$ ]]; then
    echo "ERROR: Invalid resolution format '$RESOLUTION'. Expected WIDTHxHEIGHT (e.g. 1920x1080)." >&2
    exit 1
fi

RES_X="${BASH_REMATCH[1]}"
RES_Y="${BASH_REMATCH[2]}"

echo "Using resolution: ${RES_X}x${RES_Y}"

# Update ResX and ResY in place, preserving comments and formatting
sed -i "s/^\(ResX\s*=\s*\)[0-9]*/\1${RES_X}/" "$INI_PATH"
sed -i "s/^\(ResY\s*=\s*\)[0-9]*/\1${RES_Y}/" "$INI_PATH"

echo "Resolution set to ${RES_X}x${RES_Y} in $INI_PATH"
