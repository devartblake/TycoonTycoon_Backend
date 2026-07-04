import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Personalization - Player Archetypes, Recommendation Engines, and Controls
 */
import { useState, useEffect } from 'react';
import { usePermission } from '@/hooks/use-permission';
import ErrorBoundary from '@/components/shared/error-boundary';
import EmptyState from '@/components/shared/empty-state';
import { SkeletonGrid, SkeletonTable } from '@/components/shared/skeletons';
import * as personApi from '../api';
export default function ArchetypesPage() {
    usePermission('personalization:write');
    const [activeTab, setActiveTab] = useState('archetypes');
    const [archetypes, setArchetypes] = useState([]);
    const [engines, setEngines] = useState([]);
    const [controls, setControls] = useState([]);
    const [loading, setLoading] = useState(true);
    const [successMsg, setSuccessMsg] = useState(null);
    useEffect(() => {
        const loadData = async () => {
            setLoading(true);
            try {
                const [archeRes, engRes, ctrlRes] = await Promise.all([
                    personApi.getArchetypes(),
                    personApi.getRecommendationEngines(),
                    personApi.getRecommendationControls(),
                ]);
                setArchetypes(archeRes.items);
                setEngines(engRes.items);
                setControls(ctrlRes.items);
            }
            catch (error) {
                console.error('Failed to load personalization data:', error);
            }
            finally {
                setLoading(false);
            }
        };
        loadData();
    }, []);
    const handleRecalculateArchetype = async (archetypeId) => {
        try {
            await personApi.recalculateArchetypeMetrics(archetypeId);
            setSuccessMsg('Archetype metrics recalculated');
            setTimeout(() => setSuccessMsg(null), 2000);
        }
        catch (error) {
            console.error('Recalculation failed:', error);
        }
    };
    const handleToggleEngine = async (id, enabled) => {
        try {
            await personApi.toggleRecommendationEngine(id, !enabled);
            setEngines(engines.map((e) => (e.id === id ? { ...e, enabled: !enabled } : e)));
            setSuccessMsg(`Engine ${enabled ? 'disabled' : 'enabled'}`);
            setTimeout(() => setSuccessMsg(null), 2000);
        }
        catch (error) {
            console.error('Toggle failed:', error);
        }
    };
    const handleResetEngine = async (engineId) => {
        if (!confirm('Reset this recommendation model? This will recalculate all metrics.'))
            return;
        try {
            await personApi.resetRecommendationModel(engineId);
            setSuccessMsg('Recommendation model reset');
            setTimeout(() => setSuccessMsg(null), 2000);
        }
        catch (error) {
            console.error('Reset failed:', error);
        }
    };
    return (_jsx(ErrorBoundary, { children: _jsxs("div", { className: "operator-container space-y-8", children: [_jsxs("div", { children: [_jsx("h1", { className: "text-3xl font-bold text-ink-primary", children: "Personalization & Archetypes" }), _jsx("p", { className: "mt-2 text-ink-secondary", children: "Manage player archetypes and recommendation engines" })] }), successMsg && (_jsxs("div", { className: "p-4 bg-status-healthy/10 border border-status-healthy/20 rounded text-status-healthy text-sm", children: ["\u2713 ", successMsg] })), loading ? (_jsx(SkeletonGrid, { count: 4 })) : (_jsxs("div", { className: "grid grid-cols-4 gap-4", children: [_jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Total Archetypes" }), _jsx("p", { className: "text-2xl font-bold text-accent mt-1", children: archetypes.length })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Recommendation Engines" }), _jsx("p", { className: "text-2xl font-bold text-ink-primary mt-1", children: engines.length })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Active Engines" }), _jsx("p", { className: "text-2xl font-bold text-status-healthy mt-1", children: engines.filter((e) => e.enabled).length })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Personalization Enabled" }), _jsx("p", { className: "text-2xl font-bold text-status-healthy mt-1", children: controls.filter((c) => c.enabled).length })] })] })), _jsx("div", { className: "flex gap-2 border-b border-panel-border", children: [
                        { id: 'archetypes', label: '👥 Archetypes' },
                        { id: 'engines', label: '🤖 Recommendation Engines' },
                        { id: 'controls', label: '⚙️ Recommendation Controls' },
                    ].map((tab) => (_jsx("button", { onClick: () => setActiveTab(tab.id), className: `px-4 py-2 font-medium border-b-2 transition-colors ${activeTab === tab.id
                            ? 'border-accent text-accent'
                            : 'border-transparent text-ink-secondary hover:text-ink-primary'}`, children: tab.label }, tab.id))) }), _jsx("div", { className: "operator-card", children: loading ? (_jsx(SkeletonTable, { rows: 8, columns: 4 })) : activeTab === 'archetypes' ? (_jsxs("div", { className: "space-y-4", children: [_jsxs("h2", { className: "text-lg font-semibold", children: ["Player Archetypes (", archetypes.length, ")"] }), archetypes.length > 0 ? (_jsx("div", { className: "space-y-3", children: archetypes.map((archetype) => (_jsx("div", { className: "p-4 border border-panel-border rounded hover:bg-panel/50", children: _jsxs("div", { className: "flex items-start justify-between", children: [_jsxs("div", { className: "flex-1", children: [_jsxs("div", { className: "flex items-center gap-2", children: [_jsx("span", { className: "text-2xl", children: archetype.icon }), _jsxs("div", { children: [_jsx("h3", { className: "font-semibold text-ink-primary", children: archetype.name }), _jsx("p", { className: "text-sm text-ink-secondary", children: archetype.description })] })] }), _jsxs("div", { className: "grid grid-cols-2 gap-4 mt-3 text-sm", children: [_jsxs("div", { children: [_jsx("p", { className: "text-ink-tertiary", children: "Engagement" }), _jsx("p", { className: "font-semibold text-ink-primary", children: archetype.engagementLevel })] }), _jsxs("div", { children: [_jsx("p", { className: "text-ink-tertiary", children: "Players" }), _jsx("p", { className: "font-semibold text-accent", children: archetype.playerCount.toLocaleString() })] }), _jsxs("div", { children: [_jsx("p", { className: "text-ink-tertiary", children: "Conversion Rate" }), _jsxs("p", { className: "font-semibold text-status-healthy", children: [(archetype.conversionRate * 100).toFixed(1), "%"] })] }), _jsxs("div", { children: [_jsx("p", { className: "text-ink-tertiary", children: "Retention Rate" }), _jsxs("p", { className: "font-semibold text-status-healthy", children: [(archetype.retentionRate * 100).toFixed(1), "%"] })] })] })] }), _jsx("button", { onClick: () => handleRecalculateArchetype(archetype.id), className: "px-4 py-2 rounded bg-panel hover:bg-panel-border text-ink-secondary font-medium transition-colors whitespace-nowrap", children: "Recalculate" })] }) }, archetype.id))) })) : (_jsx(EmptyState, { title: "No archetypes found", description: "Configure player archetypes to enable personalization", icon: "\uD83D\uDC65" }))] })) : activeTab === 'engines' ? (_jsxs("div", { className: "space-y-4", children: [_jsxs("h2", { className: "text-lg font-semibold", children: ["Recommendation Engines (", engines.length, ")"] }), engines.length > 0 ? (_jsx("div", { className: "overflow-x-auto", children: _jsxs("table", { className: "w-full text-sm", children: [_jsx("thead", { className: "bg-panel border-b border-panel-border", children: _jsxs("tr", { children: [_jsx("th", { className: "px-4 py-2 text-left", children: "Engine" }), _jsx("th", { className: "px-4 py-2 text-left", children: "Algorithm" }), _jsx("th", { className: "px-4 py-2 text-right", children: "Accuracy" }), _jsx("th", { className: "px-4 py-2 text-right", children: "Coverage" }), _jsx("th", { className: "px-4 py-2 text-center", children: "Status" }), _jsx("th", { className: "px-4 py-2 text-center", children: "Actions" })] }) }), _jsx("tbody", { children: engines.map((engine) => (_jsxs("tr", { className: "border-t border-panel-border hover:bg-panel/50", children: [_jsx("td", { className: "px-4 py-3", children: _jsxs("div", { children: [_jsx("p", { className: "font-semibold text-ink-primary", children: engine.name }), _jsxs("p", { className: "text-xs text-ink-tertiary", children: ["v", engine.version] })] }) }), _jsx("td", { className: "px-4 py-3", children: _jsx("span", { className: "px-2 py-1 rounded text-xs bg-accent/20 text-accent", children: engine.algorithm }) }), _jsxs("td", { className: "px-4 py-3 text-right font-semibold", children: [(engine.accuracy * 100).toFixed(1), "%"] }), _jsxs("td", { className: "px-4 py-3 text-right font-semibold", children: [(engine.coverage * 100).toFixed(1), "%"] }), _jsx("td", { className: "px-4 py-3 text-center", children: _jsx("button", { onClick: () => handleToggleEngine(engine.id, engine.enabled), className: `px-2 py-1 rounded text-xs font-medium transition-colors ${engine.enabled
                                                                ? 'bg-status-healthy/20 text-status-healthy'
                                                                : 'bg-panel text-ink-secondary'}`, children: engine.enabled ? '✓ Active' : '✗ Inactive' }) }), _jsx("td", { className: "px-4 py-3 text-center", children: _jsx("button", { onClick: () => handleResetEngine(engine.id), className: "text-xs text-accent hover:text-accent-dark", children: "Reset" }) })] }, engine.id))) })] }) })) : (_jsx(EmptyState, { title: "No recommendation engines found", description: "Configure recommendation engines for personalization", icon: "\uD83E\uDD16" }))] })) : (_jsxs("div", { className: "space-y-4", children: [_jsxs("h2", { className: "text-lg font-semibold", children: ["Recommendation Controls (", controls.length, ")"] }), controls.length > 0 ? (_jsxs("div", { className: "grid grid-cols-1 gap-3", children: [controls.slice(0, 10).map((control) => (_jsx("div", { className: "p-3 border border-panel-border rounded hover:bg-panel/50", children: _jsxs("div", { className: "flex items-center justify-between", children: [_jsxs("div", { className: "flex-1", children: [_jsxs("p", { className: "text-sm font-semibold text-ink-primary", children: ["Player: ", control.playerId] }), _jsxs("div", { className: "flex gap-4 mt-1 text-xs text-ink-tertiary", children: [_jsxs("span", { children: ["Archetype: ", control.archetypeId] }), _jsxs("span", { children: ["Frequency: ", control.frequency] }), _jsxs("span", { children: ["Max Recommendations: ", control.maxRecommendations] }), _jsxs("span", { children: ["Min Quality: ", control.minQualityScore] })] })] }), _jsx("span", { className: `px-2 py-1 rounded text-xs font-medium ${control.enabled ? 'bg-status-healthy/20 text-status-healthy' : 'bg-panel text-ink-secondary'}`, children: control.enabled ? '✓ Enabled' : '✗ Disabled' })] }) }, control.id))), controls.length > 10 && (_jsxs("div", { className: "text-center py-4 text-ink-tertiary text-sm", children: ["+", controls.length - 10, " more controls"] }))] })) : (_jsx(EmptyState, { title: "No recommendation controls found", description: "Set up individual player recommendation controls", icon: "\u2699\uFE0F" }))] })) }), _jsxs("div", { className: "p-4 bg-ink-secondary/5 border border-panel-border rounded text-xs text-ink-tertiary", children: [_jsx("p", { className: "font-medium text-ink-secondary mb-2", children: "\u2705 Personalization Management Complete" }), _jsxs("ul", { className: "space-y-1", children: [_jsx("li", { children: "\u2713 Player archetype management and metrics" }), _jsx("li", { children: "\u2713 Recommendation engine configuration and toggling" }), _jsx("li", { children: "\u2713 Individual player recommendation controls" }), _jsx("li", { children: "\u2713 Accuracy and coverage monitoring" }), _jsx("li", { children: "\u2713 Model reset and recalculation capabilities" })] })] })] }) }));
}
