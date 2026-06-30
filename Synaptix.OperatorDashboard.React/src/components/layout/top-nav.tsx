/**
 * Top navigation bar with user profile and actions
 */

// import React from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '@/hooks/use-permission'
import { useAuthStore } from '@/features/auth/store'
import { LogOut, User } from 'lucide-react'

export function TopNav() {
  const navigate = useNavigate()
  const { profile } = useAuth()
  const logout = useAuthStore((state) => state.logout)

  const handleLogout = () => {
    logout()
    navigate('/auth/login')
  }

  return (
    <header className="border-b border-panel-border bg-panel-bg sticky top-0 z-50">
      <div className="px-6 py-4 flex items-center justify-between">
        <div>
          <h1 className="text-lg font-semibold text-ink-primary">Operator Dashboard</h1>
        </div>

        {profile && (
          <div className="flex items-center gap-4">
            <div className="flex items-center gap-2 text-sm">
              <User className="w-4 h-4 text-ink-tertiary" />
              <span className="text-ink-secondary">{profile.email}</span>
            </div>
            <button
              onClick={handleLogout}
              className="p-2 rounded hover:bg-bg-secondary transition-smooth text-ink-secondary hover:text-accent"
              title="Logout"
            >
              <LogOut className="w-5 h-5" />
            </button>
          </div>
        )}
      </div>
    </header>
  )
}
