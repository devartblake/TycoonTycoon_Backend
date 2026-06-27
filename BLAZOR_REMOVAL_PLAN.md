# Blazor Operator Dashboard Removal Plan

**Date:** 2026-06-26  
**Status:** READY FOR EXECUTION  
**Risk Level:** LOW (isolated component, no dependencies)

---

## Executive Summary

The Blazor operator dashboard (`Synaptix.OperatorDashboard`) is being replaced by the Django operator dashboard. This document provides a step-by-step guide to safely remove all Blazor components from the project.

**Impact:** Zero - The Django dashboard is fully functional and ready as a drop-in replacement.

---

## Components to Remove

### 1. Source Code Directory
- **Path:** `Synaptix.OperatorDashboard/`
- **Size:** ~50MB (including bin/obj)
- **Contents:**
  - Blazor components (Pages, Components, Services)
  - wwwroot (static assets)
  - appsettings.json
  - Program.cs (Blazor-specific)

### 2. Docker Configuration Files
- **Path:** `docker/Dockerfile.dashboard`
- **Purpose:** Built Blazor dashboard image
- **Status:** No longer needed

### 3. Docker Compose Service References

**Files to modify:**
- `docker/compose.yml` - Remove `operator-dashboard-blazor` service
- `docker/compose.prod.yml` - Remove `operator-dashboard-blazor` service  
- `docker/compose.dev.yml` (if exists) - Remove if present

### 4. Build/CI Configuration
- Check for Blazor-specific build steps in CI/CD pipelines
- Remove from any build scripts

---

## Step-by-Step Removal Process

### Phase 1: Backup & Verification (5 minutes)

```bash
# 1. Create a backup of the Blazor project (just in case)
cd c:\Users\lmxbl\Documents\TycoonTycoon_Backend
cp -r Synaptix.OperatorDashboard Synaptix.OperatorDashboard.backup

# 2. Verify Django dashboard is fully functional
docker compose -f docker/compose.yml up -d operator-dashboard
curl http://localhost:8200/  # Should return login page

# 3. Check git history (optional - see what Blazor was used for)
git log --oneline Synaptix.OperatorDashboard/ | head -20
```

### Phase 2: Modify Docker Compose (10 minutes)

#### Update `docker/compose.yml`:

**BEFORE:**
```yaml
  operator-dashboard-blazor:
    build:
      context: ..
      dockerfile: docker/Dockerfile.dashboard
    container_name: synaptix_operator_dashboard_blazor
    restart: unless-stopped
    ports:
      - "8100:80"
    environment:
      ASPNETCORE_URLS: http://+:80
    networks: [synaptix-net]
    depends_on:
      backend-api:
        condition: service_healthy
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost/healthz"]
      interval: 30s
      timeout: 5s
      retries: 3
```

**AFTER:** (Delete the entire section above)

#### Update `docker/compose.prod.yml`:

**BEFORE:**
```yaml
  operator-dashboard-blazor:
    ports: !override []
    profiles: ["disabled"]
```

**AFTER:** (Delete these lines entirely)

---

### Phase 3: Remove Source Code (5 minutes)

```bash
cd c:\Users\lmxbl\Documents\TycoonTycoon_Backend

# 1. Delete the Blazor project directory
rm -rf Synaptix.OperatorDashboard

# 2. Remove the Dockerfile
rm docker/Dockerfile.dashboard

# 3. Verify deletion
ls -la Synaptix.OperatorDashboard 2>&1  # Should show "cannot access"
```

### Phase 4: Clean Build Artifacts (2 minutes)

```bash
# Remove any cached build files
find . -type d -name "bin" -path "*/OperatorDashboard/*" -exec rm -rf {} + 2>/dev/null
find . -type d -name "obj" -path "*/OperatorDashboard/*" -exec rm -rf {} + 2>/dev/null

# Clean Docker
docker system prune -f  # Remove unused images/layers
```

### Phase 5: Verify No References Remain (5 minutes)

```bash
# 1. Search for any remaining references
grep -r "OperatorDashboard" . --include="*.yml" --include="*.yaml" --include="*.sln" --include="*.csproj" 2>/dev/null | grep -v ".git" | grep -v ".claude"

# Expected: Only references to "Synaptix.OperatorDashboard.Django" should exist

# 2. Check for port 8100 (Blazor port) in configs
grep -r "8100\|operator-dashboard-blazor" docker/ --include="*.yml"

# Expected: No matches (should be clean)

# 3. Verify no build scripts reference it
grep -r "OperatorDashboard" scripts/ 2>/dev/null | grep -v Django

# Expected: No matches
```

### Phase 6: Test All Services (15 minutes)

```bash
# 1. Start fresh services
docker compose -f docker/compose.yml down
docker compose -f docker/compose.yml up -d

# 2. Verify services started
docker ps | grep synaptix

# Expected output:
# - synaptix_postgres ✓
# - synaptix_redis ✓
# - synaptix_elasticsearch ✓
# - synaptix_backend_api ✓
# - synaptix_operator_dashboard (Django) ✓
# - synaptix_migration ✓
# - synaptix_kms_api ✓

# Should NOT see:
# - synaptix_operator_dashboard_blazor ✗

# 3. Test Django dashboard works
curl -L http://localhost:8200/  # Should show login page
curl http://localhost:5000/healthz  # Backend should respond

# 4. Check logs for errors
docker compose logs | grep -i "error\|exception" | grep -v "INFO\|DEBUG"

# Expected: No critical errors
```

### Phase 7: Git Commit (5 minutes)

```bash
cd c:\Users\lmxbl\Documents\TycoonTycoon_Backend

# 1. Stage deletions
git add -A

# 2. Verify changes
git status

# Expected to see:
# - deleted: Synaptix.OperatorDashboard/...
# - deleted: docker/Dockerfile.dashboard
# - modified: docker/compose.yml
# - modified: docker/compose.prod.yml

# 3. Create commit
git commit -m "Remove deprecated Blazor operator dashboard

- Delete Synaptix.OperatorDashboard project
- Remove Dockerfile.dashboard
- Update docker-compose files to remove blazor service
- Django operator dashboard fully replaces all functionality

The Django operator dashboard (Synaptix.OperatorDashboard.Django)
is now the sole operator dashboard solution. It provides:
  * Password recovery system
  * Admin audit logs
  * User moderation tools
  * Analytics dashboard
  * Configuration management

Co-Authored-By: Claude Haiku 4.5 <noreply@anthropic.com>"

# 4. Push to remote (if ready for production)
git push origin main  # Only if staging tests pass
```

---

## Verification Checklist

- [ ] Backup created: `Synaptix.OperatorDashboard.backup/`
- [ ] Django dashboard tested and working
- [ ] Docker compose files updated (both .yml files)
- [ ] Blazor project directory deleted
- [ ] Dockerfile.dashboard deleted
- [ ] No references to "OperatorDashboard" remain (except Django)
- [ ] Port 8100 removed from configs
- [ ] Services start cleanly without Blazor
- [ ] Django dashboard accessible at `http://localhost:8200`
- [ ] Backend API healthy
- [ ] Git commit created with proper message
- [ ] All tests pass

---

## Rollback Plan (If Needed)

If issues arise, you can restore the Blazor dashboard:

```bash
# 1. Restore from backup
cp -r Synaptix.OperatorDashboard.backup Synaptix.OperatorDashboard

# 2. Restore docker files
git checkout HEAD~1 docker/compose.yml
git checkout HEAD~1 docker/compose.prod.yml
git checkout HEAD~1 docker/Dockerfile.dashboard

# 3. Rebuild and restart
docker compose -f docker/compose.yml up -d --build operator-dashboard-blazor

# 4. Reset git (undo removal commit)
git reset --soft HEAD~1
git restore docker/compose.yml docker/compose.prod.yml
```

---

## Post-Removal Tasks

### 1. Update Documentation
- [ ] Update README.md - remove Blazor dashboard references
- [ ] Update deployment guide - point to Django dashboard only
- [ ] Update troubleshooting guide - remove Blazor-specific issues

### 2. Update CI/CD (if applicable)
- [ ] Remove Blazor build steps from GitHub Actions
- [ ] Remove Blazor test steps
- [ ] Update Docker build matrix if using it

### 3. Communicate Change
- [ ] Notify team that Blazor dashboard is removed
- [ ] Share new Django dashboard documentation
- [ ] Provide login credentials for admin.synaptixplay.com

### 4. Monitor Production (if deployed)
- [ ] Watch for any issues with Django dashboard
- [ ] Monitor performance metrics
- [ ] Collect user feedback

---

## File Summary

**Files to Delete:**
```
Synaptix.OperatorDashboard/                 (~50MB)
  ├── Pages/
  ├── Components/
  ├── Services/
  ├── wwwroot/
  ├── Program.cs
  ├── appsettings.Development.json
  ├── appsettings.json
  ├── Synaptix.OperatorDashboard.csproj
  └── ... (all files)

docker/Dockerfile.dashboard                 (~5KB)
```

**Files to Modify:**
```
docker/compose.yml                          (-15 lines)
docker/compose.prod.yml                     (-5 lines)
```

**Total Storage Recovered:** ~50-60MB

---

## Estimated Timeline

| Phase | Task | Time |
|-------|------|------|
| 1 | Backup & Verification | 5 min |
| 2 | Modify Docker Compose | 10 min |
| 3 | Remove Source Code | 5 min |
| 4 | Clean Artifacts | 2 min |
| 5 | Verify References | 5 min |
| 6 | Test All Services | 15 min |
| 7 | Git Commit | 5 min |
| | **TOTAL** | **~50 min** |

---

## Safety Notes

✅ **SAFE TO REMOVE:**
- No other projects depend on the Blazor OperatorDashboard
- Django dashboard is fully functional as a replacement
- No shared libraries between Blazor and Django dashboards
- No database dependencies that require Blazor

✅ **NO BREAKING CHANGES:**
- Backend API remains unchanged
- Django dashboard API calls are identical
- No migrations needed
- No configuration changes required (except docker-compose)

✅ **TESTED BEFORE REMOVAL:**
- Django dashboard password recovery ✓
- Admin audit logging ✓
- Moderation tools ✓
- User management ✓

---

## Alternative: Disable Without Removing

If you want to keep the code in git history but not deploy it:

```yaml
# docker/compose.yml
  operator-dashboard-blazor:
    profiles: ["disabled"]  # This disables it without deleting
```

This way:
- Code remains in git history
- Services won't start unless explicitly requested
- Storage is still used locally
- Can easily re-enable if needed

---

## Questions?

Refer to:
- DEPLOYMENT_SUMMARY.md - Django dashboard deployment
- PASSWORD_RECOVERY_IMPLEMENTATION.md - New features
- IMPLEMENTATION_ROADMAP.md - Future features

**Status:** Ready to execute immediately. No dependencies on Blazor dashboard exist.
