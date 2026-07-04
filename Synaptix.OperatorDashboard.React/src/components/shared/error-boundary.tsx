import React, { ReactNode, ErrorInfo } from 'react'
import { Sentry } from '@/lib/sentry-mock'

interface Props {
  children: ReactNode
  fallback?: ReactNode
  onError?: (error: Error, errorInfo: ErrorInfo) => void
}

interface State {
  hasError: boolean
  error: Error | null
}

class ErrorBoundary extends React.Component<Props, State> {
  constructor(props: Props) {
    super(props)
    this.state = { hasError: false, error: null }
  }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error }
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    Sentry.captureException(error, { contexts: { react: errorInfo } })
    this.props.onError?.(error, errorInfo)
  }

  reset = () => {
    this.setState({ hasError: false, error: null })
  }

  render() {
    if (this.state.hasError) {
      if (this.props.fallback) {
        return this.props.fallback
      }

      return (
        <div className="operator-container space-y-4">
          <div className="operator-card bg-status-offline/5 border border-status-offline/30">
            <div className="p-6">
              <h2 className="text-xl font-bold text-status-offline mb-2">Something went wrong</h2>
              <p className="text-ink-secondary text-sm mb-4">
                {this.state.error?.message || 'An unexpected error occurred'}
              </p>
              {process.env.NODE_ENV === 'development' && (
                <pre className="bg-panel p-3 rounded text-xs text-ink-tertiary overflow-x-auto mb-4">
                  {this.state.error?.stack}
                </pre>
              )}
              <button
                onClick={this.reset}
                className="px-4 py-2 bg-accent text-white rounded hover:bg-accent-dark"
              >
                Try Again
              </button>
            </div>
          </div>
        </div>
      )
    }

    return this.props.children
  }
}

export default ErrorBoundary
