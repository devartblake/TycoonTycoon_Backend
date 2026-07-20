/**
 * React Router v6 configuration
 * Mirrors GoRouter setup from Flutter
 */

import { createBrowserRouter } from 'react-router-dom';
import ProtectedRoute from '@components/layout/ProtectedRoute';
import AppShell from '@components/layout/AppShell';
import { LoginPage } from '@features/auth/pages/LoginPage';
import { SignupPage } from '@features/auth/pages/SignupPage';
import { ForgotPasswordPage } from '@features/auth/pages/ForgotPasswordPage';
import { DashboardPage } from '@features/dashboard/pages/DashboardPage';
import { SettingsPage } from '@features/dashboard/pages/SettingsPage';
import { NotFoundPage } from '@features/dashboard/pages/NotFoundPage';
import { QuizLobbyPage } from '@features/quiz/pages/QuizLobbyPage';
import { QuizSessionScreen } from '@features/quiz/pages/QuizSessionScreen';
import { QuizResultsScreen } from '@features/quiz/pages/QuizResultsScreen';
import { SkillTreePage } from '@features/skill-tree/pages/SkillTreePage';
import { LeaderboardPage } from '@features/leaderboard/pages/LeaderboardPage';
import { ProfilePage } from '@features/profile/pages/ProfilePage';
import { FriendsPage } from '@features/social/pages/FriendsPage';
import { StorePage } from '@features/store/pages/StorePage';
import { CheckoutResultPage } from '@features/store/pages/CheckoutResultPage';
import { MissionsPage } from '@features/missions/pages/MissionsPage';
import { StudyPage } from '@features/study/pages/StudyPage';

const router = createBrowserRouter([
  {
    path: '/',
    errorElement: <NotFoundPage />,
    children: [
      // Public routes
      { path: 'login', element: <LoginPage /> },
      { path: 'signup', element: <SignupPage /> },
      { path: 'forgot-password', element: <ForgotPasswordPage /> },

      // Protected routes with app shell
      {
        element: <ProtectedRoute />,
        children: [
          {
            element: <AppShell />,
            children: [
              // Home
              { index: true, element: <DashboardPage /> },

              // Quiz routes
              { path: 'play', element: <QuizLobbyPage /> },
              { path: 'quiz/session', element: <QuizSessionScreen /> },
              { path: 'quiz/results/:sessionId', element: <QuizResultsScreen /> },

              // Skill tree routes
              { path: 'skills', element: <SkillTreePage /> },
              { path: 'skills/:branchId', element: <div className="p-8">Skill Branch Detail (Phase 3)</div> },
              { path: 'skills/planner', element: <div className="p-8">Build Planner (Phase 3)</div> },

              // Leaderboard routes
              { path: 'leaderboard', element: <LeaderboardPage /> },
              { path: 'leaderboard/:tier', element: <div className="p-8">Tier Leaderboard (Phase 2)</div> },

              // Profile routes
              { path: 'profile', element: <ProfilePage /> },
              { path: 'profile/:userId', element: <div className="p-8">Public Profile (Phase 2)</div> },
              { path: 'profile/knowledge-graph', element: <div className="p-8">Knowledge Graph (Phase 6)</div> },

              // Store
              { path: 'store', element: <StorePage /> },
              { path: 'store/checkout/success', element: <CheckoutResultPage status="success" /> },
              { path: 'store/checkout/cancelled', element: <CheckoutResultPage status="cancelled" /> },

              // Missions
              { path: 'missions', element: <MissionsPage /> },

              // Social routes
              { path: 'friends', element: <FriendsPage /> },
              { path: 'messages', element: <div className="p-8">Messages List (Phase 5)</div> },
              { path: 'messages/:threadId', element: <div className="p-8">Message Thread (Phase 5)</div> },
              { path: 'challenges', element: <div className="p-8">Challenges List (Phase 5)</div> },

              // Web-exclusive routes
              { path: 'leagues', element: <div className="p-8">Leagues Hub (Phase 6)</div> },
              { path: 'leagues/:seasonId', element: <div className="p-8">Season Division (Phase 6)</div> },
              { path: 'study', element: <StudyPage /> },

              // Settings
              { path: 'settings', element: <SettingsPage /> },

              // Catch-all
              { path: '*', element: <NotFoundPage /> },
            ],
          },
        ],
      },
    ],
  },
]);

export default router;
