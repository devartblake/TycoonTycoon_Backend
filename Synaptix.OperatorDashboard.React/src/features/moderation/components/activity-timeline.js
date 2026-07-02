import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Player activity timeline
 */
import { formatDateTime } from '@/lib/utils';
const ACTIVITY_CONFIG = {
    login: { icon: '📱', color: 'text-ink-secondary', bg: 'bg-ink-secondary/10' },
    game_played: { icon: '🎮', color: 'text-accent', bg: 'bg-accent/10' },
    purchase: { icon: '🛒', color: 'text-status-healthy', bg: 'bg-status-healthy/10' },
    violation: { icon: '⚠️', color: 'text-status-offline', bg: 'bg-status-offline/10' },
    appeal: { icon: '📤', color: 'text-ink-tertiary', bg: 'bg-ink-tertiary/10' },
    action: { icon: '⚙️', color: 'text-status-degraded', bg: 'bg-status-degraded/10' },
};
export function ActivityTimeline({ activities, isLoading }) {
    if (isLoading) {
        return (_jsx("div", { className: "operator-card space-y-2", children: [...Array(5)].map((_, i) => (_jsx("div", { className: "h-12 bg-bg-secondary rounded animate-pulse" }, i))) }));
    }
    if (activities.length === 0) {
        return (_jsx("div", { className: "text-center py-8 text-ink-secondary operator-card", children: _jsx("p", { children: "No recent activity" }) }));
    }
    return (_jsxs("div", { className: "operator-card", children: [_jsx("h3", { className: "font-semibold text-ink-primary mb-4", children: "Activity Timeline" }), _jsx("div", { className: "space-y-3", children: activities.map((activity) => {
                    const config = ACTIVITY_CONFIG[activity.type];
                    return (_jsx("div", { className: `p-3 rounded border border-panel-border ${config.bg}`, children: _jsxs("div", { className: "flex items-start gap-3", children: [_jsx("span", { className: "text-lg", children: config.icon }), _jsxs("div", { className: "flex-1 min-w-0", children: [_jsx("p", { className: `font-semibold text-sm capitalize ${config.color}`, children: activity.type.replace('_', ' ') }), _jsx("p", { className: "text-sm text-ink-secondary mt-1", children: activity.description }), activity.metadata && (_jsx("div", { className: "text-xs text-ink-tertiary mt-1 space-y-1", children: Object.entries(activity.metadata).map(([key, value]) => (_jsxs("p", { children: [key, ": ", typeof value === 'number' ? value.toLocaleString() : String(value)] }, key))) })), _jsx("p", { className: "text-xs text-ink-tertiary mt-2", children: formatDateTime(activity.timestamp) })] })] }) }, activity.id));
                }) })] }));
}
