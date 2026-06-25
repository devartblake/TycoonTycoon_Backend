/**
 * Player profile page - displays personal stats, achievements, and progress
 */

import { useEffect } from 'react';
import { useAuthStore, useProfileStore } from '@stores';
import { Trophy, Zap, TrendingUp, BookOpen, Target, AlertCircle } from 'lucide-react';
import { CardSkeleton } from '@components/skeletons/CardSkeleton';
import { EmptyState } from '@components/EmptyState';
import { PageTransition } from '@components/PageTransition';
import { useToast } from '@hooks/useToast';

export function ProfilePage() {
  const toast = useToast();
  const user = useAuthStore((state) => state.user);
  const profile = useProfileStore((state) => state.profile);
  const isLoading = useProfileStore((state) => state.isLoading);
  const error = useProfileStore((state) => state.error);

  useEffect(() => {
    if (error) {
      toast.error(error);
    }
  }, [error, toast]);

  if (!user) {
    return (
      <div className="p-8 text-center">
        <p style={{ color: 'var(--color-text-secondary)' }}>Please log in to view your profile</p>
      </div>
    );
  }

  if (error) {
    return (
      <PageTransition>
        <div className="p-8 max-w-4xl mx-auto">
          <div
            className="p-6 rounded-lg flex items-start gap-3"
            style={{ backgroundColor: 'var(--color-status-error)', color: 'white' }}
          >
            <AlertCircle size={24} className="flex-shrink-0" />
            <div>
              <h3 className="font-bold mb-1">Error Loading Profile</h3>
              <p>{error}</p>
            </div>
          </div>
        </div>
      </PageTransition>
    );
  }

  if (isLoading) {
    return (
      <PageTransition>
        <div className="p-8 max-w-4xl mx-auto space-y-4">
          {Array.from({ length: 4 }).map((_, idx) => (
            <CardSkeleton key={idx} />
          ))}
        </div>
      </PageTransition>
    );
  }

  return (
    <PageTransition>
      <div className="p-8 max-w-4xl mx-auto">
      {/* Profile Header */}
      <div className="mb-8">
        <div className="flex items-start gap-6 mb-8">
          <div
            className="w-24 h-24 rounded-full flex items-center justify-center text-4xl"
            style={{ backgroundColor: 'var(--color-bg-secondary)' }}
          >
            {user.avatar ? (
              <img src={user.avatar} alt={user.displayName} className="w-full h-full rounded-full" />
            ) : (
              '👤'
            )}
          </div>
          <div className="flex-1">
            <h1 className="text-4xl font-bold mb-2" style={{ color: 'var(--color-text-primary)' }}>
              {user.displayName}
            </h1>
            <p style={{ color: 'var(--color-text-secondary)' }}>{user.email}</p>
            {profile && (
              <div className="mt-4 flex items-center gap-4">
                <div
                  className="px-4 py-2 rounded-lg"
                  style={{ backgroundColor: 'var(--color-bg-secondary)' }}
                >
                  <p style={{ color: 'var(--color-text-secondary)', fontSize: '0.875rem' }}>
                    Level
                  </p>
                  <p className="text-2xl font-bold" style={{ color: 'var(--color-brand-primary)' }}>
                    {profile.level}
                  </p>
                </div>
                <div
                  className="px-4 py-2 rounded-lg"
                  style={{ backgroundColor: 'var(--color-bg-secondary)' }}
                >
                  <p style={{ color: 'var(--color-text-secondary)', fontSize: '0.875rem' }}>
                    Tier
                  </p>
                  <p className="text-2xl font-bold" style={{ color: 'var(--color-status-warning)' }}>
                    {profile.tier.charAt(0).toUpperCase() + profile.tier.slice(1)}
                  </p>
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Stats Grid */}
        {profile && (
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            {/* XP Card */}
            <div
              className="rounded-lg p-6"
              style={{ backgroundColor: 'var(--color-bg-secondary)' }}
            >
              <div className="flex items-center gap-3 mb-3">
                <Zap size={24} style={{ color: 'var(--color-brand-accent)' }} />
                <span style={{ color: 'var(--color-text-secondary)' }}>Total XP</span>
              </div>
              <div
                className="text-3xl font-bold"
                style={{ color: 'var(--color-text-primary)' }}
              >
                {profile.xp.toLocaleString()}
              </div>
              <div
                className="text-xs mt-2"
                style={{ color: 'var(--color-text-tertiary)' }}
              >
                {profile.xp - Math.floor(profile.xp / 1000) * 1000} to next level
              </div>
            </div>

            {/* Quizzes Card */}
            <div
              className="rounded-lg p-6"
              style={{ backgroundColor: 'var(--color-bg-secondary)' }}
            >
              <div className="flex items-center gap-3 mb-3">
                <Target size={24} style={{ color: 'var(--color-status-info)' }} />
                <span style={{ color: 'var(--color-text-secondary)' }}>Quizzes</span>
              </div>
              <div
                className="text-3xl font-bold"
                style={{ color: 'var(--color-text-primary)' }}
              >
                {profile.totalQuizzesSolved}
              </div>
              <div
                className="text-xs mt-2"
                style={{ color: 'var(--color-text-tertiary)' }}
              >
                completed
              </div>
            </div>

            {/* Accuracy Card */}
            <div
              className="rounded-lg p-6"
              style={{ backgroundColor: 'var(--color-bg-secondary)' }}
            >
              <div className="flex items-center gap-3 mb-3">
                <TrendingUp size={24} style={{ color: 'var(--color-status-success)' }} />
                <span style={{ color: 'var(--color-text-secondary)' }}>Accuracy</span>
              </div>
              <div
                className="text-3xl font-bold"
                style={{ color: 'var(--color-text-primary)' }}
              >
                {profile.accuracy.toFixed(1)}%
              </div>
              <div
                className="text-xs mt-2"
                style={{ color: 'var(--color-text-tertiary)' }}
              >
                average
              </div>
            </div>

            {/* Streak Card */}
            <div
              className="rounded-lg p-6"
              style={{ backgroundColor: 'var(--color-bg-secondary)' }}
            >
              <div className="flex items-center gap-3 mb-3">
                <span style={{ fontSize: '1.5rem' }}>🔥</span>
                <span style={{ color: 'var(--color-text-secondary)' }}>Streak</span>
              </div>
              <div
                className="text-3xl font-bold"
                style={{ color: 'var(--color-text-primary)' }}
              >
                {profile.streak}
              </div>
              <div
                className="text-xs mt-2"
                style={{ color: 'var(--color-text-tertiary)' }}
              >
                days
              </div>
            </div>
          </div>
        )}
      </div>

      {/* Achievements Section */}
      {profile && (
        <div className="mb-8">
          <h2
            className="text-2xl font-bold mb-4 flex items-center gap-2"
            style={{ color: 'var(--color-text-primary)' }}
          >
            <Trophy size={28} style={{ color: 'var(--color-status-warning)' }} />
            Achievements
          </h2>
          {profile.achievements && profile.achievements.length > 0 ? (
            <div className="grid grid-cols-1 md:grid-cols-3 lg:grid-cols-4 gap-4">
              {profile.achievements.map((achievement, idx) => (
                <div
                  key={idx}
                  className="p-4 rounded-lg text-center"
                  style={{ backgroundColor: 'var(--color-bg-secondary)' }}
                >
                  <div style={{ fontSize: '2.5rem', marginBottom: '0.5rem' }}>🏅</div>
                  <p
                    className="font-semibold text-sm"
                    style={{ color: 'var(--color-text-primary)' }}
                  >
                    {achievement}
                  </p>
                </div>
              ))}
            </div>
          ) : (
            <EmptyState
              icon="🏆"
              title="No Achievements Yet"
              description="Earn achievements by completing quizzes, maintaining streaks, and reaching milestones!"
            />
          )}
        </div>
      )}

      {/* Skills Section */}
      {profile && profile.activeSkills && profile.activeSkills.length > 0 && (
        <div className="mb-8">
          <h2
            className="text-2xl font-bold mb-4 flex items-center gap-2"
            style={{ color: 'var(--color-text-primary)' }}
          >
            <BookOpen size={28} style={{ color: 'var(--color-brand-accent)' }} />
            Active Skills
          </h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {profile.activeSkills.map((skill, idx) => (
              <div
                key={idx}
                className="p-4 rounded-lg flex items-center gap-3"
                style={{ backgroundColor: 'var(--color-bg-secondary)' }}
              >
                <span style={{ fontSize: '1.5rem' }}>⭐</span>
                <p style={{ color: 'var(--color-text-primary)' }}>{skill}</p>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Wallet Section */}
      {profile && (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-8">
          <div
            className="p-6 rounded-lg"
            style={{ backgroundColor: 'var(--color-bg-secondary)' }}
          >
            <div className="flex items-center gap-3 mb-2">
              <span style={{ fontSize: '1.5rem' }}>🪙</span>
              <span style={{ color: 'var(--color-text-secondary)' }}>Coins</span>
            </div>
            <p
              className="text-3xl font-bold"
              style={{ color: 'var(--color-status-warning)' }}
            >
              {profile.coins.toLocaleString()}
            </p>
          </div>

          <div
            className="p-6 rounded-lg"
            style={{ backgroundColor: 'var(--color-bg-secondary)' }}
          >
            <div className="flex items-center gap-3 mb-2">
              <span style={{ fontSize: '1.5rem' }}>💎</span>
              <span style={{ color: 'var(--color-text-secondary)' }}>Diamonds</span>
            </div>
            <p
              className="text-3xl font-bold"
              style={{ color: 'var(--color-status-info)' }}
            >
              {profile.diamonds.toLocaleString()}
            </p>
          </div>
        </div>
      )}

      {/* Account Info */}
      <div
        className="rounded-lg p-6"
        style={{ backgroundColor: 'var(--color-bg-secondary)' }}
      >
        <h3 className="font-bold mb-4" style={{ color: 'var(--color-text-primary)' }}>
          Account Information
        </h3>
        <div className="space-y-3">
          <div className="flex items-center justify-between pb-3" style={{
            borderBottomColor: 'var(--color-ui-border)',
            borderBottomWidth: '1px',
          }}>
            <span style={{ color: 'var(--color-text-secondary)' }}>Member Since</span>
            <span style={{ color: 'var(--color-text-primary)' }}>
              {profile && new Date(profile.createdAt).toLocaleDateString()}
            </span>
          </div>
          <div className="flex items-center justify-between pb-3" style={{
            borderBottomColor: 'var(--color-ui-border)',
            borderBottomWidth: '1px',
          }}>
            <span style={{ color: 'var(--color-text-secondary)' }}>Last Active</span>
            <span style={{ color: 'var(--color-text-primary)' }}>
              {profile && new Date(profile.lastPlayedAt).toLocaleDateString()}
            </span>
          </div>
        </div>
      </div>
      </div>
    </PageTransition>
  );
}

export default ProfilePage;
