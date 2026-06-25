/**
 * Environment configuration for Trivia Tycoon Web Companion
 * Mirrors lib/core/env.dart from Flutter app
 *
 * Pattern: API_BASE_URL is the raw backend URL (e.g. http://localhost:5000)
 * All API calls use apiV1Url which automatically appends /api/v1
 * This matches the Flutter client's approach for consistency across platforms
 */

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000';

// In development, use relative paths to leverage Vite proxy
// In production, use the full URL from environment
const apiV1Url = import.meta.env.DEV ? '/api/v1' : `${apiBaseUrl}/api/v1`;

export const env = {
  // Base API URL (without /api/v1 path)
  apiBaseUrl,
  // Full API URL with /api/v1 (used by API client)
  apiV1Url,
  // WebSocket and real-time URLs
  wsUrl: import.meta.env.VITE_WS_URL || 'ws://localhost:5000/ws',
  signalrUrl: import.meta.env.VITE_SIGNALR_URL || 'http://localhost:5000/hubs',
  // Third-party services
  stripePublishableKey: import.meta.env.VITE_STRIPE_KEY || 'pk_test_placeholder',
  complianceUrl: import.meta.env.VITE_COMPLIANCE_URL || 'http://localhost:3000/compliance',
  googleClientId: import.meta.env.VITE_GOOGLE_CLIENT_ID || '',
  // App metadata
  appVersion: import.meta.env.VITE_APP_VERSION || '1.0.0-dev',
  isDev: import.meta.env.DEV,
  isProd: import.meta.env.PROD,
} as const;

// Validate critical environment variables
if (import.meta.env.PROD && !env.stripePublishableKey.startsWith('pk_')) {
  console.warn('⚠️ Stripe key may not be properly configured for production');
}

if (!env.googleClientId && import.meta.env.PROD) {
  console.warn('⚠️ Google Client ID is not configured for production');
}

export default env;
