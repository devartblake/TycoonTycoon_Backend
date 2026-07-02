import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
export function SystemMetrics({ metrics, isLoading }) {
    if (isLoading || !metrics) {
        return (_jsx("div", { className: "grid grid-cols-2 md:grid-cols-4 gap-4", children: [...Array(4)].map((_, i) => (_jsx("div", { className: "operator-card h-24 bg-bg-secondary animate-pulse" }, i))) }));
    }
    const getResourceColor = (usage) => {
        if (usage < 50)
            return 'text-status-healthy';
        if (usage < 75)
            return 'text-status-degraded';
        return 'text-status-offline';
    };
    const getResourceBgColor = (usage) => {
        if (usage < 50)
            return 'bg-status-healthy/10';
        if (usage < 75)
            return 'bg-status-degraded/10';
        return 'bg-status-offline/10';
    };
    return (_jsxs("div", { className: "grid grid-cols-2 md:grid-cols-4 gap-4", children: [_jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "API Requests (1h)" }), _jsxs("p", { className: "text-2xl font-bold text-accent mt-2", children: [(metrics.apiGatewayRequests / 1000).toFixed(1), "k"] }), _jsx("p", { className: "text-xs text-ink-secondary mt-1", children: "requests processed" })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Active Connections" }), _jsx("p", { className: "text-2xl font-bold text-accent mt-2", children: metrics.activeConnections }), _jsx("p", { className: "text-xs text-ink-secondary mt-1", children: "clients connected" })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "CPU Usage" }), _jsxs("div", { className: "flex items-end gap-2 mt-2", children: [_jsxs("p", { className: `text-2xl font-bold ${getResourceColor(metrics.cpuUsage)}`, children: [metrics.cpuUsage.toFixed(1), "%"] }), _jsx("div", { className: "flex-1 h-8 bg-bg-secondary rounded overflow-hidden", children: _jsx("div", { className: `h-full ${getResourceBgColor(metrics.cpuUsage)}`, style: { width: `${metrics.cpuUsage}%` } }) })] })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Memory Usage" }), _jsxs("div", { className: "flex items-end gap-2 mt-2", children: [_jsxs("p", { className: `text-2xl font-bold ${getResourceColor(metrics.memoryUsage)}`, children: [metrics.memoryUsage.toFixed(1), "%"] }), _jsx("div", { className: "flex-1 h-8 bg-bg-secondary rounded overflow-hidden", children: _jsx("div", { className: `h-full ${getResourceBgColor(metrics.memoryUsage)}`, style: { width: `${metrics.memoryUsage}%` } }) })] })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Disk Usage" }), _jsxs("div", { className: "flex items-end gap-2 mt-2", children: [_jsxs("p", { className: `text-2xl font-bold ${getResourceColor(metrics.diskUsage)}`, children: [metrics.diskUsage.toFixed(1), "%"] }), _jsx("div", { className: "flex-1 h-8 bg-bg-secondary rounded overflow-hidden", children: _jsx("div", { className: `h-full ${getResourceBgColor(metrics.diskUsage)}`, style: { width: `${metrics.diskUsage}%` } }) })] })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Avg Response Time" }), _jsxs("p", { className: "text-2xl font-bold text-accent mt-2", children: [metrics.avgResponseTime, "ms"] }), _jsx("p", { className: "text-xs text-ink-secondary mt-1", children: "latency" })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Error Rate" }), _jsxs("p", { className: `text-2xl font-bold mt-2 ${getResourceColor(metrics.errorRate)}`, children: [metrics.errorRate.toFixed(2), "%"] }), _jsx("p", { className: "text-xs text-ink-secondary mt-1", children: "failed requests" })] })] }));
}
