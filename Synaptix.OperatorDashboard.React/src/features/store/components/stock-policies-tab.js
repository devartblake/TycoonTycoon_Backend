import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useState } from 'react';
import { useStockPolicies, useCreateStockPolicy, useUpdateStockPolicy, useDeleteStockPolicy } from '../hooks/useStore';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
export function StockPoliciesTab() {
    const { data: policies, isLoading, refetch } = useStockPolicies();
    const createMutation = useCreateStockPolicy();
    const updateMutation = useUpdateStockPolicy();
    const deleteMutation = useDeleteStockPolicy();
    const [selected, setSelected] = useState(null);
    const [formOpen, setFormOpen] = useState(false);
    const [formData, setFormData] = useState({
        name: '',
        reorderLevel: 100,
        reorderQuantity: 500,
        maxStock: 5000,
    });
    const handleSave = async () => {
        if (!formData.name || !formData.reorderLevel || !formData.reorderQuantity)
            return;
        try {
            if (selected) {
                await updateMutation.mutateAsync({ id: selected.id, ...formData });
            }
            else {
                await createMutation.mutateAsync(formData);
            }
            setFormOpen(false);
            setFormData({ name: '', reorderLevel: 100, reorderQuantity: 500, maxStock: 5000 });
            setSelected(null);
            refetch();
        }
        catch (error) {
            console.error('Save failed:', error);
        }
    };
    const handleDelete = async (id) => {
        if (confirm('Delete this policy?')) {
            try {
                await deleteMutation.mutateAsync(id);
                refetch();
            }
            catch (error) {
                console.error('Delete failed:', error);
            }
        }
    };
    if (isLoading) {
        return _jsx("div", { className: "text-center py-8 text-ink-secondary", children: "Loading stock policies..." });
    }
    return (_jsxs("div", { className: "space-y-4", children: [_jsxs("div", { className: "flex justify-between items-center", children: [_jsxs("span", { className: "text-sm text-ink-secondary", children: [policies?.length || 0, " policies"] }), _jsxs(Dialog, { open: formOpen, onOpenChange: setFormOpen, children: [_jsx(DialogTrigger, { asChild: true, children: _jsx(Button, { size: "sm", children: "+ Add Policy" }) }), _jsxs(DialogContent, { className: "max-w-md", children: [_jsx(DialogHeader, { children: _jsxs(DialogTitle, { children: [selected ? 'Edit' : 'Add', " Stock Policy"] }) }), _jsxs("div", { className: "space-y-4", children: [_jsxs("div", { children: [_jsx("label", { className: "text-sm font-medium", children: "Policy Name *" }), _jsx("input", { type: "text", placeholder: "e.g., Standard", value: formData.name || '', onChange: (e) => setFormData({ ...formData, name: e.target.value }), className: "w-full mt-1 px-3 py-2 border border-panel-border rounded bg-panel" })] }), _jsxs("div", { className: "grid grid-cols-2 gap-4", children: [_jsxs("div", { children: [_jsx("label", { className: "text-sm font-medium", children: "Reorder Level *" }), _jsx("input", { type: "number", value: formData.reorderLevel || '', onChange: (e) => setFormData({ ...formData, reorderLevel: Number(e.target.value) }), className: "w-full mt-1 px-3 py-2 border border-panel-border rounded bg-panel" })] }), _jsxs("div", { children: [_jsx("label", { className: "text-sm font-medium", children: "Reorder Qty *" }), _jsx("input", { type: "number", value: formData.reorderQuantity || '', onChange: (e) => setFormData({ ...formData, reorderQuantity: Number(e.target.value) }), className: "w-full mt-1 px-3 py-2 border border-panel-border rounded bg-panel" })] })] }), _jsxs("div", { children: [_jsx("label", { className: "text-sm font-medium", children: "Max Stock" }), _jsx("input", { type: "number", value: formData.maxStock || '', onChange: (e) => setFormData({ ...formData, maxStock: e.target.value ? Number(e.target.value) : null }), className: "w-full mt-1 px-3 py-2 border border-panel-border rounded bg-panel" })] }), _jsxs("div", { className: "flex gap-2 justify-end pt-2", children: [_jsx(Button, { variant: "outline", onClick: () => setFormOpen(false), children: "Cancel" }), _jsx(Button, { onClick: handleSave, disabled: createMutation.isPending || updateMutation.isPending, children: selected ? 'Update' : 'Create' })] })] })] })] })] }), policies && policies.length > 0 ? (_jsx("div", { className: "overflow-x-auto border border-panel-border rounded", children: _jsxs("table", { className: "w-full text-sm", children: [_jsx("thead", { className: "bg-panel", children: _jsxs("tr", { children: [_jsx("th", { className: "px-4 py-2 text-left", children: "Policy Name" }), _jsx("th", { className: "px-4 py-2 text-right", children: "Reorder Level" }), _jsx("th", { className: "px-4 py-2 text-right", children: "Reorder Qty" }), _jsx("th", { className: "px-4 py-2 text-right", children: "Max Stock" }), _jsx("th", { className: "px-4 py-2 text-center", children: "Actions" })] }) }), _jsx("tbody", { children: policies.map((policy) => (_jsxs("tr", { className: "border-t border-panel-border hover:bg-panel/50", children: [_jsx("td", { className: "px-4 py-3", children: policy.name }), _jsx("td", { className: "px-4 py-3 text-right", children: policy.reorderLevel }), _jsx("td", { className: "px-4 py-3 text-right", children: policy.reorderQuantity }), _jsx("td", { className: "px-4 py-3 text-right", children: policy.maxStock || '-' }), _jsxs("td", { className: "px-4 py-3 text-center space-x-2", children: [_jsx(Button, { size: "sm", variant: "outline", onClick: () => { setSelected(policy); setFormData(policy); setFormOpen(true); }, children: "Edit" }), _jsx(Button, { size: "sm", variant: "destructive", onClick: () => handleDelete(policy.id), children: "Delete" })] })] }, policy.id))) })] }) })) : (_jsx("div", { className: "text-center py-12 text-ink-secondary border border-panel-border rounded", children: _jsx("p", { children: "No stock policies" }) }))] }));
}
export default StockPoliciesTab;
