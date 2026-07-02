import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
const DIFFICULTY_CONFIG = {
    easy: { color: 'text-status-healthy', bg: 'bg-status-healthy/10', label: 'Easy' },
    medium: { color: 'text-status-degraded', bg: 'bg-status-degraded/10', label: 'Medium' },
    hard: { color: 'text-status-offline', bg: 'bg-status-offline/10', label: 'Hard' },
};
const STATUS_CONFIG = {
    pending: { color: 'text-ink-secondary', bg: 'bg-ink-secondary/10', label: 'Pending' },
    approved: { color: 'text-status-healthy', bg: 'bg-status-healthy/10', label: 'Approved' },
    rejected: { color: 'text-status-offline', bg: 'bg-status-offline/10', label: 'Rejected' },
};
export function QuestionCard({ question, isLoading }) {
    if (isLoading) {
        return (_jsx("div", { className: "operator-card space-y-4", children: [...Array(4)].map((_, i) => (_jsx("div", { className: "h-12 bg-bg-secondary rounded animate-pulse" }, i))) }));
    }
    if (!question) {
        return (_jsx("div", { className: "text-center py-12 text-ink-secondary operator-card", children: _jsx("p", { children: "No question to review" }) }));
    }
    const diffConfig = DIFFICULTY_CONFIG[question.difficulty];
    const statusConfig = STATUS_CONFIG[question.status];
    return (_jsxs("div", { className: "operator-card space-y-6", children: [_jsxs("div", { className: "space-y-3", children: [_jsxs("div", { className: "flex items-start justify-between gap-4", children: [_jsx("div", { className: "flex-1", children: _jsx("h2", { className: "text-xl font-semibold text-ink-primary leading-relaxed", children: question.text }) }), _jsxs("div", { className: "flex gap-2", children: [_jsx("span", { className: `px-3 py-1 rounded text-xs font-medium whitespace-nowrap ${diffConfig.bg} ${diffConfig.color}`, children: diffConfig.label }), _jsx("span", { className: `px-3 py-1 rounded text-xs font-medium whitespace-nowrap ${statusConfig.bg} ${statusConfig.color}`, children: statusConfig.label })] })] }), _jsxs("div", { className: "flex items-center gap-3 text-sm text-ink-secondary", children: [_jsx("span", { className: "px-2 py-1 bg-bg-secondary rounded", children: question.category }), question.tags && question.tags.length > 0 && (_jsx("div", { className: "flex gap-1", children: question.tags.map((tag) => (_jsxs("span", { className: "px-2 py-1 bg-bg-secondary rounded text-xs", children: ["#", tag] }, tag))) }))] })] }), _jsxs("div", { className: "space-y-2 pt-4 border-t border-panel-border", children: [_jsx("p", { className: "text-sm font-medium text-ink-tertiary", children: "Answers:" }), _jsx("div", { className: "space-y-2", children: question.answers.map((answer) => (_jsx("div", { className: `p-3 rounded border-l-4 ${answer.isCorrect
                                ? 'border-status-healthy bg-status-healthy/5'
                                : 'border-panel-border bg-bg-secondary'}`, children: _jsxs("div", { className: "flex items-start gap-3", children: [_jsx("span", { className: "text-lg", children: answer.isCorrect ? '✓' : '○' }), _jsx("p", { className: "text-sm text-ink-primary", children: answer.text })] }) }, answer.id))) })] }), question.explanation && (_jsxs("div", { className: "pt-4 border-t border-panel-border", children: [_jsx("p", { className: "text-sm font-medium text-ink-tertiary mb-2", children: "Explanation:" }), _jsx("p", { className: "text-sm text-ink-secondary leading-relaxed", children: question.explanation })] })), _jsxs("div", { className: "pt-4 border-t border-panel-border space-y-2 text-xs text-ink-tertiary", children: [_jsxs("p", { children: ["Submitted by: ", _jsx("span", { className: "text-ink-secondary font-medium", children: question.submittedBy })] }), _jsxs("p", { children: ["Submitted: ", new Date(question.submittedAt).toLocaleString()] }), question.reviewedBy && (_jsxs("p", { children: ["Reviewed by: ", _jsx("span", { className: "text-ink-secondary font-medium", children: question.reviewedBy })] })), question.rejectionReason && (_jsxs("p", { className: "text-status-offline", children: ["Rejection: ", question.rejectionReason] }))] })] }));
}
