/**
 * API configuration - switches between real and mock APIs
 */

// Check if mock mode is enabled via environment variable
const isMockMode = () => {
  if (typeof window !== 'undefined') {
    // Client-side: check localStorage
    return localStorage.getItem('MOCK_API_MODE') === 'true'
  }
  // Server-side: check env var
  return import.meta.env.VITE_MOCK_API_MODE === 'true'
}

export function getMockMode(): boolean {
  return isMockMode()
}

export function setMockMode(enabled: boolean): void {
  if (enabled) {
    localStorage.setItem('MOCK_API_MODE', 'true')
    console.log('✅ Mock API Mode enabled. Refresh the page to apply.')
  } else {
    localStorage.removeItem('MOCK_API_MODE')
    console.log('✅ Real API Mode enabled. Refresh the page to apply.')
  }
}

export function showMockBanner(): boolean {
  return isMockMode()
}
