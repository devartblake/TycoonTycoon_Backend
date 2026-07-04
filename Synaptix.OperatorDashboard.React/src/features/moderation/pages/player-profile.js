import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Moderation Player Profile page
 */
import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { usePermission } from '@/hooks/use-permission';
import ErrorBoundary from '@/components/shared/error-boundary';
import { SkeletonGrid } from '@/components/shared/skeletons';
import { PlayerHeader } from '../components/player-header';
import { ActionPanel } from '../components/action-panel';
import { ActionHistory } from '../components/action-history';
import { ActivityTimeline } from '../components/activity-timeline';
import { usePlayerModeration, useBanPlayer, useUnbanPlayer, useSuspendPlayer, useUnsuspendPlayer, useWarnPlayer, } from '../hooks/useModeration';
export default function PlayerProfilePage() {
    usePermission('moderation:write');
    const { playerId } = useParams();
    const navigate = useNavigate();
    const [successMessage, setSuccessMessage] = useState(null);
    if (!playerId) {
        return (_jsx("div", { className: "operator-container text-center py-12", children: _jsx("p", { className: "text-ink-secondary", children: "Player not found" }) }));
    }
    const moderationQuery = usePlayerModeration(playerId);
    const banMutation = useBanPlayer();
    const unbanMutation = useUnbanPlayer();
    const suspendMutation = useSuspendPlayer();
    const unsuspendMutation = useUnsuspendPlayer();
    const warnMutation = useWarnPlayer();
    const moderation = moderationQuery.data;
    const handleBan = async (reason, notes) => {
        await banMutation.mutateAsync({ playerId, reason, notes });
        setSuccessMessage('Player banned successfully');
        setTimeout(() => setSuccessMessage(null), 3000);
    };
    const handleUnban = async (reason) => {
        await unbanMutation.mutateAsync({ playerId, reason });
        setSuccessMessage('Player unbanned successfully');
        setTimeout(() => setSuccessMessage(null), 3000);
    };
    const handleSuspend = async (durationHours, reason, notes) => {
        await suspendMutation.mutateAsync({ playerId, durationHours, reason, notes });
        setSuccessMessage('Player suspended successfully');
        setTimeout(() => setSuccessMessage(null), 3000);
    };
    const handleUnsuspend = async (reason) => {
        await unsuspendMutation.mutateAsync({ playerId, reason });
        setSuccessMessage('Player unsuspended successfully');
        setTimeout(() => setSuccessMessage(null), 3000);
    };
    const handleWarn = async (reason, notes) => {
        await warnMutation.mutateAsync({ playerId, reason, notes });
        setSuccessMessage('Player warned successfully');
        setTimeout(() => setSuccessMessage(null), 3000);
    };
    const isLoading = moderationQuery.isLoading;
    const isMutating = banMutation.isPending || unbanMutation.isPending || suspendMutation.isPending || unsuspendMutation.isPending || warnMutation.isPending;
    return (_jsx(ErrorBoundary, { children: _jsxs("div", { className: "operator-container space-y-6", children: [_jsx("div", { className: "flex items-center justify-between", children: _jsxs("div", { children: [_jsx("button", { onClick: () => navigate(-1), className: "text-accent hover:underline text-sm mb-2", children: "\u2190 Back" }), _jsx("h1", { className: "text-2xl font-bold text-ink-primary", children: "Player Moderation" })] }) }), successMessage && (_jsxs("div", { className: "p-4 bg-status-healthy/10 border border-status-healthy/20 rounded text-status-healthy text-sm", children: ["\u2713 ", successMessage] })), isLoading ? (_jsx(SkeletonGrid, { count: 3 })) : moderation ? (_jsx(PlayerHeader, { profile: moderation.profile, isLoading: false })) : null, moderation ? (_jsxs("div", { className: "grid grid-cols-1 lg:grid-cols-3 gap-6", children: [_jsx("div", { children: _jsx(ActionPanel, { profile: moderation.profile, onBan: handleBan, onUnban: handleUnban, onSuspend: handleSuspend, onUnsuspend: handleUnsuspend, onWarn: handleWarn, isLoading: isMutating }) }), _jsxs("div", { className: "lg:col-span-2 space-y-6", children: [_jsx(ActionHistory, { actions: moderation.actions, isLoading: isLoading }), _jsx(ActivityTimeline, { activities: moderation.activity, isLoading: isLoading })] })] })) : null] }) }));
}
