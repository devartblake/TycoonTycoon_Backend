import { test, expect } from '@playwright/test'

test.describe('Smoke Tests - All Modules Load', () => {
  test.beforeEach(async ({ page }) => {
    // Login before each test
    await page.goto('/auth/login')
    await page.click('button:has-text("Enable Mock Mode")')
    await page.fill('input[type="email"]', 'admin@synaptix.com')
    await page.fill('input[type="password"]', 'password123')
    await page.click('button[type="submit"]')
    await page.waitForURL('/dashboard')
  })

  test('Dashboard loads without errors', async ({ page }) => {
    await page.goto('/dashboard')
    await expect(page.locator('h1')).toContainText('Dashboard')
    await expect(page).not.toHaveTitle(/error/i)
  })

  test('Users module loads without errors', async ({ page }) => {
    await page.goto('/users')
    await expect(page.locator('h1')).toBeVisible()
    await expect(page).not.toHaveTitle(/error/i)
  })

  test('Store module loads without errors', async ({ page }) => {
    await page.goto('/store')
    await expect(page.locator('h1')).toBeVisible()
    await expect(page).not.toHaveTitle(/error/i)
  })

  test('Config module loads without errors', async ({ page }) => {
    await page.goto('/config')
    await expect(page.locator('h1')).toBeVisible()
    await expect(page).not.toHaveTitle(/error/i)
  })

  test('Anti-Cheat module loads without errors', async ({ page }) => {
    await page.goto('/anti-cheat')
    await expect(page.locator('h1')).toBeVisible()
    await expect(page).not.toHaveTitle(/error/i)
  })

  test('Security Audit module loads without errors', async ({ page }) => {
    await page.goto('/audit')
    await expect(page.locator('h1')).toBeVisible()
    await expect(page).not.toHaveTitle(/error/i)
  })

  test('Economy module loads without errors', async ({ page }) => {
    await page.goto('/economy')
    await expect(page.locator('h1')).toBeVisible()
    await expect(page).not.toHaveTitle(/error/i)
  })

  test('Notifications module loads without errors', async ({ page }) => {
    await page.goto('/notifications')
    await expect(page.locator('h1')).toBeVisible()
    await expect(page).not.toHaveTitle(/error/i)
  })

  test('Personalization module loads without errors', async ({ page }) => {
    await page.goto('/personalization')
    await expect(page.locator('h1')).toBeVisible()
    await expect(page).not.toHaveTitle(/error/i)
  })

  test('Installer deep link shows unavailable when feature flag off', async ({ page }) => {
    await page.goto('/settings/setup')
    await expect(page.getByRole('heading', { name: /setup|installer disabled/i })).toBeVisible()
  })

  test('Storage module loads without errors', async ({ page }) => {
    await page.goto('/storage')
    await expect(page.locator('h1')).toBeVisible()
    await expect(page).not.toHaveTitle(/error/i)
  })

  test('Diagnostics deep link shows unavailable when feature flag off', async ({ page }) => {
    await page.goto('/diagnostics')
    await expect(page.getByRole('heading', { name: /diagnostics disabled/i })).toBeVisible()
  })

  test.skip('Diagnostics module full UI (requires VITE_ENABLE_DIAGNOSTICS)', async ({ page }) => {
    await page.goto('/diagnostics')
    await expect(page.locator('h1')).toBeVisible()
    await expect(page).not.toHaveTitle(/error/i)
  })

  test('Skills module loads without errors', async ({ page }) => {
    await page.goto('/skills')
    await expect(page.locator('h1')).toBeVisible()
    await expect(page).not.toHaveTitle(/error/i)
  })

  test('Match History module loads without errors', async ({ page }) => {
    await page.goto('/match-history')
    await expect(page.locator('h1')).toBeVisible()
    await expect(page).not.toHaveTitle(/error/i)
  })

  test('Event Queue module loads without errors', async ({ page }) => {
    await page.goto('/event-queue')
    await expect(page.locator('h1')).toBeVisible()
    await expect(page).not.toHaveTitle(/error/i)
  })

  test('Operations module loads without errors', async ({ page }) => {
    await page.goto('/operations')
    await expect(page.locator('h1')).toBeVisible()
    await expect(page).not.toHaveTitle(/error/i)
  })

  test('Content/Questions module loads without errors', async ({ page }) => {
    await page.goto('/content')
    await expect(page.locator('h1')).toBeVisible()
    await expect(page).not.toHaveTitle(/error/i)
  })
})
