import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Dead-letter messages tab
 */
import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { useDeadLetterMessages, useRetryDeadLetter } from '../hooks/useNotifications';
import { formatDate } from '@/lib/utils';
export function DeadLetterTab() {
    const [filterChannel, setFilterChannel] = useState('');
    const { data: messages = [], isLoading } = useDeadLetterMessages();
    const retryMessage = useRetryDeadLetter();
    const filteredMessages = filterChannel
        ? messages.filter((m) => m.channel === filterChannel)
        : messages;
    const handleRetry = (messageId) => {
        retryMessage.mutate(messageId);
    };
    return (_jsxs("div", { className: "space-y-4", children: [_jsxs("div", { className: "flex items-center justify-between", children: [_jsx("h3", { className: "text-lg font-semibold text-ink-primary", children: "Failed Messages" }), _jsxs("select", { value: filterChannel, onChange: (e) => setFilterChannel(e.target.value), className: "px-3 py-2 border border-panel-border rounded focus-ring text-sm", children: [_jsx("option", { value: "", children: "All Channels" }), _jsx("option", { value: "email", children: "Email" }), _jsx("option", { value: "push", children: "Push" }), _jsx("option", { value: "sms", children: "SMS" })] })] }), isLoading ? (_jsx("div", { className: "space-y-2", children: [...Array(3)].map((_, i) => (_jsx("div", { className: "h-20 bg-bg-secondary rounded animate-pulse" }, i))) })) : filteredMessages.length === 0 ? (_jsx("div", { className: "text-center py-12 text-ink-secondary", children: _jsx("p", { children: "No failed messages" }) })) : (_jsx("div", { className: "space-y-2", children: filteredMessages.map((message) => (_jsx("div", { className: "p-4 bg-bg-secondary border border-panel-border rounded space-y-2", children: _jsxs("div", { className: "flex items-start justify-between", children: [_jsxs("div", { className: "flex-1", children: [_jsx("h4", { className: "font-medium text-ink-primary", children: message.templateName }), _jsxs("p", { className: "text-sm text-ink-secondary mt-1", children: [message.channel, " \u2192 ", message.recipient] }), _jsx("p", { className: "text-xs text-status-offline mt-1", children: message.error }), _jsxs("p", { className: "text-xs text-ink-tertiary mt-2", children: [message.attemptCount, " attempt(s) \u2022 Last tried ", formatDate(message.lastAttemptAt)] })] }), _jsx(Button, { variant: "secondary", size: "sm", onClick: () => handleRetry(message.id), disabled: retryMessage.isPending, children: "Retry" })] }) }, message.id))) }))] }));
}
