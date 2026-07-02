import { jsxs as _jsxs, jsx as _jsx } from "react/jsx-runtime";
/**
 * Saved views dropdown for quick filter presets
 */
import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { useSavedViews, useCreateSavedView, useDeleteSavedView } from '../hooks/useSavedViews';
export function SavedViewsDropdown({ currentFilters, onLoadView }) {
    const [isOpen, setIsOpen] = useState(false);
    const [newViewName, setNewViewName] = useState('');
    const [showSaveForm, setShowSaveForm] = useState(false);
    const { data: savedViews = [], isLoading } = useSavedViews();
    const createView = useCreateSavedView();
    const deleteView = useDeleteSavedView();
    const handleSaveView = async () => {
        if (!newViewName.trim())
            return;
        await createView.mutateAsync({ name: newViewName, filters: currentFilters });
        setNewViewName('');
        setShowSaveForm(false);
    };
    const handleDeleteView = (viewId) => {
        if (confirm('Delete this saved view?')) {
            deleteView.mutate(viewId);
        }
    };
    return (_jsxs("div", { className: "relative", children: [_jsxs(Button, { variant: "outline", size: "sm", onClick: () => setIsOpen(!isOpen), children: ["\uD83D\uDCCC Saved Views (", savedViews.length, ")"] }), isOpen && (_jsxs("div", { className: "absolute top-full right-0 mt-2 w-64 bg-panel-bg border border-panel-border rounded shadow-lg z-50", children: [_jsx("div", { className: "max-h-64 overflow-y-auto", children: isLoading ? (_jsx("div", { className: "p-4 text-sm text-ink-secondary", children: "Loading..." })) : savedViews.length === 0 ? (_jsx("div", { className: "p-4 text-sm text-ink-secondary", children: "No saved views yet" })) : (_jsx("ul", { className: "space-y-1 p-2", children: savedViews.map((view) => (_jsxs("li", { className: "flex items-center justify-between hover:bg-bg-secondary rounded p-2 text-sm", children: [_jsx("button", { onClick: () => {
                                            onLoadView(view.filters);
                                            setIsOpen(false);
                                        }, className: "flex-1 text-left text-accent hover:underline", children: view.name }), _jsx("button", { onClick: () => handleDeleteView(view.id), className: "ml-2 text-ink-tertiary hover:text-status-offline text-xs px-1", title: "Delete view", children: "\u2715" })] }, view.id))) })) }), showSaveForm ? (_jsxs("div", { className: "border-t border-panel-border p-3 space-y-2", children: [_jsx("input", { type: "text", placeholder: "View name...", value: newViewName, onChange: (e) => setNewViewName(e.target.value), className: "w-full px-2 py-1 border border-panel-border rounded text-sm focus-ring", autoFocus: true }), _jsxs("div", { className: "flex gap-2", children: [_jsx(Button, { size: "sm", variant: "default", onClick: handleSaveView, disabled: !newViewName.trim() || createView.isPending, className: "flex-1 text-xs", children: createView.isPending ? 'Saving...' : 'Save' }), _jsx(Button, { size: "sm", variant: "ghost", onClick: () => {
                                            setShowSaveForm(false);
                                            setNewViewName('');
                                        }, className: "flex-1 text-xs", children: "Cancel" })] })] })) : (_jsx("div", { className: "border-t border-panel-border p-3", children: _jsx(Button, { size: "sm", variant: "secondary", onClick: () => setShowSaveForm(true), className: "w-full text-xs", children: "+ Save Current View" }) }))] }))] }));
}
