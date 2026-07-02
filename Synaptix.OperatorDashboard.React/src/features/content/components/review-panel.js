import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Question review verdict panel
 */
import { useState } from 'react';
import { Button } from '@/components/ui/button';
const REJECTION_REASONS = [
    'Incorrect answer key',
    'Ambiguous question',
    'Offensive content',
    'Duplicate question',
    'Poor grammar/spelling',
    'Unclear wording',
    'Inappropriate difficulty',
    'Missing explanation',
];
export function ReviewPanel({ onApprove, onReject, isLoading }) {
    const [verdict, setVerdict] = useState(null);
    const [reason, setReason] = useState('');
    const [notes, setNotes] = useState('');
    const [error, setError] = useState(null);
    const handleSubmit = async () => {
        setError(null);
        if (!verdict) {
            setError('Please select a verdict');
            return;
        }
        if (verdict === 'reject' && !reason) {
            setError('Please select a rejection reason');
            return;
        }
        try {
            if (verdict === 'approve') {
                await onApprove(reason, notes);
            }
            else {
                await onReject(reason, notes);
            }
            // Reset form after success
            setVerdict(null);
            setReason('');
            setNotes('');
        }
        catch (err) {
            setError(err instanceof Error ? err.message : 'Failed to submit review');
        }
    };
    return (_jsxs("div", { className: "operator-card space-y-4", children: [_jsx("h3", { className: "font-semibold text-ink-primary", children: "Review Question" }), error && (_jsx("div", { className: "p-3 bg-status-offline/10 border border-status-offline/20 rounded text-status-offline text-sm", children: error })), _jsxs("div", { className: "grid grid-cols-2 gap-2", children: [_jsx("button", { onClick: () => {
                            setVerdict('approve');
                            setReason('');
                        }, className: `px-3 py-2 rounded border-2 font-medium text-sm transition-colors ${verdict === 'approve'
                            ? 'bg-status-healthy/10 border-status-healthy text-status-healthy'
                            : 'border-panel-border hover:bg-bg-secondary text-ink-secondary'}`, children: "\u2713 Approve" }), _jsx("button", { onClick: () => setVerdict('reject'), className: `px-3 py-2 rounded border-2 font-medium text-sm transition-colors ${verdict === 'reject'
                            ? 'bg-status-offline/10 border-status-offline text-status-offline'
                            : 'border-panel-border hover:bg-bg-secondary text-ink-secondary'}`, children: "\u2715 Reject" })] }), verdict === 'reject' && (_jsxs("div", { children: [_jsx("label", { className: "block text-xs font-medium text-ink-tertiary mb-2", children: "Rejection Reason" }), _jsx("div", { className: "space-y-2", children: REJECTION_REASONS.map((r) => (_jsxs("label", { className: "flex items-center gap-2 p-2 rounded hover:bg-bg-secondary cursor-pointer", children: [_jsx("input", { type: "radio", name: "reason", value: r, checked: reason === r, onChange: (e) => setReason(e.target.value), className: "cursor-pointer" }), _jsx("span", { className: "text-xs text-ink-secondary", children: r })] }, r))) })] })), _jsxs("div", { children: [_jsx("label", { htmlFor: "notes", className: "block text-xs font-medium text-ink-tertiary mb-1", children: "Reviewer Notes (optional)" }), _jsx("textarea", { id: "notes", value: notes, onChange: (e) => setNotes(e.target.value), className: "w-full px-3 py-2 border border-panel-border rounded focus-ring text-sm h-20 resize-none", placeholder: "Add notes about your review..." })] }), _jsx(Button, { onClick: handleSubmit, disabled: isLoading || !verdict, className: "w-full", children: isLoading ? 'Submitting...' : 'Submit Review & Continue' })] }));
}
