/**
 * Sidebar navigation
 */

import { useState } from 'react'
import { NavLink } from 'react-router-dom'
import { PermissionGate } from '@/components/shared/permission-gate'

const NAV_ITEMS = [
  {
    label: 'Dashboard',
    href: '/dashboard',
  },
  {
    label: 'Users & Moderation',
    permission: 'users:read' as const,
    children: [
      { label: 'Users', href: '/users' },
      { label: 'Moderation', href: '/moderation/logs' },
      { label: 'Anti-Cheat', href: '/anti-cheat', permission: 'anti-cheat:read' as const },
    ],
  },
  {
    label: 'Audit & Security',
    permission: 'audit:read' as const,
    children: [
      { label: 'Security Audit', href: '/audit/security' },
    ],
  },
  {
    label: 'Store Management',
    permission: 'storage:read' as const,
    children: [
      { label: 'Catalog', href: '/store/catalog' },
      { label: 'Flash Sales', href: '/store/flash-sales' },
      { label: 'Stock Policies', href: '/store/stock-policies' },
      { label: 'Player Stock', href: '/store/player-stock' },
      { label: 'Reward Limits', href: '/store/reward-limits' },
      { label: 'Analytics', href: '/store/analytics' },
      { label: 'Storage Browser', href: '/storage' },
    ],
  },
  {
    label: 'Content',
    permission: 'content:read' as const,
    children: [
      { label: 'Questions', href: '/content/questions' },
      { label: 'Skills', href: '/skills' },
    ],
  },
  {
    label: 'Operations',
    permission: 'operations:read' as const,
    children: [
      { label: 'Events', href: '/operations/game-events' },
      { label: 'Seasons', href: '/operations/seasons' },
      { label: 'Notifications', href: '/notifications', permission: 'notifications:read' as const },
      { label: 'Event Queue', href: '/operations/event-queue' },
      { label: 'Match History', href: '/matches' },
    ],
  },
  {
    label: 'Economy & Rewards',
    permission: 'economy:read' as const,
    children: [
      { label: 'Player Economy', href: '/economy/player' },
      { label: 'Transactions', href: '/economy/player-transactions' },
    ],
  },
  {
    label: 'Personalization',
    permission: 'personalization:read' as const,
    children: [
      { label: 'Overview', href: '/personalization' },
      { label: 'Rules', href: '/personalization/rules' },
    ],
  },
  {
    label: 'Configuration',
    permission: 'config:read' as const,
    children: [
      { label: 'Feature Flags', href: '/config/feature-flags' },
      { label: 'Admin ACL', href: '/config/admin-permissions' },
      { label: 'Diagnostics', href: '/diagnostics' },
      { label: 'Setup', href: '/settings/setup' },
    ],
  },
]

export function Sidebar() {
  const [expandedItems, setExpandedItems] = useState<Set<string>>(new Set())

  const toggleExpanded = (label: string) => {
    const newExpanded = new Set(expandedItems)
    if (newExpanded.has(label)) {
      newExpanded.delete(label)
    } else {
      newExpanded.add(label)
    }
    setExpandedItems(newExpanded)
  }

  return (
    <aside className="operator-sidebar">
      <div className="mb-8">
        <h1 className="text-xl font-bold text-ink-primary">Synaptix</h1>
        <p className="text-xs text-ink-tertiary">Operator Dashboard</p>
      </div>

      <nav className="space-y-1 flex-1">
        {NAV_ITEMS.map((item) => (
          <PermissionGate key={item.label} permission={item.permission as any} fallback={null}>
            {item.children ? (
              <div key={item.label}>
                <button
                  onClick={() => toggleExpanded(item.label)}
                  className="w-full flex items-center gap-3 px-3 py-2 text-sm rounded hover:bg-bg-secondary transition-smooth text-ink-secondary hover:text-ink-primary"
                >
                  {/* <NavItemIcon icon={item.icon || MoreVertical} /> */}
                  <span className="font-medium">{item.label}</span>
                  <span className="ml-auto">
                    {expandedItems.has(item.label) ? '−' : '+'}
                  </span>
                </button>
                {expandedItems.has(item.label) && (
                  <div className="pl-4 space-y-1 mt-1">
                    {item.children.map((child) => {
                      const childPermission = 'permission' in child ? child.permission : undefined
                      return (
                      <PermissionGate
                        key={child.href}
                        permission={childPermission as any}
                        fallback={null}
                      >
                        <NavLink
                          to={child.href}
                          className={({ isActive }) =>
                            `block px-3 py-2 text-xs rounded transition-smooth ${
                              isActive
                                ? 'bg-accent text-white'
                                : 'text-ink-tertiary hover:text-ink-secondary hover:bg-bg-secondary'
                            }`
                          }
                        >
                          {child.label}
                        </NavLink>
                      </PermissionGate>
                      )
                    })}
                  </div>
                )}
              </div>
            ) : (
              <NavLink
                to={item.href}
                className={({ isActive }) =>
                  `flex items-center gap-3 px-3 py-2 rounded transition-smooth ${
                    isActive
                      ? 'bg-accent text-white'
                      : 'text-ink-secondary hover:text-ink-primary hover:bg-bg-secondary'
                  }`
                }
              >
                {/* <NavItemIcon icon={item.icon} /> */}
                <span className="text-sm font-medium">{item.label}</span>
              </NavLink>
            )}
          </PermissionGate>
        ))}
      </nav>
    </aside>
  )
}
