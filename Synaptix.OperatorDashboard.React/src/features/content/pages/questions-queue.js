import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Questions Queue - Content Moderation
 */
import { useState } from 'react';
import { usePermission } from '@/hooks/use-permission';
import { QuestionCard } from '../components/question-card';
import { ReviewPanel } from '../components/review-panel';
import { FilterBar } from '../components/filter-bar';
import { useQuestions, useQuestionsStats, useReviewQuestion, } from '../hooks/useContent';
export default function QuestionsQueuePage() {
    usePermission('content:write');
    const [filters, setFilters] = useState({ status: 'pending' });
    const [offset, setOffset] = useState(0);
    const [successMessage, setSuccessMessage] = useState(null);
    const limit = 50;
    const questionsQuery = useQuestions(filters, offset, limit);
    const statsQuery = useQuestionsStats();
    const reviewMutation = useReviewQuestion();
    const questions = questionsQuery.data?.items || [];
    const currentQuestion = questions.length > 0 ? questions[0] : null;
    const handleApprove = async (reason, notes) => {
        if (!currentQuestion)
            return;
        await reviewMutation.mutateAsync({
            questionId: currentQuestion.id,
            verdict: 'approve',
            reason,
            notes,
        });
        setSuccessMessage('Question approved');
        setTimeout(() => setSuccessMessage(null), 2000);
    };
    const handleReject = async (reason, notes) => {
        if (!currentQuestion)
            return;
        await reviewMutation.mutateAsync({
            questionId: currentQuestion.id,
            verdict: 'reject',
            reason,
            notes,
        });
        setSuccessMessage('Question rejected');
        setTimeout(() => setSuccessMessage(null), 2000);
    };
    return (_jsxs("div", { className: "operator-container space-y-8", children: [_jsxs("div", { children: [_jsx("h1", { className: "text-3xl font-bold text-ink-primary", children: "Questions Queue" }), _jsx("p", { className: "mt-2 text-ink-secondary", children: "Review and moderate game questions" })] }), statsQuery.data && (_jsxs("div", { className: "grid grid-cols-1 md:grid-cols-4 gap-4", children: [_jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Pending" }), _jsx("p", { className: "text-2xl font-bold text-accent mt-1", children: statsQuery.data.totalPending })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Approved" }), _jsx("p", { className: "text-2xl font-bold text-status-healthy mt-1", children: statsQuery.data.totalApproved })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Rejected" }), _jsx("p", { className: "text-2xl font-bold text-status-offline mt-1", children: statsQuery.data.totalRejected })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Approval Rate" }), _jsxs("p", { className: "text-2xl font-bold text-status-healthy mt-1", children: [Math.round(statsQuery.data.approvalRate * 100), "%"] })] })] })), successMessage && (_jsxs("div", { className: "p-4 bg-status-healthy/10 border border-status-healthy/20 rounded text-status-healthy text-sm", children: ["\u2713 ", successMessage] })), _jsxs("div", { className: "grid grid-cols-1 lg:grid-cols-4 gap-6", children: [_jsx("div", { children: _jsx(FilterBar, { filters: filters, onFiltersChange: (newFilters) => {
                                setFilters(newFilters);
                                setOffset(0);
                            } }) }), _jsxs("div", { className: "lg:col-span-2", children: [_jsx(QuestionCard, { question: currentQuestion, isLoading: questionsQuery.isLoading }), questionsQuery.data && questionsQuery.data.total > limit && (_jsxs("div", { className: "mt-4 flex justify-between", children: [_jsx("button", { onClick: () => setOffset(Math.max(0, offset - limit)), disabled: offset === 0, className: "px-3 py-1 text-xs bg-bg-secondary border border-panel-border rounded hover:bg-bg-tertiary disabled:opacity-50 disabled:cursor-not-allowed", children: "\u2190 Previous" }), _jsxs("p", { className: "text-xs text-ink-secondary", children: ["Page ", Math.floor(offset / limit) + 1, " of ", Math.ceil(questionsQuery.data.total / limit)] }), _jsx("button", { onClick: () => setOffset(offset + limit), disabled: offset + limit >= questionsQuery.data.total, className: "px-3 py-1 text-xs bg-bg-secondary border border-panel-border rounded hover:bg-bg-tertiary disabled:opacity-50 disabled:cursor-not-allowed", children: "Next \u2192" })] }))] }), currentQuestion && (_jsx("div", { children: _jsx(ReviewPanel, { onApprove: handleApprove, onReject: handleReject, isLoading: reviewMutation.isPending }) }))] }), !questionsQuery.isLoading && questions.length === 0 && (_jsxs("div", { className: "text-center py-12 text-ink-secondary", children: [_jsx("p", { className: "text-lg", children: "\u2705 Queue is clear!" }), _jsxs("p", { className: "text-sm mt-2", children: ["All ", filters.status || 'matching', " questions have been reviewed."] })] }))] }));
}
