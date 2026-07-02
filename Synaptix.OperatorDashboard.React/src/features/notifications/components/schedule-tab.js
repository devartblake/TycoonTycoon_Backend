import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Scheduled notifications tab
 */
import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { useScheduledNotifications, useCreateSchedule, useCancelSchedule } from '../hooks/useNotifications';
import { formatDate } from '@/lib/utils';
export function ScheduleTab() {
    const [showCreateForm, setShowCreateForm] = useState(false);
    const [templateId, setTemplateId] = useState('');
    const [scheduledFor, setScheduledFor] = useState('');
    const { data: schedules = [], isLoading } = useScheduledNotifications();
    const createSchedule = useCreateSchedule();
    const cancelSchedule = useCancelSchedule();
    const handleSubmit = async (e) => {
        e.preventDefault();
        if (!templateId || !scheduledFor)
            return;
        await createSchedule.mutateAsync({
            templateId,
            scheduledFor,
        });
        setTemplateId('');
        setScheduledFor('');
        setShowCreateForm(false);
    };
    const handleCancel = (scheduleId) => {
        if (confirm('Cancel this scheduled notification?')) {
            cancelSchedule.mutate(scheduleId);
        }
    };
    return (_jsxs("div", { className: "space-y-4", children: [_jsxs("div", { className: "flex items-center justify-between", children: [_jsx("h3", { className: "text-lg font-semibold text-ink-primary", children: "Scheduled Sends" }), _jsx(Button, { variant: "default", size: "sm", onClick: () => setShowCreateForm(!showCreateForm), children: showCreateForm ? '✕ Cancel' : '+ Schedule Send' })] }), showCreateForm && (_jsxs("form", { onSubmit: handleSubmit, className: "p-4 bg-bg-secondary border border-panel-border rounded space-y-4", children: [_jsxs("div", { children: [_jsx("label", { htmlFor: "template-select", className: "block text-sm font-medium text-ink-primary mb-1", children: "Template" }), _jsx("input", { id: "template-select", type: "text", value: templateId, onChange: (e) => setTemplateId(e.target.value), placeholder: "Template ID or name", className: "w-full px-3 py-2 border border-panel-border rounded focus-ring text-sm", required: true })] }), _jsxs("div", { children: [_jsx("label", { htmlFor: "schedule-time", className: "block text-sm font-medium text-ink-primary mb-1", children: "Scheduled For" }), _jsx("input", { id: "schedule-time", type: "datetime-local", value: scheduledFor, onChange: (e) => setScheduledFor(e.target.value), className: "w-full px-3 py-2 border border-panel-border rounded focus-ring text-sm", required: true })] }), _jsxs("div", { className: "flex gap-2 justify-end", children: [_jsx(Button, { type: "button", variant: "ghost", onClick: () => setShowCreateForm(false), children: "Cancel" }), _jsx(Button, { type: "submit", variant: "default", disabled: createSchedule.isPending || !templateId || !scheduledFor, children: createSchedule.isPending ? 'Creating...' : 'Schedule' })] })] })), isLoading ? (_jsx("div", { className: "space-y-2", children: [...Array(3)].map((_, i) => (_jsx("div", { className: "h-16 bg-bg-secondary rounded animate-pulse" }, i))) })) : schedules.length === 0 ? (_jsx("div", { className: "text-center py-12 text-ink-secondary", children: _jsx("p", { children: "No scheduled notifications" }) })) : (_jsx("div", { className: "space-y-2", children: schedules.map((schedule) => (_jsxs("div", { className: "p-4 bg-bg-secondary border border-panel-border rounded flex items-center justify-between", children: [_jsxs("div", { children: [_jsx("h4", { className: "font-medium text-ink-primary", children: schedule.templateName }), _jsxs("p", { className: "text-sm text-ink-secondary mt-1", children: ["Scheduled for ", formatDate(schedule.scheduledFor)] }), _jsxs("p", { className: "text-xs text-ink-tertiary mt-1", children: [schedule.targetCount, " target(s) \u2022 Status: ", schedule.status] })] }), schedule.status === 'pending' && (_jsx(Button, { variant: "ghost", size: "sm", onClick: () => handleCancel(schedule.id), disabled: cancelSchedule.isPending, children: "Cancel" }))] }, schedule.id))) }))] }));
}
