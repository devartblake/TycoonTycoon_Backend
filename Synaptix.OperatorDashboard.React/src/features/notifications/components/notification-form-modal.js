import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Notification template form modal
 */
import { useState } from 'react';
import { Button } from '@/components/ui/button';
const CHANNELS = ['email', 'push', 'sms'];
export function NotificationFormModal({ isOpen, onClose, onSubmit, initialData, isLoading = false, }) {
    const [form, setForm] = useState(initialData
        ? {
            name: initialData.name,
            subject: initialData.subject,
            body: initialData.body,
            channels: initialData.channels,
        }
        : {
            name: '',
            subject: '',
            body: '',
            channels: [],
        });
    const handleSubmit = async (e) => {
        e.preventDefault();
        await onSubmit(form);
        setForm({ name: '', subject: '', body: '', channels: [] });
    };
    if (!isOpen)
        return null;
    return (_jsx("div", { className: "fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4", children: _jsxs("div", { className: "bg-panel-bg border border-panel-border rounded-lg max-w-2xl w-full max-h-[80vh] overflow-y-auto", children: [_jsxs("div", { className: "sticky top-0 bg-panel-bg border-b border-panel-border p-6 flex items-center justify-between", children: [_jsx("h2", { className: "text-xl font-bold text-ink-primary", children: initialData ? 'Edit Template' : 'Create Template' }), _jsx("button", { onClick: onClose, className: "text-ink-tertiary hover:text-ink-primary text-2xl leading-none", children: "\u2715" })] }), _jsxs("form", { onSubmit: handleSubmit, className: "space-y-6 p-6", children: [_jsxs("div", { children: [_jsx("label", { htmlFor: "name", className: "block text-sm font-medium text-ink-primary mb-2", children: "Template Name" }), _jsx("input", { id: "name", type: "text", value: form.name, onChange: (e) => setForm({ ...form, name: e.target.value }), className: "w-full px-3 py-2 border border-panel-border rounded focus-ring", placeholder: "e.g., Welcome Email", required: true })] }), _jsxs("div", { children: [_jsx("label", { htmlFor: "subject", className: "block text-sm font-medium text-ink-primary mb-2", children: "Subject (for email templates)" }), _jsx("input", { id: "subject", type: "text", value: form.subject || '', onChange: (e) => setForm({ ...form, subject: e.target.value }), className: "w-full px-3 py-2 border border-panel-border rounded focus-ring", placeholder: "e.g., Welcome to Synaptix!" })] }), _jsxs("div", { children: [_jsx("label", { htmlFor: "body", className: "block text-sm font-medium text-ink-primary mb-2", children: "Message Body" }), _jsx("textarea", { id: "body", value: form.body, onChange: (e) => setForm({ ...form, body: e.target.value }), className: "w-full px-3 py-2 border border-panel-border rounded focus-ring h-32", placeholder: "e.g., Hi {{playerName}}, welcome to Synaptix!", required: true }), _jsxs("p", { className: "text-xs text-ink-tertiary mt-1", children: ["Use ", '{{variable}}', " for dynamic content"] })] }), _jsxs("div", { children: [_jsx("label", { className: "block text-sm font-medium text-ink-primary mb-2", children: "Channels" }), _jsx("div", { className: "space-y-2", children: CHANNELS.map((channel) => (_jsxs("label", { className: "flex items-center gap-2 cursor-pointer", children: [_jsx("input", { type: "checkbox", checked: form.channels.includes(channel), onChange: (e) => {
                                                    if (e.target.checked) {
                                                        setForm({ ...form, channels: [...form.channels, channel] });
                                                    }
                                                    else {
                                                        setForm({
                                                            ...form,
                                                            channels: form.channels.filter((c) => c !== channel),
                                                        });
                                                    }
                                                }, className: "cursor-pointer" }), _jsx("span", { className: "text-sm text-ink-primary capitalize", children: channel })] }, channel))) })] }), _jsxs("div", { className: "flex gap-2 justify-end pt-6 border-t border-panel-border", children: [_jsx(Button, { variant: "ghost", onClick: onClose, disabled: isLoading, children: "Cancel" }), _jsx(Button, { variant: "default", type: "submit", disabled: isLoading || !form.name || !form.body || form.channels.length === 0, children: isLoading ? 'Saving...' : 'Save Template' })] })] })] }) }));
}
