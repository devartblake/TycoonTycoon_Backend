import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Security Audit + IP Map page
 */
import { useState } from 'react';
import { usePermission } from '@/hooks/use-permission';
import ErrorBoundary from '@/components/shared/error-boundary';
import EmptyState from '@/components/shared/empty-state';
import { SkeletonGrid, SkeletonTable } from '@/components/shared/skeletons';
import { FilterBar } from '../components/filter-bar';
import { EventsTable } from '../components/events-table';
import { IPMap } from '../components/ip-map';
import { useAuditEvents, useAuditStats, useIPLocations, useEventLocations, } from '../hooks/useAuditEvents';
export default function SecurityAuditPage() {
    usePermission('audit:read');
    const [filters, setFilters] = useState({});
    const [offset, setOffset] = useState(0);
    const limit = 50;
    const eventsQuery = useAuditEvents(filters, offset, limit);
    const statsQuery = useAuditStats();
    const ipLocationsQuery = useIPLocations(filters);
    const eventLocations = useEventLocations(eventsQuery.data?.items);
    const handleFiltersChange = (newFilters) => {
        setFilters(newFilters);
        setOffset(0); // Reset pagination when filters change
    };
    const handleEventClick = (event) => {
        // Could navigate to event detail or open a modal
        console.log('Event clicked:', event);
    };
    return (_jsx(ErrorBoundary, { children: _jsxs("div", { className: "operator-container space-y-8", children: [_jsxs("div", { children: [_jsx("h1", { className: "text-3xl font-bold text-ink-primary", children: "Security Audit" }), _jsx("p", { className: "mt-2 text-ink-secondary", children: "Monitor administrative actions and access patterns" })] }), statsQuery.isLoading ? (_jsx(SkeletonGrid, { count: 4 })) : statsQuery.data ? (_jsxs("div", { className: "grid grid-cols-1 md:grid-cols-4 gap-4", children: [_jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Total Events" }), _jsx("p", { className: "text-2xl font-bold text-accent mt-1", children: statsQuery.data.totalEvents.toLocaleString() })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Success Rate" }), _jsxs("p", { className: "text-2xl font-bold text-status-healthy mt-1", children: [Math.round(statsQuery.data.successRate * 100), "%"] })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Admin Accounts" }), _jsx("p", { className: "text-2xl font-bold text-accent mt-1", children: statsQuery.data.uniqueAdmins })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Unique IPs" }), _jsx("p", { className: "text-2xl font-bold text-accent mt-1", children: statsQuery.data.uniqueIPs })] })] })) : null, _jsxs("div", { className: "grid grid-cols-1 lg:grid-cols-4 gap-6", children: [_jsx("div", { children: _jsx(FilterBar, { filters: filters, onFiltersChange: handleFiltersChange }) }), _jsxs("div", { className: "lg:col-span-3 space-y-6", children: [_jsx(IPMap, { locations: eventLocations.length > 0 ? eventLocations : ipLocationsQuery.data || [], isLoading: ipLocationsQuery.isLoading || eventsQuery.isLoading }), _jsxs("div", { children: [_jsxs("div", { className: "flex items-center justify-between mb-4", children: [_jsx("h2", { className: "text-lg font-semibold text-ink-primary", children: "Recent Events" }), eventsQuery.data && (_jsxs("p", { className: "text-sm text-ink-secondary", children: ["Showing ", eventsQuery.data.items.length, " of ", eventsQuery.data.total, " events"] }))] }), eventsQuery.isLoading ? (_jsx(SkeletonTable, { rows: 10, columns: 4 })) : eventsQuery.data && eventsQuery.data.items.length > 0 ? (_jsx(EventsTable, { events: eventsQuery.data.items, isLoading: false, onEventClick: handleEventClick })) : (_jsx(EmptyState, { title: "No audit events found", description: "Try adjusting your filters", icon: "\uD83D\uDCCB" }))] }), eventsQuery.data && eventsQuery.data.total > limit && (_jsxs("div", { className: "flex justify-between items-center", children: [_jsx("button", { onClick: () => setOffset(Math.max(0, offset - limit)), disabled: offset === 0, className: "px-4 py-2 bg-bg-secondary border border-panel-border rounded text-sm hover:bg-bg-tertiary disabled:opacity-50 disabled:cursor-not-allowed", children: "\u2190 Previous" }), _jsxs("p", { className: "text-sm text-ink-secondary", children: ["Page ", Math.floor(offset / limit) + 1, " of ", Math.ceil(eventsQuery.data.total / limit)] }), _jsx("button", { onClick: () => setOffset(offset + limit), disabled: offset + limit >= eventsQuery.data.total, className: "px-4 py-2 bg-bg-secondary border border-panel-border rounded text-sm hover:bg-bg-tertiary disabled:opacity-50 disabled:cursor-not-allowed", children: "Next \u2192" })] }))] })] })] }) }));
}
