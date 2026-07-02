import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Player moderation action panel
 */
import { useState } from 'react';
import { Button } from '@/components/ui/button';
const ACTION_REASONS = [
    'Cheating detected',
    'Abusive behavior',
    'Account sharing',
    'Payment fraud',
    'Terms violation',
    'Appeal granted',
    'Manual review',
];
const SUSPENSION_DURATIONS = [
    { hours: 1, label: '1 hour' },
    { hours: 24, label: '1 day' },
    { hours: 168, label: '1 week' },
    { hours: 720, label: '30 days' },
];
export function ActionPanel({ profile, onBan, onUnban, onSuspend, onUnsuspend, onWarn, isLoading }) {
    const [activeAction, setActiveAction] = useState(null);
    const [reason, setReason] = useState('');
    const [notes, setNotes] = useState('');
    const [suspensionDuration, setSuspensionDuration] = useState(24);
    const [error, setError] = useState(null);
    const handleSubmitAction = async (action) => {
        setError(null);
        try {
            await action();
            setActiveAction(null);
            setReason('');
            setNotes('');
        }
        catch (err) {
            setError(err instanceof Error ? err.message : 'Action failed');
        }
    };
    return (_jsxs("div", { className: "operator-card space-y-4", children: [_jsx("h3", { className: "font-semibold text-ink-primary", children: "Quick Actions" }), error && (_jsx("div", { className: "p-3 bg-status-offline/10 border border-status-offline/20 rounded text-status-offline text-sm", children: error })), _jsxs("div", { className: "grid grid-cols-2 gap-2", children: [profile.status !== 'banned' && (_jsx(Button, { variant: activeAction === 'ban' ? 'default' : 'outline', size: "sm", onClick: () => setActiveAction(activeAction === 'ban' ? null : 'ban'), className: "text-xs", children: "Ban Player" })), profile.status === 'banned' && (_jsx(Button, { variant: activeAction === 'unban' ? 'default' : 'outline', size: "sm", onClick: () => setActiveAction(activeAction === 'unban' ? null : 'unban'), className: "text-xs", children: "Unban" })), profile.status !== 'suspended' && (_jsx(Button, { variant: activeAction === 'suspend' ? 'default' : 'outline', size: "sm", onClick: () => setActiveAction(activeAction === 'suspend' ? null : 'suspend'), className: "text-xs", children: "Suspend" })), profile.status === 'suspended' && (_jsx(Button, { variant: activeAction === 'unsuspend' ? 'default' : 'outline', size: "sm", onClick: () => setActiveAction(activeAction === 'unsuspend' ? null : 'unsuspend'), className: "text-xs", children: "Unsuspend" })), _jsx(Button, { variant: activeAction === 'warn' ? 'default' : 'outline', size: "sm", onClick: () => setActiveAction(activeAction === 'warn' ? null : 'warn'), className: "text-xs", children: "Warn" })] }), (activeAction === 'ban' || activeAction === 'suspend' || activeAction === 'warn' || activeAction === 'unban' || activeAction === 'unsuspend') && (_jsxs("div", { className: "space-y-3 p-3 bg-bg-secondary rounded border border-panel-border", children: [_jsxs("div", { children: [_jsx("label", { className: "block text-xs font-medium text-ink-tertiary mb-1", children: "Reason" }), _jsxs("select", { value: reason, onChange: (e) => setReason(e.target.value), className: "w-full px-2 py-2 text-sm border border-panel-border rounded focus-ring bg-bg-primary", children: [_jsx("option", { value: "", children: "Select a reason..." }), ACTION_REASONS.map((r) => (_jsx("option", { value: r, children: r }, r)))] })] }), activeAction === 'suspend' && (_jsxs("div", { children: [_jsx("label", { className: "block text-xs font-medium text-ink-tertiary mb-1", children: "Duration" }), _jsx("div", { className: "grid grid-cols-2 gap-2", children: SUSPENSION_DURATIONS.map((d) => (_jsx("button", { onClick: () => setSuspensionDuration(d.hours), className: `px-2 py-1 text-xs rounded border transition-colors ${suspensionDuration === d.hours
                                        ? 'bg-accent text-white border-accent'
                                        : 'border-panel-border hover:bg-bg-tertiary'}`, children: d.label }, d.hours))) })] })), (activeAction === 'ban' || activeAction === 'suspend' || activeAction === 'warn') && (_jsxs("div", { children: [_jsx("label", { htmlFor: "notes", className: "block text-xs font-medium text-ink-tertiary mb-1", children: "Notes (optional)" }), _jsx("textarea", { id: "notes", value: notes, onChange: (e) => setNotes(e.target.value), className: "w-full px-2 py-2 text-xs border border-panel-border rounded focus-ring h-20 resize-none", placeholder: "Internal notes..." })] })), _jsxs("div", { className: "flex gap-2 pt-2", children: [_jsx(Button, { size: "sm", onClick: () => {
                                    if (!reason) {
                                        setError('Please select a reason');
                                        return;
                                    }
                                    if (activeAction === 'ban') {
                                        handleSubmitAction(() => onBan(reason, notes));
                                    }
                                    else if (activeAction === 'unban') {
                                        handleSubmitAction(() => onUnban(reason));
                                    }
                                    else if (activeAction === 'suspend') {
                                        handleSubmitAction(() => onSuspend(suspensionDuration, reason, notes));
                                    }
                                    else if (activeAction === 'unsuspend') {
                                        handleSubmitAction(() => onUnsuspend(reason));
                                    }
                                    else if (activeAction === 'warn') {
                                        handleSubmitAction(() => onWarn(reason, notes));
                                    }
                                }, disabled: isLoading, className: "flex-1 text-xs", children: isLoading ? 'Submitting...' : 'Confirm' }), _jsx(Button, { size: "sm", variant: "outline", onClick: () => {
                                    setActiveAction(null);
                                    setReason('');
                                    setNotes('');
                                }, className: "flex-1 text-xs", children: "Cancel" })] })] }))] }));
}
