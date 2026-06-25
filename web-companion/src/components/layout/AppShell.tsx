/**
 * Main app layout shell for authenticated users
 * Provides sidebar, top bar, and main content area
 */

import { Outlet, useNavigate } from 'react-router-dom';
import { useAuthStore, useUIStore } from '@stores';
import { Menu, LogOut, Settings, Home, Trophy, Zap, BookOpen, Users, Store } from 'lucide-react';

export function AppShell() {
  const navigate = useNavigate();
  const logout = useAuthStore((state) => state.logout);
  const user = useAuthStore((state) => state.user);
  const sidebarOpen = useUIStore((state) => state.sidebarOpen);
  const toggleSidebar = useUIStore((state) => state.toggleSidebar);

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
    <div className="flex h-screen bg-gray-950">
      {/* Sidebar */}
      <aside
        className={`bg-gray-900 border-r border-gray-800 transition-all duration-300 flex flex-col ${
          sidebarOpen ? 'w-64' : 'w-20'
        }`}
      >
        {/* Logo */}
        <div className="p-4 border-b border-gray-800 flex items-center justify-between">
          {sidebarOpen && (
            <div className="text-xl font-bold text-primary">Synaptix</div>
          )}
          <button
            onClick={toggleSidebar}
            className="p-2 hover:bg-gray-800 rounded-lg transition-colors"
            title={sidebarOpen ? 'Collapse' : 'Expand'}
          >
            <Menu size={20} />
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
                className="w-full flex items-center gap-3 px-4 py-3 rounded-lg hover:bg-gray-800 transition-colors text-gray-300 hover:text-white group"
                title={!sidebarOpen ? item.label : undefined}
              >
                <Icon size={20} className="flex-shrink-0" />
                {sidebarOpen && <span className="text-sm">{item.label}</span>}
              </button>
            );
          })}
        </nav>

        {/* Footer */}
        <div className="p-4 border-t border-gray-800 space-y-2">
          <button
            onClick={() => navigate('/settings')}
            className="w-full flex items-center gap-3 px-4 py-3 rounded-lg hover:bg-gray-800 transition-colors text-gray-300 hover:text-white"
            title={!sidebarOpen ? 'Settings' : undefined}
          >
            <Settings size={20} className="flex-shrink-0" />
            {sidebarOpen && <span className="text-sm">Settings</span>}
          </button>
          <button
            onClick={handleLogout}
            className="w-full flex items-center gap-3 px-4 py-3 rounded-lg hover:bg-red-900/20 transition-colors text-red-400 hover:text-red-300"
            title={!sidebarOpen ? 'Logout' : undefined}
          >
            <LogOut size={20} className="flex-shrink-0" />
            {sidebarOpen && <span className="text-sm">Logout</span>}
          </button>
        </div>
      </aside>

      {/* Main content */}
      <div className="flex-1 flex flex-col overflow-hidden">
        {/* Top bar */}
        <header className="bg-gray-900 border-b border-gray-800 px-6 py-4 flex items-center justify-between">
          <h1 className="text-2xl font-bold text-white">Trivia Tycoon</h1>
          {user && (
            <div className="flex items-center gap-4">
              <div className="text-right">
                <div className="text-sm font-medium text-white">{user.displayName}</div>
                <div className="text-xs text-gray-400">{user.email}</div>
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
