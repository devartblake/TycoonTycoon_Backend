import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
const STATUS_CONFIG = {
    healthy: {
        color: 'bg-status-healthy/10 border-status-healthy text-status-healthy',
        icon: '✓',
        label: 'Healthy',
    },
    degraded: {
        color: 'bg-status-degraded/10 border-status-degraded text-status-degraded',
        icon: '!',
        label: 'Degraded',
    },
    offline: {
        color: 'bg-status-offline/10 border-status-offline text-status-offline',
        icon: '✕',
        label: 'Offline',
    },
};
export function ServiceCard({ service, sparklineData }) {
    const config = STATUS_CONFIG[service.status];
    return (_jsxs("div", { className: `operator-card border-l-4 ${config.color}`, children: [_jsxs("div", { className: "flex items-start justify-between mb-3", children: [_jsxs("div", { children: [_jsx("h3", { className: "font-semibold text-ink-primary", children: service.displayName }), _jsx("p", { className: "text-xs text-ink-tertiary mt-1", children: service.description })] }), _jsx("span", { className: `inline-flex items-center justify-center w-8 h-8 rounded font-bold text-sm ${config.color}`, children: config.icon })] }), _jsxs("div", { className: "grid grid-cols-3 gap-2 mt-4 pt-4 border-t border-panel-border", children: [_jsxs("div", { children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Uptime" }), _jsxs("p", { className: "text-lg font-bold text-ink-primary mt-1", children: [service.uptime.toFixed(2), "%"] })] }), _jsxs("div", { children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Response" }), _jsxs("p", { className: "text-lg font-bold text-ink-primary mt-1", children: [service.responseTime, "ms"] })] }), _jsxs("div", { children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Status" }), _jsx("p", { className: "text-sm font-medium mt-1", children: config.label })] })] }), sparklineData && sparklineData.length > 0 && (_jsxs("div", { className: "mt-4 pt-4 border-t border-panel-border", children: [_jsx("p", { className: "text-xs text-ink-tertiary mb-2", children: "24h Trend" }), _jsxs("svg", { className: "w-full h-12", viewBox: "0 0 100 40", preserveAspectRatio: "none", children: [_jsx("defs", { children: _jsxs("linearGradient", { id: `gradient-${service.id}`, x1: "0%", y1: "0%", x2: "0%", y2: "100%", children: [_jsx("stop", { offset: "0%", stopColor: service.status === 'healthy' ? '#10b981' : service.status === 'degraded' ? '#f59e0b' : '#ef4444', stopOpacity: "0.3" }), _jsx("stop", { offset: "100%", stopColor: service.status === 'healthy' ? '#10b981' : service.status === 'degraded' ? '#f59e0b' : '#ef4444', stopOpacity: "0" })] }) }), _jsx("path", { d: `M 0 ${40 - (sparklineData[0] || 0) * 0.4} ${sparklineData
                                    .map((d, i) => `L ${(i / (sparklineData.length - 1)) * 100} ${40 - d * 0.4}`)
                                    .join(' ')} L 100 40 L 0 40 Z`, fill: `url(#gradient-${service.id})` }), _jsx("polyline", { points: sparklineData.map((d, i) => `${(i / (sparklineData.length - 1)) * 100},${40 - d * 0.4}`).join(' '), fill: "none", stroke: service.status === 'healthy' ? '#10b981' : service.status === 'degraded' ? '#f59e0b' : '#ef4444', strokeWidth: "1.5" })] })] })), _jsxs("p", { className: "text-xs text-ink-tertiary mt-4 pt-4 border-t border-panel-border", children: ["Last checked: ", new Date(service.lastCheckedAt).toLocaleTimeString()] })] }));
}
