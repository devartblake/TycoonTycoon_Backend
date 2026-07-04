import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Notifications Hub page
 */
import { useState } from 'react';
import { usePermission } from '@/hooks/use-permission';
import ErrorBoundary from '@/components/shared/error-boundary';
import { TemplatesTab } from '../components/templates-tab';
import { ChannelsTab } from '../components/channels-tab';
import { ScheduleTab } from '../components/schedule-tab';
import { DeadLetterTab } from '../components/dead-letter-tab';
const TABS = [
    { id: 'templates', label: 'Templates', icon: '📧' },
    { id: 'channels', label: 'Channels', icon: '📢' },
    { id: 'schedule', label: 'Schedule', icon: '📅' },
    { id: 'dead-letter', label: 'Failed', icon: '⚠️' },
];
export default function NotificationsHub() {
    usePermission('notifications:read');
    const [activeTab, setActiveTab] = useState('templates');
    return (_jsx(ErrorBoundary, { children: _jsxs("div", { className: "operator-container space-y-6", children: [_jsxs("div", { children: [_jsx("h1", { className: "text-3xl font-bold text-ink-primary", children: "Notifications Hub" }), _jsx("p", { className: "mt-2 text-ink-secondary", children: "Manage notification templates, channels, and schedules" })] }), _jsx("div", { className: "flex gap-2 border-b border-panel-border", children: TABS.map((tab) => (_jsxs("button", { onClick: () => setActiveTab(tab.id), className: `px-4 py-3 font-medium text-sm border-b-2 transition-colors ${activeTab === tab.id
                            ? 'border-accent text-accent'
                            : 'border-transparent text-ink-secondary hover:text-ink-primary'}`, children: [tab.icon, " ", tab.label] }, tab.id))) }), _jsxs("div", { children: [activeTab === 'templates' && _jsx(TemplatesTab, {}), activeTab === 'channels' && _jsx(ChannelsTab, {}), activeTab === 'schedule' && _jsx(ScheduleTab, {}), activeTab === 'dead-letter' && _jsx(DeadLetterTab, {})] })] }) }));
}
