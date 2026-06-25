/**
 * Missions page - view and complete daily/weekly missions for rewards
 */

import { useEffect, useState } from 'react';
import { useProfileStore } from '@stores';
import { apiClient } from '@core/api/client';
import { AlertCircle, Zap, Gift } from 'lucide-react';
import { CardSkeleton } from '@components/skeletons/CardSkeleton';
import { EmptyState } from '@components/EmptyState';
import { PageTransition } from '@components/PageTransition';
import { useToast } from '@hooks/useToast';

interface Mission {
  missionId: string;
  title: string;
  description: string;
  type: 'daily' | 'weekly' | 'seasonal';
  progress: number;
  target: number;
  reward: {
    xp?: number;
    coins?: number;
    diamonds?: number;
  };
  completed: boolean;
  claimed: boolean;
  icon?: string;
}

export function MissionsPage() {
  const toast = useToast();
  const addXP = useProfileStore((state) => state.addXP);
  const addCoins = useProfileStore((state) => state.addCoins);
  const addDiamonds = useProfileStore((state) => state.addDiamonds);
  const [missions, setMissions] = useState<Mission[]>([]);
  const [selectedType, setSelectedType] = useState<'all' | 'daily' | 'weekly' | 'seasonal'>('all');
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [claiming, setClaiming] = useState<string | null>(null);

  useEffect(() => {
    const fetchMissions = async () => {
      try {
        setIsLoading(true);
        setError(null);
        const data = await apiClient.getMissions();
        setMissions(data);
      } catch (err) {
        console.error('Failed to fetch missions:', err);
        const errorMsg = 'Failed to load missions. Please try again.';
        setError(errorMsg);
        toast.error(errorMsg);
      } finally {
        setIsLoading(false);
      }
    };

    fetchMissions();
  }, [toast]);

  const handleClaimReward = async (mission: Mission) => {
    try {
      setClaiming(mission.missionId);
      await apiClient.claimMissionReward(mission.missionId);

      // Update profile with rewards
      if (mission.reward.xp) addXP(mission.reward.xp);
      if (mission.reward.coins) addCoins(mission.reward.coins);
      if (mission.reward.diamonds) addDiamonds(mission.reward.diamonds);

      // Update mission status
      setMissions((prev) =>
        prev.map((m) =>
          m.missionId === mission.missionId ? { ...m, claimed: true } : m
        )
      );

      setError(null);
      toast.success(`Claimed reward! +${mission.reward.xp || 0} XP`);
    } catch (err) {
      console.error('Failed to claim reward:', err);
      const errorMsg = 'Failed to claim reward. Please try again.';
      setError(errorMsg);
      toast.error(errorMsg);
    } finally {
      setClaiming(null);
    }
  };

  const filteredMissions =
    selectedType === 'all'
      ? missions
      : missions.filter((m) => m.type === selectedType);

  const missionsByStatus = {
    completed: filteredMissions.filter((m) => m.completed),
    inProgress: filteredMissions.filter((m) => !m.completed),
  };

  const getMissionIcon = (mission: Mission) => {
    if (mission.completed && mission.claimed) return '✅';
    if (mission.completed) return '⭐';
    if (mission.progress > 0) return '📍';
    return '📋';
  };

  return (
    <PageTransition>
      <div className="p-8 max-w-4xl mx-auto">
      <h1 className="text-3xl font-bold mb-2" style={{ color: 'var(--color-text-primary)' }}>
        Missions
      </h1>
      <p style={{ color: 'var(--color-text-secondary)' }}>
        Complete missions to earn rewards
      </p>

      {/* Error Alert */}
      {error && (
        <div
          className="my-6 p-4 rounded-lg flex items-start gap-3"
          style={{ backgroundColor: 'var(--color-status-error)', color: 'white' }}
        >
          <AlertCircle size={20} className="flex-shrink-0 mt-0.5" />
          <div>
            <h3 className="font-semibold mb-1">Error</h3>
            <p className="text-sm">{error}</p>
          </div>
        </div>
      )}

      {/* Type Filter */}
      <div className="my-8 flex gap-2 flex-wrap">
        {['all', 'daily', 'weekly', 'seasonal'].map((type) => (
          <button
            key={type}
            onClick={() => setSelectedType(type as any)}
            className="px-4 py-2 rounded-lg font-semibold text-sm transition-all"
            style={{
              backgroundColor:
                selectedType === type
                  ? 'var(--color-brand-primary)'
                  : 'var(--color-bg-secondary)',
              color:
                selectedType === type
                  ? 'white'
                  : 'var(--color-text-primary)',
              borderWidth: selectedType === type ? '0' : '1px',
              borderColor: 'var(--color-ui-border)',
            }}
          >
            {type.charAt(0).toUpperCase() + type.slice(1)}
          </button>
        ))}
      </div>

      {/* Stats Cards */}
      {!isLoading && (
        <div className="mb-8 grid grid-cols-1 md:grid-cols-3 gap-4">
          <div
            className="p-4 rounded-lg"
            style={{ backgroundColor: 'var(--color-bg-secondary)' }}
          >
            <p style={{ color: 'var(--color-text-secondary)', fontSize: '0.875rem' }}>
              In Progress
            </p>
            <p
              className="text-3xl font-bold mt-1"
              style={{ color: 'var(--color-brand-primary)' }}
            >
              {missionsByStatus.inProgress.length}
            </p>
          </div>

          <div
            className="p-4 rounded-lg"
            style={{ backgroundColor: 'var(--color-bg-secondary)' }}
          >
            <p style={{ color: 'var(--color-text-secondary)', fontSize: '0.875rem' }}>
              Completed
            </p>
            <p
              className="text-3xl font-bold mt-1"
              style={{ color: 'var(--color-status-success)' }}
            >
              {missionsByStatus.completed.filter((m) => m.claimed).length}
            </p>
          </div>

          <div
            className="p-4 rounded-lg"
            style={{ backgroundColor: 'var(--color-bg-secondary)' }}
          >
            <p style={{ color: 'var(--color-text-secondary)', fontSize: '0.875rem' }}>
              Awaiting Claim
            </p>
            <p
              className="text-3xl font-bold mt-1"
              style={{ color: 'var(--color-status-warning)' }}
            >
              {missionsByStatus.completed.filter((m) => !m.claimed).length}
            </p>
          </div>
        </div>
      )}

      {/* Missions List */}
      {isLoading ? (
        <div className="space-y-4">
          {Array.from({ length: 5 }).map((_, idx) => (
            <CardSkeleton key={idx} />
          ))}
        </div>
      ) : filteredMissions.length === 0 ? (
        <EmptyState
          icon="📋"
          title="No Missions Available"
          description={selectedType === 'all'
            ? 'All missions completed! Check back tomorrow for new daily missions.'
            : `No ${selectedType} missions available. Try another type!`}
          action={{
            label: selectedType === 'all' ? 'View All' : 'Show All Missions',
            onClick: () => setSelectedType('all'),
          }}
        />
      ) : (
        <div className="space-y-4">
          {/* In Progress */}
          {missionsByStatus.inProgress.length > 0 && (
            <div>
              <h2 className="text-lg font-bold mb-3" style={{ color: 'var(--color-text-primary)' }}>
                In Progress
              </h2>
              <div className="space-y-3">
                {missionsByStatus.inProgress.map((mission) => (
                  <div
                    key={mission.missionId}
                    className="p-4 rounded-lg"
                    style={{ backgroundColor: 'var(--color-bg-secondary)' }}
                  >
                    <div className="flex items-start gap-4">
                      <div className="text-3xl flex-shrink-0">
                        {getMissionIcon(mission)}
                      </div>

                      <div className="flex-1">
                        <h3
                          className="font-bold text-lg mb-1"
                          style={{ color: 'var(--color-text-primary)' }}
                        >
                          {mission.title}
                        </h3>
                        <p
                          className="text-sm mb-3"
                          style={{ color: 'var(--color-text-secondary)' }}
                        >
                          {mission.description}
                        </p>

                        {/* Progress Bar */}
                        <div className="mb-3">
                          <div
                            className="w-full h-2 rounded-full overflow-hidden"
                            style={{ backgroundColor: 'var(--color-bg-tertiary)' }}
                          >
                            <div
                              className="h-full transition-all"
                              style={{
                                width: `${(mission.progress / mission.target) * 100}%`,
                                backgroundColor: 'var(--color-brand-primary)',
                              }}
                            />
                          </div>
                          <p
                            className="text-xs mt-1"
                            style={{ color: 'var(--color-text-secondary)' }}
                          >
                            {mission.progress} / {mission.target}
                          </p>
                        </div>

                        {/* Rewards */}
                        <div className="flex gap-2 flex-wrap">
                          {mission.reward.xp && (
                            <span
                              className="px-2 py-1 rounded text-xs font-bold flex items-center gap-1"
                              style={{
                                backgroundColor: 'var(--color-bg-tertiary)',
                                color: 'var(--color-brand-accent)',
                              }}
                            >
                              <Zap size={12} />
                              {mission.reward.xp} XP
                            </span>
                          )}
                          {mission.reward.coins && (
                            <span
                              className="px-2 py-1 rounded text-xs font-bold"
                              style={{
                                backgroundColor: 'var(--color-bg-tertiary)',
                                color: 'var(--color-status-warning)',
                              }}
                            >
                              🪙 {mission.reward.coins}
                            </span>
                          )}
                          {mission.reward.diamonds && (
                            <span
                              className="px-2 py-1 rounded text-xs font-bold"
                              style={{
                                backgroundColor: 'var(--color-bg-tertiary)',
                                color: 'var(--color-status-info)',
                              }}
                            >
                              💎 {mission.reward.diamonds}
                            </span>
                          )}
                        </div>
                      </div>

                      <div className="text-xs" style={{ color: 'var(--color-text-secondary)' }}>
                        {mission.type.toUpperCase()}
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Completed */}
          {missionsByStatus.completed.length > 0 && (
            <div>
              <h2 className="text-lg font-bold mb-3" style={{ color: 'var(--color-text-primary)' }}>
                Completed
              </h2>
              <div className="space-y-3">
                {missionsByStatus.completed.map((mission) => (
                  <div
                    key={mission.missionId}
                    className="p-4 rounded-lg"
                    style={{
                      backgroundColor: 'var(--color-bg-secondary)',
                      opacity: mission.claimed ? 0.7 : 1,
                    }}
                  >
                    <div className="flex items-start justify-between">
                      <div className="flex items-start gap-4 flex-1">
                        <div className="text-3xl flex-shrink-0">
                          {getMissionIcon(mission)}
                        </div>

                        <div className="flex-1">
                          <h3
                            className="font-bold text-lg mb-1"
                            style={{ color: 'var(--color-text-primary)' }}
                          >
                            {mission.title}
                          </h3>
                          <p
                            className="text-sm"
                            style={{
                              color: mission.claimed
                                ? 'var(--color-text-tertiary)'
                                : 'var(--color-text-secondary)',
                            }}
                          >
                            {mission.description}
                          </p>
                        </div>
                      </div>

                      <button
                        onClick={() => handleClaimReward(mission)}
                        disabled={mission.claimed || claiming === mission.missionId}
                        className="py-2 px-4 rounded-lg font-bold text-sm flex items-center gap-2 transition-all ml-4 flex-shrink-0"
                        style={{
                          backgroundColor: mission.claimed
                            ? 'var(--color-bg-tertiary)'
                            : 'var(--color-status-success)',
                          color: mission.claimed ? 'var(--color-text-secondary)' : 'white',
                          opacity: claiming === mission.missionId ? 0.6 : 1,
                          cursor:
                            mission.claimed || claiming === mission.missionId
                              ? 'not-allowed'
                              : 'pointer',
                        }}
                      >
                        <Gift size={16} />
                        {claiming === mission.missionId
                          ? 'Claiming...'
                          : mission.claimed
                            ? 'Claimed'
                            : 'Claim'}
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      )}
    </div>
    </PageTransition>
  );
}

export default MissionsPage;
