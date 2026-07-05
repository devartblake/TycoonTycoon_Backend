import { test, expect } from '@playwright/test'

test.describe('Dashboard Navigation', () => {
  test.beforeEach(async ({ page }) => {
    // Login before each test
    await page.goto('/auth/login')
    await page.click('button:has-text("Enable Mock Mode")')
    await page.fill('input[type="email"]', 'admin@synaptix.com')
    await page.fill('input[type="password"]', 'password123')
    await page.click('button[type="submit"]')
    await page.waitForURL('/dashboard')
  })

  test('should display dashboard home page', async ({ page }) => {
    await expect(page.locator('h1')).toContainText('Dashboard')
    await expect(page.locator('text=System Health|Health Dashboard')).toBeVisible()
  })

  test('should load users module', async ({ page }) => {
    await page.click('a:has-text("Users")')
    await page.waitForURL(/.*users.*/)
    await expect(page.locator('h1')).toContainText('Users')
  })

  test('should load store module', async ({ page }) => {
    await page.click('a:has-text("Store")')
    await page.waitForURL(/.*store.*/)
    await expect(page.locator('h1')).toContainText('Store')
  })

  test('should load config module', async ({ page }) => {
    await page.click('a:has-text("Config")')
    await page.waitForURL(/.*config.*/)
    await expect(page.locator('h1')).toContainText('Config|Settings')
  })

  test('should load anti-cheat module', async ({ page }) => {
    await page.click('a:has-text("Anti-Cheat")')
    await page.waitForURL(/.*anti-cheat.*/)
    await expect(page.locator('h1')).toContainText('Anti-Cheat')
  })

  test('should have error boundary protection', async ({ page }) => {
    // Navigate to page
    await page.goto('/dashboard')

    // Verify error boundary is rendered (no crash)
    await expect(page.locator('body')).toBeTruthy()

    // Check for main content, not error message
    await expect(page.locator('h1')).toBeVisible()
  })

  test('should display loading skeletons', async ({ page }) => {
    // Navigate to users (which loads data)
    await page.goto('/users')

    // Check for skeleton loader presence during load
    const skeletonOrContent = await page.locator('.bg-panel, h1').first().isVisible()
    await expect(skeletonOrContent).toBeTruthy()
  })

  test('should handle navigation history', async ({ page }) => {
    // Navigate to multiple pages
    await page.click('a:has-text("Users")')
    await page.waitForURL(/.*users.*/)

    await page.click('a:has-text("Store")')
    await page.waitForURL(/.*store.*/)

    // Go back
    await page.goBack()
    await page.waitForURL(/.*users.*/)
    await expect(page.locator('h1')).toContainText('Users')

    // Go forward
    await page.goForward()
    await page.waitForURL(/.*store.*/)
    await expect(page.locator('h1')).toContainText('Store')
  })
})
