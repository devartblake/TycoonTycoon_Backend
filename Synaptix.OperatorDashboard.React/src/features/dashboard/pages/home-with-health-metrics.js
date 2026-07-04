import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Dashboard Home page - System Health Overview (with real health metrics)
 *
 * This is an enhanced version that uses real health check data from /health endpoint
 * alongside the mock dashboard stats for gradual migration.
 */
import { useMemo } from 'react';
import { usePermission } from '@/hooks/use-permission';
import { useHealthMetrics } from '@/hooks/use-health-metrics';
import { ServiceCard } from '../components/service-card';
import { SystemMetrics } from '../components/system-metrics';
import { AlertsSection } from '../components/alerts-section';
import { useDashboardStats, useAllServiceHistory } from '../hooks/useDashboard';
export default function DashboardHomePageWithHealth() {
    usePermission('operations:read');
    // Existing dashboard stats (can be phased out)
    const statsQuery = useDashboardStats();
    const historyQuery = useAllServiceHistory(24);
    // New: Real health metrics from backend
    const { metrics: healthMetrics, isLoading: healthLoading, error: healthError } = useHealthMetrics({
        enabled: true,
        pollInterval: 30000,
        onError: (error) => {
            console.warn('Health metrics unavailable, using fallback:', error);
        },
    });
    // Create a map of service histories for sparkline data
    const sparklineMap = useMemo(() => {
        if (!historyQuery.data)
            return {};
        return Object.fromEntries(historyQuery.data.map((h) => [h.serviceId, h.metrics.map((m) => m.value)]));
    }, [historyQuery.data]);
    const stats = statsQuery.data;
    const isLoading = statsQuery.isLoading || historyQuery.isLoading;
    // Merge health metrics with stats for display
    // Prefer real health metrics over mock data
    const displayMetrics = healthMetrics || stats?.metrics;
    return (_jsxs("div", { className: "operator-container space-y-8", children: [_jsxs("div", { children: [_jsx("h1", { className: "text-3xl font-bold text-ink-primary", children: "Dashboard" }), _jsx("p", { className: "mt-2 text-ink-secondary", children: "System health and performance overview" }), healthMetrics && (_jsxs("div", { className: "mt-4 flex items-center gap-2", children: [_jsx("div", { className: `w-3 h-3 rounded-full ${healthMetrics.isHealthy ? 'bg-status-healthy' : 'bg-status-offline'}` }), _jsx("span", { className: `text-sm font-medium ${healthMetrics.isHealthy ? 'text-status-healthy' : 'text-status-offline'}`, children: healthMetrics.isHealthy ? 'All Systems Operational' : 'System Issues Detected' }), healthError && (_jsx("span", { className: "text-xs text-status-offline ml-4", children: "(Fallback mode: real metrics unavailable)" }))] }))] }), _jsxs("div", { children: [_jsx("h2", { className: "text-lg font-semibold text-ink-primary mb-4", children: "System Metrics" }), _jsx(SystemMetrics, { metrics: displayMetrics, isLoading: isLoading || healthLoading })] }), healthMetrics && (_jsxs("div", { className: "operator-card space-y-3", children: [_jsx("h3", { className: "font-semibold text-ink-primary", children: "Health Check Details" }), _jsxs("div", { className: "grid grid-cols-1 md:grid-cols-3 gap-4", children: [_jsxs("div", { className: "space-y-1", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Response Time (P95)" }), _jsxs("p", { className: "text-lg font-semibold text-accent", children: [healthMetrics.responseTime.toFixed(0), "ms"] }), _jsx("p", { className: `text-xs ${healthMetrics.responseTime < 300 ? 'text-status-healthy' : healthMetrics.responseTime < 500 ? 'text-status-degraded' : 'text-status-offline'}`, children: healthMetrics.responseTime < 300 ? '✓ Excellent' : healthMetrics.responseTime < 500 ? '⚠ Elevated' : '✗ Degraded' })] }), _jsxs("div", { className: "space-y-1", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Error Rate" }), _jsxs("p", { className: "text-lg font-semibold text-accent", children: [(healthMetrics.errorRate * 100).toFixed(2), "%"] }), _jsx("p", { className: `text-xs ${healthMetrics.errorRate < 0.001 ? 'text-status-healthy' : healthMetrics.errorRate < 0.01 ? 'text-status-degraded' : 'text-status-offline'}`, children: healthMetrics.errorRate < 0.001 ? '✓ Healthy' : healthMetrics.errorRate < 0.01 ? '⚠ Elevated' : '✗ High' })] }), _jsxs("div", { className: "space-y-1", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Uptime" }), _jsx("p", { className: "text-lg font-semibold text-accent", children: formatUptime(healthMetrics.uptime) }), _jsx("p", { className: "text-xs text-status-healthy", children: "\u2713 Running" })] })] })] })), stats && (_jsx(AlertsSection, { alertCount: stats.alertsActive, services: stats.services })), _jsxs("div", { children: [_jsxs("div", { className: "flex items-center justify-between mb-4", children: [_jsx("h2", { className: "text-lg font-semibold text-ink-primary", children: "Service Status" }), stats && (_jsxs("p", { className: "text-sm text-ink-secondary", children: [stats.services.filter((s) => s.status === 'healthy').length, " / ", stats.services.length, " operational"] }))] }), isLoading ? (_jsx("div", { className: "grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4", children: [...Array(6)].map((_, i) => (_jsx("div", { className: "operator-card h-48 bg-bg-secondary animate-pulse" }, i))) })) : stats ? (_jsx("div", { className: "grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4", children: stats.services.map((service) => (_jsx(ServiceCard, { service: service, sparklineData: sparklineMap[service.id] }, service.id))) })) : null] }), stats && (_jsxs("div", { className: "text-xs text-ink-tertiary text-center pt-4 border-t border-panel-border space-y-1", children: [_jsxs("p", { children: ["Auto-refreshing every 30 seconds \u2022 Last updated: ", new Date(stats.lastUpdatedAt).toLocaleTimeString()] }), _jsxs("p", { children: ["Total health checks: ", stats.checksPerformed.toLocaleString()] }), healthMetrics && (_jsxs("p", { className: "text-ink-secondary", children: ["Real-time metrics from /health endpoint \u2022 ", healthMetrics.isHealthy ? 'Healthy' : 'Issues Detected'] }))] }))] }));
}
/**
 * Format uptime duration
 */
function formatUptime(seconds) {
    if (seconds < 60)
        return `${Math.floor(seconds)}s`;
    if (seconds < 3600)
        return `${Math.floor(seconds / 60)}m`;
    if (seconds < 86400)
        return `${Math.floor(seconds / 3600)}h`;
    return `${Math.floor(seconds / 86400)}d`;
}
