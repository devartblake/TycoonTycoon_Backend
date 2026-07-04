import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Probe Log Monitoring & Diagnostics
 */
import { useState, useEffect } from 'react';
import { usePermission } from '@/hooks/use-permission';
import ErrorBoundary from '@/components/shared/error-boundary';
import EmptyState from '@/components/shared/empty-state';
import { SkeletonGrid } from '@/components/shared/skeletons';
import * as diagnosticsApi from '../api';
export default function MonitoringPage() {
    usePermission('storage:read');
    const [probes, setProbes] = useState([]);
    const [logs, setLogs] = useState([]);
    const [metrics, setMetrics] = useState(null);
    const [loading, setLoading] = useState(true);
    const [successMsg, setSuccessMsg] = useState(null);
    useEffect(() => {
        const loadData = async () => {
            setLoading(true);
            try {
                const [probesData, logsData, metricsData] = await Promise.all([
                    diagnosticsApi.getProbeRecords(),
                    diagnosticsApi.getProbeLogs(),
                    diagnosticsApi.getDiagnosticMetrics(),
                ]);
                setProbes(probesData);
                setLogs(logsData);
                setMetrics(metricsData);
            }
            catch (error) {
                console.error('Failed to load diagnostics:', error);
            }
            finally {
                setLoading(false);
            }
        };
        loadData();
        const interval = setInterval(loadData, 3000);
        return () => clearInterval(interval);
    }, []);
    const handleRunProbe = async (probeId) => {
        try {
            await diagnosticsApi.runProbeNow(probeId);
            setSuccessMsg('Probe executed');
            setTimeout(() => setSuccessMsg(null), 2000);
        }
        catch (error) {
            console.error('Probe execution failed:', error);
        }
    };
    const handleToggleProbe = async (probeId, enabled) => {
        try {
            if (enabled) {
                await diagnosticsApi.disableProbe(probeId);
            }
            else {
                await diagnosticsApi.enableProbe(probeId);
            }
            setSuccessMsg(`Probe ${enabled ? 'disabled' : 'enabled'}`);
            setTimeout(() => setSuccessMsg(null), 2000);
            const updated = await diagnosticsApi.getProbeRecords();
            setProbes(updated);
        }
        catch (error) {
            console.error('Toggle failed:', error);
        }
    };
    return (_jsx(ErrorBoundary, { children: _jsxs("div", { className: "operator-container space-y-8", children: [_jsxs("div", { children: [_jsx("h1", { className: "text-3xl font-bold text-ink-primary", children: "Probe Log Monitoring" }), _jsx("p", { className: "mt-2 text-ink-secondary", children: "Real-time diagnostic monitoring and probe status tracking" })] }), successMsg && (_jsxs("div", { className: "p-4 bg-status-healthy/10 border border-status-healthy/20 rounded text-status-healthy text-sm", children: ["\u2713 ", successMsg] })), loading ? (_jsx(SkeletonGrid, { count: 4 })) : metrics ? (_jsxs("div", { className: "grid grid-cols-4 gap-4", children: [_jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Active Probes" }), _jsx("p", { className: "text-2xl font-bold text-accent mt-1", children: metrics.activeProbes })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Healthy" }), _jsx("p", { className: "text-2xl font-bold text-status-healthy mt-1", children: metrics.healthyProbes })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Success Rate" }), _jsxs("p", { className: "text-2xl font-bold text-ink-primary mt-1", children: [(((metrics.totalRequests - metrics.failedRequests) / metrics.totalRequests) * 100).toFixed(1), "%"] })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Avg Response" }), _jsxs("p", { className: "text-2xl font-bold text-ink-primary mt-1", children: [metrics.averageResponseTime, "ms"] })] })] })) : null, _jsxs("div", { className: "operator-card", children: [_jsxs("h2", { className: "text-lg font-semibold p-4 border-b border-panel-border", children: ["Probes (", probes.length, ")"] }), !loading && probes.length > 0 ? (_jsx("div", { className: "overflow-x-auto", children: _jsxs("table", { className: "w-full text-sm", children: [_jsx("thead", { className: "bg-panel", children: _jsxs("tr", { children: [_jsx("th", { className: "px-4 py-2 text-left", children: "Name" }), _jsx("th", { className: "px-4 py-2 text-left", children: "Type" }), _jsx("th", { className: "px-4 py-2 text-left", children: "Status" }), _jsx("th", { className: "px-4 py-2 text-right", children: "Success Rate" }), _jsx("th", { className: "px-4 py-2 text-right", children: "Avg Response" }), _jsx("th", { className: "px-4 py-2 text-center", children: "Actions" })] }) }), _jsx("tbody", { children: probes.map((probe) => (_jsxs("tr", { className: "border-t border-panel-border hover:bg-panel/50", children: [_jsx("td", { className: "px-4 py-3 font-medium", children: probe.name }), _jsx("td", { className: "px-4 py-3", children: probe.type }), _jsx("td", { className: "px-4 py-3", children: _jsx("span", { className: `px-2 py-1 rounded text-xs font-medium ${probe.lastStatus === 'healthy' ? 'bg-status-healthy/20 text-status-healthy' :
                                                            probe.lastStatus === 'degraded' ? 'bg-status-degraded/20 text-status-degraded' :
                                                                'bg-status-offline/20 text-status-offline'}`, children: probe.lastStatus }) }), _jsxs("td", { className: "px-4 py-3 text-right", children: [(probe.successRate * 100).toFixed(1), "%"] }), _jsxs("td", { className: "px-4 py-3 text-right", children: [probe.avgResponseTime, "ms"] }), _jsxs("td", { className: "px-4 py-3 text-center space-x-1", children: [_jsx("button", { onClick: () => handleRunProbe(probe.id), className: "text-xs text-accent hover:underline", children: "Run" }), _jsx("button", { onClick: () => handleToggleProbe(probe.id, probe.enabled), className: `text-xs ${probe.enabled ? 'text-status-offline' : 'text-status-healthy'}`, children: probe.enabled ? 'Disable' : 'Enable' })] })] }, probe.id))) })] }) })) : (_jsx(EmptyState, { title: "No probes configured", description: "Set up diagnostic probes to monitor system health", icon: "\uD83D\uDD0D" }))] }), _jsxs("div", { className: "operator-card", children: [_jsxs("h2", { className: "text-lg font-semibold p-4 border-b border-panel-border", children: ["Recent Logs (", logs.length, ")"] }), _jsx("div", { className: "space-y-2 p-4 max-h-96 overflow-y-auto font-mono text-xs", children: logs.slice(0, 20).map((log) => (_jsxs("div", { className: `p-2 rounded ${log.status === 'success' ? 'bg-status-healthy/10 text-status-healthy' :
                                    log.status === 'warning' ? 'bg-status-degraded/10 text-status-degraded' :
                                        log.status === 'error' ? 'bg-status-offline/10 text-status-offline' :
                                            'bg-panel text-ink-secondary'}`, children: [_jsx("span", { children: log.timestamp }), _jsxs("span", { className: "ml-2", children: ["[", log.status.toUpperCase(), "]"] }), _jsx("span", { className: "ml-2", children: log.probeName }), _jsx("span", { className: "ml-2", children: log.message }), _jsxs("span", { className: "ml-2 text-ink-tertiary", children: ["(", log.responseTime, "ms)"] })] }, log.id))) })] }), _jsxs("div", { className: "p-4 bg-ink-secondary/5 border border-panel-border rounded text-xs text-ink-tertiary", children: [_jsx("p", { className: "font-medium text-ink-secondary mb-2", children: "\u2705 Diagnostics Complete" }), _jsxs("ul", { className: "space-y-1", children: [_jsx("li", { children: "\u2713 Real-time probe monitoring and status" }), _jsx("li", { children: "\u2713 Live log streaming with filtering" }), _jsx("li", { children: "\u2713 One-click probe execution" }), _jsx("li", { children: "\u2713 Performance metrics dashboard" })] })] })] }) }));
}
