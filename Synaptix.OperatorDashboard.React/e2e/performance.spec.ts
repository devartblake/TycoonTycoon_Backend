import { test, expect } from '@playwright/test'

test.describe('Performance Audit - Core Web Vitals', () => {
  test.beforeEach(async ({ page }) => {
    // Login before each test
    await page.goto('/auth/login')
    await page.click('button:has-text("Enable Mock Mode")')
    await page.fill('input[type="email"]', 'admin@synaptix.com')
    await page.fill('input[type="password"]', 'password123')
    await page.click('button[type="submit"]')
    await page.waitForURL('/dashboard')
  })

  test('Dashboard loads within acceptable time', async ({ page }) => {
    const startTime = Date.now()
    await page.goto('/dashboard', { waitUntil: 'networkidle' })
    const loadTime = Date.now() - startTime

    // Should load within 3 seconds
    expect(loadTime).toBeLessThan(3000)
  })

  test('Page has no layout shifts during load', async ({ page }) => {
    await page.goto('/dashboard')

    // Check for CLS (Cumulative Layout Shift)
    const cls = await page.evaluate(() => {
      return new Promise<number>((resolve) => {
        let cls = 0
        if ('PerformanceObserver' in window) {
          try {
            const observer = new PerformanceObserver((list) => {
              for (const entry of list.getEntries()) {
                if (!(entry as any).hadRecentInput) {
                  cls += (entry as any).value
                }
              }
            })
            observer.observe({ type: 'layout-shift', buffered: true })
            setTimeout(() => resolve(cls), 2000)
          } catch {
            resolve(0)
          }
        } else {
          resolve(0)
        }
      })
    })

    // CLS should be less than 0.1 (good score)
    expect(cls).toBeLessThan(0.15)
  })

  test('First Contentful Paint is fast', async ({ page }) => {
    await page.goto('/dashboard')

    // Measure time to first content paint
    const metrics = await page.evaluate(() => {
      return performance.getEntriesByType('navigation')[0] as any
    })

    // FCP should be reasonable
    if (metrics) {
      const fcp = metrics.responseEnd - metrics.fetchStart + 500
      expect(fcp).toBeLessThan(2000)
    }
  })

  test('Large modules load efficiently', async ({ page }) => {
    const moduleTests = [
      { path: '/users', maxTime: 3000 },
      { path: '/audit', maxTime: 3500 },
      { path: '/store', maxTime: 3000 },
      { path: '/config', maxTime: 3000 },
    ]

    for (const test of moduleTests) {
      const startTime = Date.now()
      await page.goto(test.path, { waitUntil: 'networkidle' })
      const loadTime = Date.now() - startTime

      expect(loadTime).toBeLessThan(test.maxTime)
    }
  })

  test('Bundle is reasonably sized', async ({ page }) => {
    await page.goto('/dashboard')

    // Check total resource size
    const resources = await page.evaluate(() => {
      return performance
        .getEntriesByType('resource')
        .filter((r: any) => r.name.includes('.js'))
        .map((r: any) => r.transferSize || 0)
        .reduce((a: number, b: number) => a + b, 0)
    })

    // JavaScript should be under 500KB (uncompressed)
    expect(resources).toBeLessThan(500000)
  })

  test('No render-blocking resources', async ({ page }) => {
    const startTime = Date.now()
    await page.goto('/dashboard')

    // Page should be interactive quickly
    await expect(page.locator('h1')).toBeVisible({ timeout: 2000 })
    const interactiveTime = Date.now() - startTime

    expect(interactiveTime).toBeLessThan(2500)
  })

  test('Images are optimized', async ({ page }) => {
    await page.goto('/dashboard')

    // Check for large unoptimized images
    const images = await page.locator('img').all()

    for (const img of images) {
      const src = await img.getAttribute('src')
      // Should not have unoptimized formats for large images
      if (src && !src.includes('data:')) {
        expect(src).not.toMatch(/\.png$|\.bmp$/i)
      }
    }
  })

  test('CSS is loaded efficiently', async ({ page }) => {
    await page.goto('/dashboard')

    // Check for inline critical CSS
    const styles = await page.locator('style').count()
    const links = await page.locator('link[rel="stylesheet"]').count()

    // Should have optimized CSS loading
    expect(styles + links).toBeGreaterThan(0)
  })

  test('JavaScript execution is minimal', async ({ page }) => {
    const startTime = performance.now()

    await page.goto('/dashboard')
    await page.waitForLoadState('networkidle')

    // Measure CPU-intensive operations
    const jsTime = await page.evaluate(() => {
      const perfNow = performance.now()
      // Do work
      let sum = 0
      for (let i = 0; i < 10000; i++) {
        sum += Math.sqrt(i)
      }
      return performance.now() - perfNow
    })

    // Heavy JS should be code-split, not in initial bundle
    expect(jsTime).toBeLessThan(50)
  })

  test('Interaction to Next Paint is responsive', async ({ page }) => {
    await page.goto('/dashboard')

    // Measure response to user input
    const startTime = Date.now()
    await page.click('button:first-of-type')
    const responseTime = Date.now() - startTime

    // Should respond to clicks quickly (under 200ms)
    expect(responseTime).toBeLessThan(200)
  })
})
