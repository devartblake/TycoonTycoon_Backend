// Mock Sentry for development - production would use real Sentry SDK
export const Sentry = {
  captureException: (error: Error, context?: any) => {
    if (process.env.NODE_ENV === 'development') {
      console.error('Sentry Error:', error, context)
    }
  },
  withErrorBoundary: (Component: any) => Component,
}
