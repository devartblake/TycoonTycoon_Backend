import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Users Triage + Saved Views page
 */
import { useState } from 'react';
import { usePermission } from '@/hooks/use-permission';
import ErrorBoundary from '@/components/shared/error-boundary';
import EmptyState from '@/components/shared/empty-state';
import { SkeletonTable } from '@/components/shared/skeletons';
import { FilterBar } from '../components/filter-bar';
import { SavedViewsDropdown } from '../components/saved-views-dropdown';
import { UsersTable } from '../components/users-table';
import { BulkActionsBar } from '../components/bulk-actions-bar';
import { useUsers } from '../hooks/useUsers';
export default function UsersList() {
    usePermission('users:read');
    const [selectedIds, setSelectedIds] = useState([]);
    const { users, filters, isLoading, isError, applyFilters, clearFilters } = useUsers();
    const handleLoadView = (viewFilters) => {
        applyFilters(viewFilters);
    };
    return (_jsx(ErrorBoundary, { children: _jsxs("div", { className: "operator-container space-y-6 pb-24", children: [_jsxs("div", { children: [_jsx("h1", { className: "text-3xl font-bold text-ink-primary", children: "Users Triage" }), _jsx("p", { className: "mt-2 text-ink-secondary", children: "Investigate, filter, and manage player accounts" })] }), _jsx("div", { className: "flex items-center gap-4", children: _jsx(SavedViewsDropdown, { currentFilters: filters, onLoadView: handleLoadView }) }), _jsx(FilterBar, { filters: filters, onFiltersChange: applyFilters, onClearFilters: clearFilters }), isError && (_jsx("div", { className: "p-4 bg-status-offline/10 border border-status-offline/20 rounded text-status-offline text-sm", children: "Failed to load users. Please try again." })), isLoading ? (_jsx(SkeletonTable, { rows: 10, columns: 5 })) : users.length > 0 ? (_jsx(UsersTable, { users: users, isLoading: isLoading, onSelectionChange: setSelectedIds })) : (_jsx(EmptyState, { title: "No users found", description: "Try adjusting your filters to find users", icon: "\uD83D\uDC65" })), _jsx(BulkActionsBar, { selectedIds: selectedIds, onActionComplete: () => setSelectedIds([]) })] }) }));
}
