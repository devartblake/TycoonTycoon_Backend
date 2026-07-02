import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Player transactions table
 */
import { formatDateTime } from '@/lib/utils';
const TYPE_CONFIG = {
    purchase: { icon: '🛒', color: 'text-status-offline', label: 'Purchase' },
    earn: { icon: '📈', color: 'text-status-healthy', label: 'Earn' },
    refund: { icon: '↩️', color: 'text-accent', label: 'Refund' },
    adjustment: { icon: '⚙️', color: 'text-ink-secondary', label: 'Adjustment' },
    reward: { icon: '🎁', color: 'text-status-healthy', label: 'Reward' },
    penalty: { icon: '⚠️', color: 'text-status-offline', label: 'Penalty' },
};
const STATUS_CONFIG = {
    completed: { color: 'text-status-healthy', bg: 'bg-status-healthy/10' },
    pending: { color: 'text-status-degraded', bg: 'bg-status-degraded/10' },
    failed: { color: 'text-status-offline', bg: 'bg-status-offline/10' },
    reversed: { color: 'text-ink-secondary', bg: 'bg-ink-secondary/10' },
};
export function TransactionsTable({ transactions, isLoading, onRefundClick }) {
    if (isLoading) {
        return (_jsx("div", { className: "operator-card space-y-2", children: [...Array(5)].map((_, i) => (_jsx("div", { className: "h-12 bg-bg-secondary rounded animate-pulse" }, i))) }));
    }
    if (transactions.length === 0) {
        return (_jsx("div", { className: "text-center py-12 text-ink-secondary operator-card", children: _jsx("p", { children: "No transactions found" }) }));
    }
    return (_jsx("div", { className: "operator-card overflow-x-auto", children: _jsxs("table", { className: "w-full text-sm", children: [_jsx("thead", { children: _jsxs("tr", { className: "border-b border-panel-border", children: [_jsx("th", { className: "px-3 py-2 text-left text-xs font-semibold text-ink-tertiary", children: "Type" }), _jsx("th", { className: "px-3 py-2 text-left text-xs font-semibold text-ink-tertiary", children: "Description" }), _jsx("th", { className: "px-3 py-2 text-right text-xs font-semibold text-ink-tertiary", children: "Amount" }), _jsx("th", { className: "px-3 py-2 text-left text-xs font-semibold text-ink-tertiary", children: "Balance" }), _jsx("th", { className: "px-3 py-2 text-center text-xs font-semibold text-ink-tertiary", children: "Status" }), _jsx("th", { className: "px-3 py-2 text-left text-xs font-semibold text-ink-tertiary", children: "Date" }), _jsx("th", { className: "px-3 py-2 text-center text-xs font-semibold text-ink-tertiary", children: "Action" })] }) }), _jsx("tbody", { children: transactions.map((txn) => {
                        const typeConfig = TYPE_CONFIG[txn.type];
                        const statusConfig = STATUS_CONFIG[txn.status];
                        return (_jsxs("tr", { className: "border-b border-panel-border hover:bg-bg-secondary transition-colors", children: [_jsx("td", { className: "px-3 py-2", children: _jsx("span", { className: `text-lg ${typeConfig.color}`, children: typeConfig.icon }) }), _jsxs("td", { className: "px-3 py-2", children: [_jsx("p", { className: "text-ink-primary font-medium", children: typeConfig.label }), txn.description && (_jsx("p", { className: "text-xs text-ink-secondary mt-1", children: txn.description })), txn.reference && (_jsx("p", { className: "text-xs text-ink-tertiary", children: txn.reference }))] }), _jsx("td", { className: "px-3 py-2 text-right", children: _jsxs("p", { className: `font-semibold ${txn.amount > 0 ? 'text-status-healthy' : 'text-status-offline'}`, children: [txn.amount > 0 ? '+' : '', txn.amount.toLocaleString()] }) }), _jsx("td", { className: "px-3 py-2", children: _jsx("div", { className: "space-y-1", children: _jsxs("p", { className: "text-xs text-ink-tertiary", children: [txn.balanceBefore.toLocaleString(), " \u2192 ", txn.balanceAfter.toLocaleString()] }) }) }), _jsx("td", { className: "px-3 py-2 text-center", children: _jsx("span", { className: `px-2 py-1 rounded text-xs font-medium ${statusConfig.bg} ${statusConfig.color}`, children: txn.status }) }), _jsx("td", { className: "px-3 py-2", children: _jsx("p", { className: "text-xs text-ink-secondary", children: formatDateTime(txn.createdAt) }) }), _jsx("td", { className: "px-3 py-2 text-center", children: txn.status === 'completed' && txn.type === 'purchase' && onRefundClick && (_jsx("button", { onClick: () => onRefundClick(txn), className: "text-xs text-accent hover:underline font-medium", children: "Refund" })) })] }, txn.id));
                    }) })] }) }));
}
