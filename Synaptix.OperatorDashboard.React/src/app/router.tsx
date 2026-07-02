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
const NotificationsHubPage = React.lazy(() => import('@/features/notifications/pages/hub'))
const AntiCheatQueuePage = React.lazy(() => import('@/features/anti-cheat/pages/queue'))
const SecurityAuditPage = React.lazy(() => import('@/features/audit/pages/security'))
const PlayerProfilePage = React.lazy(() => import('@/features/moderation/pages/player-profile'))
const PlayerEconomyPage = React.lazy(() => import('@/features/economy/pages/player-economy'))
const QuestionsQueuePage = React.lazy(() => import('@/features/content/pages/questions-queue'))
const LifecyclePage = React.lazy(() => import('@/features/operations/pages/lifecycle'))
const StoreManagementPage = React.lazy(() => import('@/features/store/pages/store-management'))
const NotFoundPage = React.lazy(() => import('@/components/shared/not-found'))

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
            element: <div>User Detail - Coming Soon</div>,
          },
          {
            path: ':userId/investigation',
            element: <div>User Investigation - Coming Soon</div>,
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
            element: <div>Audit Event Detail - Coming Soon</div>,
          },
        ],
      },
      // Moderation section
      {
        path: 'moderation',
        children: [
          {
            path: 'logs',
            element: <div>Moderation Logs - Coming Soon</div>,
          },
          {
            path: 'logs/:logId',
            element: <div>Moderation Log Detail - Coming Soon</div>,
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
            element: <div>Player Stock - Coming Soon</div>,
          },
          {
            path: 'analytics',
            element: <div>Store Analytics - Coming Soon</div>,
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
        ],
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
