import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Moderation action history timeline
 */
import { formatDateTime } from '@/lib/utils';
const ACTION_CONFIG = {
    ban: { icon: '🚫', color: 'text-status-offline', bg: 'bg-status-offline/10' },
    unban: { icon: '✓', color: 'text-status-healthy', bg: 'bg-status-healthy/10' },
    suspend: { icon: '⏸', color: 'text-status-degraded', bg: 'bg-status-degraded/10' },
    unsuspend: { icon: '▶', color: 'text-status-healthy', bg: 'bg-status-healthy/10' },
    warn: { icon: '⚠', color: 'text-status-degraded', bg: 'bg-status-degraded/10' },
    note: { icon: '📝', color: 'text-ink-secondary', bg: 'bg-ink-secondary/10' },
};
export function ActionHistory({ actions, isLoading }) {
    if (isLoading) {
        return (_jsx("div", { className: "operator-card space-y-2", children: [...Array(4)].map((_, i) => (_jsx("div", { className: "h-12 bg-bg-secondary rounded animate-pulse" }, i))) }));
    }
    if (actions.length === 0) {
        return (_jsx("div", { className: "text-center py-8 text-ink-secondary operator-card", children: _jsx("p", { children: "No moderation actions" }) }));
    }
    return (_jsxs("div", { className: "operator-card", children: [_jsx("h3", { className: "font-semibold text-ink-primary mb-4", children: "Moderation History" }), _jsx("div", { className: "space-y-3", children: actions.map((action) => {
                    const config = ACTION_CONFIG[action.action];
                    return (_jsx("div", { className: `p-3 rounded border border-panel-border ${config.bg}`, children: _jsxs("div", { className: "flex items-start gap-3", children: [_jsx("span", { className: "text-lg", children: config.icon }), _jsxs("div", { className: "flex-1 min-w-0", children: [_jsxs("div", { className: "flex items-center gap-2", children: [_jsx("p", { className: `font-semibold text-sm capitalize ${config.color}`, children: action.action === 'note' ? 'Moderator Note' : action.action }), action.status !== 'active' && (_jsxs("span", { className: "text-xs text-ink-tertiary", children: ["(", action.status, ")"] }))] }), _jsx("p", { className: "text-sm text-ink-secondary mt-1", children: action.reason }), action.notes && (_jsxs("p", { className: "text-xs text-ink-tertiary mt-1 italic", children: ["Notes: ", action.notes] })), action.duration && (_jsxs("p", { className: "text-xs text-ink-tertiary mt-1", children: ["Duration: ", Math.round(action.duration / (60 * 60 * 1000)), "h", action.expiresAt && ` (expires: ${new Date(action.expiresAt).toLocaleDateString()})`] })), _jsxs("div", { className: "flex items-center justify-between gap-2 mt-2", children: [_jsxs("p", { className: "text-xs text-ink-tertiary", children: ["by ", action.adminEmail] }), _jsx("p", { className: "text-xs text-ink-tertiary", children: formatDateTime(action.createdAt) })] })] })] }) }, action.id));
                }) })] }));
}
