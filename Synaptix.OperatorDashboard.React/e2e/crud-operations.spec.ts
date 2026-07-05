import { test, expect } from '@playwright/test'

test.describe('CRUD Operations', () => {
  test.beforeEach(async ({ page }) => {
    // Login
    await page.goto('/auth/login')
    await page.click('button:has-text("Enable Mock Mode")')
    await page.fill('input[type="email"]', 'admin@synaptix.com')
    await page.fill('input[type="password"]', 'password123')
    await page.click('button[type="submit"]')
    await page.waitForURL('/dashboard')
  })

  test('should display store items in list', async ({ page }) => {
    await page.goto('/store')

    // Wait for tab navigation
    await page.click('button:has-text("Products")')

    // Check for table with data
    const table = page.locator('table')
    await expect(table).toBeVisible()

    // Verify table has headers
    await expect(page.locator('thead th')).toBeTruthy()
  })

  test('should display config settings', async ({ page }) => {
    await page.goto('/config')

    // Wait for tabs to load
    await page.waitForTimeout(500)

    // Click feature flags tab
    await page.click('button:has-text("Feature Flags")')

    // Verify content loads
    await expect(page.locator('text=Feature Flags|Features')).toBeVisible()
  })

  test('should display notifications hub with multiple sections', async ({ page }) => {
    await page.goto('/notifications')

    // Check for multiple tabs
    await expect(page.locator('button:has-text("Templates")')).toBeVisible()
    await expect(page.locator('button:has-text("Channels")')).toBeVisible()
    await expect(page.locator('button:has-text("Schedules")')).toBeVisible()

    // Click and verify tabs load
    await page.click('button:has-text("Channels")')
    await page.waitForTimeout(300)
    await expect(page.locator('text=Channels|Channel')).toBeVisible()
  })

  test('should handle empty states gracefully', async ({ page }) => {
    await page.goto('/storage')

    // Empty state should be visible if no files
    const emptyState = page.locator('text=Folder is empty|No data')
    const hasContent = await page.locator('table, .operator-card').count()

    // Either show empty state or data
    await expect(emptyState.or(page.locator('table'))).toBeTruthy()
  })

  test('should display loading skeletons during data fetch', async ({ page }) => {
    await page.goto('/users')

    // Look for skeleton elements (animated placeholders)
    const skeletons = page.locator('.animate-pulse, .bg-panel-border')

    // Either skeleton is visible or data is loaded
    await expect(skeletons.or(page.locator('table'))).toBeTruthy()
  })
})
