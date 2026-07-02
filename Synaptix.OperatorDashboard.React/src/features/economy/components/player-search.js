import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Player search dropdown
 */
import { useState } from 'react';
import { useSearchPlayers } from '../hooks/useEconomy';
export function PlayerSearch({ onSelectPlayer }) {
    const [searchQuery, setSearchQuery] = useState('');
    const [showResults, setShowResults] = useState(false);
    const searchResults = useSearchPlayers(searchQuery, 20);
    return (_jsxs("div", { className: "operator-card space-y-3 relative", children: [_jsx("label", { htmlFor: "search", className: "block text-sm font-medium text-ink-primary", children: "Search Player" }), _jsxs("div", { className: "relative", children: [_jsx("input", { id: "search", type: "text", value: searchQuery, onChange: (e) => {
                            setSearchQuery(e.target.value);
                            setShowResults(true);
                        }, onFocus: () => setShowResults(true), placeholder: "Email, handle, or player ID...", className: "w-full px-3 py-2 border border-panel-border rounded focus-ring" }), showResults && searchQuery && (_jsx("div", { className: "absolute top-full left-0 right-0 mt-1 bg-bg-primary border border-panel-border rounded shadow-lg z-10 max-h-60 overflow-y-auto", children: searchResults.isLoading ? (_jsx("div", { className: "p-3 text-sm text-ink-secondary", children: "Searching..." })) : searchResults.data && searchResults.data.length > 0 ? (_jsx("div", { children: searchResults.data.map((player) => (_jsxs("button", { onClick: () => {
                                    onSelectPlayer(player.playerId);
                                    setSearchQuery('');
                                    setShowResults(false);
                                }, className: "w-full text-left px-3 py-2 hover:bg-bg-secondary transition-colors border-b border-panel-border last:border-b-0", children: [_jsx("p", { className: "font-medium text-ink-primary text-sm", children: player.handle }), _jsx("p", { className: "text-xs text-ink-secondary", children: player.email }), _jsxs("p", { className: "text-xs text-ink-tertiary", children: ["Balance: ", player.currentBalance.toLocaleString()] })] }, player.playerId))) })) : (_jsx("div", { className: "p-3 text-sm text-ink-secondary", children: "No players found" })) }))] })] }));
}
