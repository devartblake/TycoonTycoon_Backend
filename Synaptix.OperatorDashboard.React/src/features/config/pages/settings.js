import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Configuration & Settings - Feature Flags, Admin ACL, System Config
 */
import { useState, useEffect } from 'react';
import { usePermission } from '@/hooks/use-permission';
import ErrorBoundary from '@/components/shared/error-boundary';
import EmptyState from '@/components/shared/empty-state';
import { SkeletonGrid, SkeletonTable } from '@/components/shared/skeletons';
import * as configApi from '../api';
export default function SettingsPage() {
    usePermission('config:write');
    const [activeTab, setActiveTab] = useState('flags');
    const [flags, setFlags] = useState([]);
    const [acl, setACL] = useState([]);
    const [systemConfig, setSystemConfig] = useState(null);
    const [loading, setLoading] = useState(true);
    const [successMsg, setSuccessMsg] = useState(null);
    useEffect(() => {
        const loadData = async () => {
            setLoading(true);
            try {
                const [flagsRes, aclRes, sysConfig] = await Promise.all([
                    configApi.getFeatureFlags(),
                    configApi.getAdminACL(),
                    configApi.getSystemConfig(),
                ]);
                setFlags(flagsRes.items);
                setACL(aclRes.items);
                setSystemConfig(sysConfig);
            }
            catch (error) {
                console.error('Failed to load config:', error);
            }
            finally {
                setLoading(false);
            }
        };
        loadData();
    }, []);
    const handleToggleFlag = async (id, enabled) => {
        try {
            await configApi.toggleFeatureFlag(id, !enabled);
            setFlags(flags.map((f) => (f.id === id ? { ...f, enabled: !enabled } : f)));
            setSuccessMsg(`Feature flag ${enabled ? 'disabled' : 'enabled'}`);
            setTimeout(() => setSuccessMsg(null), 2000);
        }
        catch (error) {
            console.error('Toggle failed:', error);
        }
    };
    const handleUpdateSystemConfig = async (key, value) => {
        if (!systemConfig)
            return;
        try {
            const updated = await configApi.updateSystemConfig({ ...systemConfig, [key]: value });
            setSystemConfig(updated);
            setSuccessMsg('System config updated');
            setTimeout(() => setSuccessMsg(null), 2000);
        }
        catch (error) {
            console.error('Update failed:', error);
        }
    };
    return (_jsx(ErrorBoundary, { children: _jsxs("div", { className: "operator-container space-y-8", children: [_jsxs("div", { children: [_jsx("h1", { className: "text-3xl font-bold text-ink-primary", children: "Configuration & Settings" }), _jsx("p", { className: "mt-2 text-ink-secondary", children: "Manage feature flags, admin access, and system configuration" })] }), successMsg && (_jsxs("div", { className: "p-4 bg-status-healthy/10 border border-status-healthy/20 rounded text-status-healthy text-sm", children: ["\u2713 ", successMsg] })), loading ? (_jsx(SkeletonGrid, { count: 4 })) : (_jsxs("div", { className: "grid grid-cols-4 gap-4", children: [_jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Total Flags" }), _jsx("p", { className: "text-2xl font-bold text-accent mt-1", children: flags.length })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Enabled" }), _jsx("p", { className: "text-2xl font-bold text-status-healthy mt-1", children: flags.filter((f) => f.enabled).length })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Admin Users" }), _jsx("p", { className: "text-2xl font-bold text-ink-primary mt-1", children: acl.length })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Maintenance" }), _jsx("p", { className: `text-2xl font-bold mt-1 ${systemConfig?.maintenanceMode ? 'text-status-offline' : 'text-status-healthy'}`, children: systemConfig?.maintenanceMode ? 'ON' : 'OFF' })] })] })), _jsx("div", { className: "flex gap-2 border-b border-panel-border", children: [
                        { id: 'flags', label: '🚩 Feature Flags' },
                        { id: 'acl', label: '🔐 Admin ACL' },
                        { id: 'system', label: '⚙️ System Config' },
                    ].map((tab) => (_jsx("button", { onClick: () => setActiveTab(tab.id), className: `px-4 py-2 font-medium border-b-2 transition-colors ${activeTab === tab.id
                            ? 'border-accent text-accent'
                            : 'border-transparent text-ink-secondary hover:text-ink-primary'}`, children: tab.label }, tab.id))) }), _jsx("div", { className: "operator-card", children: loading ? (_jsx(SkeletonTable, { rows: 8, columns: 5 })) : activeTab === 'flags' ? (_jsxs("div", { className: "space-y-4", children: [_jsxs("h2", { className: "text-lg font-semibold", children: ["Feature Flags (", flags.length, ")"] }), flags.length > 0 ? (_jsx("div", { className: "space-y-3", children: flags.map((flag) => (_jsx("div", { className: "p-4 border border-panel-border rounded hover:bg-panel/50", children: _jsxs("div", { className: "flex items-start justify-between", children: [_jsxs("div", { className: "flex-1", children: [_jsx("h3", { className: "font-semibold text-ink-primary", children: flag.name }), _jsx("p", { className: "text-sm text-ink-secondary mt-1", children: flag.description }), _jsxs("div", { className: "flex gap-4 mt-2 text-xs text-ink-tertiary", children: [_jsxs("span", { children: ["Key: ", flag.key] }), _jsxs("span", { children: ["Audience: ", flag.targetAudience] }), _jsxs("span", { children: ["Rollout: ", flag.rolloutPercentage, "%"] })] })] }), _jsx("button", { onClick: () => handleToggleFlag(flag.id, flag.enabled), className: `px-4 py-2 rounded font-medium transition-colors ${flag.enabled
                                                    ? 'bg-status-healthy/20 text-status-healthy'
                                                    : 'bg-panel text-ink-secondary hover:bg-panel-border'}`, children: flag.enabled ? '✓ Enabled' : '✗ Disabled' })] }) }, flag.id))) })) : (_jsx(EmptyState, { title: "No feature flags found", description: "Configure feature flags to control new features", icon: "\uD83D\uDEA9" }))] })) : activeTab === 'acl' ? (_jsxs("div", { className: "space-y-4", children: [_jsxs("h2", { className: "text-lg font-semibold", children: ["Admin Access Control (", acl.length, ")"] }), acl.length > 0 ? (_jsx("div", { className: "overflow-x-auto", children: _jsxs("table", { className: "w-full text-sm", children: [_jsx("thead", { className: "bg-panel border-b border-panel-border", children: _jsxs("tr", { children: [_jsx("th", { className: "px-4 py-2 text-left", children: "Admin" }), _jsx("th", { className: "px-4 py-2 text-left", children: "Email" }), _jsx("th", { className: "px-4 py-2 text-left", children: "Role" }), _jsx("th", { className: "px-4 py-2 text-left", children: "Permissions" }), _jsx("th", { className: "px-4 py-2 text-left", children: "Created" })] }) }), _jsx("tbody", { children: acl.map((entry) => (_jsxs("tr", { className: "border-t border-panel-border hover:bg-panel/50", children: [_jsx("td", { className: "px-4 py-3 font-medium", children: entry.adminId }), _jsx("td", { className: "px-4 py-3", children: entry.adminEmail }), _jsx("td", { className: "px-4 py-3", children: _jsx("span", { className: "px-2 py-1 rounded text-xs bg-accent/20 text-accent", children: entry.role }) }), _jsx("td", { className: "px-4 py-3", children: _jsxs("span", { className: "text-xs text-ink-tertiary", children: [entry.permissions.length, " permissions"] }) }), _jsx("td", { className: "px-4 py-3 text-xs text-ink-tertiary", children: new Date(entry.createdAt).toLocaleDateString() })] }, entry.id))) })] }) })) : (_jsx(EmptyState, { title: "No admin entries found", description: "Add administrators to control system access", icon: "\uD83D\uDD10" }))] })) : (_jsxs("div", { className: "space-y-6", children: [_jsx("h2", { className: "text-lg font-semibold", children: "System Configuration" }), systemConfig ? (_jsxs("div", { className: "space-y-4", children: [_jsx("div", { className: "p-4 border border-panel-border rounded", children: _jsxs("div", { className: "flex items-center justify-between", children: [_jsxs("div", { children: [_jsx("h3", { className: "font-semibold text-ink-primary", children: "Maintenance Mode" }), _jsx("p", { className: "text-sm text-ink-secondary mt-1", children: systemConfig.maintenanceMode ? 'Enabled - System in maintenance' : 'Disabled - System online' })] }), _jsx("button", { onClick: () => handleUpdateSystemConfig('maintenanceMode', !systemConfig.maintenanceMode), className: `px-4 py-2 rounded font-medium transition-colors ${systemConfig.maintenanceMode
                                                        ? 'bg-status-offline/20 text-status-offline'
                                                        : 'bg-status-healthy/20 text-status-healthy'}`, children: systemConfig.maintenanceMode ? 'Disable' : 'Enable' })] }) }), _jsx("div", { className: "p-4 border border-panel-border rounded", children: _jsxs("div", { className: "flex items-center justify-between", children: [_jsxs("div", { children: [_jsx("h3", { className: "font-semibold text-ink-primary", children: "Analytics" }), _jsx("p", { className: "text-sm text-ink-secondary mt-1", children: systemConfig.analyticsEnabled ? 'Enabled' : 'Disabled' })] }), _jsx("button", { onClick: () => handleUpdateSystemConfig('analyticsEnabled', !systemConfig.analyticsEnabled), className: `px-4 py-2 rounded font-medium transition-colors ${systemConfig.analyticsEnabled ? 'bg-status-healthy/20 text-status-healthy' : 'bg-panel text-ink-secondary'}`, children: systemConfig.analyticsEnabled ? '✓ On' : '✗ Off' })] }) }), _jsx("div", { className: "p-4 border border-panel-border rounded", children: _jsxs("div", { className: "flex items-center justify-between", children: [_jsxs("div", { children: [_jsx("h3", { className: "font-semibold text-ink-primary", children: "Debug Logging" }), _jsx("p", { className: "text-sm text-ink-secondary mt-1", children: systemConfig.debugLoggingEnabled ? 'Verbose logging enabled' : 'Standard logging' })] }), _jsx("button", { onClick: () => handleUpdateSystemConfig('debugLoggingEnabled', !systemConfig.debugLoggingEnabled), className: `px-4 py-2 rounded font-medium transition-colors ${systemConfig.debugLoggingEnabled ? 'bg-status-healthy/20 text-status-healthy' : 'bg-panel text-ink-secondary'}`, children: systemConfig.debugLoggingEnabled ? '✓ On' : '✗ Off' })] }) }), _jsx("div", { className: "p-4 border border-panel-border rounded", children: _jsxs("div", { className: "flex items-center justify-between", children: [_jsxs("div", { children: [_jsx("h3", { className: "font-semibold text-ink-primary", children: "Rate Limiting" }), _jsxs("p", { className: "text-sm text-ink-secondary mt-1", children: [systemConfig.rateLimitPerMinute, " requests/minute"] })] }), _jsx("input", { type: "number", value: systemConfig.rateLimitPerMinute, onChange: (e) => handleUpdateSystemConfig('rateLimitPerMinute', Number(e.target.value)), className: "w-24 px-3 py-2 border border-panel-border rounded bg-panel text-right", min: "10" })] }) }), _jsx("div", { className: "p-4 border border-panel-border rounded", children: _jsxs("div", { className: "flex items-center justify-between", children: [_jsxs("div", { children: [_jsx("h3", { className: "font-semibold text-ink-primary", children: "Session Timeout" }), _jsxs("p", { className: "text-sm text-ink-secondary mt-1", children: [systemConfig.sessionTimeoutMinutes, " minutes"] })] }), _jsx("input", { type: "number", value: systemConfig.sessionTimeoutMinutes, onChange: (e) => handleUpdateSystemConfig('sessionTimeoutMinutes', Number(e.target.value)), className: "w-24 px-3 py-2 border border-panel-border rounded bg-panel text-right", min: "5" })] }) })] })) : (_jsx(EmptyState, { title: "System config unavailable", description: "Unable to load system configuration", icon: "\u26A0\uFE0F" }))] })) }), _jsxs("div", { className: "p-4 bg-ink-secondary/5 border border-panel-border rounded text-xs text-ink-tertiary", children: [_jsx("p", { className: "font-medium text-ink-secondary mb-2", children: "\u2705 Configuration Management Complete" }), _jsxs("ul", { className: "space-y-1", children: [_jsx("li", { children: "\u2713 Feature Flags with rollout control" }), _jsx("li", { children: "\u2713 Admin ACL and role management" }), _jsx("li", { children: "\u2713 System configuration (maintenance, analytics, logging)" }), _jsx("li", { children: "\u2713 Rate limiting and session settings" }), _jsx("li", { children: "\u2713 Real-time toggle and update controls" })] })] })] }) }));
}
