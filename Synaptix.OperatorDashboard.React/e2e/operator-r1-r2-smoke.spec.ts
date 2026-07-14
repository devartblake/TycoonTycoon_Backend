/**
 * Operator R1/R2 smoke: core modules load under mock mode.
 * Installer + diagnostics stay off by default and show unavailable copy.
 */
import { test, expect } from '@playwright/test'

async function loginWithMock(page: import('@playwright/test').Page) {
  await page.goto('/auth/login')
  const mockBtn = page.getByRole('button', { name: /mock mode/i })
  if (await mockBtn.isVisible().catch(() => false)) {
    await mockBtn.click()
  }
  await page.fill('input[type="email"]', 'admin@synaptix.com')
  await page.fill('input[type="password"]', 'password123')
  await page.click('button[type="submit"]')
  await page.waitForURL(/\/(dashboard)?/, { timeout: 15_000 })
}

test.describe('Operator R1/R2 smoke (mock)', () => {
  test.beforeEach(async ({ page }) => {
    await loginWithMock(page)
  })

  const modules: { path: string; heading?: RegExp }[] = [
    { path: '/dashboard', heading: /dashboard/i },
    { path: '/users', heading: /user/i },
    { path: '/moderation/logs', heading: /moderat/i },
    { path: '/audit/security', heading: /audit|security/i },
    { path: '/notifications', heading: /notif/i },
    { path: '/store/catalog', heading: /store|catalog|product/i },
    { path: '/economy/player', heading: /econom/i },
    { path: '/content/questions', heading: /question|content/i },
    { path: '/storage', heading: /storage/i },
    { path: '/personalization', heading: /personal|archetype/i },
  ]

  for (const mod of modules) {
    test(`loads ${mod.path}`, async ({ page }) => {
      await page.goto(mod.path)
      await expect(page.locator('body')).not.toContainText(/Unhandled Runtime Error/i)
      if (mod.heading) {
        await expect(page.locator('h1, h2').first()).toBeVisible({ timeout: 10_000 })
      }
    })
  }

  test('installer deep link shows unavailable when flag off', async ({ page }) => {
    await page.goto('/settings/setup')
    await expect(page.getByRole('heading', { name: /setup|installer disabled/i })).toBeVisible()
    await expect(page.getByText(/Setup CLI/i)).toBeVisible()
  })

  test('diagnostics deep link shows unavailable when flag off', async ({ page }) => {
    await page.goto('/diagnostics')
    await expect(page.getByRole('heading', { name: /diagnostics disabled/i })).toBeVisible()
    await expect(page.getByText(/health/i).first()).toBeVisible()
  })

  test('sidebar hides Setup and Diagnostics by default', async ({ page }) => {
    await page.goto('/dashboard')
    // Expand Configuration if present
    const configBtn = page.getByRole('button', { name: /configuration/i })
    if (await configBtn.isVisible().catch(() => false)) {
      await configBtn.click()
    }
    await expect(page.getByRole('link', { name: /^Setup$/i })).toHaveCount(0)
    await expect(page.getByRole('link', { name: /^Diagnostics$/i })).toHaveCount(0)
  })
})
