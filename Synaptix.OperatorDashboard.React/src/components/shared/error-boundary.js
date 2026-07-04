import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import React from 'react';
import { Sentry } from '@/lib/sentry-mock';
class ErrorBoundary extends React.Component {
    constructor(props) {
        super(props);
        Object.defineProperty(this, "reset", {
            enumerable: true,
            configurable: true,
            writable: true,
            value: () => {
                this.setState({ hasError: false, error: null });
            }
        });
        this.state = { hasError: false, error: null };
    }
    static getDerivedStateFromError(error) {
        return { hasError: true, error };
    }
    componentDidCatch(error, errorInfo) {
        Sentry.captureException(error, { contexts: { react: errorInfo } });
        this.props.onError?.(error, errorInfo);
    }
    render() {
        if (this.state.hasError) {
            if (this.props.fallback) {
                return this.props.fallback;
            }
            return (_jsx("div", { className: "operator-container space-y-4", children: _jsx("div", { className: "operator-card bg-status-offline/5 border border-status-offline/30", children: _jsxs("div", { className: "p-6", children: [_jsx("h2", { className: "text-xl font-bold text-status-offline mb-2", children: "Something went wrong" }), _jsx("p", { className: "text-ink-secondary text-sm mb-4", children: this.state.error?.message || 'An unexpected error occurred' }), process.env.NODE_ENV === 'development' && (_jsx("pre", { className: "bg-panel p-3 rounded text-xs text-ink-tertiary overflow-x-auto mb-4", children: this.state.error?.stack })), _jsx("button", { onClick: this.reset, className: "px-4 py-2 bg-accent text-white rounded hover:bg-accent-dark", children: "Try Again" })] }) }) }));
        }
        return this.props.children;
    }
}
export default ErrorBoundary;
