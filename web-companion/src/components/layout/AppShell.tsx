/**
 * Main app layout shell for authenticated users
 * Provides sidebar, top bar, and main content area
 */

import { Outlet, useNavigate } from 'react-router-dom';
import { useAuthStore, useUIStore, useProfileStore } from '@stores';
import { Menu, LogOut, Settings, Home, Trophy, Zap, BookOpen, Users, Store, Coins } from 'lucide-react';

export function AppShell() {
  const navigate = useNavigate();
  const logout = useAuthStore((state) => state.logout);
  const user = useAuthStore((state) => state.user);
  const sidebarOpen = useUIStore((state) => state.sidebarOpen);
  const toggleSidebar = useUIStore((state) => state.toggleSidebar);
  const profile = useProfileStore((state) => state.profile);

  const handleLogout = () => {
    // TODO: Call logout API endpoint first
    logout();
    navigate('/login');
  };

  const navItems = [
    { label: 'Dashboard', icon: Home, path: '/' },
    { label: 'Play', icon: Trophy, path: '/play' },
    { label: 'Skills', icon: Zap, path: '/skills' },
    { label: 'Leaderboard', icon: Trophy, path: '/leaderboard' },
    { label: 'Study', icon: BookOpen, path: '/study' },
    { label: 'Friends', icon: Users, path: '/friends' },
    { label: 'Store', icon: Store, path: '/store' },
  ];

  return (
    <div className="flex h-screen" style={{ backgroundColor: 'var(--color-bg-primary)' }}>
      {/* Sidebar */}
      <aside
        className={`transition-all duration-300 flex flex-col ${
          sidebarOpen ? 'w-64' : 'w-20'
        }`}
        style={{
          backgroundColor: 'var(--color-bg-secondary)',
          borderRightColor: 'var(--color-ui-border)',
        }}
      >
        {/* Logo */}
        <div
          className="p-4 flex items-center justify-between"
          style={{
            borderBottomColor: 'var(--color-ui-border)',
            borderBottomWidth: '1px',
          }}
        >
          {sidebarOpen && (
            <div className="text-xl font-bold" style={{ color: 'var(--color-brand-primary)' }}>
              Synaptix
            </div>
          )}
          <button
            onClick={toggleSidebar}
            className="p-2 rounded-lg transition-colors"
            style={{ backgroundColor: 'var(--color-bg-tertiary)' }}
            title={sidebarOpen ? 'Collapse' : 'Expand'}
          >
            <Menu size={20} style={{ color: 'var(--color-text-secondary)' }} />
          </button>
        </div>

        {/* Navigation */}
        <nav className="flex-1 p-4 space-y-2">
          {navItems.map((item) => {
            const Icon = item.icon;
            return (
              <button
                key={item.path}
                onClick={() => navigate(item.path)}
                className="w-full flex items-center gap-3 px-4 py-3 rounded-lg transition-colors group"
                style={{ backgroundColor: 'var(--color-bg-tertiary)' }}
                title={!sidebarOpen ? item.label : undefined}
              >
                <Icon size={20} className="flex-shrink-0" style={{ color: 'var(--color-text-secondary)' }} />
                {sidebarOpen && <span className="text-sm" style={{ color: 'var(--color-text-primary)' }}>{item.label}</span>}
              </button>
            );
          })}
        </nav>

        {/* Footer */}
        <div
          className="p-4 space-y-2"
          style={{
            borderTopColor: 'var(--color-ui-border)',
            borderTopWidth: '1px',
          }}
        >
          <button
            onClick={() => navigate('/settings')}
            className="w-full flex items-center gap-3 px-4 py-3 rounded-lg transition-colors"
            style={{ backgroundColor: 'var(--color-bg-tertiary)' }}
            title={!sidebarOpen ? 'Settings' : undefined}
          >
            <Settings size={20} className="flex-shrink-0" style={{ color: 'var(--color-text-secondary)' }} />
            {sidebarOpen && <span className="text-sm" style={{ color: 'var(--color-text-primary)' }}>Settings</span>}
          </button>
          <button
            onClick={handleLogout}
            className="w-full flex items-center gap-3 px-4 py-3 rounded-lg transition-colors"
            style={{ backgroundColor: 'var(--color-status-error)' }}
            title={!sidebarOpen ? 'Logout' : undefined}
          >
            <LogOut size={20} className="flex-shrink-0" style={{ color: 'white' }} />
            {sidebarOpen && <span className="text-sm" style={{ color: 'white' }}>Logout</span>}
          </button>
        </div>
      </aside>

      {/* Main content */}
      <div className="flex-1 flex flex-col overflow-hidden">
        {/* Top bar */}
        <header
          className="px-6 py-4 flex items-center justify-between"
          style={{
            backgroundColor: 'var(--color-bg-secondary)',
            borderBottomColor: 'var(--color-ui-border)',
            borderBottomWidth: '1px',
          }}
        >
          <h1 className="text-2xl font-bold" style={{ color: 'var(--color-text-primary)' }}>
            Trivia Tycoon
          </h1>
          {user && (
            <div className="flex items-center gap-6">
              {/* Wallet Display */}
              {profile && (
                <div className="flex items-center gap-3">
                  <div className="flex items-center gap-2 px-3 py-2 rounded-lg" style={{ backgroundColor: 'var(--color-bg-tertiary)' }}>
                    <Coins size={18} style={{ color: 'var(--color-status-warning)' }} />
                    <span className="font-semibold text-sm" style={{ color: 'var(--color-text-primary)' }}>
                      {profile.coins}
                    </span>
                  </div>
                  <div className="flex items-center gap-2 px-3 py-2 rounded-lg" style={{ backgroundColor: 'var(--color-bg-tertiary)' }}>
                    <span style={{ fontSize: '1.2rem' }}>💎</span>
                    <span className="font-semibold text-sm" style={{ color: 'var(--color-text-primary)' }}>
                      {profile.diamonds}
                    </span>
                  </div>
                </div>
              )}

              <div className="text-right">
                <div className="text-sm font-medium" style={{ color: 'var(--color-text-primary)' }}>
                  {user.displayName}
                </div>
                <div className="text-xs" style={{ color: 'var(--color-text-secondary)' }}>
                  {user.email}
                </div>
              </div>
              {user.avatar && (
                <img
                  src={user.avatar}
                  alt={user.displayName}
                  className="w-10 h-10 rounded-full"
                />
              )}
            </div>
          )}
        </header>

        {/* Content area */}
        <main className="flex-1 overflow-auto">
          <Outlet />
        </main>
      </div>
    </div>
  );
}

export default AppShell;
