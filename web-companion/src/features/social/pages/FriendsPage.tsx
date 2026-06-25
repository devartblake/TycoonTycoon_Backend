/**
 * Friends page - manage friends, view friend stats, and send challenges
 */

import { useEffect, useState } from 'react';
import { apiClient } from '@core/api/client';
import { UserPlus, Trophy, Zap, AlertCircle, Check, X } from 'lucide-react';
import { GridSkeleton } from '@components/skeletons/GridSkeleton';
import { EmptyState } from '@components/EmptyState';
import { PageTransition } from '@components/PageTransition';
import { useToast } from '@hooks/useToast';

interface Friend {
  playerId: string;
  username: string;
  level: number;
  xp: number;
  avatar?: string;
  isOnline?: boolean;
}

interface FriendRequest {
  requestId: string;
  fromPlayerId: string;
  fromUsername: string;
  timestamp: string;
  avatar?: string;
}

export function FriendsPage() {
  const toast = useToast();
  const [friends, setFriends] = useState<Friend[]>([]);
  const [friendRequests, setFriendRequests] = useState<FriendRequest[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchUsername, setSearchUsername] = useState('');
  const [addingFriend, setAddingFriend] = useState(false);

  const fetchFriendsData = async () => {
    try {
      setIsLoading(true);
      setError(null);

      const [friendsData, requestsData] = await Promise.all([
        apiClient.getFriends(),
        apiClient.getFriendRequests(),
      ]);

      setFriends(friendsData);
      setFriendRequests(requestsData);
    } catch (err) {
      console.error('Failed to fetch friends:', err);
      const errorMsg = 'Failed to load friends. Please try again.';
      setError(errorMsg);
      toast.error(errorMsg);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchFriendsData();
  }, [toast]);

  const handleAddFriend = async () => {
    if (!searchUsername.trim()) return;

    try {
      setAddingFriend(true);
      await apiClient.addFriend(searchUsername);
      setSearchUsername('');
      toast.success(`Added ${searchUsername} as a friend!`);
      // Refresh friends list
      await fetchFriendsData();
    } catch (err) {
      console.error('Failed to add friend:', err);
      const errorMsg = 'Failed to add friend. User might not exist.';
      setError(errorMsg);
      toast.error(errorMsg);
    } finally {
      setAddingFriend(false);
    }
  };

  const handleAcceptRequest = async (requestId: string) => {
    try {
      await apiClient.acceptFriendRequest(requestId);
      toast.success('Friend request accepted!');
      await fetchFriendsData();
    } catch (err) {
      console.error('Failed to accept friend request:', err);
      const errorMsg = 'Failed to accept friend request.';
      setError(errorMsg);
      toast.error(errorMsg);
    }
  };

  const handleDeclineRequest = async (requestId: string) => {
    try {
      await apiClient.declineFriendRequest(requestId);
      setFriendRequests((prev) => prev.filter((r) => r.requestId !== requestId));
      toast.info('Friend request declined');
    } catch (err) {
      console.error('Failed to decline friend request:', err);
      const errorMsg = 'Failed to decline friend request.';
      setError(errorMsg);
      toast.error(errorMsg);
    }
  };

  const handleRemoveFriend = async (friendId: string) => {
    try {
      await apiClient.removeFriend(friendId);
      setFriends((prev) => prev.filter((f) => f.playerId !== friendId));
      toast.info('Friend removed');
    } catch (err) {
      console.error('Failed to remove friend:', err);
      const errorMsg = 'Failed to remove friend.';
      setError(errorMsg);
      toast.error(errorMsg);
    }
  };

  return (
    <PageTransition>
      <div className="p-8 max-w-6xl mx-auto">
      <h1 className="text-3xl font-bold mb-2" style={{ color: 'var(--color-text-primary)' }}>
        Friends
      </h1>
      <p style={{ color: 'var(--color-text-secondary)' }}>
        Connect with other players and send challenges
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

      {/* Add Friend Section */}
      <div className="my-8 p-6 rounded-lg" style={{ backgroundColor: 'var(--color-bg-secondary)' }}>
        <h2 className="font-bold mb-4 flex items-center gap-2" style={{ color: 'var(--color-text-primary)' }}>
          <UserPlus size={20} />
          Add Friend
        </h2>
        <div className="flex gap-2">
          <input
            type="text"
            placeholder="Enter username..."
            value={searchUsername}
            onChange={(e) => setSearchUsername(e.target.value)}
            onKeyPress={(e) => e.key === 'Enter' && handleAddFriend()}
            style={{
              backgroundColor: 'var(--color-bg-tertiary)',
              color: 'var(--color-text-primary)',
              border: '1px solid var(--color-ui-border)',
              padding: '0.5rem 1rem',
              borderRadius: '0.5rem',
              flex: 1,
            }}
            disabled={addingFriend}
          />
          <button
            onClick={handleAddFriend}
            disabled={addingFriend || !searchUsername.trim()}
            style={{
              backgroundColor: 'var(--color-brand-primary)',
              color: 'white',
              padding: '0.5rem 1rem',
              borderRadius: '0.5rem',
              fontWeight: 'bold',
              opacity: addingFriend || !searchUsername.trim() ? 0.6 : 1,
              cursor: addingFriend || !searchUsername.trim() ? 'not-allowed' : 'pointer',
            }}
          >
            {addingFriend ? 'Adding...' : 'Add'}
          </button>
        </div>
      </div>

      {/* Friend Requests */}
      {friendRequests.length > 0 && (
        <div className="mb-8">
          <h2 className="text-xl font-bold mb-4" style={{ color: 'var(--color-text-primary)' }}>
            Friend Requests ({friendRequests.length})
          </h2>
          <div className="space-y-3">
            {friendRequests.map((request) => (
              <div
                key={request.requestId}
                className="p-4 rounded-lg flex items-center justify-between"
                style={{ backgroundColor: 'var(--color-bg-secondary)' }}
              >
                <div className="flex items-center gap-3">
                  <div
                    className="w-10 h-10 rounded-full flex items-center justify-center"
                    style={{ backgroundColor: 'var(--color-bg-tertiary)' }}
                  >
                    {request.avatar ? (
                      <img src={request.avatar} alt={request.fromUsername} className="w-full h-full rounded-full" />
                    ) : (
                      '👤'
                    )}
                  </div>
                  <div>
                    <p style={{ color: 'var(--color-text-primary)', fontWeight: 'bold' }}>
                      {request.fromUsername}
                    </p>
                    <p style={{ color: 'var(--color-text-secondary)', fontSize: '0.875rem' }}>
                      {new Date(request.timestamp).toLocaleDateString()}
                    </p>
                  </div>
                </div>
                <div className="flex gap-2">
                  <button
                    onClick={() => handleAcceptRequest(request.requestId)}
                    className="p-2 rounded-lg transition-colors"
                    style={{
                      backgroundColor: 'var(--color-status-success)',
                      color: 'white',
                    }}
                  >
                    <Check size={18} />
                  </button>
                  <button
                    onClick={() => handleDeclineRequest(request.requestId)}
                    className="p-2 rounded-lg transition-colors"
                    style={{
                      backgroundColor: 'var(--color-status-error)',
                      color: 'white',
                    }}
                  >
                    <X size={18} />
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Friends List */}
      <div>
        <h2 className="text-xl font-bold mb-4" style={{ color: 'var(--color-text-primary)' }}>
          Friends ({friends.length})
        </h2>

        {isLoading ? (
          <GridSkeleton items={6} columns={3} />
        ) : friends.length === 0 ? (
          <EmptyState
            icon="👥"
            title="No Friends Yet"
            description="Add friends to compete, compare scores, and send challenges!"
            action={{
              label: 'Add a Friend',
              onClick: () => {
                const input = document.querySelector('input[placeholder*="Enter username"]') as HTMLInputElement;
                if (input) input.focus();
              },
            }}
          />
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {friends.map((friend) => (
              <div
                key={friend.playerId}
                className="p-4 rounded-lg"
                style={{ backgroundColor: 'var(--color-bg-secondary)' }}
              >
                <div className="flex items-start justify-between mb-3">
                  <div className="flex items-center gap-3">
                    <div
                      className="w-12 h-12 rounded-full flex items-center justify-center text-lg"
                      style={{ backgroundColor: 'var(--color-bg-tertiary)' }}
                    >
                      {friend.avatar ? (
                        <img src={friend.avatar} alt={friend.username} className="w-full h-full rounded-full" />
                      ) : (
                        '👤'
                      )}
                    </div>
                    <div>
                      <p style={{ color: 'var(--color-text-primary)', fontWeight: 'bold' }}>
                        {friend.username}
                      </p>
                      <p
                        className="text-sm flex items-center gap-1"
                        style={{ color: friend.isOnline ? 'var(--color-status-success)' : 'var(--color-text-secondary)' }}
                      >
                        <span
                          className="w-2 h-2 rounded-full"
                          style={{
                            backgroundColor: friend.isOnline ? 'var(--color-status-success)' : 'transparent',
                          }}
                        />
                        {friend.isOnline ? 'Online' : 'Offline'}
                      </p>
                    </div>
                  </div>
                </div>

                <div className="space-y-2 mb-4 pb-4" style={{ borderBottomColor: 'var(--color-ui-border)', borderBottomWidth: '1px' }}>
                  <div className="flex items-center justify-between">
                    <span style={{ color: 'var(--color-text-secondary)', fontSize: '0.875rem' }}>Level</span>
                    <span style={{ color: 'var(--color-text-primary)', fontWeight: 'bold' }}>
                      {friend.level}
                    </span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span style={{ color: 'var(--color-text-secondary)', fontSize: '0.875rem' }}>XP</span>
                    <span className="flex items-center gap-1" style={{ color: 'var(--color-brand-accent)' }}>
                      <Zap size={14} />
                      {friend.xp.toLocaleString()}
                    </span>
                  </div>
                </div>

                <div className="flex gap-2">
                  <button
                    className="flex-1 py-2 px-3 rounded-lg font-semibold text-sm flex items-center justify-center gap-1 transition-all"
                    style={{
                      backgroundColor: 'var(--color-brand-primary)',
                      color: 'white',
                    }}
                  >
                    <Trophy size={16} />
                    Challenge
                  </button>
                  <button
                    onClick={() => handleRemoveFriend(friend.playerId)}
                    className="py-2 px-3 rounded-lg font-semibold text-sm transition-all"
                    style={{
                      backgroundColor: 'var(--color-bg-tertiary)',
                      color: 'var(--color-text-secondary)',
                    }}
                  >
                    <X size={16} />
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
    </PageTransition>
  );
}

export default FriendsPage;
