import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Notification channels tab
 */
// import React from 'react'
import { useNotificationChannels, useUpdateChannel } from '../hooks/useNotifications';
import { formatDate } from '@/lib/utils';
export function ChannelsTab() {
    const { data: channels = [], isLoading } = useNotificationChannels();
    const updateChannel = useUpdateChannel();
    const handleToggle = (channelId, currentEnabled) => {
        updateChannel.mutate({
            channelId,
            enabled: !currentEnabled,
        });
    };
    if (isLoading) {
        return (_jsx("div", { className: "space-y-3", children: [...Array(3)].map((_, i) => (_jsx("div", { className: "h-20 bg-bg-secondary rounded animate-pulse" }, i))) }));
    }
    if (channels.length === 0) {
        return (_jsx("div", { className: "text-center py-12 text-ink-secondary", children: _jsx("p", { children: "No channels configured" }) }));
    }
    return (_jsx("div", { className: "space-y-3", children: channels.map((channel) => (_jsx("div", { className: "p-4 bg-bg-secondary border border-panel-border rounded", children: _jsxs("div", { className: "flex items-center justify-between", children: [_jsxs("div", { className: "flex-1", children: [_jsx("h4", { className: "font-medium text-ink-primary capitalize", children: channel.type }), _jsx("p", { className: "text-sm text-ink-secondary mt-1", children: channel.name }), _jsxs("p", { className: "text-xs text-ink-tertiary mt-2", children: ["Created ", formatDate(channel.createdAt)] })] }), _jsxs("label", { className: "flex items-center gap-2 cursor-pointer", children: [_jsx("input", { type: "checkbox", checked: channel.enabled, onChange: () => handleToggle(channel.id, channel.enabled), disabled: updateChannel.isPending, className: "cursor-pointer" }), _jsx("span", { className: "text-sm font-medium text-ink-primary", children: channel.enabled ? 'Enabled' : 'Disabled' })] })] }) }, channel.id))) }));
}
