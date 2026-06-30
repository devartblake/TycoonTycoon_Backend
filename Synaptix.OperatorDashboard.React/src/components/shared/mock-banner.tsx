/**
 * Mock mode banner - displays when using mock API
 */

import { showMockBanner, setMockMode } from '@/lib/api-config'

export function MockBanner() {
  if (!showMockBanner()) {
    return null
  }

  return (
    <div className="fixed top-0 left-0 right-0 z-40 bg-yellow-500/20 border-b border-yellow-500/50 px-4 py-2 flex items-center justify-between">
      <div className="text-sm font-medium text-yellow-900">
        🎭 <strong>MOCK API MODE</strong> — Using simulated data (no backend connection required)
      </div>
      <button
        onClick={() => setMockMode(false)}
        className="text-xs px-2 py-1 hover:bg-yellow-500/30 rounded transition-colors"
        title="Switch to real API"
      >
        ✕ Disable Mock Mode
      </button>
    </div>
  )
}
