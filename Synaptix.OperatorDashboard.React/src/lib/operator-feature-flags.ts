/**
 * Operator dashboard feature flags.
 *
 * Installer and probe-based diagnostics call admin routes that do not exist on
 * Synaptix.Backend.Api. Keep them off by default; enable only when backend
 * surfaces are implemented (or for local UI work).
 *
 * Env (build-time):
 *   VITE_ENABLE_INSTALLER=true
 *   VITE_ENABLE_DIAGNOSTICS=true
 *
 * Runtime override (dev tools):
 *   localStorage.setItem('OP_ENABLE_INSTALLER', 'true')
 *   localStorage.setItem('OP_ENABLE_DIAGNOSTICS', 'true')
 */

function envFlag(name: string, defaultValue = false): boolean {
  const raw = import.meta.env[name]
  if (raw === undefined || raw === '') return defaultValue
  return raw === 'true' || raw === '1'
}

function storageFlag(key: string): boolean | null {
  try {
    if (typeof localStorage === 'undefined') return null
    const v = localStorage.getItem(key)
    if (v === null) return null
    return v === 'true' || v === '1'
  } catch {
    return null
  }
}

/** Backend installer UI (/settings/setup) — prefer Setup CLI. */
export function isInstallerEnabled(): boolean {
  const fromStorage = storageFlag('OP_ENABLE_INSTALLER')
  if (fromStorage !== null) return fromStorage
  return envFlag('VITE_ENABLE_INSTALLER', false)
}

/** Probe/log diagnostics UI (/diagnostics) — use /health* and Grafana instead. */
export function isDiagnosticsEnabled(): boolean {
  const fromStorage = storageFlag('OP_ENABLE_DIAGNOSTICS')
  if (fromStorage !== null) return fromStorage
  return envFlag('VITE_ENABLE_DIAGNOSTICS', false)
}

export function getOperatorFeatureFlags() {
  return {
    installer: isInstallerEnabled(),
    diagnostics: isDiagnosticsEnabled(),
  }
}
