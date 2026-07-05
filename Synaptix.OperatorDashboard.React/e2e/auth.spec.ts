import { test, expect } from '@playwright/test'

test.describe('Authentication Flow', () => {
  test('should login with mock credentials', async ({ page }) => {
    await page.goto('/auth/login')

    // Verify login page loads
    await expect(page).toHaveTitle(/Login|Sign in/)
    await expect(page.locator('h2')).toContainText('Sign in')

    // Check mock mode banner exists
    await expect(page.locator('text=Mock mode')).toBeVisible()

    // Enable mock mode
    await page.click('button:has-text("Enable Mock Mode")')
    await page.waitForURL('/auth/login')

    // Enter credentials
    await page.fill('input[type="email"]', 'admin@synaptix.com')
    await page.fill('input[type="password"]', 'password123')

    // Submit form
    await page.click('button[type="submit"]')

    // Should redirect to dashboard
    await page.waitForURL('/dashboard')
    await expect(page.locator('h1')).toContainText('Dashboard')
  })

  test('should show validation errors on invalid email', async ({ page }) => {
    await page.goto('/auth/login')

    await page.fill('input[type="email"]', 'invalid-email')
    await page.fill('input[type="password"]', 'password123')
    await page.click('button[type="submit"]')

    // Validation error should appear
    await expect(page.locator('text=Invalid email')).toBeVisible()
  })

  test('should show validation errors on short password', async ({ page }) => {
    await page.goto('/auth/login')

    await page.fill('input[type="email"]', 'admin@synaptix.com')
    await page.fill('input[type="password"]', 'short')
    await page.click('button[type="submit"]')

    // Validation error should appear
    await expect(page.locator('text=at least 6 characters')).toBeVisible()
  })

  test('should display forgot password link', async ({ page }) => {
    await page.goto('/auth/login')

    const forgotLink = page.locator('a:has-text("Forgot your password")')
    await expect(forgotLink).toBeVisible()
    await forgotLink.click()

    await page.waitForURL('/auth/forgot-password')
    await expect(page.locator('h2')).toContainText('Reset your password')
  })
})
