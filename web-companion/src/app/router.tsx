/**
 * React Router v6 configuration
 * Mirrors GoRouter setup from Flutter
 */

import { createBrowserRouter } from 'react-router-dom';

// Placeholder pages (to be implemented)
const NotFoundPage = () => <div className="p-4">404 - Page Not Found</div>;
const DashboardPage = () => <div className="p-4">Dashboard (TODO)</div>;
const LoginPage = () => <div className="p-4">Login (TODO)</div>;

const router = createBrowserRouter([
  {
    path: '/',
    element: <DashboardPage />, // TODO: Replace with AppShell layout
    errorElement: <NotFoundPage />,
    children: [
      // Home
      { index: true, element: <DashboardPage /> },

      // Auth routes
      { path: 'login', element: <LoginPage /> },
      { path: 'signup', element: <div>Signup (TODO)</div> },

      // Quiz routes
      { path: 'play', element: <div>Quiz Lobby (TODO)</div> },
      { path: 'play/:sessionId', element: <div>Quiz Session (TODO)</div> },

      // Skill tree routes
      { path: 'skills', element: <div>Skill Tree Hub (TODO)</div> },
      { path: 'skills/:branchId', element: <div>Skill Branch Detail (TODO)</div> },
      { path: 'skills/planner', element: <div>Build Planner (TODO)</div> },

      // Leaderboard routes
      { path: 'leaderboard', element: <div>Leaderboard (TODO)</div> },
      { path: 'leaderboard/:tier', element: <div>Tier Leaderboard (TODO)</div> },

      // Profile routes
      { path: 'profile', element: <div>My Profile (TODO)</div> },
      { path: 'profile/:userId', element: <div>Public Profile (TODO)</div> },
      { path: 'profile/knowledge-graph', element: <div>Knowledge Graph (TODO)</div> },

      // Store
      { path: 'store', element: <div>Store (TODO)</div> },

      // Missions
      { path: 'missions', element: <div>Missions (TODO)</div> },

      // Social routes
      { path: 'friends', element: <div>Friends List (TODO)</div> },
      { path: 'messages', element: <div>Messages List (TODO)</div> },
      { path: 'messages/:threadId', element: <div>Message Thread (TODO)</div> },
      { path: 'challenges', element: <div>Challenges List (TODO)</div> },

      // Web-exclusive routes
      { path: 'leagues', element: <div>Leagues Hub (TODO)</div> },
      { path: 'leagues/:seasonId', element: <div>Season Division (TODO)</div> },
      { path: 'study', element: <div>Study Mode (TODO)</div> },

      // Settings
      { path: 'settings', element: <div>Settings (TODO)</div> },

      // Catch-all
      { path: '*', element: <NotFoundPage /> },
    ],
  },
]);

export default router;
