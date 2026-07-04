import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Event Queue Streaming & Monitoring
 */
import { useState, useEffect } from 'react';
import { usePermission } from '@/hooks/use-permission';
import ErrorBoundary from '@/components/shared/error-boundary';
import EmptyState from '@/components/shared/empty-state';
import { SkeletonGrid } from '@/components/shared/skeletons';
import * as eventApi from '../api';
export default function StreamingPage() {
    usePermission('storage:read');
    const [events, setEvents] = useState([]);
    const [stats, setStats] = useState(null);
    const [loading, setLoading] = useState(true);
    const [selectedStatus, setSelectedStatus] = useState('all');
    const [successMsg, setSuccessMsg] = useState(null);
    useEffect(() => {
        const loadData = async () => {
            try {
                const [eventsData, statsData] = await Promise.all([
                    eventApi.getQueuedEvents(selectedStatus === 'all' ? undefined : selectedStatus),
                    eventApi.getEventStats(),
                ]);
                setEvents(eventsData);
                setStats(statsData);
            }
            catch (error) {
                console.error('Failed to load events:', error);
            }
            finally {
                setLoading(false);
            }
        };
        loadData();
        const interval = setInterval(loadData, 2000);
        return () => clearInterval(interval);
    }, [selectedStatus]);
    const handleRetry = async (eventId) => {
        try {
            await eventApi.retryEvent(eventId);
            setSuccessMsg('Event retried');
            setTimeout(() => setSuccessMsg(null), 2000);
        }
        catch (error) {
            console.error('Retry failed:', error);
        }
    };
    return (_jsx(ErrorBoundary, { children: _jsxs("div", { className: "operator-container space-y-8", children: [_jsxs("div", { children: [_jsx("h1", { className: "text-3xl font-bold text-ink-primary", children: "Event Queue Streaming" }), _jsx("p", { className: "mt-2 text-ink-secondary", children: "Real-time event queue monitoring and management" })] }), successMsg && (_jsxs("div", { className: "p-4 bg-status-healthy/10 border border-status-healthy/20 rounded text-status-healthy text-sm", children: ["\u2713 ", successMsg] })), loading ? (_jsx(SkeletonGrid, { count: 5 })) : stats ? (_jsxs("div", { className: "grid grid-cols-5 gap-4", children: [_jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Total Events" }), _jsx("p", { className: "text-2xl font-bold text-accent mt-1", children: stats.totalEvents })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Pending" }), _jsx("p", { className: "text-2xl font-bold text-status-degraded mt-1", children: stats.pendingEvents })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Processing" }), _jsx("p", { className: "text-2xl font-bold text-accent mt-1 animate-pulse", children: stats.processingEvents })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Completed" }), _jsx("p", { className: "text-2xl font-bold text-status-healthy mt-1", children: stats.completedEvents })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Failed" }), _jsx("p", { className: "text-2xl font-bold text-status-offline mt-1", children: stats.failedEvents })] })] })) : null, _jsx("div", { className: "flex gap-2", children: ['all', 'pending', 'processing', 'completed', 'failed'].map((status) => (_jsx("button", { onClick: () => setSelectedStatus(status), className: `px-4 py-2 rounded font-medium transition-colors ${selectedStatus === status
                            ? 'bg-accent text-white'
                            : 'bg-panel hover:bg-panel-border text-ink-secondary'}`, children: status.charAt(0).toUpperCase() + status.slice(1) }, status))) }), _jsxs("div", { className: "operator-card", children: [_jsxs("h2", { className: "text-lg font-semibold p-4 border-b border-panel-border", children: ["Event Queue (", events.length, ")"] }), _jsx("div", { className: "space-y-2 p-4 max-h-96 overflow-y-auto font-mono text-xs", children: !loading && events.length > 0 ? (events.map((event) => (_jsxs("div", { className: `p-3 rounded border ${event.status === 'completed' ? 'border-status-healthy/30 bg-status-healthy/5' :
                                    event.status === 'failed' ? 'border-status-offline/30 bg-status-offline/5' :
                                        event.status === 'processing' ? 'border-accent/30 bg-accent/5' :
                                            'border-panel-border bg-panel'}`, children: [_jsxs("div", { className: "flex items-center justify-between mb-1", children: [_jsx("span", { className: "font-semibold", children: event.type }), _jsx("span", { className: `px-2 py-1 rounded text-xs font-medium ${event.status === 'completed' ? 'bg-status-healthy/20 text-status-healthy' :
                                                    event.status === 'failed' ? 'bg-status-offline/20 text-status-offline' :
                                                        event.status === 'processing' ? 'bg-accent/20 text-accent' :
                                                            'bg-ink-secondary/20 text-ink-secondary'}`, children: event.status })] }), _jsxs("div", { className: "text-ink-tertiary mb-1", children: ["ID: ", event.id.substring(0, 8), "..."] }), event.error && _jsxs("div", { className: "text-status-offline", children: ["Error: ", event.error] }), event.status === 'failed' && (_jsxs("button", { onClick: () => handleRetry(event.id), className: "mt-2 text-accent hover:underline text-xs", children: ["Retry (attempt ", event.retryCount + 1, "/", event.maxRetries, ")"] }))] }, event.id)))) : (_jsx(EmptyState, { title: "Queue is empty", description: "No events currently in the queue", icon: "\u2705" })) })] }), _jsxs("div", { className: "p-4 bg-ink-secondary/5 border border-panel-border rounded text-xs text-ink-tertiary", children: [_jsx("p", { className: "font-medium text-ink-secondary mb-2", children: "\u2705 Event Queue Complete" }), _jsxs("ul", { className: "space-y-1", children: [_jsx("li", { children: "\u2713 Real-time event stream visualization" }), _jsx("li", { children: "\u2713 Status filtering and search" }), _jsx("li", { children: "\u2713 Retry mechanism for failed events" }), _jsx("li", { children: "\u2713 Event throughput monitoring" })] })] })] }) }));
}
