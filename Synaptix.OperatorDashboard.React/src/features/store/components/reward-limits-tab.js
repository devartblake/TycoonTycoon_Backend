import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useState } from 'react';
import { useRewardLimits, useCreateRewardLimit, useUpdateRewardLimit, useDeleteRewardLimit } from '../hooks/useStore';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
export function RewardLimitsTab() {
    const { data: limits, isLoading, refetch } = useRewardLimits();
    const createMutation = useCreateRewardLimit();
    const updateMutation = useUpdateRewardLimit();
    const deleteMutation = useDeleteRewardLimit();
    const [selected, setSelected] = useState(null);
    const [formOpen, setFormOpen] = useState(false);
    const [formData, setFormData] = useState({
        rewardType: '',
        dailyLimit: 100,
        weeklyLimit: 700,
        monthlyLimit: 3000,
    });
    const handleSave = async () => {
        if (!formData.rewardType || formData.dailyLimit === undefined)
            return;
        try {
            if (selected) {
                await updateMutation.mutateAsync({ id: selected.id, ...formData });
            }
            else {
                await createMutation.mutateAsync(formData);
            }
            setFormOpen(false);
            setFormData({ rewardType: '', dailyLimit: 100, weeklyLimit: 700, monthlyLimit: 3000 });
            setSelected(null);
            refetch();
        }
        catch (error) {
            console.error('Save failed:', error);
        }
    };
    const handleDelete = async (id) => {
        if (confirm('Delete this limit?')) {
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
        return _jsx("div", { className: "text-center py-8 text-ink-secondary", children: "Loading reward limits..." });
    }
    return (_jsxs("div", { className: "space-y-4", children: [_jsxs("div", { className: "flex justify-between items-center", children: [_jsxs("span", { className: "text-sm text-ink-secondary", children: [limits?.length || 0, " reward types"] }), _jsxs(Dialog, { open: formOpen, onOpenChange: setFormOpen, children: [_jsx(DialogTrigger, { asChild: true, children: _jsx(Button, { size: "sm", children: "+ Add Limit" }) }), _jsxs(DialogContent, { className: "max-w-md", children: [_jsx(DialogHeader, { children: _jsxs(DialogTitle, { children: [selected ? 'Edit' : 'Add', " Reward Limit"] }) }), _jsxs("div", { className: "space-y-4", children: [_jsxs("div", { children: [_jsx("label", { className: "text-sm font-medium", children: "Reward Type *" }), _jsx("input", { type: "text", placeholder: "e.g., Coins", value: formData.rewardType || '', onChange: (e) => setFormData({ ...formData, rewardType: e.target.value }), className: "w-full mt-1 px-3 py-2 border border-panel-border rounded bg-panel" })] }), _jsxs("div", { className: "grid grid-cols-3 gap-2", children: [_jsxs("div", { children: [_jsx("label", { className: "text-sm font-medium", children: "Daily *" }), _jsx("input", { type: "number", value: formData.dailyLimit || '', onChange: (e) => setFormData({ ...formData, dailyLimit: Number(e.target.value) }), className: "w-full mt-1 px-2 py-2 border border-panel-border rounded bg-panel text-sm" })] }), _jsxs("div", { children: [_jsx("label", { className: "text-sm font-medium", children: "Weekly" }), _jsx("input", { type: "number", value: formData.weeklyLimit || '', onChange: (e) => setFormData({ ...formData, weeklyLimit: e.target.value ? Number(e.target.value) : null }), className: "w-full mt-1 px-2 py-2 border border-panel-border rounded bg-panel text-sm" })] }), _jsxs("div", { children: [_jsx("label", { className: "text-sm font-medium", children: "Monthly" }), _jsx("input", { type: "number", value: formData.monthlyLimit || '', onChange: (e) => setFormData({ ...formData, monthlyLimit: e.target.value ? Number(e.target.value) : null }), className: "w-full mt-1 px-2 py-2 border border-panel-border rounded bg-panel text-sm" })] })] }), _jsxs("div", { className: "flex gap-2 justify-end pt-2", children: [_jsx(Button, { variant: "outline", onClick: () => setFormOpen(false), children: "Cancel" }), _jsx(Button, { onClick: handleSave, disabled: createMutation.isPending || updateMutation.isPending, children: selected ? 'Update' : 'Create' })] })] })] })] })] }), limits && limits.length > 0 ? (_jsx("div", { className: "overflow-x-auto border border-panel-border rounded", children: _jsxs("table", { className: "w-full text-sm", children: [_jsx("thead", { className: "bg-panel", children: _jsxs("tr", { children: [_jsx("th", { className: "px-4 py-2 text-left", children: "Reward Type" }), _jsx("th", { className: "px-4 py-2 text-right", children: "Daily" }), _jsx("th", { className: "px-4 py-2 text-right", children: "Weekly" }), _jsx("th", { className: "px-4 py-2 text-right", children: "Monthly" }), _jsx("th", { className: "px-4 py-2 text-center", children: "Actions" })] }) }), _jsx("tbody", { children: limits.map((limit) => (_jsxs("tr", { className: "border-t border-panel-border hover:bg-panel/50", children: [_jsx("td", { className: "px-4 py-3", children: limit.rewardType }), _jsx("td", { className: "px-4 py-3 text-right", children: limit.dailyLimit }), _jsx("td", { className: "px-4 py-3 text-right", children: limit.weeklyLimit || '-' }), _jsx("td", { className: "px-4 py-3 text-right", children: limit.monthlyLimit || '-' }), _jsxs("td", { className: "px-4 py-3 text-center space-x-2", children: [_jsx(Button, { size: "sm", variant: "outline", onClick: () => { setSelected(limit); setFormData(limit); setFormOpen(true); }, children: "Edit" }), _jsx(Button, { size: "sm", variant: "destructive", onClick: () => handleDelete(limit.id), children: "Delete" })] })] }, limit.id))) })] }) })) : (_jsx("div", { className: "text-center py-12 text-ink-secondary border border-panel-border rounded", children: _jsx("p", { children: "No reward limits" }) }))] }));
}
export default RewardLimitsTab;
