/**
 * Leaderboard page - displays top players ranked by XP
 */

import { useEffect, useState } from 'react';
import { useAuthStore } from '@stores';
import { apiClient } from '@core/api/client';
import { Trophy, Zap, AlertCircle, RefreshCw } from 'lucide-react';
import { TableSkeleton } from '@components/skeletons/TableSkeleton';
import { EmptyState } from '@components/EmptyState';
import { PageTransition } from '@components/PageTransition';
import { useToast } from '@hooks/useToast';

interface LeaderboardEntry {
  rank: number;
  playerId: string;
  username: string;
  xp: number;
  level: number;
  tier: string;
  avatar?: string;
}

export function LeaderboardPage() {
  const user = useAuthStore((state) => state.user);
  const toast = useToast();
  const [entries, setEntries] = useState<LeaderboardEntry[]>([]);
  const [userRank, setUserRank] = useState<LeaderboardEntry | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchLeaderboard = async () => {
    try {
      setIsLoading(true);
      setError(null);

      // Fetch top 50 players
      const data = await apiClient.getLeaderboard(50);
      setEntries(data);

      // Fetch user's rank if authenticated
      if (user?.id) {
        const rankData = await apiClient.getPlayerRank(user.id);
        setUserRank(rankData);
      }
    } catch (err) {
      console.error('Failed to fetch leaderboard:', err);
      const errorMsg = 'Failed to load leaderboard. Please try again.';
      setError(errorMsg);
      toast.error(errorMsg);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchLeaderboard();
  }, [toast]);

  const getMedalEmoji = (rank: number) => {
    if (rank === 1) return '🥇';
    if (rank === 2) return '🥈';
    if (rank === 3) return '🥉';
    return `#${rank}`;
  };

  const getTierColor = (tier: string) => {
    switch (tier.toLowerCase()) {
      case 'diamond':
        return 'var(--color-status-info)';
      case 'platinum':
        return 'var(--color-brand-primary)';
      case 'gold':
        return 'var(--color-status-warning)';
      case 'silver':
        return 'var(--color-text-secondary)';
      default:
        return 'var(--color-text-secondary)';
    }
  };

  return (
    <PageTransition>
      <div className="p-8 max-w-6xl mx-auto">
      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-3xl font-bold mb-2" style={{ color: 'var(--color-text-primary)' }}>
            Leaderboard
          </h1>
          <p style={{ color: 'var(--color-text-secondary)' }}>
            Compete with players worldwide
          </p>
        </div>
        <button
          onClick={fetchLeaderboard}
          disabled={isLoading}
          className="px-4 py-2 rounded-lg font-semibold transition-all flex items-center gap-2"
          style={{
            backgroundColor: 'var(--color-brand-primary)',
            color: 'white',
            opacity: isLoading ? 0.6 : 1,
          }}
        >
          <RefreshCw size={16} className={isLoading ? 'animate-spin' : ''} />
          Refresh
        </button>
      </div>

      {/* Error Alert */}
      {error && (
        <div
          className="mb-8 p-4 rounded-lg flex items-start gap-3"
          style={{ backgroundColor: 'var(--color-status-error)', color: 'white' }}
        >
          <AlertCircle size={20} className="flex-shrink-0 mt-0.5" />
          <div>
            <h3 className="font-semibold mb-1">Error</h3>
            <p className="text-sm">{error}</p>
          </div>
        </div>
      )}

      {/* User's Current Rank */}
      {userRank && (
        <div className="mb-8 p-6 rounded-lg border-2" style={{
          backgroundColor: 'var(--color-bg-secondary)',
          borderColor: 'var(--color-brand-primary)',
        }}>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-4">
              <div className="text-4xl font-bold" style={{ color: 'var(--color-brand-primary)' }}>
                {getMedalEmoji(userRank.rank)}
              </div>
              <div>
                <h3 className="text-lg font-bold" style={{ color: 'var(--color-text-primary)' }}>
                  Your Rank
                </h3>
                <p style={{ color: 'var(--color-text-secondary)' }}>
                  You are ranked #{userRank.rank} • {userRank.xp.toLocaleString()} XP
                </p>
              </div>
            </div>
            <div className="text-right">
              <div className="text-3xl font-bold" style={{ color: 'var(--color-text-primary)' }}>
                Level {userRank.level}
              </div>
              <div
                className="text-sm font-semibold"
                style={{ color: getTierColor(userRank.tier) }}
              >
                {userRank.tier.charAt(0).toUpperCase() + userRank.tier.slice(1)}
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Leaderboard Table */}
      <div className="rounded-lg overflow-hidden border" style={{
        backgroundColor: 'var(--color-bg-secondary)',
        borderColor: 'var(--color-ui-border)',
      }}>
        {isLoading ? (
          <TableSkeleton rows={10} columns={5} />
        ) : entries.length === 0 ? (
          <EmptyState
            icon="🏆"
            title="No Leaderboard Data"
            description="Start playing quizzes to climb the global rankings!"
            action={{
              label: 'Start a Quiz',
              onClick: () => window.location.href = '/quiz/lobby',
            }}
          />
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr style={{ backgroundColor: 'var(--color-bg-tertiary)' }}>
                  <th className="px-6 py-4 text-left" style={{ color: 'var(--color-text-secondary)' }}>
                    Rank
                  </th>
                  <th className="px-6 py-4 text-left" style={{ color: 'var(--color-text-secondary)' }}>
                    Player
                  </th>
                  <th className="px-6 py-4 text-center" style={{ color: 'var(--color-text-secondary)' }}>
                    Level
                  </th>
                  <th className="px-6 py-4 text-center" style={{ color: 'var(--color-text-secondary)' }}>
                    Tier
                  </th>
                  <th className="px-6 py-4 text-right" style={{ color: 'var(--color-text-secondary)' }}>
                    XP
                  </th>
                </tr>
              </thead>
              <tbody>
                {entries.map((entry, idx) => (
                  <tr
                    key={entry.playerId}
                    className={idx % 2 === 0 ? '' : 'bg-gray-800/30'}
                    style={{
                      backgroundColor: entry.playerId === user?.id
                        ? 'var(--color-brand-primary)'
                        : undefined,
                    }}
                  >
                    <td className="px-6 py-4 font-bold" style={{
                      color: entry.playerId === user?.id ? 'white' : 'var(--color-text-primary)',
                    }}>
                      <span className="text-xl">{getMedalEmoji(entry.rank)}</span>
                    </td>
                    <td className="px-6 py-4">
                      <div className="flex items-center gap-3">
                        {entry.avatar && (
                          <img
                            src={entry.avatar}
                            alt={entry.username}
                            className="w-8 h-8 rounded-full"
                          />
                        )}
                        <span style={{
                          color: entry.playerId === user?.id ? 'white' : 'var(--color-text-primary)',
                          fontWeight: entry.playerId === user?.id ? 'bold' : 'normal',
                        }}>
                          {entry.username}
                          {entry.playerId === user?.id && ' (You)'}
                        </span>
                      </div>
                    </td>
                    <td className="px-6 py-4 text-center" style={{
                      color: entry.playerId === user?.id ? 'white' : 'var(--color-text-secondary)',
                    }}>
                      Level {entry.level}
                    </td>
                    <td className="px-6 py-4 text-center" style={{
                      color: getTierColor(entry.tier),
                      fontWeight: 'bold',
                    }}>
                      {entry.tier.charAt(0).toUpperCase() + entry.tier.slice(1)}
                    </td>
                    <td className="px-6 py-4 text-right" style={{
                      color: entry.playerId === user?.id ? 'white' : 'var(--color-text-primary)',
                    }}>
                      <div className="flex items-center justify-end gap-2">
                        <Zap size={16} style={{
                          color: entry.playerId === user?.id ? 'white' : 'var(--color-brand-accent)',
                        }} />
                        <span>{entry.xp.toLocaleString()}</span>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Info Section */}
      <div className="mt-8 grid grid-cols-1 md:grid-cols-2 gap-4">
        <div className="p-4 rounded-lg" style={{ backgroundColor: 'var(--color-bg-secondary)' }}>
          <div className="flex items-center gap-2 mb-2">
            <Trophy size={20} style={{ color: 'var(--color-status-warning)' }} />
            <h3 className="font-semibold" style={{ color: 'var(--color-text-primary)' }}>
              Global Rankings
            </h3>
          </div>
          <p style={{ color: 'var(--color-text-secondary)', fontSize: '0.875rem' }}>
            Ranked by total XP earned across all quizzes and challenges
          </p>
        </div>

        <div className="p-4 rounded-lg" style={{ backgroundColor: 'var(--color-bg-secondary)' }}>
          <div className="flex items-center gap-2 mb-2">
            <Zap size={20} style={{ color: 'var(--color-brand-accent)' }} />
            <h3 className="font-semibold" style={{ color: 'var(--color-text-primary)' }}>
              How to Rank Up
            </h3>
          </div>
          <p style={{ color: 'var(--color-text-secondary)', fontSize: '0.875rem' }}>
            Complete quizzes, achieve higher accuracy, and unlock achievements to earn XP
          </p>
        </div>
      </div>
      </div>
    </PageTransition>
  );
}

export default LeaderboardPage;
