#!/usr/bin/env bash
set -euo pipefail

# Installs/verifies prerequisites needed for scripts/run-health-pass.sh.
# Supports Linux/macOS shells where curl is available.

DOTNET_CHANNEL="${DOTNET_CHANNEL:-9.0}"
DOTNET_INSTALL_DIR="${DOTNET_INSTALL_DIR:-$HOME/.dotnet}"

say() { echo "[health-pass-prereqs] $*"; }

ensure_dotnet() {
  if command -v dotnet >/dev/null 2>&1; then
    say "dotnet already available: $(dotnet --version)"
    return
  fi

  say "dotnet not found. Installing .NET SDK channel ${DOTNET_CHANNEL} to ${DOTNET_INSTALL_DIR}..."
  local tmp_script
  tmp_script="$(mktemp)"
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o "${tmp_script}"
  bash "${tmp_script}" --channel "${DOTNET_CHANNEL}" --install-dir "${DOTNET_INSTALL_DIR}"
  rm -f "${tmp_script}"

  export PATH="${DOTNET_INSTALL_DIR}:${DOTNET_INSTALL_DIR}/tools:${PATH}"

  if ! command -v dotnet >/dev/null 2>&1; then
    say "ERROR: dotnet installation completed but dotnet is still not on PATH."
    say "Add this to your shell profile, then reopen your shell:"
    say "  export PATH=\"${DOTNET_INSTALL_DIR}:${DOTNET_INSTALL_DIR}/tools:\$PATH\""
    exit 1
  fi

  say "Installed dotnet: $(dotnet --version)"
}

ensure_dotnet_ef() {
  export PATH="${DOTNET_INSTALL_DIR}:${DOTNET_INSTALL_DIR}/tools:${PATH}"

  if command -v dotnet-ef >/dev/null 2>&1; then
    say "dotnet-ef already available."
    return
  fi

  say "Installing dotnet-ef global tool..."
  dotnet tool install --global dotnet-ef >/dev/null
  say "dotnet-ef installed."
}

ensure_docker() {
  if ! command -v docker >/dev/null 2>&1; then
    say "ERROR: docker CLI is not installed."
    say "Install Docker Desktop (macOS/Windows) or docker engine + compose plugin (Linux), then re-run."
    exit 1
  fi

  if ! docker info >/dev/null 2>&1; then
    say "ERROR: docker is installed but daemon is not reachable."
    say "Start Docker Desktop / docker daemon and retry."
    exit 1
  fi

  say "docker is available and daemon is reachable."
}

ensure_dotnet
ensure_dotnet_ef
ensure_docker

say "All health-pass prerequisites are ready."
say "Now run: bash scripts/run-health-pass.sh"
