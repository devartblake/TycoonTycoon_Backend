import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Player Economy & Transactions page
 */
import { useState } from 'react';
import { usePermission } from '@/hooks/use-permission';
import { PlayerSearch } from '../components/player-search';
import { EconomySummary } from '../components/economy-summary';
import { TransactionsTable } from '../components/transactions-table';
import { BalanceAdjustmentModal } from '../components/balance-adjustment-modal';
import { usePlayerEconomy, usePlayerTransactions, useAdjustBalance, useIssueRefund, } from '../hooks/useEconomy';
export default function PlayerEconomyPage() {
    usePermission('economy:read');
    const [selectedPlayerId, setSelectedPlayerId] = useState(null);
    const [offset, setOffset] = useState(0);
    const [showAdjustModal, setShowAdjustModal] = useState(false);
    const [successMessage, setSuccessMessage] = useState(null);
    const limit = 50;
    const economyQuery = usePlayerEconomy(selectedPlayerId || '');
    const transactionsQuery = usePlayerTransactions(selectedPlayerId || '', undefined, offset, limit);
    const adjustBalanceMutation = useAdjustBalance();
    const issueRefundMutation = useIssueRefund();
    const handleSelectPlayer = (playerId) => {
        setSelectedPlayerId(playerId);
        setOffset(0);
    };
    const handleAdjustBalance = async (amount, reason, note) => {
        if (!selectedPlayerId)
            return;
        await adjustBalanceMutation.mutateAsync({ playerId: selectedPlayerId, amount, reason, adminNote: note });
        setSuccessMessage('Balance adjusted successfully');
        setTimeout(() => setSuccessMessage(null), 3000);
    };
    const handleIssueRefund = async (transaction) => {
        if (!selectedPlayerId)
            return;
        // Open refund confirmation modal (simplified to direct call here)
        const reason = 'Player requested refund';
        await issueRefundMutation.mutateAsync({ playerId: selectedPlayerId, transactionId: transaction.id, reason });
        setSuccessMessage('Refund processed successfully');
        setTimeout(() => setSuccessMessage(null), 3000);
    };
    const economy = economyQuery.data;
    const transactions = transactionsQuery.data?.items || [];
    const isLoading = economyQuery.isLoading || transactionsQuery.isLoading;
    return (_jsxs("div", { className: "operator-container space-y-6", children: [_jsxs("div", { children: [_jsx("h1", { className: "text-3xl font-bold text-ink-primary", children: "Player Economy" }), _jsx("p", { className: "mt-2 text-ink-secondary", children: "Manage player balance and view transaction history" })] }), successMessage && (_jsxs("div", { className: "p-4 bg-status-healthy/10 border border-status-healthy/20 rounded text-status-healthy text-sm", children: ["\u2713 ", successMessage] })), _jsxs("div", { className: "grid grid-cols-1 lg:grid-cols-3 gap-6", children: [_jsxs("div", { className: "space-y-4", children: [_jsx(PlayerSearch, { onSelectPlayer: handleSelectPlayer }), selectedPlayerId && economy && (_jsx("button", { onClick: () => setShowAdjustModal(true), className: "w-full px-4 py-2 bg-accent text-white rounded font-medium hover:bg-accent-dark transition-smooth disabled:opacity-50", disabled: adjustBalanceMutation.isPending, children: adjustBalanceMutation.isPending ? 'Adjusting...' : 'Adjust Balance' }))] }), selectedPlayerId && (_jsx("div", { className: "lg:col-span-2", children: _jsx(EconomySummary, { economy: economy, isLoading: isLoading }) }))] }), selectedPlayerId && (_jsxs("div", { className: "space-y-4", children: [_jsxs("div", { children: [_jsx("h2", { className: "text-lg font-semibold text-ink-primary mb-4", children: "Transaction History" }), _jsx(TransactionsTable, { transactions: transactions, isLoading: transactionsQuery.isLoading, onRefundClick: handleIssueRefund })] }), transactionsQuery.data && transactionsQuery.data.total > limit && (_jsxs("div", { className: "flex justify-between items-center", children: [_jsx("button", { onClick: () => setOffset(Math.max(0, offset - limit)), disabled: offset === 0, className: "px-4 py-2 bg-bg-secondary border border-panel-border rounded text-sm hover:bg-bg-tertiary disabled:opacity-50 disabled:cursor-not-allowed", children: "\u2190 Previous" }), _jsxs("p", { className: "text-sm text-ink-secondary", children: ["Page ", Math.floor(offset / limit) + 1, " of ", Math.ceil(transactionsQuery.data.total / limit)] }), _jsx("button", { onClick: () => setOffset(offset + limit), disabled: offset + limit >= transactionsQuery.data.total, className: "px-4 py-2 bg-bg-secondary border border-panel-border rounded text-sm hover:bg-bg-tertiary disabled:opacity-50 disabled:cursor-not-allowed", children: "Next \u2192" })] }))] })), selectedPlayerId && economy && (_jsx(BalanceAdjustmentModal, { playerId: selectedPlayerId, currentBalance: economy.currentBalance, isOpen: showAdjustModal, onClose: () => setShowAdjustModal(false), onSubmit: handleAdjustBalance, isLoading: adjustBalanceMutation.isPending })), !selectedPlayerId && (_jsx("div", { className: "text-center py-12 text-ink-secondary", children: _jsx("p", { children: "Select a player to view their economy information" }) }))] }));
}
