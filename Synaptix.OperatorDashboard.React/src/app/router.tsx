/**
 * React Router configuration
 * Defines all routes and their RBAC guards
 */

import React from 'react'
import { createBrowserRouter, Navigate } from 'react-router-dom'

// Layouts
const AuthLayout = React.lazy(() => import('@/components/layout/auth-layout'))
const AppLayout = React.lazy(() => import('@/components/layout/app-layout'))

// Auth pages (public)
const LoginPage = React.lazy(() => import('@/features/auth/pages/login'))
const ForgotPasswordPage = React.lazy(() => import('@/features/auth/pages/forgot-password'))
const ResetPasswordPage = React.lazy(() => import('@/features/auth/pages/reset-password'))

// App pages (protected by auth)
const DashboardPage = React.lazy(() => import('@/features/dashboard/pages/home'))
const UsersListPage = React.lazy(() => import('@/features/users/pages/list'))
const UserDetailPage = React.lazy(() => import('@/features/users/pages/detail'))
const UserInvestigationPage = React.lazy(() => import('@/features/users/pages/investigation'))
const NotificationsHubPage = React.lazy(() => import('@/features/notifications/pages/hub'))
const AntiCheatQueuePage = React.lazy(() => import('@/features/anti-cheat/pages/queue'))
const SecurityAuditPage = React.lazy(() => import('@/features/audit/pages/security'))
const AuditEventDetailPage = React.lazy(() => import('@/features/audit/pages/event-detail'))
const PlayerProfilePage = React.lazy(() => import('@/features/moderation/pages/player-profile'))
const ModerationLogsPage = React.lazy(() => import('@/features/moderation/pages/logs'))
const ModerationLogDetailPage = React.lazy(() => import('@/features/moderation/pages/log-detail'))
const PlayerEconomyPage = React.lazy(() => import('@/features/economy/pages/player-economy'))
const PaymentsPage = React.lazy(() => import('@/features/payments/pages/payments'))
const PaymentReconciliationPage = React.lazy(() => import('@/features/payments/pages/reconciliation'))
const QuestionsQueuePage = React.lazy(() => import('@/features/content/pages/questions-queue'))
const LifecyclePage = React.lazy(() => import('@/features/operations/pages/lifecycle'))
const StoreManagementPage = React.lazy(() => import('@/features/store/pages/store-management'))
const PlayerStockPage = React.lazy(() => import('@/features/store/pages/player-stock'))
const StoreAnalyticsPage = React.lazy(() => import('@/features/store/pages/store-analytics'))
const EventQueueStreamingPage = React.lazy(() => import('@/features/event-queue/pages/streaming'))
const PersonalizationArchetypesPage = React.lazy(() => import('@/features/personalization/pages/archetypes'))
const ConfigSettingsPage = React.lazy(() => import('@/features/config/pages/settings'))
const InstallerSetupPage = React.lazy(() => import('@/features/installer/pages/setup'))
const InstallerUnavailablePage = React.lazy(() => import('@/features/installer/pages/unavailable'))
const DiagnosticsMonitoringPage = React.lazy(() => import('@/features/diagnostics/pages/monitoring'))
const DiagnosticsUnavailablePage = React.lazy(() => import('@/features/diagnostics/pages/unavailable'))
const StorageBrowserPage = React.lazy(() => import('@/features/storage/pages/browser'))
const MatchHistoryReplayPage = React.lazy(() => import('@/features/match-history/pages/replay'))
const SkillsManagementPage = React.lazy(() => import('@/features/skills/pages/management'))
const NotFoundPage = React.lazy(() => import('@/components/shared/not-found'))

import { isDiagnosticsEnabled, isInstallerEnabled } from '@/lib/operator-feature-flags'

function InstallerRoute() {
  return isInstallerEnabled() ? <InstallerSetupPage /> : <InstallerUnavailablePage />
}

function DiagnosticsRoute() {
  return isDiagnosticsEnabled() ? <DiagnosticsMonitoringPage /> : <DiagnosticsUnavailablePage />
}

export const router = createBrowserRouter([
  {
    path: '/',
    element: <AppLayout />,
    errorElement: <NotFoundPage />,
    children: [
      {
        index: true,
        element: <Navigate to="/dashboard" replace />,
      },
      {
        path: 'dashboard',
        element: <DashboardPage />,
      },
      // Users section
      {
        path: 'users',
        children: [
          {
            index: true,
            element: <UsersListPage />,
          },
          {
            path: ':userId',
            element: <UserDetailPage />,
          },
          {
            path: ':userId/investigation',
            element: <UserInvestigationPage />,
          },
        ],
      },
      // Notifications section
      {
        path: 'notifications',
        children: [
          {
            index: true,
            element: <NotificationsHubPage />,
          },
        ],
      },
      // Audit section
      {
        path: 'audit',
        children: [
          {
            index: true,
            element: <Navigate to="/audit/security" replace />,
          },
          {
            path: 'security',
            element: <SecurityAuditPage />,
          },
          {
            path: 'security/:eventId',
            element: <AuditEventDetailPage />,
          },
        ],
      },
      // Moderation section
      {
        path: 'moderation',
        children: [
          {
            path: 'logs',
            element: <ModerationLogsPage />,
          },
          {
            path: 'logs/:logId',
            element: <ModerationLogDetailPage />,
          },
          {
            path: 'players/:playerId',
            element: <PlayerProfilePage />,
          },
        ],
      },
      // Anti-cheat section
      {
        path: 'anti-cheat',
        children: [
          {
            index: true,
            element: <AntiCheatQueuePage />,
          },
        ],
      },
      // Store section
      {
        path: 'store',
        children: [
          {
            index: true,
            element: <StoreManagementPage />,
          },
          {
            path: 'catalog',
            element: <StoreManagementPage />,
          },
          {
            path: 'flash-sales',
            element: <StoreManagementPage />,
          },
          {
            path: 'stock-policies',
            element: <StoreManagementPage />,
          },
          {
            path: 'player-stock',
            element: <PlayerStockPage />,
          },
          {
            path: 'analytics',
            element: <StoreAnalyticsPage />,
          },
          {
            path: 'reward-limits',
            element: <StoreManagementPage />,
          },
        ],
      },
      // Economy section
      {
        path: 'economy',
        children: [
          {
            index: true,
            element: <PlayerEconomyPage />,
          },
          {
            path: 'player',
            element: <PlayerEconomyPage />,
          },
          {
            path: 'player-transactions',
            element: <PlayerEconomyPage />,
          },
        ],
      },
      // Payments section
      {
        path: 'payments',
        children: [
          {
            index: true,
            element: <PaymentsPage />,
          },
          {
            path: 'reconciliation',
            element: <PaymentReconciliationPage />,
          },
        ],
      },
      // Content section
      {
        path: 'content',
        children: [
          {
            index: true,
            element: <QuestionsQueuePage />,
          },
          {
            path: 'questions',
            element: <QuestionsQueuePage />,
          },
        ],
      },
      // Operations section
      {
        path: 'operations',
        children: [
          {
            index: true,
            element: <LifecyclePage />,
          },
          {
            path: 'seasons',
            element: <LifecyclePage />,
          },
          {
            path: 'game-events',
            element: <LifecyclePage />,
          },
          {
            path: 'event-queue',
            element: <EventQueueStreamingPage />,
          },
        ],
      },
      // Personalization section
      {
        path: 'personalization',
        children: [
          {
            index: true,
            element: <PersonalizationArchetypesPage />,
          },
          {
            path: 'rules',
            element: <PersonalizationArchetypesPage />,
          },
        ],
      },
      // Configuration section
      {
        path: 'config',
        children: [
          {
            index: true,
            element: <ConfigSettingsPage />,
          },
          {
            path: 'feature-flags',
            element: <ConfigSettingsPage />,
          },
          {
            path: 'admin-permissions',
            element: <ConfigSettingsPage />,
          },
        ],
      },
      // Setup & diagnostics — gated (default off; see operator-feature-flags.ts)
      {
        path: 'settings/setup',
        element: <InstallerRoute />,
      },
      {
        path: 'diagnostics',
        element: <DiagnosticsRoute />,
      },
      // Storage section
      {
        path: 'storage',
        element: <StorageBrowserPage />,
      },
      // Match history section
      {
        path: 'matches',
        element: <MatchHistoryReplayPage />,
      },
      // Skills section
      {
        path: 'skills',
        element: <SkillsManagementPage />,
      },
      // Add more sections as needed...
    ],
  },
  {
    path: '/auth',
    element: <AuthLayout />,
    errorElement: <NotFoundPage />,
    children: [
      {
        path: 'login',
        element: <LoginPage />,
      },
      {
        path: 'forgot-password',
        element: <ForgotPasswordPage />,
      },
      {
        path: 'reset-password',
        element: <ResetPasswordPage />,
      },
    ],
  },
  {
    path: '*',
    element: <NotFoundPage />,
  },
])
