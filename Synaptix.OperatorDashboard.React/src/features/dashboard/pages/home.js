import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Dashboard Home page - System Health Overview
 */
import { useMemo } from 'react';
import { usePermission } from '@/hooks/use-permission';
import { ServiceCard } from '../components/service-card';
import { SystemMetrics } from '../components/system-metrics';
import { AlertsSection } from '../components/alerts-section';
import { useDashboardStats, useAllServiceHistory } from '../hooks/useDashboard';
export default function DashboardHomePage() {
    usePermission('operations:read');
    const statsQuery = useDashboardStats();
    const historyQuery = useAllServiceHistory(24);
    // Create a map of service histories for sparkline data
    const sparklineMap = useMemo(() => {
        if (!historyQuery.data)
            return {};
        return Object.fromEntries(historyQuery.data.map((h) => [h.serviceId, h.metrics.map((m) => m.value)]));
    }, [historyQuery.data]);
    const stats = statsQuery.data;
    const isLoading = statsQuery.isLoading || historyQuery.isLoading;
    return (_jsxs("div", { className: "operator-container space-y-8", children: [_jsxs("div", { children: [_jsx("h1", { className: "text-3xl font-bold text-ink-primary", children: "Dashboard" }), _jsx("p", { className: "mt-2 text-ink-secondary", children: "System health and performance overview" })] }), _jsxs("div", { children: [_jsx("h2", { className: "text-lg font-semibold text-ink-primary mb-4", children: "System Metrics" }), _jsx(SystemMetrics, { metrics: stats?.metrics, isLoading: isLoading })] }), stats && (_jsx(AlertsSection, { alertCount: stats.alertsActive, services: stats.services })), _jsxs("div", { children: [_jsxs("div", { className: "flex items-center justify-between mb-4", children: [_jsx("h2", { className: "text-lg font-semibold text-ink-primary", children: "Service Status" }), stats && (_jsxs("p", { className: "text-sm text-ink-secondary", children: [stats.services.filter((s) => s.status === 'healthy').length, " / ", stats.services.length, " operational"] }))] }), isLoading ? (_jsx("div", { className: "grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4", children: [...Array(6)].map((_, i) => (_jsx("div", { className: "operator-card h-48 bg-bg-secondary animate-pulse" }, i))) })) : stats ? (_jsx("div", { className: "grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4", children: stats.services.map((service) => (_jsx(ServiceCard, { service: service, sparklineData: sparklineMap[service.id] }, service.id))) })) : null] }), stats && (_jsxs("div", { className: "text-xs text-ink-tertiary text-center pt-4 border-t border-panel-border", children: [_jsxs("p", { children: ["Auto-refreshing every 30 seconds \u2022 Last updated: ", new Date(stats.lastUpdatedAt).toLocaleTimeString()] }), _jsxs("p", { className: "mt-1", children: ["Total health checks: ", stats.checksPerformed.toLocaleString()] })] }))] }));
}
