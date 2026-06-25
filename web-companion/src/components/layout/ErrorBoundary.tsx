/**
 * Error boundary component - catches React errors and displays fallback UI
 */

import { Component } from 'react';
import type { ReactNode } from 'react';
import { AlertTriangle, RotateCcw, Home } from 'lucide-react';

interface Props {
  children: ReactNode;
  fallback?: (error: Error, reset: () => void) => ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
}

export class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false, error: null };
  }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
    console.error('Error caught by boundary:', error, errorInfo);
  }

  resetError = () => {
    this.setState({ hasError: false, error: null });
  };

  render() {
    if (this.state.hasError && this.state.error) {
      if (this.props.fallback) {
        return this.props.fallback(this.state.error, this.resetError);
      }

      return (
        <div
          className="min-h-screen flex items-center justify-center p-4"
          style={{ backgroundColor: 'var(--color-bg-primary)' }}
        >
          <div className="max-w-md w-full text-center">
            <div className="mb-6">
              <AlertTriangle
                size={64}
                style={{ color: 'var(--color-status-error)', margin: '0 auto' }}
              />
            </div>

            <h1
              className="text-3xl font-bold mb-2"
              style={{ color: 'var(--color-text-primary)' }}
            >
              Oops! Something went wrong
            </h1>

            <p
              className="mb-6 text-sm"
              style={{ color: 'var(--color-text-secondary)' }}
            >
              We encountered an unexpected error. Try refreshing the page or go back home.
            </p>

            <div className="bg-red-900/20 p-4 rounded-lg mb-6 text-left">
              <p
                className="text-xs font-mono break-all"
                style={{ color: 'var(--color-status-error)' }}
              >
                {this.state.error.message}
              </p>
            </div>

            <div className="flex gap-3">
              <button
                onClick={this.resetError}
                className="flex-1 py-2 px-4 rounded-lg font-semibold flex items-center justify-center gap-2 transition-all"
                style={{
                  backgroundColor: 'var(--color-brand-primary)',
                  color: 'white',
                }}
              >
                <RotateCcw size={18} />
                Try Again
              </button>

              <button
                onClick={() => (window.location.href = '/')}
                className="flex-1 py-2 px-4 rounded-lg font-semibold flex items-center justify-center gap-2 transition-all border-2"
                style={{
                  backgroundColor: 'var(--color-bg-secondary)',
                  color: 'var(--color-text-primary)',
                  borderColor: 'var(--color-ui-border)',
                }}
              >
                <Home size={18} />
                Home
              </button>
            </div>

            <p
              className="text-xs mt-6"
              style={{ color: 'var(--color-text-tertiary)' }}
            >
              If this problem persists, please contact support
            </p>
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}

export default ErrorBoundary;
