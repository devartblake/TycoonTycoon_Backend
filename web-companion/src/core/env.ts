/**
 * Environment configuration for Trivia Tycoon Web Companion
 * Mirrors lib/core/env.dart from Flutter app
 */

export const env = {
  apiUrl: import.meta.env.VITE_API_URL || 'http://localhost:5000',
  wsUrl: import.meta.env.VITE_WS_URL || 'ws://localhost:5000',
  signalrUrl: import.meta.env.VITE_SIGNALR_URL || 'http://localhost:5000/hubs',
  stripePublishableKey: import.meta.env.VITE_STRIPE_KEY || 'pk_test_placeholder',
  complianceUrl: import.meta.env.VITE_COMPLIANCE_URL || 'http://localhost:3000/compliance',
  googleClientId: import.meta.env.VITE_GOOGLE_CLIENT_ID || '',
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
