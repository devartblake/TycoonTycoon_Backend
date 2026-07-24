/**
 * Sidebar navigation
 */

import { useState } from 'react'
import { NavLink } from 'react-router-dom'
import { PermissionGate } from '@/components/shared/permission-gate'
import { isDiagnosticsEnabled, isInstallerEnabled } from '@/lib/operator-feature-flags'

type NavChild = {
  label: string
  href: string
  permission?: string
  /** When set, child is shown only if this flag is true */
  featureFlag?: 'installer' | 'diagnostics'
}

type NavItem = {
  label: string
  href?: string
  permission?: string
  children?: NavChild[]
}

function buildNavItems(): NavItem[] {
  const showInstaller = isInstallerEnabled()
  const showDiagnostics = isDiagnosticsEnabled()

  const configChildren: NavChild[] = [
    { label: 'Feature Flags', href: '/config/feature-flags' },
    { label: 'Admin ACL', href: '/config/admin-permissions' },
  ]
  if (showDiagnostics) {
    configChildren.push({ label: 'Diagnostics', href: '/diagnostics', featureFlag: 'diagnostics' })
  }
  if (showInstaller) {
    configChildren.push({ label: 'Setup', href: '/settings/setup', featureFlag: 'installer' })
  }

  return [
    {
      label: 'Dashboard',
      href: '/dashboard',
    },
    {
      label: 'Users & Moderation',
      permission: 'users:read',
      children: [
        { label: 'Users', href: '/users' },
        { label: 'Moderation', href: '/moderation/logs' },
        { label: 'Anti-Cheat', href: '/anti-cheat', permission: 'anti-cheat:read' },
      ],
    },
    {
      label: 'Audit & Security',
      permission: 'audit:read',
      children: [{ label: 'Security Audit', href: '/audit/security' }],
    },
    {
      label: 'Store Management',
      permission: 'storage:read',
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
      permission: 'content:read',
      children: [
        { label: 'Questions', href: '/content/questions' },
        { label: 'Skills', href: '/skills' },
      ],
    },
    {
      label: 'Operations',
      permission: 'operations:read',
      children: [
        { label: 'Events', href: '/operations/game-events' },
        { label: 'Seasons', href: '/operations/seasons' },
        { label: 'Notifications', href: '/notifications', permission: 'notifications:read' },
        { label: 'Event Queue', href: '/operations/event-queue' },
        { label: 'Match History', href: '/matches' },
      ],
    },
    {
      label: 'Economy & Rewards',
      permission: 'economy:read',
      children: [
        { label: 'Player Economy', href: '/economy/player' },
        { label: 'Transactions', href: '/economy/player-transactions' },
        { label: 'Payments', href: '/payments' },
        { label: 'Reconciliation', href: '/payments/reconciliation' },
      ],
    },
    {
      label: 'Personalization',
      permission: 'personalization:read',
      children: [
        { label: 'Overview', href: '/personalization' },
        { label: 'Rules', href: '/personalization/rules' },
      ],
    },
    {
      label: 'Configuration',
      permission: 'config:read',
      children: configChildren,
    },
  ]
}

export function Sidebar() {
  const [expandedItems, setExpandedItems] = useState<Set<string>>(new Set())
  // Rebuild when flags change (localStorage toggles require refresh in practice)
  const navItems = buildNavItems()

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
        {navItems.map((item) => (
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
            ) : item.href ? (
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
            ) : null}
          </PermissionGate>
        ))}
      </nav>
    </aside>
  )
}
