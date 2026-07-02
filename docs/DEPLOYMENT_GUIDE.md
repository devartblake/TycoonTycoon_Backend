# Production Deployment Guide

**Feature:** Arcade Leaderboard + Quiz Review System  
**Status:** Ready for Production  
**Date:** July 1, 2026

---

## Pre-Deployment Checklist

### Backend

#### 1. Code Quality Verification

- [ ] All 215 tests passing
```bash
cd Synaptix.Backend.Application.Tests
dotnet test --configuration Release
```

- [ ] API project compiles without errors
```bash
cd Synaptix.Backend.Api
dotnet build --configuration Release
```

- [ ] No critical security warnings
```bash
# dotnet roslynator analyze
# Check for SQL injection risks (none - using EF Core)
# Check for auth bypasses (none - RequireAuthorization present)
```

#### 2. Database Preparation

- [ ] EF Core migrations up to date
```bash
# List pending migrations
dotnet ef migrations list

# Expected: 20260701000000_AddArcadeScoreEntries (not applied yet)
```

- [ ] Backup production database (CRITICAL)
```sql
-- Before migration, backup:
-- - Users table
-- - Players table
-- - Any existing LeaderboardEntries table
```

- [ ] Test migration on staging database
```bash
# Apply migration to staging first
dotnet ef database update --environment Staging
```

#### 3. Environment Configuration

- [ ] Production connection string configured
```appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=prod-db-host;Database=synaptix_prod;..."
  }
}
```

- [ ] No hardcoded secrets in code
```bash
# Verify no API keys, connection strings in committed files
git grep -E "password|secret|apikey" | grep -v test
```

- [ ] Logging configured for production
```appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft": "Warning"
    }
  }
}
```

#### 4. API Documentation

- [ ] OpenAPI/Swagger documentation updated
```
GET /swagger/v1/swagger.json should include:
- POST /leaderboards/arcade/submit
- GET /leaderboards/arcade/{gameId}/{difficulty}
```

- [ ] Endpoint documentation accessible
```
https://api.synaptix.com/swagger/index.html
```

### Flutter

#### 1. Code Quality Verification

- [ ] No compilation errors
```bash
cd trivia_tycoon
flutter analyze
```

- [ ] No null safety warnings
```bash
flutter analyze --fatal-warnings
```

- [ ] Tests passing (if any)
```bash
flutter test
```

#### 2. Build Configuration

- [ ] Release build works
```bash
# For Android
flutter build apk --release
flutter build appbundle --release

# For iOS
flutter build ios --release

# For Web
flutter build web --release
```

- [ ] Version numbers updated
```dart
// pubspec.yaml
version: 1.X.0+X (increment appropriately)
```

- [ ] Environment configuration correct
```dart
// .env.prod should be used for production builds
const apiBaseUrl = 'https://api.synaptix.com';
```

#### 3. Feature Testing

- [ ] Quiz review works end-to-end
  - [ ] Play Pattern Sprint game
  - [ ] Answer mix of correct/incorrect
  - [ ] Tap "Review Answers"
  - [ ] Verify all answers displayed correctly

- [ ] Leaderboard submission works
  - [ ] Complete arcade game
  - [ ] Verify score appears in global leaderboard within 5 seconds
  - [ ] Verify personal best not exceeded can't overwrite

- [ ] Local/Global toggle works
  - [ ] Switch between Local and Global views
  - [ ] Verify correct data shown
  - [ ] Verify offline fallback works

- [ ] Main leaderboard Arcade tab works
  - [ ] Navigate to main leaderboard
  - [ ] Click "Arcade" tab
  - [ ] Select different games
  - [ ] Select different difficulties
  - [ ] Verify data loads correctly

#### 4. Device Testing

- [ ] Android: Test on device with API 21+ (recommended 24+)
- [ ] iOS: Test on device with iOS 12+
- [ ] Web: Test on Chrome, Safari, Firefox
- [ ] Network: Test on 3G/4G and WiFi
- [ ] Offline: Verify graceful degradation

---

## Deployment Steps

### Step 1: Backend Deployment

#### 1.1 Database Migration

```bash
# Connect to production database
# Run EF Core migration to create arcade_scores table

dotnet ef database update \
  --configuration Release \
  --environment Production \
  --project Synaptix.Backend.Migrations

# Verify table was created
SELECT COUNT(*) FROM arcade_scores;  -- Should return 0
```

**Rollback plan (if needed):**
```bash
# Remove the migration
dotnet ef database update \
  --configuration Release \
  --environment Production \
  --migration 20260625_AddPasswordResetTokens  # previous migration

# Or restore from backup
```

#### 1.2 API Deployment

**Option A: Direct Deployment**
```bash
# Build release
dotnet publish --configuration Release \
  --output ./publish \
  --self-contained false

# Copy to production server
scp -r publish/* prod-server:/app/synaptix-api/

# Restart service
ssh prod-server "systemctl restart synaptix-api"

# Verify
curl https://api.synaptix.com/health
```

**Option B: Container Deployment (Docker)**
```bash
# Build Docker image
docker build -t synaptix-api:v1.0.0 .

# Push to registry
docker push registry.internal/synaptix-api:v1.0.0

# Deploy to Kubernetes/container orchestrator
kubectl apply -f deployment.yml
```

#### 1.3 Post-Deployment Verification

```bash
# Test endpoints are responding
curl -X GET https://api.synaptix.com/leaderboards/arcade/patternSprint/normal

# Verify database connectivity
# Check application logs for errors

# Monitor API response times
# (should be < 200ms for leaderboard queries)

# Smoke test: Submit a score
curl -X POST https://api.synaptix.com/leaderboards/arcade/submit \
  -H "Authorization: Bearer <test-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "gameId": "patternSprint",
    "difficulty": "normal",
    "score": 1000,
    "durationMs": 30000
  }'
```

### Step 2: Flutter Deployment

#### 2.1 Android Deployment

```bash
# Build signed release APK
flutter build apk --release \
  --target-platform android-arm64 \
  --split-per-abi

# Upload to Google Play Console
# - Create release version (1.X.0)
# - Add release notes
# - Set rollout to 10% initially for canary testing
# - Monitor crash reports for 24 hours
# - If stable, increase to 100%
```

#### 2.2 iOS Deployment

```bash
# Build release IPA
flutter build ios --release

# Archive in Xcode
# Sign with production certificate
# Upload to App Store Connect via Xcode or Transporter

# Submit for review (Apple reviews typically within 24-48 hours)
# Once approved, enable in-store availability
```

#### 2.3 Web Deployment

```bash
# Build web release
flutter build web --release --dart-define=kReleaseMode=true

# Deploy to hosting (Vercel, Netlify, AWS S3+CloudFront, etc.)
vercel deploy --prod

# Verify
# - Quiz review works at https://trivia-tycoon.com/arcade
# - Leaderboard integration works
# - No console errors in browser dev tools
```

---

## Monitoring & Rollback

### Health Checks (First 24 Hours)

Monitor these metrics continuously:

**Backend API**
```
- Request latency (should be < 200ms)
- Error rate (should be < 0.1%)
- Database connection pool (should have available connections)
- Disk space on database server (should have > 10GB free)
```

**Flutter Apps**
```
- Crash rate (should be < 0.01%)
- ANRs (Application Not Responding) - should be 0
- Quiz review feature usage
- Leaderboard queries per minute
```

### Rollback Procedures

**If Backend Fails:**

```bash
# Option 1: Remove migration (quick, loses data)
dotnet ef database update --migration 20260625_AddPasswordResetTokens

# Option 2: Restore from backup (safest)
# Restore production database to pre-deployment backup

# Re-deploy previous API version
kubectl rollout undo deployment/synaptix-api
```

**If Flutter Fails:**

```bash
# Android: Google Play Console → Rollout settings → Rollout to 0% (stops update)
# iOS: App Store Connect → Version Release → Remove availability → Restore previous version
# Web: Rollback deployment via Vercel/hosting provider
```

---

## Post-Deployment Tasks

### 1. Notify Users
- [ ] Send in-app notification: "New: Quiz Review & Global Leaderboards!"
- [ ] Post to social media
- [ ] Update release notes in app stores

### 2. Collect Feedback
- [ ] Monitor support tickets for issues
- [ ] Watch social media mentions
- [ ] Check app store reviews
- [ ] Review analytics for feature usage

### 3. Performance Optimization
- [ ] Run database query analysis
- [ ] Look for slow queries on arcade_scores table
- [ ] Monitor cache hit rates (if caching added)
- [ ] Adjust indexes if needed

### 4. Security Validation
- [ ] Run security scan on deployed API
- [ ] Verify authentication enforcement
- [ ] Check for SQL injection vulnerabilities
- [ ] Audit database access logs

---

## Success Criteria

**Deployment is successful when:**

- ✅ All 215 backend tests passing
- ✅ API health check returning 200 OK
- ✅ Quiz review feature working on all platforms
- ✅ Leaderboard data syncing correctly
- ✅ No crash spikes in first 24 hours
- ✅ User feedback is positive
- ✅ Performance metrics within acceptable ranges

---

## Deployment Timeline

| Phase | Duration | Owner |
|-------|----------|-------|
| Pre-deployment testing | 30 min | QA/Dev |
| Database migration | 5 min | DBA |
| Backend deployment | 10 min | DevOps |
| Backend verification | 10 min | QA |
| Flutter build & upload | 30 min | CI/CD |
| Flutter app store review | 24-48 hours | Apple/Google |
| Full rollout monitoring | 24 hours | Ops |
| **Total** | **1.5-2.5 days** | |

---

## Support Contacts

In case of emergency during deployment:

| Issue | Contact | Phone |
|-------|---------|-------|
| Database issues | DBA On-Call | xxx-xxx-xxxx |
| API issues | Backend Lead | xxx-xxx-xxxx |
| App Store issues | Mobile Lead | xxx-xxx-xxxx |
| Monitoring/Alerts | DevOps | xxx-xxx-xxxx |

---

## Sign-Off

- [ ] Backend Lead: Approved for deployment
- [ ] Mobile Lead: Approved for deployment
- [ ] DevOps Lead: Deployment infrastructure ready
- [ ] QA Lead: All testing completed
- [ ] Product Manager: Feature approved for launch

---

**Deployment Status:** 🟢 READY FOR PRODUCTION

**Next Steps:** Execute deployment according to above steps, monitor for 24 hours, then proceed with Phase 2 enhancements.
