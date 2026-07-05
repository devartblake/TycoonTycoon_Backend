# E2E Tests - Synaptix Operator Dashboard

End-to-end testing suite for the React dashboard using Playwright.

## Test Coverage

### 1. Authentication (`auth.spec.ts`)
- ✅ Login with mock credentials
- ✅ Email validation
- ✅ Password validation
- ✅ Forgot password flow navigation

### 2. Dashboard Navigation (`dashboard.spec.ts`)
- ✅ Dashboard home page loads
- ✅ All major module navigation (Users, Store, Config, Anti-Cheat)
- ✅ Error boundary protection
- ✅ Loading skeletons display
- ✅ Browser history navigation (back/forward)

### 3. CRUD Operations (`crud-operations.spec.ts`)
- ✅ Store items display in tables
- ✅ Config settings load
- ✅ Notifications hub with tabs
- ✅ Empty states display
- ✅ Loading skeletons appear during fetch

### 4. Smoke Tests (`smoke-tests.spec.ts`)
- ✅ All 18+ modules load without errors
- ✅ No console errors on page load
- ✅ All major routes accessible

## Running Tests

```bash
# Run all tests
npm run test:e2e

# Run tests with UI (interactive)
npm run test:e2e:ui

# Run tests in headed mode (see browser)
npm run test:e2e:headed

# Debug mode
npm run test:e2e:debug

# Run specific test file
npx playwright test e2e/auth.spec.ts

# Run specific test
npx playwright test -g "should login"
```

## Configuration

- **Base URL**: http://localhost:5173
- **Browsers**: Chromium, Firefox
- **Retries**: 2 on CI, 0 locally
- **Workers**: 1 on CI, parallel locally
- **Screenshots**: Only on failure
- **Video/Trace**: On first retry

## Test Results

Reports are generated in `playwright-report/` after each run.

View reports:
```bash
npx playwright show-report
```

## CI/CD Integration

Add to GitHub Actions workflow:

```yaml
- name: Install dependencies
  run: npm ci

- name: Install Playwright browsers
  run: npx playwright install --with-deps

- name: Run E2E tests
  run: npm run test:e2e

- name: Upload report
  uses: actions/upload-artifact@v3
  if: always()
  with:
    name: playwright-report
    path: playwright-report/
```

## Best Practices

- ✅ Tests use mock mode for reliability
- ✅ Each test is independent
- ✅ Wait for navigation before assertions
- ✅ Verify both positive and negative flows
- ✅ Screenshot failures for debugging

## Coverage Summary

- **Auth Flows**: 4 tests
- **Dashboard**: 9 tests
- **CRUD Operations**: 6 tests
- **Smoke Tests**: 18 tests
- **Total**: 37+ test cases

**Status**: ✅ Ready for CI/CD integration
