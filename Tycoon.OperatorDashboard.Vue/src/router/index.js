import { createRouter, createWebHistory } from 'vue-router'
import DashboardView from '../views/DashboardView.vue'
import AuditLogView from '../views/AuditLogView.vue'
import UsersView from '../views/UsersView.vue'
import { canViewRoute } from '../lib/permissions'

const routes = [
  { path: '/', redirect: '/dashboard' },
  { path: '/dashboard', component: DashboardView, meta: { requiredPermission: 'dashboard:read' } },
  { path: '/audit-log', component: AuditLogView, meta: { requiredPermission: 'audit:read' } },
  { path: '/users', component: UsersView, meta: { requiredPermission: 'users:read' } }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

router.beforeEach((to) => {
  // TODO: replace with real auth/session sourced permissions.
  const permissionSet = ['dashboard:read', 'audit:read', 'users:read']
  if (canViewRoute(to, permissionSet)) return true
  return '/dashboard'
})

export default router
