#!/usr/bin/env bash
# Adds InsertPlay to the current user's XDG autostart directory.
# The application will start automatically on login (GNOME, KDE, XFCE, etc.).
# No root rights required.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
EXECUTABLE="$SCRIPT_DIR/InsertPlay.Service"
AUTOSTART_DIR="${XDG_CONFIG_HOME:-$HOME/.config}/autostart"
DESKTOP_SRC="$SCRIPT_DIR/insertplay.desktop"
DESKTOP_DEST="$AUTOSTART_DIR/insertplay.desktop"

if [[ ! -f "$EXECUTABLE" ]]; then
    echo "Erro: InsertPlay.Service não encontrado em '$EXECUTABLE'." >&2
    echo "Execute este script no mesmo diretório que o executável." >&2
    exit 1
fi

mkdir -p "$AUTOSTART_DIR"

# Write the .desktop file, fixing the Exec path to point to this install.
sed "s|Exec=.*|Exec=$EXECUTABLE|" "$DESKTOP_SRC" > "$DESKTOP_DEST"
chmod +x "$DESKTOP_DEST"

echo ""
echo "InsertPlay adicionado ao inicializar do sistema."
echo "Arquivo criado em: $DESKTOP_DEST"
echo ""
echo "Para remover do inicializar:"
echo "  rm '$DESKTOP_DEST'"
