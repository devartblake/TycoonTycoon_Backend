import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useState } from 'react';
import { useFlashSales, useCreateFlashSale, useUpdateFlashSale, useDeleteFlashSale } from '../hooks/useStore';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
export function FlashSalesTab() {
    const { data: sales, isLoading, refetch } = useFlashSales();
    const createMutation = useCreateFlashSale();
    const updateMutation = useUpdateFlashSale();
    const deleteMutation = useDeleteFlashSale();
    const [selected, setSelected] = useState(null);
    const [formOpen, setFormOpen] = useState(false);
    const [formData, setFormData] = useState({
        productId: '',
        discountPercent: 0,
        startTime: '',
        endTime: '',
    });
    const handleSave = async () => {
        if (!formData.productId || formData.discountPercent === undefined)
            return;
        try {
            if (selected) {
                await updateMutation.mutateAsync({ id: selected.id, ...formData });
            }
            else {
                await createMutation.mutateAsync(formData);
            }
            setFormOpen(false);
            setFormData({ productId: '', discountPercent: 0, startTime: '', endTime: '' });
            setSelected(null);
            refetch();
        }
        catch (error) {
            console.error('Save failed:', error);
        }
    };
    const handleDelete = async (id) => {
        if (confirm('Delete this sale?')) {
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
        return _jsx("div", { className: "text-center py-8 text-ink-secondary", children: "Loading flash sales..." });
    }
    return (_jsxs("div", { className: "space-y-4", children: [_jsxs("div", { className: "flex justify-between items-center", children: [_jsxs("span", { className: "text-sm text-ink-secondary", children: [sales?.length || 0, " active sales"] }), _jsxs(Dialog, { open: formOpen, onOpenChange: setFormOpen, children: [_jsx(DialogTrigger, { asChild: true, children: _jsx(Button, { size: "sm", children: "+ Add Sale" }) }), _jsxs(DialogContent, { className: "max-w-md", children: [_jsx(DialogHeader, { children: _jsxs(DialogTitle, { children: [selected ? 'Edit' : 'Add', " Flash Sale"] }) }), _jsxs("div", { className: "space-y-4", children: [_jsxs("div", { children: [_jsx("label", { className: "text-sm font-medium", children: "Product ID *" }), _jsx("input", { type: "text", value: formData.productId || '', onChange: (e) => setFormData({ ...formData, productId: e.target.value }), className: "w-full mt-1 px-3 py-2 border border-panel-border rounded bg-panel" })] }), _jsxs("div", { children: [_jsx("label", { className: "text-sm font-medium", children: "Discount % *" }), _jsx("input", { type: "number", min: "0", max: "100", value: formData.discountPercent || '', onChange: (e) => setFormData({ ...formData, discountPercent: Number(e.target.value) }), className: "w-full mt-1 px-3 py-2 border border-panel-border rounded bg-panel" })] }), _jsxs("div", { children: [_jsx("label", { className: "text-sm font-medium", children: "Start Time" }), _jsx("input", { type: "datetime-local", value: formData.startTime || '', onChange: (e) => setFormData({ ...formData, startTime: e.target.value }), className: "w-full mt-1 px-3 py-2 border border-panel-border rounded bg-panel" })] }), _jsxs("div", { children: [_jsx("label", { className: "text-sm font-medium", children: "End Time" }), _jsx("input", { type: "datetime-local", value: formData.endTime || '', onChange: (e) => setFormData({ ...formData, endTime: e.target.value }), className: "w-full mt-1 px-3 py-2 border border-panel-border rounded bg-panel" })] }), _jsxs("div", { className: "flex gap-2 justify-end pt-2", children: [_jsx(Button, { variant: "outline", onClick: () => setFormOpen(false), children: "Cancel" }), _jsx(Button, { onClick: handleSave, disabled: createMutation.isPending || updateMutation.isPending, children: selected ? 'Update' : 'Create' })] })] })] })] })] }), sales && sales.length > 0 ? (_jsx("div", { className: "overflow-x-auto border border-panel-border rounded", children: _jsxs("table", { className: "w-full text-sm", children: [_jsx("thead", { className: "bg-panel", children: _jsxs("tr", { children: [_jsx("th", { className: "px-4 py-2 text-left", children: "Product" }), _jsx("th", { className: "px-4 py-2 text-right", children: "Discount" }), _jsx("th", { className: "px-4 py-2 text-left", children: "Start" }), _jsx("th", { className: "px-4 py-2 text-left", children: "End" }), _jsx("th", { className: "px-4 py-2 text-center", children: "Actions" })] }) }), _jsx("tbody", { children: sales.map((sale) => (_jsxs("tr", { className: "border-t border-panel-border hover:bg-panel/50", children: [_jsx("td", { className: "px-4 py-3", children: sale.productId }), _jsxs("td", { className: "px-4 py-3 text-right", children: [sale.discountPercent, "%"] }), _jsx("td", { className: "px-4 py-3", children: new Date(sale.startTime).toLocaleString() }), _jsx("td", { className: "px-4 py-3", children: new Date(sale.endTime).toLocaleString() }), _jsxs("td", { className: "px-4 py-3 text-center space-x-2", children: [_jsx(Button, { size: "sm", variant: "outline", onClick: () => { setSelected(sale); setFormData(sale); setFormOpen(true); }, children: "Edit" }), _jsx(Button, { size: "sm", variant: "destructive", onClick: () => handleDelete(sale.id), children: "Delete" })] })] }, sale.id))) })] }) })) : (_jsx("div", { className: "text-center py-12 text-ink-secondary border border-panel-border rounded", children: _jsx("p", { children: "No active flash sales" }) }))] }));
}
export default FlashSalesTab;
