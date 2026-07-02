import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Anti-cheat flag details viewer
 */
import { formatDate } from '@/lib/utils';
export function FlagDetails({ flag, isLoading }) {
    if (isLoading) {
        return (_jsx("div", { className: "space-y-4", children: _jsx("div", { className: "h-48 bg-bg-secondary rounded animate-pulse" }) }));
    }
    if (!flag) {
        return (_jsx("div", { className: "text-center py-12 text-ink-secondary", children: _jsx("p", { children: "No flag to review" }) }));
    }
    const severityColor = {
        low: 'bg-status-healthy/10 text-status-healthy',
        medium: 'bg-status-degraded/10 text-status-degraded',
        high: 'bg-status-offline/10 text-status-offline',
        critical: 'bg-red-500/10 text-red-600',
    };
    return (_jsxs("div", { className: "space-y-6", children: [_jsxs("div", { className: "operator-card space-y-4", children: [_jsxs("div", { className: "flex items-start justify-between", children: [_jsxs("div", { children: [_jsx("h2", { className: "text-2xl font-bold text-ink-primary", children: flag.playerEmail }), _jsxs("p", { className: "text-sm text-ink-secondary mt-1", children: ["Session ", flag.sessionId] })] }), _jsx("span", { className: `inline-block px-3 py-1 rounded text-sm font-medium capitalize ${severityColor[flag.flagSeverity]}`, children: flag.flagSeverity })] }), _jsxs("div", { className: "grid grid-cols-2 gap-4 pt-4 border-t border-panel-border", children: [_jsxs("div", { children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Session Time" }), _jsx("p", { className: "text-sm text-ink-primary mt-1", children: formatDate(flag.sessionTime) })] }), _jsxs("div", { children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Flag Reason" }), _jsx("p", { className: "text-sm text-ink-primary mt-1", children: flag.flagReason })] })] })] }), _jsxs("div", { className: "operator-card space-y-4", children: [_jsx("h3", { className: "font-semibold text-ink-primary", children: "Telemetry Analysis" }), _jsxs("div", { className: "grid grid-cols-1 md:grid-cols-3 gap-4", children: [_jsxs("div", { className: "bg-bg-secondary rounded p-3", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Avg Response Time" }), _jsxs("p", { className: "text-lg font-bold text-ink-primary mt-1", children: [Math.round(flag.telemetryData.avgResponseTime), "ms"] })] }), _jsxs("div", { className: "bg-bg-secondary rounded p-3", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Response Variance" }), _jsxs("p", { className: "text-lg font-bold text-ink-primary mt-1", children: ["\u00B1", Math.round(flag.telemetryData.responseTimeVariance), "ms"] })] }), _jsxs("div", { className: "bg-bg-secondary rounded p-3", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Accuracy Rate" }), _jsxs("p", { className: "text-lg font-bold text-status-healthy mt-1", children: [Math.round(flag.telemetryData.accuracyRate), "%"] })] })] }), _jsxs("div", { className: "pt-4 border-t border-panel-border", children: [_jsx("p", { className: "text-sm font-medium text-ink-primary mb-2", children: "Suspicious Patterns" }), _jsx("ul", { className: "space-y-1", children: flag.telemetryData.suspiciousPatterns.map((pattern, i) => (_jsxs("li", { className: "text-sm text-ink-secondary flex gap-2", children: [_jsx("span", { className: "text-status-offline", children: "\u26A0" }), _jsx("span", { children: pattern })] }, i))) })] })] })] }));
}
