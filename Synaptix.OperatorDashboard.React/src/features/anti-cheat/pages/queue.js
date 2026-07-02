import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Anti-Cheat Review Queue page
 */
import { usePermission } from '@/hooks/use-permission';
import { QueueStats } from '../components/queue-stats';
import { FlagDetails } from '../components/flag-details';
import { VerdictForm } from '../components/verdict-form';
import { useQueueStats, useCurrentFlag, useSubmitVerdict, } from '../hooks/useAntiCheatQueue';
export default function AntiCheatQueuePage() {
    usePermission('anti-cheat:read');
    const statsQuery = useQueueStats();
    const currentFlagQuery = useCurrentFlag();
    const submitVerdictMutation = useSubmitVerdict();
    const handleSubmitVerdict = async (payload) => {
        await submitVerdictMutation.mutateAsync(payload);
    };
    return (_jsxs("div", { className: "operator-container space-y-8", children: [_jsxs("div", { children: [_jsx("h1", { className: "text-3xl font-bold text-ink-primary", children: "Anti-Cheat Review Queue" }), _jsx("p", { className: "mt-2 text-ink-secondary", children: "Review and verdict suspected cheating activity" })] }), _jsx(QueueStats, { stats: statsQuery.data || { pendingCount: 0, reviewedThisWeek: 0, completionRate: 0 }, isLoading: statsQuery.isLoading }), statsQuery.data && statsQuery.data.pendingCount > 0 ? (_jsxs("div", { className: "grid grid-cols-1 lg:grid-cols-3 gap-6", children: [_jsx("div", { className: "lg:col-span-2", children: _jsx(FlagDetails, { flag: currentFlagQuery.data || null, isLoading: currentFlagQuery.isLoading }) }), _jsx("div", { children: _jsx(VerdictForm, { flagId: currentFlagQuery.data?.id || '', onSubmit: handleSubmitVerdict, isLoading: submitVerdictMutation.isPending }) })] })) : (_jsxs("div", { className: "text-center py-12 text-ink-secondary operator-card", children: [_jsx("p", { className: "text-lg", children: "\u2705 Queue is clear!" }), _jsx("p", { className: "text-sm mt-2", children: "All pending flags have been reviewed. Check back later." })] })), submitVerdictMutation.isSuccess && (_jsx("div", { className: "p-4 bg-status-healthy/10 border border-status-healthy/20 rounded text-status-healthy text-sm", children: "\u2713 Verdict submitted. Loading next flag..." })), submitVerdictMutation.isError && (_jsx("div", { className: "p-4 bg-status-offline/10 border border-status-offline/20 rounded text-status-offline text-sm", children: "\u2715 Failed to submit verdict. Please try again." }))] }));
}
