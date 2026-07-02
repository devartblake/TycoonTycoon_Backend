import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Balance adjustment modal dialog
 */
import { useState } from 'react';
import { Button } from '@/components/ui/button';
const ADJUSTMENT_REASONS = [
    'Customer complaint resolution',
    'Event bonus',
    'Bug compensation',
    'Refund correction',
    'Account error fix',
    'Support adjustment',
];
export function BalanceAdjustmentModal({ currentBalance, isOpen, onClose, onSubmit, isLoading, }) {
    const [amount, setAmount] = useState(0);
    const [reason, setReason] = useState('');
    const [note, setNote] = useState('');
    const [error, setError] = useState(null);
    if (!isOpen)
        return null;
    const newBalance = currentBalance + amount;
    const handleSubmit = async () => {
        setError(null);
        if (!reason) {
            setError('Please select a reason');
            return;
        }
        if (amount === 0) {
            setError('Please enter an amount');
            return;
        }
        try {
            await onSubmit(amount, reason, note);
            onClose();
            setAmount(0);
            setReason('');
            setNote('');
        }
        catch (err) {
            setError(err instanceof Error ? err.message : 'Failed to adjust balance');
        }
    };
    return (_jsx("div", { className: "fixed inset-0 bg-black/50 flex items-center justify-center z-50", children: _jsxs("div", { className: "operator-card max-w-md w-full mx-4 space-y-4", children: [_jsx("h2", { className: "text-lg font-semibold text-ink-primary", children: "Adjust Player Balance" }), _jsxs("div", { className: "p-3 bg-bg-secondary rounded", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Current Balance" }), _jsx("p", { className: "text-2xl font-bold text-accent mt-1", children: currentBalance.toLocaleString() })] }), _jsxs("div", { children: [_jsx("label", { htmlFor: "amount", className: "block text-sm font-medium text-ink-primary mb-1", children: "Amount Change" }), _jsx("input", { id: "amount", type: "number", value: amount, onChange: (e) => setAmount(Number(e.target.value)), className: "w-full px-3 py-2 border border-panel-border rounded focus-ring", placeholder: "Positive or negative amount" }), _jsxs("p", { className: "text-xs text-ink-secondary mt-2", children: ["New Balance: ", _jsx("span", { className: newBalance < 0 ? 'text-status-offline font-bold' : 'text-status-healthy font-bold', children: newBalance.toLocaleString() })] })] }), _jsxs("div", { children: [_jsx("label", { htmlFor: "reason", className: "block text-sm font-medium text-ink-primary mb-1", children: "Reason" }), _jsxs("select", { id: "reason", value: reason, onChange: (e) => setReason(e.target.value), className: "w-full px-3 py-2 border border-panel-border rounded focus-ring bg-bg-primary", children: [_jsx("option", { value: "", children: "Select a reason..." }), ADJUSTMENT_REASONS.map((r) => (_jsx("option", { value: r, children: r }, r)))] })] }), _jsxs("div", { children: [_jsx("label", { htmlFor: "note", className: "block text-sm font-medium text-ink-primary mb-1", children: "Internal Note (optional)" }), _jsx("textarea", { id: "note", value: note, onChange: (e) => setNote(e.target.value), className: "w-full px-3 py-2 border border-panel-border rounded focus-ring h-20 resize-none text-sm", placeholder: "Additional context for this adjustment..." })] }), error && (_jsx("div", { className: "p-3 bg-status-offline/10 border border-status-offline/20 rounded text-status-offline text-sm", children: error })), _jsxs("div", { className: "flex gap-2 pt-4 border-t border-panel-border", children: [_jsx(Button, { onClick: handleSubmit, disabled: isLoading, className: "flex-1", children: isLoading ? 'Adjusting...' : 'Confirm' }), _jsx(Button, { onClick: onClose, variant: "outline", className: "flex-1", disabled: isLoading, children: "Cancel" })] })] }) }));
}
