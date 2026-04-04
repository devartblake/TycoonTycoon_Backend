#!/usr/bin/env bash
set -euo pipefail

# Installs .NET SDK locally (no sudo) for this workspace/user.
# Default channel is LTS (8.0 as of 2026-04-03); override with DOTNET_CHANNEL.

DOTNET_CHANNEL="${DOTNET_CHANNEL:-8.0}"
DOTNET_INSTALL_DIR="${DOTNET_INSTALL_DIR:-$HOME/.dotnet}"
DOTNET_INSTALL_SCRIPT_URL_PRIMARY="https://dot.net/v1/dotnet-install.sh"
DOTNET_INSTALL_SCRIPT_URL_FALLBACK="https://builds.dotnet.microsoft.com/dotnet/scripts/v1/dotnet-install.sh"
DOTNET_INSTALL_SCRIPT_PATH="${DOTNET_INSTALL_SCRIPT_PATH:-}"

echo "[dotnet-bootstrap] channel: $DOTNET_CHANNEL"
echo "[dotnet-bootstrap] install dir: $DOTNET_INSTALL_DIR"

mkdir -p "$DOTNET_INSTALL_DIR"

tmp_script="$(mktemp)"
trap 'rm -f "$tmp_script"' EXIT

if [[ -n "$DOTNET_INSTALL_SCRIPT_PATH" ]]; then
  echo "[dotnet-bootstrap] Using local installer script: $DOTNET_INSTALL_SCRIPT_PATH"
  cp "$DOTNET_INSTALL_SCRIPT_PATH" "$tmp_script"
else
  if ! curl -fsSL "$DOTNET_INSTALL_SCRIPT_URL_PRIMARY" -o "$tmp_script"; then
    echo "[dotnet-bootstrap] Primary installer URL failed, trying fallback..."
    if ! curl -fsSL "$DOTNET_INSTALL_SCRIPT_URL_FALLBACK" -o "$tmp_script"; then
      echo "[dotnet-bootstrap] ERROR: Unable to download dotnet-install.sh from both URLs." >&2
      echo "[dotnet-bootstrap] Options:" >&2
      echo "  1) Provide local script path: DOTNET_INSTALL_SCRIPT_PATH=/path/to/dotnet-install.sh ./scripts/bootstrap-dotnet.sh" >&2
      echo "  2) Enable outbound access to dot.net and builds.dotnet.microsoft.com" >&2
      echo "  3) Use a prebuilt image/runner that already contains .NET SDK $DOTNET_CHANNEL" >&2
      exit 1
    fi
  fi
fi
bash "$tmp_script" --channel "$DOTNET_CHANNEL" --install-dir "$DOTNET_INSTALL_DIR"

export PATH="$DOTNET_INSTALL_DIR:$PATH"
export DOTNET_ROOT="$DOTNET_INSTALL_DIR"

echo
echo "[dotnet-bootstrap] Installed SDK(s):"
"$DOTNET_INSTALL_DIR/dotnet" --list-sdks

echo
echo "[dotnet-bootstrap] Add this to your shell session before build/test:"
echo "  export DOTNET_ROOT=\"$DOTNET_INSTALL_DIR\""
echo "  export PATH=\"$DOTNET_INSTALL_DIR:\$PATH\""
