/**
 * Skill tree hub - view the catalog, unlock nodes with coins/diamonds, respec
 * Wired to /skills/tree, /skills/state, /skills/unlock, /skills/respec
 */

import { useEffect, useState } from 'react';
import { useProfileStore } from '@stores';
import { apiClient } from '@core/api/client';
import { Lock, Check, Sparkles, RotateCcw, AlertCircle } from 'lucide-react';
import { GridSkeleton } from '@components/skeletons/GridSkeleton';
import { EmptyState } from '@components/EmptyState';
import { PageTransition } from '@components/PageTransition';
import { useToast } from '@hooks/useToast';

interface SkillCost {
  currency: 'Xp' | 'Coins' | 'Diamonds';
  amount: number;
}

interface SkillNode {
  key: string;
  branch: string; // Knowledge | Strategy | Powerups
  tier: number;
  title: string;
  description: string;
  prereqKeys: string[];
  costs: SkillCost[];
  effects: Record<string, number>;
}

const BRANCH_ORDER = ['Knowledge', 'Strategy', 'Powerups'];

const BRANCH_META: Record<string, { icon: string; blurb: string }> = {
  Knowledge: { icon: '📚', blurb: 'Answer smarter — time and accuracy boosts' },
  Strategy: { icon: '♟️', blurb: 'Play smarter — streaks and scoring' },
  Powerups: { icon: '⚡', blurb: 'Power-up mastery and utility' },
};

const RESPEC_REFUND_PERCENT = 80;

function formatCost(cost: SkillCost) {
  const icon = cost.currency === 'Diamonds' ? '💎' : cost.currency === 'Xp' ? '✨' : '💰';
  return `${icon} ${cost.amount.toLocaleString()}`;
}

function formatEffect(key: string, value: number) {
  // e.g. timeBonusSec -> "time bonus sec"
  const label = key.replace(/([A-Z])/g, ' $1').toLowerCase().trim();
  return `${label}: ${value}`;
}

export function SkillTreePage() {
  const toast = useToast();
  const profile = useProfileStore((state) => state.profile);
  const [nodes, setNodes] = useState<SkillNode[]>([]);
  const [unlockedKeys, setUnlockedKeys] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [unlocking, setUnlocking] = useState<string | null>(null);
  const [respeccing, setRespeccing] = useState(false);

  const fetchSkillData = async () => {
    try {
      setIsLoading(true);
      setError(null);
      const [tree, state] = await Promise.all([
        apiClient.getSkillTree(),
        apiClient.getSkillState(),
      ]);
      setNodes(tree);
      setUnlockedKeys(state.unlockedKeys || []);
    } catch (err) {
      console.error('Failed to fetch skill tree:', err);
      const errorMsg = 'Failed to load the skill tree. Please try again.';
      setError(errorMsg);
      toast.error(errorMsg);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchSkillData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const nodeByKey = new Map(nodes.map((n) => [n.key, n]));
  const isUnlocked = (key: string) => unlockedKeys.includes(key);
  const prereqsMet = (node: SkillNode) => node.prereqKeys.every((k) => isUnlocked(k));

  const canAfford = (node: SkillNode) => {
    if (!profile) return true; // Don't block on missing wallet data; the server enforces
    return node.costs.every((c) =>
      c.currency === 'Coins'
        ? profile.coins >= c.amount
        : c.currency === 'Diamonds'
          ? profile.diamonds >= c.amount
          : true
    );
  };

  const deductCosts = (node: SkillNode) => {
    useProfileStore.setState((state) => {
      if (!state.profile) return state;
      let { coins, diamonds } = state.profile;
      for (const c of node.costs) {
        if (c.currency === 'Coins') coins -= c.amount;
        if (c.currency === 'Diamonds') diamonds -= c.amount;
      }
      return { profile: { ...state.profile, coins, diamonds } };
    });
  };

  const handleUnlock = async (node: SkillNode) => {
    try {
      setUnlocking(node.key);
      const result = await apiClient.unlockSkill(node.key);
      switch (result.status) {
        case 'Unlocked':
          setUnlockedKeys(result.unlockedKeys);
          deductCosts(node);
          toast.success(`Unlocked ${node.title}!`);
          break;
        case 'Duplicate':
          setUnlockedKeys(result.unlockedKeys);
          toast.info(`${node.title} is already unlocked.`);
          break;
        case 'InsufficientFunds':
          toast.error(`Not enough currency to unlock ${node.title}.`);
          break;
        case 'MissingPrereq':
          toast.error('Unlock the prerequisite skills first.');
          break;
        default:
          toast.error(`Could not unlock ${node.title}.`);
      }
    } catch (err) {
      console.error('Failed to unlock skill:', err);
      toast.error('Failed to unlock skill. Please try again.');
    } finally {
      setUnlocking(null);
    }
  };

  const handleRespec = async () => {
    if (
      !confirm(
        `Reset all unlocked skills for a ${RESPEC_REFUND_PERCENT}% refund of what you spent?`
      )
    ) {
      return;
    }

    try {
      setRespeccing(true);
      const result = await apiClient.respecSkills(RESPEC_REFUND_PERCENT);
      setUnlockedKeys(result.unlockedKeys || []);
      if (result.status === 'Respecced') {
        useProfileStore.setState((state) =>
          state.profile
            ? {
                profile: {
                  ...state.profile,
                  coins: state.profile.coins + (result.refundedCoins || 0),
                  diamonds: state.profile.diamonds + (result.refundedDiamonds || 0),
                },
              }
            : state
        );
        toast.success(
          `Skills reset — refunded ${result.refundedCoins || 0} coins and ${result.refundedDiamonds || 0} diamonds.`
        );
      } else {
        toast.info('Skills were already reset.');
      }
    } catch (err) {
      console.error('Failed to respec skills:', err);
      toast.error('Failed to reset skills. Please try again.');
    } finally {
      setRespeccing(false);
    }
  };

  if (error) {
    return (
      <PageTransition>
        <div className="p-8 max-w-6xl mx-auto">
          <div
            className="p-6 rounded-lg flex items-start gap-3"
            style={{ backgroundColor: 'var(--color-status-error)', color: 'white' }}
          >
            <AlertCircle size={24} className="flex-shrink-0" />
            <div>
              <h3 className="font-bold mb-1">Error Loading Skill Tree</h3>
              <p>{error}</p>
              <button
                onClick={fetchSkillData}
                className="mt-4 px-4 py-2 rounded-lg"
                style={{ backgroundColor: 'rgba(255, 255, 255, 0.2)' }}
              >
                Retry
              </button>
            </div>
          </div>
        </div>
      </PageTransition>
    );
  }

  const branches = BRANCH_ORDER.filter((b) => nodes.some((n) => n.branch === b)).concat(
    [...new Set(nodes.map((n) => n.branch))].filter((b) => !BRANCH_ORDER.includes(b))
  );

  return (
    <PageTransition>
      <div className="p-8 max-w-7xl mx-auto">
        <div className="flex items-center justify-between mb-2">
          <h1 className="text-3xl font-bold" style={{ color: 'var(--color-text-primary)' }}>
            Skill Tree
          </h1>
          <button
            onClick={handleRespec}
            disabled={respeccing || unlockedKeys.length === 0}
            className="px-4 py-2 rounded-lg font-semibold transition-all flex items-center gap-2 disabled:opacity-50"
            style={{
              backgroundColor: 'var(--color-bg-tertiary)',
              color: 'var(--color-text-primary)',
              border: '1px solid var(--color-ui-border)',
            }}
          >
            <RotateCcw size={16} className={respeccing ? 'animate-spin' : ''} />
            Respec ({RESPEC_REFUND_PERCENT}% refund)
          </button>
        </div>
        <p className="mb-8" style={{ color: 'var(--color-text-secondary)' }}>
          Spend coins and diamonds to unlock permanent bonuses •{' '}
          {unlockedKeys.length}/{nodes.length} unlocked
        </p>

        {isLoading ? (
          <GridSkeleton items={6} columns={3} />
        ) : nodes.length === 0 ? (
          <EmptyState
            icon="🌳"
            title="No Skills Available"
            description="The skill catalog hasn't been published yet. Check back soon!"
          />
        ) : (
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            {branches.map((branch) => {
              const branchNodes = nodes
                .filter((n) => n.branch === branch)
                .sort((a, b) => a.tier - b.tier || a.title.localeCompare(b.title));
              const meta = BRANCH_META[branch] ?? { icon: '🌿', blurb: '' };

              return (
                <div
                  key={branch}
                  className="rounded-lg border p-4"
                  style={{
                    backgroundColor: 'var(--color-bg-secondary)',
                    borderColor: 'var(--color-ui-border)',
                  }}
                >
                  <div className="mb-4">
                    <h2
                      className="text-xl font-bold flex items-center gap-2"
                      style={{ color: 'var(--color-text-primary)' }}
                    >
                      <span>{meta.icon}</span> {branch}
                    </h2>
                    {meta.blurb && (
                      <p className="text-sm" style={{ color: 'var(--color-text-secondary)' }}>
                        {meta.blurb}
                      </p>
                    )}
                  </div>

                  <div className="space-y-3">
                    {branchNodes.map((node) => {
                      const unlocked = isUnlocked(node.key);
                      const available = !unlocked && prereqsMet(node);
                      const affordable = canAfford(node);
                      const missingPrereqs = node.prereqKeys
                        .filter((k) => !isUnlocked(k))
                        .map((k) => nodeByKey.get(k)?.title ?? k);

                      return (
                        <div
                          key={node.key}
                          className="p-4 rounded-lg border-2 transition-all"
                          style={{
                            backgroundColor: 'var(--color-bg-primary)',
                            borderColor: unlocked
                              ? 'var(--color-status-success)'
                              : available
                                ? 'var(--color-brand-primary)'
                                : 'var(--color-ui-border)',
                            opacity: unlocked || available ? 1 : 0.6,
                          }}
                        >
                          <div className="flex items-start justify-between gap-2 mb-1">
                            <h3
                              className="font-bold"
                              style={{ color: 'var(--color-text-primary)' }}
                            >
                              {node.title}
                            </h3>
                            <span
                              className="text-xs px-2 py-0.5 rounded-full flex-shrink-0"
                              style={{
                                backgroundColor: 'var(--color-bg-tertiary)',
                                color: 'var(--color-text-secondary)',
                              }}
                            >
                              Tier {node.tier}
                            </span>
                          </div>
                          <p
                            className="text-sm mb-2"
                            style={{ color: 'var(--color-text-secondary)' }}
                          >
                            {node.description}
                          </p>

                          {Object.keys(node.effects).length > 0 && (
                            <div className="flex flex-wrap gap-1 mb-3">
                              {Object.entries(node.effects).map(([k, v]) => (
                                <span
                                  key={k}
                                  className="text-xs px-2 py-0.5 rounded-full flex items-center gap-1"
                                  style={{
                                    backgroundColor: 'var(--color-bg-tertiary)',
                                    color: 'var(--color-brand-accent)',
                                  }}
                                >
                                  <Sparkles size={10} /> {formatEffect(k, v)}
                                </span>
                              ))}
                            </div>
                          )}

                          {unlocked ? (
                            <div
                              className="flex items-center gap-2 text-sm font-semibold"
                              style={{ color: 'var(--color-status-success)' }}
                            >
                              <Check size={16} /> Unlocked
                            </div>
                          ) : available ? (
                            <button
                              onClick={() => handleUnlock(node)}
                              disabled={unlocking !== null || !affordable}
                              className="w-full py-2 px-4 rounded-lg font-semibold transition-all disabled:opacity-50"
                              style={{
                                backgroundColor: affordable
                                  ? 'var(--color-brand-primary)'
                                  : 'var(--color-bg-tertiary)',
                                color: affordable ? 'white' : 'var(--color-text-secondary)',
                              }}
                            >
                              {unlocking === node.key
                                ? 'Unlocking…'
                                : `Unlock — ${node.costs.map(formatCost).join(' + ') || 'Free'}`}
                            </button>
                          ) : (
                            <div
                              className="flex items-center gap-2 text-sm"
                              style={{ color: 'var(--color-text-secondary)' }}
                            >
                              <Lock size={14} />
                              {missingPrereqs.length > 0
                                ? `Requires: ${missingPrereqs.join(', ')}`
                                : 'Locked'}
                            </div>
                          )}
                        </div>
                      );
                    })}
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>
    </PageTransition>
  );
}

export default SkillTreePage;
