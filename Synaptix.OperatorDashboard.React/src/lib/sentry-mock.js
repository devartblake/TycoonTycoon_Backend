// Mock Sentry for development - production would use real Sentry SDK
export const Sentry = {
    captureException: (error, context) => {
        if (process.env.NODE_ENV === 'development') {
            console.error('Sentry Error:', error, context);
        }
    },
    withErrorBoundary: (Component) => Component,
};
