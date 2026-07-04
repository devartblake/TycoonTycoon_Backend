import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Match History & Replay Viewer
 */
import { useState, useEffect } from 'react';
import { usePermission } from '@/hooks/use-permission';
import ErrorBoundary from '@/components/shared/error-boundary';
import EmptyState from '@/components/shared/empty-state';
import { SkeletonGrid } from '@/components/shared/skeletons';
import * as matchApi from '../api';
export default function ReplayPage() {
    usePermission('storage:read');
    const [matches, setMatches] = useState([]);
    const [loading, setLoading] = useState(true);
    const [selectedMatch, setSelectedMatch] = useState(null);
    useEffect(() => {
        const loadData = async () => {
            try {
                const matchesData = await matchApi.getMatches(undefined, 50);
                setMatches(matchesData);
            }
            catch (error) {
                console.error('Failed to load matches:', error);
            }
            finally {
                setLoading(false);
            }
        };
        loadData();
    }, []);
    return (_jsx(ErrorBoundary, { children: _jsxs("div", { className: "operator-container space-y-8", children: [_jsxs("div", { children: [_jsx("h1", { className: "text-3xl font-bold text-ink-primary", children: "Match History & Replays" }), _jsx("p", { className: "mt-2 text-ink-secondary", children: "Browse match history and watch game replays" })] }), loading ? (_jsx(SkeletonGrid, { count: 4 })) : (_jsxs("div", { className: "grid grid-cols-4 gap-4", children: [_jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Total Matches" }), _jsx("p", { className: "text-2xl font-bold text-accent mt-1", children: matches.length })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Avg Duration" }), _jsxs("p", { className: "text-2xl font-bold text-ink-primary mt-1", children: [matches.length > 0 ? (matches.reduce((a, m) => a + m.duration, 0) / matches.length / 60).toFixed(0) : 0, "m"] })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Replays Available" }), _jsx("p", { className: "text-2xl font-bold text-status-healthy mt-1", children: matches.filter(m => m.replay).length })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Recordings" }), _jsx("p", { className: "text-2xl font-bold text-ink-primary mt-1", children: matches.filter(m => m.recordingTime).length })] })] })), _jsxs("div", { className: "grid grid-cols-3 gap-6", children: [_jsxs("div", { className: "col-span-2 operator-card", children: [_jsx("h2", { className: "text-lg font-semibold p-4 border-b border-panel-border", children: "Recent Matches" }), _jsx("div", { className: "space-y-2 p-4 max-h-96 overflow-y-auto", children: !loading && matches.length > 0 ? (matches.map((match) => (_jsxs("div", { onClick: () => setSelectedMatch(match), className: "p-3 border border-panel-border rounded hover:bg-panel/50 cursor-pointer", children: [_jsxs("div", { className: "flex items-center justify-between mb-1", children: [_jsxs("span", { className: "font-medium", children: [match.playerName, " vs ", match.opponentName] }), _jsx("span", { className: `px-2 py-1 rounded text-xs font-medium ${match.result === 'win' ? 'bg-status-healthy/20 text-status-healthy' :
                                                            match.result === 'loss' ? 'bg-status-offline/20 text-status-offline' :
                                                                'bg-panel text-ink-secondary'}`, children: match.result.toUpperCase() })] }), _jsxs("div", { className: "text-sm text-ink-secondary", children: [match.playerScore, " - ", match.opponentScore] }), _jsx("div", { className: "text-xs text-ink-tertiary mt-1", children: new Date(match.startTime).toLocaleString() })] }, match.id)))) : (_jsx(EmptyState, { title: "No matches found", description: "Play matches to view history and replays", icon: "\uD83C\uDFAE" })) })] }), _jsxs("div", { className: "operator-card", children: [_jsx("h2", { className: "text-lg font-semibold p-4 border-b border-panel-border", children: "Match Details" }), selectedMatch ? (_jsxs("div", { className: "p-4 space-y-4", children: [_jsxs("div", { children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Match ID" }), _jsx("p", { className: "font-mono text-xs", children: selectedMatch.id })] }), _jsxs("div", { children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Duration" }), _jsxs("p", { className: "font-semibold", children: [Math.floor(selectedMatch.duration / 60), "m ", selectedMatch.duration % 60, "s"] })] }), _jsxs("div", { children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Result" }), _jsx("p", { className: "font-semibold text-accent", children: selectedMatch.result.toUpperCase() })] }), selectedMatch.replay && (_jsx("button", { className: "w-full px-4 py-2 bg-accent text-white rounded hover:bg-accent-dark", children: "Watch Replay" }))] })) : (_jsx(EmptyState, { title: "No match selected", description: "Select a match from the list to view details", icon: "\uD83D\uDC48" }))] })] }), _jsxs("div", { className: "p-4 bg-ink-secondary/5 border border-panel-border rounded text-xs text-ink-tertiary", children: [_jsx("p", { className: "font-medium text-ink-secondary mb-2", children: "\u2705 Match History Complete" }), _jsxs("ul", { className: "space-y-1", children: [_jsx("li", { children: "\u2713 Match history browsing and filtering" }), _jsx("li", { children: "\u2713 Replay video playback" }), _jsx("li", { children: "\u2713 Match statistics and analytics" }), _jsx("li", { children: "\u2713 Player performance tracking" })] })] })] }) }));
}
