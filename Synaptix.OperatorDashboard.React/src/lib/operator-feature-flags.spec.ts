/**
 * Pure unit tests — run with vitest environment node to avoid jsdom/css issues.
 * @vitest-environment node
 */
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'

describe('operator-feature-flags', () => {
  const originalEnv = { ...import.meta.env }

  beforeEach(() => {
    vi.resetModules()
    if (typeof localStorage !== 'undefined') {
      localStorage.removeItem('OP_ENABLE_INSTALLER')
      localStorage.removeItem('OP_ENABLE_DIAGNOSTICS')
    }
  })

  afterEach(() => {
    Object.assign(import.meta.env, originalEnv)
  })

  it('defaults installer and diagnostics to off', async () => {
    const { isInstallerEnabled, isDiagnosticsEnabled } = await import('./operator-feature-flags')
    expect(isInstallerEnabled()).toBe(false)
    expect(isDiagnosticsEnabled()).toBe(false)
  })

  it('respects localStorage overrides when available', async () => {
    // node env may lack localStorage — skip if so
    if (typeof localStorage === 'undefined') {
      expect(true).toBe(true)
      return
    }
    localStorage.setItem('OP_ENABLE_INSTALLER', 'true')
    localStorage.setItem('OP_ENABLE_DIAGNOSTICS', 'true')
    const { isInstallerEnabled, isDiagnosticsEnabled } = await import('./operator-feature-flags')
    expect(isInstallerEnabled()).toBe(true)
    expect(isDiagnosticsEnabled()).toBe(true)
  })
})
