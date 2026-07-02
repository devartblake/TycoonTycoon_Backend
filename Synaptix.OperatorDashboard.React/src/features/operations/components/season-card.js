import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Season status card
 */
import { Button } from '@/components/ui/button';
const STATUS_CONFIG = {
    draft: { color: 'text-ink-secondary', bg: 'bg-ink-secondary/10', label: 'Draft', icon: '📝' },
    scheduled: { color: 'text-ink-tertiary', bg: 'bg-ink-tertiary/10', label: 'Scheduled', icon: '📅' },
    active: { color: 'text-status-healthy', bg: 'bg-status-healthy/10', label: 'Active', icon: '🔴' },
    ended: { color: 'text-status-offline', bg: 'bg-status-offline/10', label: 'Ended', icon: '⏹' },
};
export function SeasonCard({ season, onAction, isLoading }) {
    const config = STATUS_CONFIG[season.status];
    const now = new Date();
    const start = new Date(season.startDate);
    const end = new Date(season.endDate);
    const daysRemaining = Math.ceil((end.getTime() - now.getTime()) / (1000 * 60 * 60 * 24));
    return (_jsxs("div", { className: `operator-card border-l-4 ${config.bg}`, children: [_jsxs("div", { className: "flex items-start justify-between gap-4 mb-4", children: [_jsxs("div", { children: [_jsxs("div", { className: "flex items-center gap-2 mb-1", children: [_jsx("span", { className: "text-lg", children: config.icon }), _jsx("h3", { className: "text-lg font-bold text-ink-primary", children: season.name })] }), _jsx("p", { className: "text-sm text-ink-secondary", children: season.description })] }), _jsx("span", { className: `px-3 py-1 rounded text-xs font-medium whitespace-nowrap ${config.bg} ${config.color}`, children: config.label })] }), _jsxs("div", { className: "pt-3 pb-3 border-t border-b border-panel-border", children: [_jsxs("div", { className: "flex items-center gap-2 text-xs text-ink-tertiary mb-2", children: [_jsxs("span", { children: ["Start: ", start.toLocaleDateString()] }), _jsx("span", { children: "\u2192" }), _jsxs("span", { children: ["End: ", end.toLocaleDateString()] })] }), season.status === 'active' && daysRemaining > 0 && (_jsxs("p", { className: "text-xs text-status-healthy font-medium", children: [daysRemaining, " days remaining"] }))] }), _jsxs("div", { className: "grid grid-cols-3 gap-3 mt-4 mb-4", children: [_jsxs("div", { children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Reward Pool" }), _jsxs("p", { className: "text-sm font-bold text-accent mt-1", children: [(season.rewardPool / 1000).toFixed(0), "k"] })] }), _jsxs("div", { children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Points Multiplier" }), _jsxs("p", { className: "text-sm font-bold text-accent mt-1", children: [season.pointsMultiplier.toFixed(1), "x"] })] }), _jsxs("div", { children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Season" }), _jsxs("p", { className: "text-sm font-bold text-accent mt-1", children: ["# ", season.number] })] })] }), _jsxs("div", { className: "flex gap-2 pt-4 border-t border-panel-border", children: [season.status === 'scheduled' && (_jsx(Button, { onClick: () => onAction('start'), disabled: isLoading, size: "sm", className: "flex-1 text-xs", children: isLoading ? 'Starting...' : 'Start Season' })), season.status === 'active' && (_jsx(Button, { onClick: () => onAction('close'), disabled: isLoading, variant: "outline", size: "sm", className: "flex-1 text-xs", children: isLoading ? 'Closing...' : 'End Season' })), (season.status === 'draft' || season.status === 'ended') && (_jsx("div", { className: "flex-1 text-xs text-ink-tertiary", children: season.status === 'ended' ? 'Season ended' : 'Awaiting schedule' }))] })] }));
}
