import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Seasons & Game Events - Lifecycle Management
 */
import { useState } from 'react';
import { usePermission } from '@/hooks/use-permission';
import { SeasonCard } from '../components/season-card';
import { EventCard } from '../components/event-card';
import { useSeasons, useGameEvents, useOperationsStats, usePerformLifecycleAction, } from '../hooks/useOperations';
export default function LifecyclePage() {
    usePermission('operations:write');
    const [seasonFilters, setSeasonFilters] = useState({});
    const [eventFilters, setEventFilters] = useState({});
    const [successMessage, setSuccessMessage] = useState(null);
    const seasonsQuery = useSeasons(seasonFilters);
    const eventsQuery = useGameEvents(eventFilters);
    const statsQuery = useOperationsStats();
    const actionMutation = usePerformLifecycleAction();
    const seasons = seasonsQuery.data?.items || [];
    const events = eventsQuery.data?.items || [];
    const handleSeasonAction = async (seasonId, action) => {
        try {
            await actionMutation.mutateAsync({
                resourceId: seasonId,
                action,
                notes: `${action === 'start' ? 'Started' : 'Closed'} via operator dashboard`,
            });
            const message = action === 'start' ? 'Season started' : 'Season ended';
            setSuccessMessage(message);
            setTimeout(() => setSuccessMessage(null), 2000);
        }
        catch (err) {
            setSuccessMessage(err instanceof Error ? err.message : 'Action failed');
        }
    };
    const handleEventAction = async (eventId, action) => {
        try {
            await actionMutation.mutateAsync({
                resourceId: eventId,
                action,
                notes: `Event ${action} via operator dashboard`,
            });
            const messages = { start: 'Event opened', close: 'Event closed', cancel: 'Event cancelled' };
            setSuccessMessage(messages[action]);
            setTimeout(() => setSuccessMessage(null), 2000);
        }
        catch (err) {
            setSuccessMessage(err instanceof Error ? err.message : 'Action failed');
        }
    };
    return (_jsxs("div", { className: "operator-container space-y-8", children: [_jsxs("div", { children: [_jsx("h1", { className: "text-3xl font-bold text-ink-primary", children: "Seasons & Game Events" }), _jsx("p", { className: "mt-2 text-ink-secondary", children: "Manage lifecycle and monitor progress" })] }), statsQuery.data && (_jsxs("div", { className: "grid grid-cols-1 md:grid-cols-4 gap-4", children: [_jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Active Seasons" }), _jsx("p", { className: "text-2xl font-bold text-accent mt-1", children: statsQuery.data.activeSeasons })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Upcoming Events" }), _jsx("p", { className: "text-2xl font-bold text-accent mt-1", children: statsQuery.data.upcomingEvents })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Total Participants" }), _jsxs("p", { className: "text-2xl font-bold text-status-healthy mt-1", children: [(statsQuery.data.totalParticipants / 1000).toFixed(0), "k"] })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Reward Pool" }), _jsxs("p", { className: "text-2xl font-bold text-accent mt-1", children: [(statsQuery.data.rewardPoolRemaining / 1000000).toFixed(1), "M"] })] })] })), successMessage && (_jsxs("div", { className: "p-4 bg-status-healthy/10 border border-status-healthy/20 rounded text-status-healthy text-sm", children: ["\u2713 ", successMessage] })), _jsxs("div", { children: [_jsxs("div", { className: "flex items-center justify-between mb-4", children: [_jsx("h2", { className: "text-xl font-semibold text-ink-primary", children: "Seasons" }), _jsx("div", { className: "flex gap-2", children: ['draft', 'scheduled', 'active', 'ended'].map((status) => (_jsx("button", { onClick: () => setSeasonFilters({ status: seasonFilters.status === status ? undefined : status }), className: `px-3 py-1 text-xs rounded border transition-colors ${seasonFilters.status === status
                                        ? 'bg-accent text-white border-accent'
                                        : 'border-panel-border hover:bg-bg-secondary text-ink-secondary'}`, children: status }, status))) })] }), seasonsQuery.isLoading ? (_jsx("div", { className: "grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4", children: [...Array(3)].map((_, i) => (_jsx("div", { className: "operator-card h-64 bg-bg-secondary animate-pulse" }, i))) })) : seasons.length > 0 ? (_jsx("div", { className: "grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4", children: seasons.map((season) => (_jsx(SeasonCard, { season: season, onAction: (action) => handleSeasonAction(season.id, action), isLoading: actionMutation.isPending }, season.id))) })) : (_jsx("div", { className: "text-center py-8 text-ink-secondary operator-card", children: _jsx("p", { children: "No seasons found" }) }))] }), _jsxs("div", { children: [_jsxs("div", { className: "flex items-center justify-between mb-4", children: [_jsx("h2", { className: "text-xl font-semibold text-ink-primary", children: "Game Events" }), _jsx("div", { className: "flex gap-2 overflow-x-auto", children: ['draft', 'upcoming', 'active', 'ended', 'cancelled'].map((status) => (_jsx("button", { onClick: () => setEventFilters({ status: eventFilters.status === status ? undefined : status }), className: `px-3 py-1 text-xs rounded border transition-colors whitespace-nowrap ${eventFilters.status === status
                                        ? 'bg-accent text-white border-accent'
                                        : 'border-panel-border hover:bg-bg-secondary text-ink-secondary'}`, children: status }, status))) })] }), eventsQuery.isLoading ? (_jsx("div", { className: "grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4", children: [...Array(3)].map((_, i) => (_jsx("div", { className: "operator-card h-64 bg-bg-secondary animate-pulse" }, i))) })) : events.length > 0 ? (_jsx("div", { className: "grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4", children: events.map((event) => (_jsx(EventCard, { event: event, onAction: (action) => handleEventAction(event.id, action), isLoading: actionMutation.isPending }, event.id))) })) : (_jsx("div", { className: "text-center py-8 text-ink-secondary operator-card", children: _jsx("p", { children: "No events found" }) }))] })] }));
}
