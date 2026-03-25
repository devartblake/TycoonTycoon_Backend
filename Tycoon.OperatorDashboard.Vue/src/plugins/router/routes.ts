export const routes = [
  { path: '/', redirect: '/dashboard' },
  {
    path: '/',
    component: () => import('@/layouts/default.vue'),
    children: [
      {
        path: 'dashboard',
        component: () => import('@/pages/dashboard.vue'),
      },
      {
        path: 'users',
        component: () => import('@/pages/users.vue'),
      },
      {
        path: 'season-points',
        component: () => import('@/pages/season-points.vue'),
      },
      {
        path: 'anti-cheat',
        component: () => import('@/pages/anti-cheat.vue'),
      },
      {
        path: 'escalations',
        component: () => import('@/pages/escalations.vue'),
      },
      {
        path: 'economy',
        component: () => import('@/pages/economy.vue'),
      },
      {
        path: 'player-transactions',
        component: () => import('@/pages/player-transactions.vue'),
      },
      {
        path: 'notifications',
        component: () => import('@/pages/notifications.vue'),
      },
      {
        path: 'security',
        component: () => import('@/pages/security.vue'),
      },
    ],
  },
  {
    path: '/',
    component: () => import('@/layouts/blank.vue'),
    children: [
      {
        path: 'login',
        component: () => import('@/pages/login.vue'),
      },
      {
        path: '/:pathMatch(.*)*',
        component: () => import('@/pages/[...error].vue'),
      },
    ],
  },
]
