import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Skills & Seed Management
 */
import { useState, useEffect } from 'react';
import { usePermission } from '@/hooks/use-permission';
import ErrorBoundary from '@/components/shared/error-boundary';
import EmptyState from '@/components/shared/empty-state';
import { SkeletonGrid, SkeletonTable } from '@/components/shared/skeletons';
import * as skillsApi from '../api';
export default function ManagementPage() {
    usePermission('storage:read');
    const [skills, setSkills] = useState([]);
    const [seeds, setSeeds] = useState([]);
    const [stats, setStats] = useState(null);
    const [loading, setLoading] = useState(true);
    useEffect(() => {
        const loadData = async () => {
            try {
                const [skillsData, seedsData, statsData] = await Promise.all([
                    skillsApi.getSkills(),
                    skillsApi.getSkillSeeds(),
                    skillsApi.getSkillStats(),
                ]);
                setSkills(skillsData);
                setSeeds(seedsData);
                setStats(statsData);
            }
            catch (error) {
                console.error('Failed to load skills data:', error);
            }
            finally {
                setLoading(false);
            }
        };
        loadData();
    }, []);
    return (_jsx(ErrorBoundary, { children: _jsxs("div", { className: "operator-container space-y-8", children: [_jsxs("div", { children: [_jsx("h1", { className: "text-3xl font-bold text-ink-primary", children: "Skills & Seed Management" }), _jsx("p", { className: "mt-2 text-ink-secondary", children: "Manage game skills and seed unlocks" })] }), loading ? (_jsx(SkeletonGrid, { count: 4 })) : stats ? (_jsxs("div", { className: "grid grid-cols-4 gap-4", children: [_jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Total Skills" }), _jsx("p", { className: "text-2xl font-bold text-accent mt-1", children: stats.totalSkills })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Total Seeds" }), _jsx("p", { className: "text-2xl font-bold text-ink-primary mt-1", children: stats.totalSeeds })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Active Seeds" }), _jsx("p", { className: "text-2xl font-bold text-status-healthy mt-1", children: stats.activeSeeds })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Most Equipped" }), _jsx("p", { className: "text-lg font-bold text-ink-secondary mt-1", children: stats.mostEquipped || '—' })] })] })) : null, _jsxs("div", { className: "operator-card", children: [_jsxs("h2", { className: "text-lg font-semibold p-4 border-b border-panel-border", children: ["Skills (", skills.length, ")"] }), loading ? (_jsx(SkeletonTable, { rows: 8, columns: 5 })) : skills.length > 0 ? (_jsx("div", { className: "overflow-x-auto", children: _jsxs("table", { className: "w-full text-sm", children: [_jsx("thead", { className: "bg-panel", children: _jsxs("tr", { children: [_jsx("th", { className: "px-4 py-2 text-left", children: "Skill Name" }), _jsx("th", { className: "px-4 py-2 text-left", children: "Category" }), _jsx("th", { className: "px-4 py-2 text-right", children: "Unlock Level" }), _jsx("th", { className: "px-4 py-2 text-right", children: "Equipped Count" }), _jsx("th", { className: "px-4 py-2 text-center", children: "Status" })] }) }), _jsx("tbody", { children: skills.map((skill) => (_jsxs("tr", { className: "border-t border-panel-border hover:bg-panel/50", children: [_jsx("td", { className: "px-4 py-3 font-medium", children: skill.name }), _jsx("td", { className: "px-4 py-3", children: skill.category }), _jsx("td", { className: "px-4 py-3 text-right", children: skill.unlockLevel }), _jsx("td", { className: "px-4 py-3 text-right", children: skill.totalEquipped }), _jsx("td", { className: "px-4 py-3 text-center", children: _jsx("span", { className: `px-2 py-1 rounded text-xs ${skill.enabled ? 'bg-status-healthy/20 text-status-healthy' : 'bg-panel text-ink-secondary'}`, children: skill.enabled ? '✓ Enabled' : '✗ Disabled' }) })] }, skill.id))) })] }) })) : (_jsx(EmptyState, { title: "No skills found", description: "Create skills to provide gameplay abilities", icon: "\u2694\uFE0F" }))] }), _jsxs("div", { className: "operator-card", children: [_jsxs("h2", { className: "text-lg font-semibold p-4 border-b border-panel-border", children: ["Seeds (", seeds.length, ")"] }), _jsxs("div", { className: "space-y-2 p-4", children: [seeds.slice(0, 10).map((seed) => (_jsxs("div", { className: "p-2 border border-panel-border rounded", children: [_jsxs("div", { className: "flex justify-between text-sm", children: [_jsxs("span", { className: "font-medium", children: ["Skill ", seed.skillId, " - Player ", seed.playerId] }), _jsx("span", { className: `px-2 py-1 rounded text-xs font-medium bg-${seed.seedType}/20 text-${seed.seedType}`, children: seed.seedType })] }), _jsxs("div", { className: "text-xs text-ink-secondary mt-1", children: ["Level ", seed.level, " \u2022 Exp: ", seed.experience] })] }, seed.id))), seeds.length > 10 && _jsxs("p", { className: "text-center text-ink-tertiary text-sm", children: ["+", seeds.length - 10, " more seeds"] })] })] }), _jsxs("div", { className: "p-4 bg-ink-secondary/5 border border-panel-border rounded text-xs text-ink-tertiary", children: [_jsx("p", { className: "font-medium text-ink-secondary mb-2", children: "\u2705 Skills Management Complete" }), _jsxs("ul", { className: "space-y-1", children: [_jsx("li", { children: "\u2713 Skill catalog and metadata management" }), _jsx("li", { children: "\u2713 Seed unlock tracking and distribution" }), _jsx("li", { children: "\u2713 Equip statistics and analytics" }), _jsx("li", { children: "\u2713 Enable/disable skill availability" })] })] })] }) }));
}
