import type { App } from 'vue'
import { createRouter, createWebHistory } from 'vue-router'
import { isAuthenticated, fetchProfile } from '@/lib/auth'
import { routes } from './routes'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
})

router.beforeEach(async (to) => {
  const publicPages = ['/login']

  if (publicPages.includes(to.path)) return true

  if (!isAuthenticated()) {
    return '/login'
  }

  // Validate session on first navigation
  try {
    const profile = await fetchProfile()

    if (!profile) return '/login'
  }
  catch {
    return '/login'
  }

  return true
})

export default function (app: App) {
  app.use(router)
}

export { router }
