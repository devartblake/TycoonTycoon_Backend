import { jsx as _jsx, jsxs as _jsxs, Fragment as _Fragment } from "react/jsx-runtime";
/**
 * Game event status card
 */
import { Button } from '@/components/ui/button';
const STATUS_CONFIG = {
    draft: { color: 'text-ink-secondary', bg: 'bg-ink-secondary/10', label: 'Draft', icon: '📝' },
    upcoming: { color: 'text-ink-tertiary', bg: 'bg-ink-tertiary/10', label: 'Upcoming', icon: '⏳' },
    active: { color: 'text-status-healthy', bg: 'bg-status-healthy/10', label: 'Active', icon: '🔴' },
    ended: { color: 'text-status-offline', bg: 'bg-status-offline/10', label: 'Ended', icon: '⏹' },
    cancelled: { color: 'text-status-offline', bg: 'bg-status-offline/10', label: 'Cancelled', icon: '✕' },
};
const TYPE_ICON = {
    tournament: '🏆',
    challenge: '⚔️',
    promotion: '🎉',
    special: '✨',
};
export function EventCard({ event, onAction, isLoading }) {
    const config = STATUS_CONFIG[event.status];
    const participation = (event.participantCount / event.maxParticipants) * 100;
    return (_jsxs("div", { className: `operator-card border-l-4 ${config.bg}`, children: [_jsxs("div", { className: "flex items-start justify-between gap-4 mb-4", children: [_jsxs("div", { children: [_jsxs("div", { className: "flex items-center gap-2 mb-1", children: [_jsx("span", { className: "text-lg", children: TYPE_ICON[event.type] }), _jsx("h3", { className: "text-lg font-bold text-ink-primary", children: event.name })] }), _jsx("p", { className: "text-sm text-ink-secondary", children: event.description })] }), _jsx("span", { className: `px-3 py-1 rounded text-xs font-medium whitespace-nowrap ${config.bg} ${config.color}`, children: config.label })] }), _jsx("div", { className: "pt-3 pb-3 border-t border-b border-panel-border", children: _jsxs("div", { className: "flex items-center gap-2 text-xs text-ink-tertiary", children: [_jsx("span", { children: new Date(event.startDate).toLocaleDateString() }), _jsx("span", { children: "\u2192" }), _jsx("span", { children: new Date(event.endDate).toLocaleDateString() })] }) }), _jsxs("div", { className: "grid grid-cols-3 gap-3 mt-4 mb-4", children: [_jsxs("div", { children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Reward" }), _jsxs("p", { className: "text-sm font-bold text-accent mt-1", children: [(event.reward / 1000).toFixed(0), "k"] })] }), _jsxs("div", { children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Participants" }), _jsx("p", { className: "text-sm font-bold text-accent mt-1", children: event.participantCount.toLocaleString() })] }), _jsxs("div", { children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Capacity" }), _jsxs("p", { className: "text-sm font-bold text-accent mt-1", children: [Math.round(participation), "%"] })] })] }), _jsx("div", { className: "mb-4", children: _jsx("div", { className: "w-full h-2 bg-bg-secondary rounded overflow-hidden", children: _jsx("div", { className: `h-full transition-all ${participation > 80
                            ? 'bg-status-offline'
                            : participation > 50
                                ? 'bg-status-degraded'
                                : 'bg-status-healthy'}`, style: { width: `${Math.min(participation, 100)}%` } }) }) }), _jsxs("div", { className: "flex gap-2 pt-4 border-t border-panel-border", children: [event.status === 'upcoming' && (_jsx(Button, { onClick: () => onAction('start'), disabled: isLoading, size: "sm", className: "flex-1 text-xs", children: isLoading ? 'Opening...' : 'Open Event' })), event.status === 'active' && (_jsxs(_Fragment, { children: [_jsx(Button, { onClick: () => onAction('close'), disabled: isLoading, variant: "outline", size: "sm", className: "flex-1 text-xs", children: isLoading ? 'Closing...' : 'Close' }), _jsx(Button, { onClick: () => onAction('cancel'), disabled: isLoading, variant: "outline", size: "sm", className: "flex-1 text-xs text-status-offline", children: "Cancel" })] })), (event.status === 'draft' || event.status === 'ended' || event.status === 'cancelled') && (_jsx("div", { className: "flex-1 text-xs text-ink-tertiary text-center", children: event.status === 'ended' ? 'Event concluded' : 'Awaiting schedule' }))] })] }));
}
