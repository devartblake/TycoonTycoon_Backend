import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Anti-cheat verdict submission form
 */
import { useState } from 'react';
import { Button } from '@/components/ui/button';
const VERDICTS = [
    { value: 'innocent', label: '✓ Innocent', color: 'bg-status-healthy/10 text-status-healthy' },
    { value: 'suspicious', label: '? Suspicious', color: 'bg-status-degraded/10 text-status-degraded' },
    { value: 'confirmed', label: '✕ Confirmed Cheat', color: 'bg-status-offline/10 text-status-offline' },
];
export function VerdictForm({ flagId, onSubmit, isLoading }) {
    const [verdict, setVerdict] = useState(null);
    const [notes, setNotes] = useState('');
    const [submitError, setSubmitError] = useState(null);
    const handleSubmit = async () => {
        if (!verdict) {
            setSubmitError('Please select a verdict');
            return;
        }
        setSubmitError(null);
        try {
            await onSubmit({
                flagId,
                verdict,
                notes: notes || undefined,
            });
            // Reset form after success
            setVerdict(null);
            setNotes('');
        }
        catch (err) {
            setSubmitError(err instanceof Error ? err.message : 'Failed to submit verdict');
        }
    };
    return (_jsxs("div", { className: "operator-card space-y-4", children: [_jsx("h3", { className: "font-semibold text-ink-primary", children: "Submit Verdict" }), _jsx("div", { className: "space-y-2", children: VERDICTS.map((v) => (_jsxs("label", { className: "flex items-center gap-3 p-3 border border-panel-border rounded cursor-pointer hover:bg-bg-secondary transition-colors", style: { borderColor: verdict === v.value ? 'var(--accent)' : undefined }, children: [_jsx("input", { type: "radio", name: "verdict", value: v.value, checked: verdict === v.value, onChange: (e) => setVerdict(e.target.value), className: "cursor-pointer" }), _jsx("span", { className: `px-2 py-1 rounded text-xs font-medium ${v.color}`, children: v.label })] }, v.value))) }), _jsxs("div", { children: [_jsx("label", { htmlFor: "notes", className: "block text-sm font-medium text-ink-primary mb-1", children: "Reviewer Notes (optional)" }), _jsx("textarea", { id: "notes", value: notes, onChange: (e) => setNotes(e.target.value), className: "w-full px-3 py-2 border border-panel-border rounded focus-ring h-24", placeholder: "Add notes about your verdict..." })] }), submitError && (_jsx("div", { className: "p-3 bg-status-offline/10 border border-status-offline/20 rounded text-status-offline text-sm", children: submitError })), _jsx(Button, { onClick: handleSubmit, disabled: isLoading || !verdict, className: "w-full", children: isLoading ? 'Submitting...' : 'Submit Verdict & Continue' })] }));
}
