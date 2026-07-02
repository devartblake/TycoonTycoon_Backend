import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Sidebar navigation
 */
import { useState } from 'react';
import { NavLink } from 'react-router-dom';
import { PermissionGate } from '@/components/shared/permission-gate';
const NAV_ITEMS = [
    {
        label: 'Dashboard',
        href: '/dashboard',
    },
    {
        label: 'Users & Moderation',
        permission: 'users:read',
        children: [
            { label: 'Users', href: '/users' },
            { label: 'Moderation', href: '/moderation/logs' },
            { label: 'Anti-Cheat', href: '/anti-cheat', permission: 'anti-cheat:read' },
        ],
    },
    {
        label: 'Audit & Security',
        permission: 'audit:read',
        children: [
            { label: 'Security Audit', href: '/audit/security' },
        ],
    },
    {
        label: 'Store Management',
        permission: 'storage:read',
        children: [
            { label: 'Catalog', href: '/store/catalog' },
            { label: 'Flash Sales', href: '/store/flash-sales' },
            { label: 'Stock Policies', href: '/store/stock-policies' },
            { label: 'Player Stock', href: '/store/player-stock' },
            { label: 'Reward Limits', href: '/store/reward-limits' },
            { label: 'Analytics', href: '/store/analytics' },
        ],
    },
    {
        label: 'Content',
        permission: 'content:read',
        children: [
            { label: 'Questions', href: '/content/questions' },
        ],
    },
    {
        label: 'Operations',
        permission: 'operations:read',
        children: [
            { label: 'Events', href: '/events/game-events' },
            { label: 'Seasons', href: '/operations/seasons' },
            { label: 'Notifications', href: '/notifications', permission: 'notifications:read' },
            { label: 'Event Queue', href: '/operations/event-queue' },
        ],
    },
    {
        label: 'Economy & Rewards',
        permission: 'economy:read',
        children: [
            { label: 'Player Economy', href: '/economy/player' },
            { label: 'Transactions', href: '/economy/player-transactions' },
            { label: 'Powerups', href: '/economy/powerups' },
            { label: 'Season Points', href: '/operations/season-points' },
        ],
    },
    {
        label: 'Personalization',
        permission: 'personalization:read',
        children: [
            { label: 'Overview', href: '/personalization' },
            { label: 'Rules', href: '/personalization/rules' },
        ],
    },
    {
        label: 'Configuration',
        permission: 'config:read',
        children: [
            { label: 'Feature Flags', href: '/config/feature-flags' },
            { label: 'Admin ACL', href: '/config/admin-permissions' },
            { label: 'Diagnostics', href: '/settings/setup' },
        ],
    },
];
export function Sidebar() {
    const [expandedItems, setExpandedItems] = useState(new Set());
    const toggleExpanded = (label) => {
        const newExpanded = new Set(expandedItems);
        if (newExpanded.has(label)) {
            newExpanded.delete(label);
        }
        else {
            newExpanded.add(label);
        }
        setExpandedItems(newExpanded);
    };
    return (_jsxs("aside", { className: "operator-sidebar", children: [_jsxs("div", { className: "mb-8", children: [_jsx("h1", { className: "text-xl font-bold text-ink-primary", children: "Synaptix" }), _jsx("p", { className: "text-xs text-ink-tertiary", children: "Operator Dashboard" })] }), _jsx("nav", { className: "space-y-1 flex-1", children: NAV_ITEMS.map((item) => (_jsx(PermissionGate, { permission: item.permission, fallback: null, children: item.children ? (_jsxs("div", { children: [_jsxs("button", { onClick: () => toggleExpanded(item.label), className: "w-full flex items-center gap-3 px-3 py-2 text-sm rounded hover:bg-bg-secondary transition-smooth text-ink-secondary hover:text-ink-primary", children: [_jsx("span", { className: "font-medium", children: item.label }), _jsx("span", { className: "ml-auto", children: expandedItems.has(item.label) ? '−' : '+' })] }), expandedItems.has(item.label) && (_jsx("div", { className: "pl-4 space-y-1 mt-1", children: item.children.map((child) => {
                                    const childPermission = 'permission' in child ? child.permission : undefined;
                                    return (_jsx(PermissionGate, { permission: childPermission, fallback: null, children: _jsx(NavLink, { to: child.href, className: ({ isActive }) => `block px-3 py-2 text-xs rounded transition-smooth ${isActive
                                                ? 'bg-accent text-white'
                                                : 'text-ink-tertiary hover:text-ink-secondary hover:bg-bg-secondary'}`, children: child.label }) }, child.href));
                                }) }))] }, item.label)) : (_jsx(NavLink, { to: item.href, className: ({ isActive }) => `flex items-center gap-3 px-3 py-2 rounded transition-smooth ${isActive
                            ? 'bg-accent text-white'
                            : 'text-ink-secondary hover:text-ink-primary hover:bg-bg-secondary'}`, children: _jsx("span", { className: "text-sm font-medium", children: item.label }) })) }, item.label))) })] }));
}
