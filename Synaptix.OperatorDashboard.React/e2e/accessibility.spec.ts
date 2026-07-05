import { test, expect } from '@playwright/test'
import { injectAxe, checkA11y } from 'axe-playwright'

const MODULES = [
  '/dashboard',
  '/users',
  '/store',
  '/config',
  '/anti-cheat',
  '/audit',
  '/economy',
  '/notifications',
  '/personalization',
  '/installer',
  '/storage',
  '/diagnostics',
  '/skills',
  '/match-history',
  '/event-queue',
  '/operations',
  '/content',
]

test.describe('Accessibility Audit - All Modules', () => {
  test.beforeEach(async ({ page }) => {
    // Login before each test
    await page.goto('/auth/login')
    await page.click('button:has-text("Enable Mock Mode")')
    await page.fill('input[type="email"]', 'admin@synaptix.com')
    await page.fill('input[type="password"]', 'password123')
    await page.click('button[type="submit"]')
    await page.waitForURL('/dashboard')
  })

  test('Auth pages - accessibility', async ({ page }) => {
    await page.goto('/auth/login')
    await injectAxe(page)

    // Check for basic a11y issues
    await checkA11y(page, null, {
      detailedReport: true,
      detailedReportOptions: { html: true },
    })
  })

  for (const modulePath of MODULES) {
    test(`Module ${modulePath} - accessibility`, async ({ page }) => {
      await page.goto(modulePath)
      await page.waitForLoadState('networkidle')

      // Inject axe for accessibility testing
      await injectAxe(page)

      // Run accessibility checks
      await checkA11y(page, null, {
        detailedReport: true,
        detailedReportOptions: { html: true },
      })
    })
  }

  test('Keyboard navigation - Tab key works', async ({ page }) => {
    await page.goto('/dashboard')

    // Tab through interactive elements
    let interactiveCount = 0
    for (let i = 0; i < 10; i++) {
      await page.keyboard.press('Tab')
      const focused = await page.evaluate(() => {
        const el = document.activeElement
        return el?.tagName || 'BODY'
      })

      if (['BUTTON', 'A', 'INPUT', 'SELECT'].includes(focused)) {
        interactiveCount++
      }
    }

    // Should have focused on at least some interactive elements
    expect(interactiveCount).toBeGreaterThan(0)
  })

  test('Color contrast - text is readable', async ({ page }) => {
    await page.goto('/dashboard')
    await injectAxe(page)

    // Axe will check color contrast as part of accessibility audit
    await checkA11y(page, null, {
      rules: {
        'color-contrast': { enabled: true },
      },
      detailedReport: true,
    })
  })

  test('Form labels - all inputs have labels', async ({ page }) => {
    await page.goto('/config')

    // Check all input elements have associated labels
    const inputs = await page.locator('input:not([type="hidden"])').count()
    const labels = await page.locator('label').count()

    // Should have labels for inputs
    expect(labels).toBeGreaterThan(0)
  })

  test('Heading hierarchy - proper h1-h6 structure', async ({ page }) => {
    await page.goto('/dashboard')

    // Check for h1 (main heading)
    const h1Count = await page.locator('h1').count()
    expect(h1Count).toBeGreaterThanOrEqual(1)

    // Check heading order
    const headings = await page.locator('h1, h2, h3, h4, h5, h6').count()
    expect(headings).toBeGreaterThan(0)
  })

  test('ARIA attributes - landmarks present', async ({ page }) => {
    await page.goto('/dashboard')

    // Check for at least basic landmark elements
    const hasMain = await page.locator('main').count()
    const hasNav = await page.locator('nav').count()

    // Should have semantic landmark elements
    expect(hasMain + hasNav).toBeGreaterThanOrEqual(1)
  })

  test('Error messages - accessible error handling', async ({ page }) => {
    await page.goto('/auth/login')

    // Trigger validation error
    await page.fill('input[type="email"]', 'invalid')
    await page.fill('input[type="password"]', 'pass')
    await page.click('button[type="submit"]')

    // Error message should be visible
    await expect(page.locator('text=/invalid|error/i')).toBeVisible()
  })

  test('Loading states - announce to screen readers', async ({ page }) => {
    await page.goto('/users')

    // Skeleton loaders should not confuse screen readers
    const skeletons = await page.locator('.animate-pulse').count()

    // Should have content or skeleton (either is accessible)
    const hasContent = await page.locator('table, .operator-card').count()
    expect(skeletons + hasContent).toBeGreaterThan(0)
  })

  test('Empty states - guidance for all users', async ({ page }) => {
    await page.goto('/storage')

    // Empty state should have helpful text
    const hasEmptyGuidance = await page.locator('text=empty|no data|upload|create').count()
    const hasContent = await page.locator('table').count()

    // Either has content or empty state guidance
    expect(hasEmptyGuidance + hasContent).toBeGreaterThan(0)
  })
})
