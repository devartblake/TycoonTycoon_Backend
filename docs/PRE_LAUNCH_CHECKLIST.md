# Pre-Launch Checklist — Quiz Review + Arcade Leaderboard (v4.0.0)

**Release Date:** 2026-07-01 (TARGET)  
**Release Version:** 4.0.0  
**Status:** 🟢 READY FOR DEPLOYMENT

---

## 📋 Pre-Deployment Verification (1-2 hours)

### Backend Checklist

- [ ] **All tests passing**
  ```bash
  cd Synaptix.Backend.Application.Tests
  dotnet test --configuration Release
  # Expected: 215/215 tests passing ✅
  ```

- [ ] **Database migrations reviewed**
  ```bash
  dotnet ef migrations list
  # Expected: 20260701000000_AddArcadeScoreEntries (pending)
  ```

- [ ] **Production database backed up**
  ```sql
  BACKUP DATABASE synaptix_prod TO DISK = '/backups/synaptix_prod_2026-07-01.bak'
  ```

- [ ] **API endpoints documented in Swagger**
  - [ ] POST /leaderboards/arcade/submit
  - [ ] GET /leaderboards/arcade/{gameId}/{difficulty}
  - [ ] All endpoints accessible at /swagger/v1/swagger.json

- [ ] **No hardcoded secrets in code**
  ```bash
  git grep -E "password|secret|apikey|token" -- '*.cs' | grep -v test | wc -l
  # Expected: 0 results
  ```

- [ ] **Error logging configured**
  - [ ] appsettings.Production.json reviewed
  - [ ] Log level: Warning (reduce noise in production)
  - [ ] Error tracking service configured (Sentry, AppInsights, etc.)

- [ ] **CORS configured correctly**
  - [ ] AllowedOrigins includes production domain
  - [ ] AllowedMethods includes POST, GET, OPTIONS
  - [ ] Credentials policy reviewed

### Flutter Frontend Checklist

- [ ] **No compilation errors**
  ```bash
  flutter analyze
  # Expected: 0 errors, 0 warnings
  ```

- [ ] **No null safety issues**
  ```bash
  flutter analyze --fatal-warnings
  # Expected: 0 issues
  ```

- [ ] **Release builds succeed**
  ```bash
  flutter build apk --release
  flutter build ios --release
  flutter build web --release
  flutter build windows --release
  ```

- [ ] **Version updated in pubspec.yaml**
  - [ ] version: 4.0.0+4 ✅

- [ ] **Environment configuration correct**
  - [ ] .env.prod configured with production API URL
  - [ ] API_BASE_URL set to production backend
  - [ ] Feature flags reviewed and updated
  - [ ] Debug mode disabled in production builds

- [ ] **Quiz Review feature tested**
  - [ ] Play Pattern Sprint game (2-3 runs)
  - [ ] Verify "Review Answers" button appears in results
  - [ ] Test correct/incorrect indicators
  - [ ] Test expandable question tiles
  - [ ] Verify accuracy summary calculation
  - [ ] Confirm no impact on Memory Flip or Quick Math

- [ ] **Arcade Leaderboard feature tested**
  - [ ] Complete Pattern Sprint game
  - [ ] Verify score appears in local leaderboard immediately
  - [ ] Verify score appears in global leaderboard within 5 seconds
  - [ ] Test Local/Global toggle
  - [ ] Test game picker (Pattern Sprint, Memory Flip, Quick Math Rush)
  - [ ] Test difficulty picker (Easy, Normal, Hard, Insane)
  - [ ] Verify offline fallback works
  - [ ] Test with poor network connectivity (throttle to 3G)
  - [ ] Verify personal best not exceeded by lower scores

- [ ] **Network error handling**
  - [ ] Disconnect internet, complete game
  - [ ] Verify local fallback works
  - [ ] Reconnect, verify sync occurs
  - [ ] Test with timeout scenarios

### Device Testing Checklist

- [ ] **Android** (Target API 24+)
  - [ ] Build and install release APK on Pixel 4+ device
  - [ ] Test all Quiz Review flows
  - [ ] Test all Arcade Leaderboard flows
  - [ ] Verify no crashes (check logcat for errors)
  - [ ] Memory usage reasonable (< 200MB)

- [ ] **iOS** (iOS 12+)
  - [ ] Build and install release IPA on iPhone device
  - [ ] Test all Quiz Review flows
  - [ ] Test all Arcade Leaderboard flows
  - [ ] Verify no crashes (check system logs)
  - [ ] Battery usage reasonable

- [ ] **Web** (Chrome, Safari, Firefox)
  - [ ] Build web release
  - [ ] Test on Chrome (latest)
  - [ ] Test on Safari (latest)
  - [ ] Test on Firefox (latest)
  - [ ] Verify no console errors (Ctrl+Shift+K)
  - [ ] Test on 3G network (Chrome DevTools throttle)

- [ ] **Windows** (Windows 10+)
  - [ ] Build release executable
  - [ ] Install and run on Windows 10/11
  - [ ] Test UI responsiveness
  - [ ] Verify no crashes

### Documentation Checklist

- [ ] **Deployment guide reviewed**
  - [ ] DEPLOYMENT_GUIDE.md current and accurate
  - [ ] All steps executable and tested

- [ ] **API documentation complete**
  - [ ] Swagger docs include Quiz Review & Leaderboard endpoints
  - [ ] Example requests/responses provided
  - [ ] Error codes documented

- [ ] **Rollback procedures documented**
  - [ ] Database rollback steps clear
  - [ ] API rollback steps clear
  - [ ] App store rollback procedures understood

- [ ] **Release notes prepared**
  - [ ] Summary of changes written
  - [ ] Known limitations listed
  - [ ] Screenshots captured (if applicable)

---

## 🚀 Deployment Execution (1-2 hours)

### Phase 1: Database & Backend (5-15 minutes)

```bash
# Step 1: Apply database migration
cd Synaptix.Backend.Api
dotnet ef database update --configuration Release --environment Production

# Verify migration applied
SELECT COUNT(*) FROM arcade_scores;  # Should return 0

# Step 2: Publish API
dotnet publish --configuration Release --output ./publish --self-contained false

# Step 3: Deploy to production (choose A or B)

# Option A: Direct deployment
scp -r publish/* prod-server:/app/synaptix-api/
ssh prod-server "systemctl restart synaptix-api"

# Option B: Container deployment
docker build -t synaptix-api:4.0.0 .
docker push registry.internal/synaptix-api:4.0.0
kubectl apply -f deployment.yml

# Step 4: Verify API health
curl https://api.synaptix.com/health
# Expected: 200 OK
```

### Phase 2: App Store Submissions (immediate)

**Android Play Store:**
```bash
# Build signed release
flutter build appbundle --release

# Upload to Play Store Console
# - Create release version 4.0.0
# - Set rollout to 10% (canary)
# - Monitor crash reports for 24 hours
# - If stable, increase to 100%
```

**iOS App Store:**
```bash
# Build release
flutter build ios --release

# Archive and sign in Xcode
# - Select "Generic iOS Device" or simulator
# - Product → Archive
# - Distribute App
# - Select "App Store Connect"

# Or use Transporter
transporter upload -file TriviaTycoon.ipa -apple_id user@example.com
```

**Web Deployment:**
```bash
flutter build web --release
# Deploy to hosting (Vercel, Netlify, AWS S3+CloudFront)
vercel deploy --prod
# Verify at https://trivia-tycoon.com/
```

**Windows Deployment:**
```bash
# Build release
flutter build windows --release

# Create installer (optional, MSI)
# Or distribute .exe directly

# Update download link on website
```

### Phase 3: Post-Deployment Verification (10-30 minutes)

```bash
# Step 1: API health check
curl https://api.synaptix.com/health
# Expected: 200 OK, {"status":"healthy"}

# Step 2: Database connectivity
curl https://api.synaptix.com/leaderboards/arcade/patternSprint/normal?page=1
# Expected: 200 OK, empty leaderboard or sample data

# Step 3: Smoke test score submission
curl -X POST https://api.synaptix.com/leaderboards/arcade/submit \
  -H "Authorization: Bearer <test-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "gameId": "patternSprint",
    "difficulty": "normal",
    "score": 1000,
    "durationMs": 30000
  }'
# Expected: 200 OK, score recorded

# Step 4: App store submission confirmation
# - Android: Check Play Console for upload success
# - iOS: Check App Store Connect for submission
# - Web: Verify deployment at trivia-tycoon.com
```

---

## 📊 Post-Deployment Monitoring (24 hours)

### Metrics to Monitor

| Metric | Target | Check Interval |
|--------|--------|---|
| API response time (P95) | < 200ms | Every 5 minutes |
| API error rate | < 0.1% | Every 5 minutes |
| Database connections | < 50 | Every 15 minutes |
| Crash rate (Android) | < 0.01% | Every 30 minutes |
| Crash rate (iOS) | < 0.01% | Every 30 minutes |
| User feedback (reviews) | Monitor | Continuous |

### Daily Checklist (First 24 Hours)

- [ ] **Hour 0-1:** API response times normal, no error spikes
- [ ] **Hour 1-6:** Monitor app store reviews for critical issues
- [ ] **Hour 6-12:** Verify quiz review feature usage in analytics
- [ ] **Hour 12-24:** Confirm leaderboard scores syncing correctly
- [ ] **After 24h:** If stable, increase app store rollout to 100%

### Escalation Procedures

**If API response time > 500ms:**
1. Check database connection pool
2. Verify database query performance (use query analyzer)
3. Check memory/CPU on server
4. Consider database replication if needed

**If error rate > 1%:**
1. Check error logs for patterns
2. Verify backend changes applied correctly
3. Roll back to previous version if necessary

**If crash rate > 0.1%:**
1. Check crash logs in app store console
2. Identify common crash signature
3. Prepare hotfix if needed
4. Consider rollback if critical

---

## 🔄 Rollback Procedures

### Backend Rollback (If Critical Issue)

```bash
# Option 1: Revert migration (loses data, quick)
cd Synaptix.Backend.Api
dotnet ef database update --configuration Release --environment Production \
  --migration 20260625_AddPasswordResetTokens

# Option 2: Restore from backup (safe, takes time)
RESTORE DATABASE synaptix_prod FROM DISK = '/backups/synaptix_prod_2026-07-01.bak'

# Redeploy previous API version
kubectl rollout undo deployment/synaptix-api
```

### App Rollback

**Android:**
```
Play Store Console → Select App → Releases → Production
→ Set rollout percentage to 0% → Restore previous version
```

**iOS:**
```
App Store Connect → Version Release → Remove availability
→ Restore previous version (manual resubmission)
```

**Web:**
```
Vercel/Netlify → Deployments → Rollback to previous
```

---

## ✅ Success Criteria

Deployment is **SUCCESSFUL** when:

- ✅ All 215 backend tests pass
- ✅ API health check returns 200 OK
- ✅ Quiz review feature working on all platforms
- ✅ Leaderboard scores syncing correctly
- ✅ Error rate < 0.1% in first 24 hours
- ✅ Crash rate < 0.01% in first 24 hours
- ✅ No critical user-facing bugs reported
- ✅ App store submissions successful

---

## 📞 Support Contacts

| Role | Contact | Escalation |
|------|---------|---|
| Backend Lead | backend-lead@company.com | On-call rotation |
| Mobile Lead | mobile-lead@company.com | On-call rotation |
| DevOps Lead | devops-lead@company.com | On-call rotation |
| DBA | database-admin@company.com | Critical DB issues |
| Support Lead | support-lead@company.com | User issue coordination |

---

## 📝 Sign-Off

| Role | Status | Date | Notes |
|------|--------|------|-------|
| Backend Lead | ⏳ Pending | | |
| Mobile Lead | ⏳ Pending | | |
| DevOps Lead | ⏳ Pending | | |
| QA Lead | ⏳ Pending | | |
| Product Manager | ⏳ Pending | | |

---

**Deployment Status:** 🟢 READY FOR LAUNCH  
**Target Launch Date:** 2026-07-01  
**Version:** 4.0.0  
**Last Updated:** 2026-07-01
