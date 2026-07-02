import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Audit event filter bar
 */
import { useState } from 'react';
import { Button } from '@/components/ui/button';
const EVENT_TYPES = ['login', 'api_call', 'permission_change', 'data_export', 'deletion', 'configuration_change'];
const STATUSES = ['success', 'failure'];
const COUNTRIES = ['US', 'UK', 'CA', 'DE', 'JP', 'AU', 'FR', 'SG', 'Other'];
export function FilterBar({ filters, onFiltersChange }) {
    const [searchText, setSearchText] = useState(filters.searchText || '');
    const handleEventTypeChange = (eventType) => {
        onFiltersChange({
            ...filters,
            eventType: filters.eventType === eventType ? undefined : eventType,
        });
    };
    const handleStatusChange = (status) => {
        onFiltersChange({
            ...filters,
            status: filters.status === status ? undefined : status,
        });
    };
    const handleCountryChange = (country) => {
        onFiltersChange({
            ...filters,
            country: filters.country === country ? undefined : country,
        });
    };
    const handleSearchChange = (text) => {
        setSearchText(text);
        onFiltersChange({
            ...filters,
            searchText: text || undefined,
        });
    };
    const handleClearFilters = () => {
        setSearchText('');
        onFiltersChange({});
    };
    const hasActiveFilters = Object.values(filters).some((v) => v != null);
    return (_jsxs("div", { className: "operator-card space-y-4", children: [_jsxs("div", { className: "flex items-center justify-between", children: [_jsx("h3", { className: "font-semibold text-ink-primary", children: "Filters" }), hasActiveFilters && (_jsx(Button, { variant: "ghost", size: "sm", onClick: handleClearFilters, className: "text-xs", children: "Clear Filters" }))] }), _jsxs("div", { children: [_jsx("label", { className: "block text-xs font-medium text-ink-tertiary mb-1", children: "Search" }), _jsx("input", { type: "text", value: searchText, onChange: (e) => handleSearchChange(e.target.value), className: "w-full px-3 py-2 border border-panel-border rounded text-sm focus-ring", placeholder: "Search email, IP, resource..." })] }), _jsxs("div", { children: [_jsx("label", { className: "block text-xs font-medium text-ink-tertiary mb-2", children: "Event Type" }), _jsx("div", { className: "flex flex-wrap gap-2", children: EVENT_TYPES.map((type) => (_jsx("button", { onClick: () => handleEventTypeChange(type), className: `px-2 py-1 text-xs rounded border transition-colors ${filters.eventType === type
                                ? 'bg-accent text-white border-accent'
                                : 'border-panel-border text-ink-secondary hover:bg-bg-secondary'}`, children: type }, type))) })] }), _jsxs("div", { children: [_jsx("label", { className: "block text-xs font-medium text-ink-tertiary mb-2", children: "Status" }), _jsx("div", { className: "flex gap-2", children: STATUSES.map((status) => (_jsx("button", { onClick: () => handleStatusChange(status), className: `px-3 py-1 text-xs rounded border transition-colors ${filters.status === status
                                ? `border-${status === 'success' ? 'status-healthy' : 'status-offline'} bg-${status === 'success' ? 'status-healthy' : 'status-offline'}/10 text-${status === 'success' ? 'status-healthy' : 'status-offline'}`
                                : 'border-panel-border text-ink-secondary hover:bg-bg-secondary'}`, children: status === 'success' ? '✓ Success' : '✕ Failure' }, status))) })] }), _jsxs("div", { children: [_jsx("label", { className: "block text-xs font-medium text-ink-tertiary mb-2", children: "Country" }), _jsx("div", { className: "flex flex-wrap gap-2", children: COUNTRIES.map((country) => (_jsx("button", { onClick: () => handleCountryChange(country), className: `px-2 py-1 text-xs rounded border transition-colors ${filters.country === country
                                ? 'bg-accent text-white border-accent'
                                : 'border-panel-border text-ink-secondary hover:bg-bg-secondary'}`, children: country }, country))) })] })] }));
}
