import { jsxs as _jsxs, jsx as _jsx } from "react/jsx-runtime";
/**
 * Bulk actions bar for selected users
 */
import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { useBulkBanUsers, useBulkUnbanUsers } from '../hooks/useUsers';
export function BulkActionsBar({ selectedIds, onActionComplete }) {
    const [reason, setReason] = useState('');
    const [showReasonForm, setShowReasonForm] = useState(false);
    const bulkBan = useBulkBanUsers();
    const bulkUnban = useBulkUnbanUsers();
    if (selectedIds.length === 0) {
        return null;
    }
    const handleBulkBan = async () => {
        if (confirm(`Ban ${selectedIds.length} user(s)?`)) {
            await bulkBan.mutateAsync({ userIds: selectedIds, reason: reason || undefined });
            setReason('');
            onActionComplete();
        }
    };
    const handleBulkUnban = async () => {
        if (confirm(`Unban ${selectedIds.length} user(s)?`)) {
            await bulkUnban.mutateAsync(selectedIds);
            onActionComplete();
        }
    };
    return (_jsxs("div", { className: "sticky bottom-0 bg-bg-secondary border-t border-panel-border p-4 space-y-3", children: [_jsxs("div", { className: "flex items-center justify-between", children: [_jsxs("span", { className: "text-sm font-medium text-ink-primary", children: [selectedIds.length, " user(s) selected"] }), _jsxs("div", { className: "flex gap-2", children: [_jsx(Button, { variant: "destructive", size: "sm", onClick: () => setShowReasonForm(!showReasonForm), disabled: bulkBan.isPending, children: "\uD83D\uDEAB Ban" }), _jsx(Button, { variant: "secondary", size: "sm", onClick: handleBulkUnban, disabled: bulkUnban.isPending, children: "\u2713 Unban" })] })] }), showReasonForm && (_jsxs("div", { className: "flex gap-2", children: [_jsx("input", { type: "text", placeholder: "Reason (optional)...", value: reason, onChange: (e) => setReason(e.target.value), className: "flex-1 px-3 py-2 border border-panel-border rounded focus-ring text-sm" }), _jsx(Button, { variant: "default", size: "sm", onClick: handleBulkBan, disabled: bulkBan.isPending, children: bulkBan.isPending ? 'Banning...' : 'Confirm Ban' }), _jsx(Button, { variant: "ghost", size: "sm", onClick: () => {
                            setShowReasonForm(false);
                            setReason('');
                        }, children: "Cancel" })] })), bulkBan.isError && (_jsxs("div", { className: "text-xs text-status-offline", children: ["Error: ", String(bulkBan.error)] })), bulkUnban.isError && (_jsxs("div", { className: "text-xs text-status-offline", children: ["Error: ", String(bulkUnban.error)] }))] }));
}
