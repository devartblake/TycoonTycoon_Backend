import { jsx as _jsx } from "react/jsx-runtime";
/**
 * React Router configuration
 * Defines all routes and their RBAC guards
 */
import React from 'react';
import { createBrowserRouter, Navigate } from 'react-router-dom';
// Layouts
const AuthLayout = React.lazy(() => import('@/components/layout/auth-layout'));
const AppLayout = React.lazy(() => import('@/components/layout/app-layout'));
// Auth pages (public)
const LoginPage = React.lazy(() => import('@/features/auth/pages/login'));
const ForgotPasswordPage = React.lazy(() => import('@/features/auth/pages/forgot-password'));
const ResetPasswordPage = React.lazy(() => import('@/features/auth/pages/reset-password'));
// App pages (protected by auth)
const DashboardPage = React.lazy(() => import('@/features/dashboard/pages/home'));
const UsersListPage = React.lazy(() => import('@/features/users/pages/list'));
const NotificationsHubPage = React.lazy(() => import('@/features/notifications/pages/hub'));
const AntiCheatQueuePage = React.lazy(() => import('@/features/anti-cheat/pages/queue'));
const SecurityAuditPage = React.lazy(() => import('@/features/audit/pages/security'));
const PlayerProfilePage = React.lazy(() => import('@/features/moderation/pages/player-profile'));
const PlayerEconomyPage = React.lazy(() => import('@/features/economy/pages/player-economy'));
const QuestionsQueuePage = React.lazy(() => import('@/features/content/pages/questions-queue'));
const LifecyclePage = React.lazy(() => import('@/features/operations/pages/lifecycle'));
const StoreManagementPage = React.lazy(() => import('@/features/store/pages/store-management'));
const NotFoundPage = React.lazy(() => import('@/components/shared/not-found'));
export const router = createBrowserRouter([
    {
        path: '/',
        element: _jsx(AppLayout, {}),
        errorElement: _jsx(NotFoundPage, {}),
        children: [
            {
                index: true,
                element: _jsx(Navigate, { to: "/dashboard", replace: true }),
            },
            {
                path: 'dashboard',
                element: _jsx(DashboardPage, {}),
            },
            // Users section
            {
                path: 'users',
                children: [
                    {
                        index: true,
                        element: _jsx(UsersListPage, {}),
                    },
                    {
                        path: ':userId',
                        element: _jsx("div", { children: "User Detail - Coming Soon" }),
                    },
                    {
                        path: ':userId/investigation',
                        element: _jsx("div", { children: "User Investigation - Coming Soon" }),
                    },
                ],
            },
            // Notifications section
            {
                path: 'notifications',
                children: [
                    {
                        index: true,
                        element: _jsx(NotificationsHubPage, {}),
                    },
                ],
            },
            // Audit section
            {
                path: 'audit',
                children: [
                    {
                        index: true,
                        element: _jsx(Navigate, { to: "/audit/security", replace: true }),
                    },
                    {
                        path: 'security',
                        element: _jsx(SecurityAuditPage, {}),
                    },
                    {
                        path: 'security/:eventId',
                        element: _jsx("div", { children: "Audit Event Detail - Coming Soon" }),
                    },
                ],
            },
            // Moderation section
            {
                path: 'moderation',
                children: [
                    {
                        path: 'logs',
                        element: _jsx("div", { children: "Moderation Logs - Coming Soon" }),
                    },
                    {
                        path: 'logs/:logId',
                        element: _jsx("div", { children: "Moderation Log Detail - Coming Soon" }),
                    },
                    {
                        path: 'players/:playerId',
                        element: _jsx(PlayerProfilePage, {}),
                    },
                ],
            },
            // Anti-cheat section
            {
                path: 'anti-cheat',
                children: [
                    {
                        index: true,
                        element: _jsx(AntiCheatQueuePage, {}),
                    },
                ],
            },
            // Store section
            {
                path: 'store',
                children: [
                    {
                        index: true,
                        element: _jsx(StoreManagementPage, {}),
                    },
                    {
                        path: 'catalog',
                        element: _jsx(StoreManagementPage, {}),
                    },
                    {
                        path: 'flash-sales',
                        element: _jsx(StoreManagementPage, {}),
                    },
                    {
                        path: 'stock-policies',
                        element: _jsx(StoreManagementPage, {}),
                    },
                    {
                        path: 'player-stock',
                        element: _jsx("div", { children: "Player Stock - Coming Soon" }),
                    },
                    {
                        path: 'analytics',
                        element: _jsx("div", { children: "Store Analytics - Coming Soon" }),
                    },
                    {
                        path: 'reward-limits',
                        element: _jsx(StoreManagementPage, {}),
                    },
                ],
            },
            // Economy section
            {
                path: 'economy',
                children: [
                    {
                        index: true,
                        element: _jsx(PlayerEconomyPage, {}),
                    },
                    {
                        path: 'player',
                        element: _jsx(PlayerEconomyPage, {}),
                    },
                    {
                        path: 'player-transactions',
                        element: _jsx(PlayerEconomyPage, {}),
                    },
                ],
            },
            // Content section
            {
                path: 'content',
                children: [
                    {
                        index: true,
                        element: _jsx(QuestionsQueuePage, {}),
                    },
                    {
                        path: 'questions',
                        element: _jsx(QuestionsQueuePage, {}),
                    },
                ],
            },
            // Operations section
            {
                path: 'operations',
                children: [
                    {
                        index: true,
                        element: _jsx(LifecyclePage, {}),
                    },
                    {
                        path: 'seasons',
                        element: _jsx(LifecyclePage, {}),
                    },
                    {
                        path: 'game-events',
                        element: _jsx(LifecyclePage, {}),
                    },
                ],
            },
            // Add more sections as needed...
        ],
    },
    {
        path: '/auth',
        element: _jsx(AuthLayout, {}),
        errorElement: _jsx(NotFoundPage, {}),
        children: [
            {
                path: 'login',
                element: _jsx(LoginPage, {}),
            },
            {
                path: 'forgot-password',
                element: _jsx(ForgotPasswordPage, {}),
            },
            {
                path: 'reset-password',
                element: _jsx(ResetPasswordPage, {}),
            },
        ],
    },
    {
        path: '*',
        element: _jsx(NotFoundPage, {}),
    },
]);
