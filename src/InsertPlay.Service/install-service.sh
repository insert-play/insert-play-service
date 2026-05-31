#!/usr/bin/env bash
# install-service.sh — Installs InsertPlay as a systemd system service.
#
# Usage:
#   sudo ./install-service.sh [username]
#
# Arguments:
#   username  (optional) The OS user account under which the service will run.
#             Defaults to the user who invoked sudo ($SUDO_USER), or the
#             logged-in user reported by logname(1).
#
# The script installs the service to run in-place from the directory where this
# script lives, so place it alongside the InsertPlay.Service binary before running.

set -euo pipefail

INSTALL_DIR="$(cd "$(dirname "$0")" && pwd)"
EXEC_PATH="$INSTALL_DIR/InsertPlay.Service"
SERVICE_NAME="insertplay"
SERVICE_FILE="/etc/systemd/system/${SERVICE_NAME}.service"

# ---------------------------------------------------------------------------
# Prerequisite checks
# ---------------------------------------------------------------------------

if [[ $EUID -ne 0 ]]; then
    echo "Error: This script must be run as root. Use: sudo $0 [username]" >&2
    exit 1
fi

if [[ ! -f "$EXEC_PATH" ]]; then
    echo "Error: Executable not found at '$EXEC_PATH'." >&2
    echo "       Make sure this script is in the same directory as InsertPlay.Service." >&2
    exit 1
fi

# Resolve the target user (argument → $SUDO_USER → logname)
SERVICE_USER="${1:-${SUDO_USER:-$(logname 2>/dev/null || true)}}"

if [[ -z "$SERVICE_USER" ]]; then
    echo "Error: Could not determine the target user." >&2
    echo "       Pass a username as the first argument: sudo $0 <username>" >&2
    exit 1
fi

echo "Installing InsertPlay service..."
echo "  Install dir  : $INSTALL_DIR"
echo "  Run as user  : $SERVICE_USER"

# ---------------------------------------------------------------------------
# Make the binary executable
# ---------------------------------------------------------------------------

chmod +x "$EXEC_PATH"

# ---------------------------------------------------------------------------
# Write the systemd unit file
# ---------------------------------------------------------------------------

cat > "$SERVICE_FILE" <<EOF
[Unit]
Description=InsertPlay — SD game card auto-launcher
After=network.target

[Service]
Type=simple
ExecStart=$EXEC_PATH
Restart=on-failure
RestartSec=5
User=$SERVICE_USER

[Install]
WantedBy=multi-user.target
EOF

echo "  Service file : $SERVICE_FILE"

# ---------------------------------------------------------------------------
# Enable and start the service
# ---------------------------------------------------------------------------

systemctl daemon-reload
systemctl enable "${SERVICE_NAME}.service"
systemctl start  "${SERVICE_NAME}.service"

echo ""
echo "InsertPlay service installed and started successfully."
echo ""
echo "Useful commands:"
echo "  Status  : systemctl status $SERVICE_NAME"
echo "  Logs    : journalctl -u $SERVICE_NAME -f"
echo "  Stop    : systemctl stop $SERVICE_NAME"
echo "  Disable : systemctl disable $SERVICE_NAME"
