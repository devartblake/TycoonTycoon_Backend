# React Dashboard — Quick Start Testing Guide

**For**: React team members starting Phase 1  
**Time to first screen**: 5 minutes  
**Complexity**: Beginner

---

## 1-Minute Setup

### Prerequisites
- Node.js 20+ installed (`node --version`)
- Git installed
- Code editor (VS Code recommended)

### Clone & Install

```bash
# Navigate to backend directory
cd C:\Users\lmxbl\Documents\TycoonTycoon_Backend

# Enter React project
cd Synaptix.OperatorDashboard.React

# Install dependencies (if not already done)
npm install

# Start dev server
npm run dev
```

**Expected output:**
```
VITE v5.0.0  ready in 123 ms

➜  Local:   http://localhost:5173/
➜  press h to show help
```

### Open in Browser
- Navigate to: `http://localhost:5173`
- You should see the login page
- Open DevTools: `F12` or `Ctrl+Shift+I`

---

## 2. First Test: Authentication

### Test Login Form (1 min)

1. **Form Validation**:
   - Leave email empty, click Login → should show error
   - Type invalid email, click Login → should show error format
   - Clear and test again → should clear error

2. **Visual Check**:
   - Does form look professional?
   - Are error messages clear?
   - Is text readable?

3. **Console Check**:
   - Open DevTools → Console tab
   - **Look for**: Red errors? Yellow warnings?
   - **If errors**: Screenshot and log them

### Try to Login (1-2 min)
- Use test credentials (ask team lead if unsure)
- **Expected**: Redirect to /dashboard
- **If fails**: Check Network tab for 401/403/500 errors

### Test Error Scenario (1 min)
- Try wrong password → should show error
- Don't re-submit 10 times (avoid lockout)

**✅ If all pass**: Auth is working, move to navigation test

---

## 3. Navigation Test (2-3 min)

### Sidebar Navigation

1. Look at left sidebar
2. Click each menu item:
   - Dashboard ✓
   - Users → users/
   - Notifications → notifications/
   - Audit → audit/security
   - Anti-Cheat → anti-cheat/
   - Moderation → moderation/ (or similar)
   - Economy → economy/
   - Content → content/
   - Store → store/
   - Operations → operations/

### Each Click Should:
- Load a new page (URL changes)
- **Not** show "Coming Soon" (if it does, note it)
- **Not** show blank page
- **Not** show console errors (check DevTools)

### Document Findings

**Template:**
```
Navigation Tests:
✅ Dashboard loads
✅ Users page loads
✅ Notifications page loads
⚠️ Moderation shows "Coming Soon"
❌ Economy page shows 404 error
```

---

## 4. Feature Spot-Check (5-10 min per feature)

### Users Triage Page (`/users`)

**Checklist:**
```
□ Page loads (no 404 errors)
□ Table visible
□ Column headers present
□ Data rows present (or "No users" message)
□ Filter dropdown works (click → options appear)
□ Bulk select checkbox appears
□ Sidebar highlights "Users"
□ Mobile check: Can you scroll horizontally? (Should NOT need to)
```

**If table shows data:**
- Click column header → sort arrow appears?
- Click filter → dropdown opens?
- Select row → highlight changes color?

**Log failures:**
- Screenshot of issue
- Note exact error from DevTools Console
- Include URL

### Notifications Page (`/notifications`)

**Checklist:**
```
□ Page loads
□ Multiple tabs visible (Templates, Channels, Schedules, Dead Letter)
□ Click each tab → content changes
□ Table within each tab loads
□ Tab doesn't refresh page (observe URL)
```

### Security Audit Page (`/audit/security`)

**Checklist:**
```
□ Page loads
□ Table visible
□ Leaflet map loads (look for gray background with zoom controls)
□ Map shows IP points
□ Filter bar present
□ When you filter → both table AND map update
□ No console errors related to Leaflet
```

**If map doesn't load:**
- Check DevTools Console: "Leaflet not defined"?
- Check Network tab: Failed requests?

---

## 5. Error Scenario Testing (2-3 min)

### Simulate Network Error

1. Open DevTools → Network tab
2. Click the throttle dropdown (top-right)
3. Select "Offline"
4. Try to navigate to Users page
5. **Expected**: Error message or loading spinner with retry button
6. Set back to "Online"

### Simulate 500 Error

1. Open DevTools → Console tab (not Network)
2. Paste this:
   ```javascript
   // Mock a 500 error on next request
   sessionStorage.setItem('mockError', '500');
   location.reload();
   ```
3. Try to load a feature
4. **Expected**: Error message, not crash
5. Clear it:
   ```javascript
   sessionStorage.removeItem('mockError');
   location.reload();
   ```

---

## 6. Performance Check (2 min)

### Lighthouse Audit

1. DevTools → Lighthouse tab
2. Click "Analyze page load"
3. Wait for results (30 seconds)
4. **Look for:**
   - Performance score ≥80 ✅
   - Accessibility score ≥80 ✅
   - Best Practices score ≥80 ✅

**If scores low:**
- Click the issue → read suggestion
- Log it: "Lighthouse: Performance 65/100 - Large images not optimized"

### Manual Performance Check

1. DevTools → Network tab
2. Hard refresh: `Ctrl+Shift+R`
3. **Watch for:**
   - Page load time (target: <3s)
   - Total data size (target: <2MB)
   - Failed requests (target: 0)

---

## 7. Responsive Design Check (3-5 min)

### Mobile View

1. DevTools → Click device toggle (or `Ctrl+Shift+M`)
2. Select "iPhone 12" or similar
3. Navigate through pages
4. **Check:**
   - Sidebar collapses? (Menu icon appears)
   - Text readable (not tiny)?
   - No horizontal scroll?
   - Buttons clickable (not too small)?

### Tablet View

1. Select "iPad" or manually set 768px width
2. Same checks as mobile

### Return to Desktop
- Close device mode: `Ctrl+Shift+M` again

---

## 8. Code Quality Checks (2-3 min)

### TypeScript Check

```bash
npm run type-check
```

**Expected**: "✓ All good!"  
**If errors**:
```
src/features/users/pages/list.tsx:15 - error TS2345: Argument of type 'string'
is not assignable to parameter of type 'number'.
```

Screenshot and log it.

### Linting Check

```bash
npm run lint
```

**Expected**: No errors or minimal warnings  
**If issues**: Each line shows violation, fix them

---

## 9. Common Issues & Fixes

### Issue: "Cannot find module"

**Cause**: Cached imports, stale cache  
**Fix**:
```bash
# Clear cache and reinstall
rm -r node_modules package-lock.json
npm install
npm run dev
```

### Issue: Port 5173 already in use

**Cause**: Another process using it  
**Fix**:
```bash
# Kill the process
lsof -i :5173  # Find PID
kill -9 <PID>
# Or just use different port
npm run dev -- --port 5174
```

### Issue: "Cannot read property 'x' of undefined"

**Cause**: API not returning expected data  
**Check**:
1. Network tab: API request succeeded?
2. Response payload: Has the field?
3. Add console.log to debug

### Issue: Map doesn't render

**Cause**: Leaflet not loading  
**Check**:
```bash
# Verify Leaflet installed
npm list react-leaflet
# Should show react-leaflet@4.2.0

# Clear cache
rm -r node_modules/.vite
npm run dev
```

### Issue: Form submission does nothing

**Cause**: onSubmit handler missing  
**Check**:
1. DevTools Console: Any JS errors?
2. Network tab: Request sent?
3. Form component: Has proper submit handler

---

## 10. Reporting Issues

### Issue Template

**File**: [Filename + line number]  
**Severity**: Critical | High | Medium | Low  
**Steps to reproduce**:
1. Navigate to X
2. Click Y
3. Observe Z

**Expected**: [What should happen]  
**Actual**: [What actually happened]  

**Console errors**: [Copy full stack trace]  
**Screenshot**: [Attach image]

### Example

```
File: src/features/users/pages/list.tsx:45
Severity: Critical
Steps:
1. Navigate to /users
2. Click "Filter" dropdown
3. Observe no options appear

Expected: Dropdown shows filter options (Name, Status, Level)
Actual: Dropdown appears empty

Console error:
TypeError: Cannot read property 'map' of undefined
    at UsersTable (list.tsx:45)

Screenshot: [attached]
```

---

## 11. Testing Commands

### Run Everything

```bash
npm run dev              # Start dev server
npm run build            # Production build (check for errors)
npm run type-check       # TypeScript check
npm run lint             # Code style check
npm run test:unit        # Run unit tests
npm run test:e2e         # Run end-to-end tests (slow)
npm run test:e2e:ui      # Run e2e with UI (interactive)
```

### Recommended Daily Workflow

```bash
# Morning
npm run dev              # Start dev server
npm run type-check       # Quick type check
npm run lint             # Quick lint check

# Before committing
npm run build            # Verify build succeeds
npm run test:unit        # Run unit tests
npm run type-check       # Final type check
```

---

## 12. Developer Handoff Checklist

Before moving to next task, confirm:

```
□ Development server starts without errors
□ Login page loads and is interactive
□ Navigation to all 10+ pages works
□ At least 3 feature pages show data or proper empty states
□ No critical TypeScript errors (npm run type-check)
□ No critical linting errors (npm run lint)
□ Lighthouse performance score ≥75
□ At least one feature responds to user input (button clicks, filters)
□ Error scenarios handled gracefully (no crashes)
□ Mobile view is usable (no horizontal scroll)
```

**Pass** ✅ → Ready for Phase 1 systematic testing  
**Fail** ❌ → Log issues, coordinate with team lead

---

## Quick Reference

| Task | Command | Expected Time |
|------|---------|---|
| Setup | `npm install && npm run dev` | 2-3 min |
| Type check | `npm run type-check` | <1 min |
| Lint | `npm run lint` | <1 min |
| Build | `npm run build` | 10-30 sec |
| Test (unit) | `npm run test:unit` | 5-30 sec |
| Test (e2e) | `npm run test:e2e` | 1-5 min |
| Manual feature test | Click through 1 page | 3-5 min |

---

## Getting Help

**If blocked:**
1. Check DevTools Console for error messages
2. Ask team lead for test credentials
3. Pair with another developer
4. Check `REACT_PHASE1_VERIFICATION_PLAN_2026_07_05.md` for guidance

**Slack**: #react-dashboard channel  
**Issues**: Create GitHub issue with template above

---

**Status**: Ready to test 🚀  
**Next**: Proceed to `REACT_PHASE1_VERIFICATION_PLAN_2026_07_05.md` for systematic testing

