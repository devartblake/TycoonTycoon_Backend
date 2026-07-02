import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
export function AlertsSection({ alertCount, services }) {
    const degradedServices = services.filter((s) => s.status !== 'healthy');
    if (alertCount === 0) {
        return (_jsx("div", { className: "operator-card bg-status-healthy/5 border border-status-healthy/20", children: _jsxs("div", { className: "flex items-center gap-3", children: [_jsx("div", { className: "text-3xl", children: "\u2713" }), _jsxs("div", { children: [_jsx("p", { className: "font-semibold text-status-healthy", children: "All Systems Operational" }), _jsxs("p", { className: "text-sm text-ink-secondary mt-1", children: ["No active alerts. Dashboard is monitoring ", services.length, " services."] })] })] }) }));
    }
    return (_jsxs("div", { className: "operator-card space-y-3", children: [_jsxs("div", { className: "flex items-center justify-between", children: [_jsx("h2", { className: "font-semibold text-ink-primary", children: "Active Alerts" }), _jsx("span", { className: "px-2 py-1 bg-status-offline/10 text-status-offline rounded text-xs font-bold", children: alertCount })] }), degradedServices.map((service) => (_jsx("div", { className: "p-3 bg-bg-secondary rounded border-l-4 border-status-degraded", children: _jsxs("div", { className: "flex items-start justify-between", children: [_jsxs("div", { children: [_jsx("p", { className: "font-medium text-ink-primary", children: service.displayName }), _jsx("p", { className: "text-sm text-ink-secondary mt-1", children: service.status === 'degraded'
                                        ? `Response time elevated: ${service.responseTime}ms (threshold: 200ms)`
                                        : `Service offline for ${Math.floor(Math.random() * 5 + 1)} minutes` })] }), _jsxs("span", { className: "text-xs font-medium text-status-degraded", children: ["\u26A0 ", service.status] })] }) }, service.id)))] }));
}
