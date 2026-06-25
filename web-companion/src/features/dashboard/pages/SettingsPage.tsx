/**
 * Settings page with theme, account, and preference management
 */

import { useAuthStore } from '@stores';
import { useTheme } from '@hooks/useTheme';
import { Sun, Moon, Palette, User, Bell, Lock } from 'lucide-react';
import type { SynaptixMode } from '@theme/themes';

export function SettingsPage() {
  const user = useAuthStore((state) => state.user);
  const { synaptixMode, themeVariant, setSynaptixMode, toggleThemeVariant } =
    useTheme();

  const synaptixModes: Array<{ id: SynaptixMode; label: string; description: string }> = [
    {
      id: 'kids',
      label: '👧 Kids Mode',
      description: 'Bright colors, playful design, family-friendly',
    },
    {
      id: 'teens',
      label: '👦 Teens Mode',
      description: 'Modern, energetic, balanced design',
    },
    {
      id: 'adults',
      label: '👔 Adults Mode',
      description: 'Professional, dark, sophisticated theme',
    },
  ];

  return (
    <div className="p-8 max-w-4xl mx-auto">
      <h1 className="text-3xl font-bold mb-8">Settings</h1>

      {/* Account Section */}
      <div className="card mb-6">
        <div className="flex items-center gap-3 mb-4">
          <User size={24} style={{ color: 'var(--color-brand-primary)' }} />
          <h2 className="text-xl font-bold">Account</h2>
        </div>
        <div className="space-y-4">
          <div className="flex items-center justify-between p-4 rounded-lg" style={{ backgroundColor: 'var(--color-bg-tertiary)' }}>
            <div>
              <p className="text-sm" style={{ color: 'var(--color-text-secondary)' }}>Email</p>
              <p className="font-medium">{user?.email}</p>
            </div>
          </div>
          <div className="flex items-center justify-between p-4 rounded-lg" style={{ backgroundColor: 'var(--color-bg-tertiary)' }}>
            <div>
              <p className="text-sm" style={{ color: 'var(--color-text-secondary)' }}>Display Name</p>
              <p className="font-medium">{user?.displayName}</p>
            </div>
          </div>
        </div>
      </div>

      {/* Theme Section */}
      <div className="card mb-6">
        <div className="flex items-center gap-3 mb-6">
          <Palette size={24} style={{ color: 'var(--color-brand-primary)' }} />
          <h2 className="text-xl font-bold">Theme</h2>
        </div>

        {/* Light/Dark Toggle */}
        <div className="mb-8">
          <h3 className="font-semibold mb-4">Theme Variant</h3>
          <div className="flex gap-3">
            <button
              onClick={toggleThemeVariant}
              className="flex items-center gap-2 px-4 py-3 rounded-lg transition-all duration-200"
              style={{
                backgroundColor:
                  themeVariant === 'light'
                    ? 'var(--color-brand-primary)'
                    : 'var(--color-bg-tertiary)',
                color:
                  themeVariant === 'light'
                    ? 'white'
                    : 'var(--color-text-primary)',
              }}
            >
              <Sun size={20} />
              Light
            </button>
            <button
              onClick={toggleThemeVariant}
              className="flex items-center gap-2 px-4 py-3 rounded-lg transition-all duration-200"
              style={{
                backgroundColor:
                  themeVariant === 'dark'
                    ? 'var(--color-brand-primary)'
                    : 'var(--color-bg-tertiary)',
                color:
                  themeVariant === 'dark'
                    ? 'white'
                    : 'var(--color-text-primary)',
              }}
            >
              <Moon size={20} />
              Dark
            </button>
          </div>
        </div>

        {/* Synaptix Mode Selection */}
        <div>
          <h3 className="font-semibold mb-4">User Mode (Synaptix)</h3>
          <div className="space-y-2">
            {synaptixModes.map((mode) => (
              <button
                key={mode.id}
                onClick={() => setSynaptixMode(mode.id)}
                className="w-full p-4 rounded-lg text-left transition-all duration-200 border"
                style={{
                  backgroundColor:
                    synaptixMode === mode.id
                      ? 'var(--color-brand-primary)'
                      : 'var(--color-bg-tertiary)',
                  borderColor:
                    synaptixMode === mode.id
                      ? 'var(--color-brand-secondary)'
                      : 'var(--color-ui-border)',
                  color:
                    synaptixMode === mode.id
                      ? 'white'
                      : 'var(--color-text-primary)',
                }}
              >
                <div className="font-semibold">{mode.label}</div>
                <div
                  className="text-sm mt-1"
                  style={{
                    color:
                      synaptixMode === mode.id
                        ? 'rgba(255, 255, 255, 0.8)'
                        : 'var(--color-text-secondary)',
                  }}
                >
                  {mode.description}
                </div>
              </button>
            ))}
          </div>
        </div>
      </div>

      {/* Notifications Section */}
      <div className="card mb-6">
        <div className="flex items-center gap-3 mb-4">
          <Bell size={24} style={{ color: 'var(--color-brand-primary)' }} />
          <h2 className="text-xl font-bold">Notifications</h2>
        </div>
        <div className="space-y-3">
          {[
            { label: 'Game Results', enabled: true },
            { label: 'Friend Requests', enabled: true },
            { label: 'Challenges', enabled: true },
            { label: 'Daily Reminder', enabled: false },
          ].map((notif) => (
            <div
              key={notif.label}
              className="flex items-center justify-between p-3 rounded-lg"
              style={{ backgroundColor: 'var(--color-bg-tertiary)' }}
            >
              <label>{notif.label}</label>
              <input
                type="checkbox"
                defaultChecked={notif.enabled}
                className="w-4 h-4"
              />
            </div>
          ))}
        </div>
      </div>

      {/* Privacy Section */}
      <div className="card">
        <div className="flex items-center gap-3 mb-4">
          <Lock size={24} style={{ color: 'var(--color-brand-primary)' }} />
          <h2 className="text-xl font-bold">Privacy & Security</h2>
        </div>
        <div className="space-y-3">
          <button
            className="w-full p-3 rounded-lg text-left transition-all"
            style={{ backgroundColor: 'var(--color-bg-tertiary)' }}
          >
            Change Password
          </button>
          <button
            className="w-full p-3 rounded-lg text-left transition-all"
            style={{ backgroundColor: 'var(--color-bg-tertiary)' }}
          >
            Two-Factor Authentication
          </button>
          <button
            className="w-full p-3 rounded-lg text-left transition-all"
            style={{
              backgroundColor: 'var(--color-status-error)',
              color: 'white',
            }}
          >
            Delete Account
          </button>
        </div>
      </div>
    </div>
  );
}

export default SettingsPage;
