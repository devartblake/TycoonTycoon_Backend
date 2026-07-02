import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Audit events table
 */
import { formatDateTime } from '@/lib/utils';
const EVENT_TYPE_LABELS = {
    login: 'Login',
    api_call: 'API Call',
    permission_change: 'Permission Change',
    data_export: 'Data Export',
    deletion: 'Deletion',
    configuration_change: 'Configuration Change',
};
const STATUS_COLOR = {
    success: 'bg-status-healthy/10 text-status-healthy',
    failure: 'bg-status-offline/10 text-status-offline',
};
export function EventsTable({ events, isLoading, onEventClick }) {
    if (isLoading) {
        return (_jsx("div", { className: "operator-card space-y-2", children: [...Array(5)].map((_, i) => (_jsx("div", { className: "h-12 bg-bg-secondary rounded animate-pulse" }, i))) }));
    }
    if (events.length === 0) {
        return (_jsx("div", { className: "text-center py-12 text-ink-secondary operator-card", children: _jsx("p", { children: "No events found" }) }));
    }
    return (_jsx("div", { className: "operator-card overflow-x-auto", children: _jsxs("table", { className: "w-full text-sm", children: [_jsx("thead", { children: _jsxs("tr", { className: "border-b border-panel-border", children: [_jsx("th", { className: "px-4 py-2 text-left text-xs font-semibold text-ink-tertiary", children: "Timestamp" }), _jsx("th", { className: "px-4 py-2 text-left text-xs font-semibold text-ink-tertiary", children: "Admin" }), _jsx("th", { className: "px-4 py-2 text-left text-xs font-semibold text-ink-tertiary", children: "Event Type" }), _jsx("th", { className: "px-4 py-2 text-left text-xs font-semibold text-ink-tertiary", children: "Resource" }), _jsx("th", { className: "px-4 py-2 text-left text-xs font-semibold text-ink-tertiary", children: "IP / Location" }), _jsx("th", { className: "px-4 py-2 text-center text-xs font-semibold text-ink-tertiary", children: "Status" })] }) }), _jsx("tbody", { children: events.map((event) => (_jsxs("tr", { onClick: () => onEventClick?.(event), className: "border-b border-panel-border hover:bg-bg-secondary transition-colors cursor-pointer", children: [_jsx("td", { className: "px-4 py-2 text-ink-primary", children: formatDateTime(event.timestamp) }), _jsx("td", { className: "px-4 py-2 text-ink-secondary text-xs", children: event.adminEmail }), _jsx("td", { className: "px-4 py-2", children: _jsx("span", { className: "px-2 py-1 bg-bg-secondary rounded text-xs text-ink-primary", children: EVENT_TYPE_LABELS[event.eventType] }) }), _jsxs("td", { className: "px-4 py-2 text-ink-secondary text-xs", children: [event.resourceType, " / ", event.resourceId] }), _jsxs("td", { className: "px-4 py-2 text-ink-secondary text-xs", children: [event.ipAddress, event.city && ` (${event.city}, ${event.country})`] }), _jsx("td", { className: "px-4 py-2 text-center", children: _jsxs("span", { className: `inline-block px-2 py-1 rounded text-xs font-medium ${STATUS_COLOR[event.status]}`, children: [event.status === 'success' ? '✓' : '✕', " ", event.status] }) })] }, event.id))) })] }) }));
}
