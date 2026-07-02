import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Notification templates tab
 */
import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { NotificationFormModal } from './notification-form-modal';
import { useNotificationTemplates, useCreateTemplate, useUpdateTemplate, useDeleteTemplate } from '../hooks/useNotifications';
import { formatDate } from '@/lib/utils';
export function TemplatesTab() {
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [editingTemplate, setEditingTemplate] = useState(null);
    const { data: templates = [], isLoading } = useNotificationTemplates();
    const createTemplate = useCreateTemplate();
    const updateTemplate = useUpdateTemplate();
    const deleteTemplate = useDeleteTemplate();
    const handleSubmit = async (payload) => {
        if (editingTemplate) {
            await updateTemplate.mutateAsync({ templateId: editingTemplate.id, payload });
            setEditingTemplate(null);
        }
        else {
            await createTemplate.mutateAsync(payload);
        }
        setIsModalOpen(false);
    };
    const handleDelete = (templateId) => {
        if (confirm('Delete this template?')) {
            deleteTemplate.mutate(templateId);
        }
    };
    return (_jsxs("div", { className: "space-y-4", children: [_jsxs("div", { className: "flex items-center justify-between", children: [_jsx("h3", { className: "text-lg font-semibold text-ink-primary", children: "Templates" }), _jsx(Button, { variant: "default", size: "sm", onClick: () => {
                            setEditingTemplate(null);
                            setIsModalOpen(true);
                        }, children: "+ New Template" })] }), isLoading ? (_jsx("div", { className: "space-y-2", children: [...Array(3)].map((_, i) => (_jsx("div", { className: "h-16 bg-bg-secondary rounded animate-pulse" }, i))) })) : templates.length === 0 ? (_jsx("div", { className: "text-center py-12 text-ink-secondary", children: _jsx("p", { children: "No templates yet" }) })) : (_jsx("div", { className: "space-y-2", children: templates.map((template) => (_jsx("div", { className: "p-4 bg-bg-secondary border border-panel-border rounded hover:bg-bg-tertiary transition-colors", children: _jsxs("div", { className: "flex items-start justify-between", children: [_jsxs("div", { className: "flex-1", children: [_jsx("h4", { className: "font-medium text-ink-primary", children: template.name }), _jsx("p", { className: "text-sm text-ink-secondary mt-1 line-clamp-2", children: template.body }), _jsx("div", { className: "flex gap-2 mt-2", children: template.channels.map((channel) => (_jsx("span", { className: "inline-block px-2 py-1 rounded text-xs bg-accent/10 text-accent", children: channel }, channel))) }), _jsxs("p", { className: "text-xs text-ink-tertiary mt-2", children: ["Updated ", formatDate(template.updatedAt)] })] }), _jsxs("div", { className: "flex gap-2 ml-4", children: [_jsx(Button, { variant: "ghost", size: "sm", onClick: () => {
                                            setEditingTemplate(template);
                                            setIsModalOpen(true);
                                        }, children: "Edit" }), _jsx(Button, { variant: "ghost", size: "sm", onClick: () => handleDelete(template.id), disabled: deleteTemplate.isPending, children: "Delete" })] })] }) }, template.id))) })), _jsx(NotificationFormModal, { isOpen: isModalOpen, onClose: () => {
                    setIsModalOpen(false);
                    setEditingTemplate(null);
                }, onSubmit: handleSubmit, initialData: editingTemplate || undefined, isLoading: createTemplate.isPending || updateTemplate.isPending })] }));
}
