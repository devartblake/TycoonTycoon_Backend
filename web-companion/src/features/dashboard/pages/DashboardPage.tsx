/**
 * Home dashboard page - shows player stats, quick actions, and featured content
 */

import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuthStore, useProfileStore } from '@stores';
import { apiClient } from '@core/api/client';
import { Play, Trophy, Zap, Users, BookOpen, AlertCircle } from 'lucide-react';

export function DashboardPage() {
  const navigate = useNavigate();
  const user = useAuthStore((state) => state.user);
  const profile = useProfileStore((state) => state.profile);
  const setProfile = useProfileStore((state) => state.setProfile);
  const setLoading = useProfileStore((state) => state.setLoading);
  const setError = useProfileStore((state) => state.setError);
  const clearError = useProfileStore((state) => state.clearError);
  const isLoading = useProfileStore((state) => state.isLoading);
  const error = useProfileStore((state) => state.error);

  useEffect(() => {
    const fetchProfile = async () => {
      try {
        setLoading(true);
        clearError();
        const userData = await apiClient.getCurrentUser();
        setProfile(userData);
      } catch (err: any) {
        console.error('Failed to fetch user profile:', err);

        // Check if it's a CORS error or authentication error
        const isCorsError = err?.message?.includes('Network Error') || err?.code === 'CORS';
        const isAuthError = err?.response?.status === 401 || err?.response?.status === 403;

        if (isCorsError) {
          setError('API server is not accessible from your location. Please check the server configuration.');
        } else if (isAuthError) {
          setError('Please log in to view your profile.');
        } else {
          setError('Failed to load your profile. Please try again.');
        }
      } finally {
        setLoading(false);
      }
    };

    if (!profile && !isLoading && user) {
      fetchProfile();
    }
  }, [profile, isLoading, user, setProfile, setLoading, setError, clearError]);

  const quickActions = [
    {
      icon: Play,
      label: 'Play Quiz',
      description: 'Test your knowledge',
      path: '/play',
      color: 'from-blue-600 to-blue-700',
    },
    {
      icon: Trophy,
      label: 'Leaderboard',
      description: 'View rankings',
      path: '/leaderboard',
      color: 'from-yellow-600 to-yellow-700',
    },
    {
      icon: Zap,
      label: 'Skills',
      description: 'Unlock abilities',
      path: '/skills',
      color: 'from-purple-600 to-purple-700',
    },
    {
      icon: Users,
      label: 'Friends',
      description: 'Challenge friends',
      path: '/friends',
      color: 'from-green-600 to-green-700',
    },
    {
      icon: BookOpen,
      label: 'Study Mode',
      description: 'Learn & practice',
      path: '/study',
      color: 'from-orange-600 to-orange-700',
    },
  ];

  return (
    <div className="p-8 max-w-7xl mx-auto">
      {/* Welcome Section */}
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-white mb-2">
          Welcome back, {user?.displayName || 'Player'}! 👋
        </h1>
        <p className="text-gray-400">
          {profile
            ? `Level ${profile.level} • Rank #${profile.rank} • Streak: ${profile.streak} days`
            : 'Load your profile to see your progress'}
        </p>
      </div>

      {/* Error Alert */}
      {error && (
        <div className="mb-8 p-4 bg-red-900/30 border border-red-700 rounded-lg flex items-start gap-3">
          <AlertCircle size={20} className="text-red-500 flex-shrink-0 mt-0.5" />
          <div>
            <h3 className="font-semibold text-red-400 mb-1">Error</h3>
            <p className="text-red-300 text-sm">{error}</p>
            <button
              onClick={() => window.location.reload()}
              className="mt-2 px-3 py-1 bg-red-700 hover:bg-red-600 text-white rounded text-sm transition-colors"
            >
              Retry
            </button>
          </div>
        </div>
      )}

      {/* Stats Grid */}
      {profile && (
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-8">
          {/* Level Card */}
          <div className="bg-gray-900 rounded-lg border border-gray-800 p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-gray-400 text-sm mb-1">Level</p>
                <p className="text-3xl font-bold text-white">{profile.level}</p>
              </div>
              <div className="text-4xl">📈</div>
            </div>
            <div className="mt-4 w-full bg-gray-800 rounded-full h-2">
              <div
                className="bg-primary h-2 rounded-full"
                style={{ width: `${(profile.xp / profile.xpForNextLevel) * 100}%` }}
              />
            </div>
            <p className="text-xs text-gray-500 mt-2">
              {profile.xp} / {profile.xpForNextLevel} XP
            </p>
          </div>

          {/* Rank Card */}
          <div className="bg-gray-900 rounded-lg border border-gray-800 p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-gray-400 text-sm mb-1">Rank</p>
                <p className="text-3xl font-bold text-yellow-400">#{profile.rank}</p>
              </div>
              <div className="text-4xl">🏆</div>
            </div>
            <p className="text-xs text-gray-500 mt-4">
              Tier: <span className="text-yellow-400 font-semibold capitalize">{profile.tier}</span>
            </p>
          </div>

          {/* Streak Card */}
          <div className="bg-gray-900 rounded-lg border border-gray-800 p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-gray-400 text-sm mb-1">Streak</p>
                <p className="text-3xl font-bold text-orange-400">{profile.streak}</p>
              </div>
              <div className="text-4xl">🔥</div>
            </div>
            <p className="text-xs text-gray-500 mt-4">Days in a row</p>
          </div>

          {/* Accuracy Card */}
          <div className="bg-gray-900 rounded-lg border border-gray-800 p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-gray-400 text-sm mb-1">Accuracy</p>
                <p className="text-3xl font-bold text-green-400">{profile.accuracy.toFixed(1)}%</p>
              </div>
              <div className="text-4xl">🎯</div>
            </div>
            <p className="text-xs text-gray-500 mt-4">
              {profile.totalQuizzesSolved} quizzes solved
            </p>
          </div>
        </div>
      )}

      {/* Quick Actions */}
      <div className="mb-8">
        <h2 className="text-xl font-bold text-white mb-4">Quick Actions</h2>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-4">
          {quickActions.map((action) => {
            const Icon = action.icon;
            return (
              <button
                key={action.path}
                onClick={() => navigate(action.path)}
                className={`group rounded-lg border border-gray-800 p-6 hover:border-gray-700 transition-all hover:shadow-lg overflow-hidden`}
              >
                <div className={`absolute inset-0 bg-gradient-to-br ${action.color} opacity-10 group-hover:opacity-20 transition-opacity`} />
                <div className="relative">
                  <div className={`w-12 h-12 bg-gradient-to-br ${action.color} rounded-lg flex items-center justify-center mb-3 text-white`}>
                    <Icon size={24} />
                  </div>
                  <h3 className="text-white font-bold text-left">{action.label}</h3>
                  <p className="text-gray-400 text-xs text-left mt-1">{action.description}</p>
                </div>
              </button>
            );
          })}
        </div>
      </div>

      {/* Recent Activity Section */}
      <div className="bg-gray-900 rounded-lg border border-gray-800 p-6">
        <h2 className="text-xl font-bold text-white mb-4">Recent Activity</h2>
        {profile ? (
          <div className="space-y-3">
            <div className="flex items-center justify-between p-3 bg-gray-800/50 rounded-lg">
              <div>
                <p className="text-white text-sm">Last played</p>
                <p className="text-gray-400 text-xs">
                  {new Date(profile.lastPlayedAt).toLocaleDateString()}
                </p>
              </div>
              <span className="text-gray-500">📅</span>
            </div>
            <div className="text-center py-6 text-gray-400">
              <p className="text-sm">No recent activity yet</p>
              <button
                onClick={() => navigate('/play')}
                className="mt-3 px-4 py-2 bg-primary hover:bg-secondary text-white rounded-lg text-sm transition-colors"
              >
                Play Now
              </button>
            </div>
          </div>
        ) : (
          <div className="text-center py-8">
            <p className="text-gray-400 mb-4">Loading your profile...</p>
            <div className="animate-spin inline-block w-8 h-8 border-2 border-primary border-t-transparent rounded-full" />
          </div>
        )}
      </div>
    </div>
  );
}

export default DashboardPage;
