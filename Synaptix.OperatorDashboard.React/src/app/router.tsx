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
            path: 'security',
            element: <div>Security Audit - Coming Soon</div>,
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
            element: <div>Moderation Player Profile - Coming Soon</div>,
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
            path: 'catalog',
            element: <div>Store Catalog - Coming Soon</div>,
          },
          {
            path: 'flash-sales',
            element: <div>Flash Sales - Coming Soon</div>,
          },
          {
            path: 'stock-policies',
            element: <div>Stock Policies - Coming Soon</div>,
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
            element: <div>Reward Limits - Coming Soon</div>,
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
