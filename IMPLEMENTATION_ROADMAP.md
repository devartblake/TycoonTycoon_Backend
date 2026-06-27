# Synaptix Backend Implementation Roadmap

**Last Updated:** 2026-06-26  
**Status:** In Progress  
**Priority:** High

---

## Executive Summary

This document outlines:
1. ✅ **Password Recovery** - Already implemented for Django operator dashboard
2. 🔍 **Backend Endpoint Audit** - Comparison of Flutter client requirements vs. current API
3. 🚀 **Performance Optimization** - Django operator dashboard improvements
4. 📋 **Missing Endpoints** - Detailed implementation plan
5. 🏗️ **Backend Components** - Missing services and database models

---

## 1. Password Recovery Implementation ✅

**Status:** COMPLETE for Django Operator Dashboard

### Already Implemented:
- ✅ Backend password reset endpoints (`/admin/auth/forgot-password`, `/admin/auth/reset-password`, `/admin/auth/validate-reset-token`)
- ✅ Email service integration (SMTP/SendGrid)
- ✅ Django UI templates (forgot-password, reset-password)
- ✅ Database migration (`PasswordResetToken` entity)
- ✅ Security features:
  - 15-minute token expiry
  - One-time use enforcement
  - Session revocation after reset
  - IP/User-Agent logging
  - Rate limiting
  - Audit trail logging

### Deploy to Production:
1. Run migration: `./scripts/run-migrations-docker.sh`
2. Verify email config in `.env.production`
3. Test end-to-end in staging
4. Deploy to production

See [PASSWORD_RECOVERY_IMPLEMENTATION.md](PASSWORD_RECOVERY_IMPLEMENTATION.md) for details.

---

## 2. Flutter Client Endpoint Analysis

### Endpoints Required by Flutter Client (1058 lines analyzed)

#### ✅ IMPLEMENTED ENDPOINTS
These endpoints have backend support:

**Authentication & Users**
- `GET /users/{userId}` - Player profile
- `PATCH /users/{userId}` - Update profile
- `GET /players/{playerId}` - Player details
- `POST /players` - Create player

**Leaderboards**
- `GET /leaderboard` - Global leaderboard (paginated)
- `GET /leaderboard/user/{userId}` - User rank
- `GET /seasons/{seasonId}/leaderboard` - Season leaderboard

**Matches**
- `GET /matches/{matchId}` - Match details
- `GET /matches?playerId=X` - List player matches
- `POST /matches/{matchId}/submit` - Submit match results
- `POST /matchmaking/queue` - Queue for match
- `DELETE /matchmaking/queue/{playerId}` - Cancel matchmaking

**Seasons**
- `GET /seasons/active` - Active season
- `GET /seasons/player-state/{playerId}` - Player season progression
- `GET /seasons/current` - Current season
- `POST /seasons/rewards/claim/{playerId}` - Claim seasonal reward
- `GET /seasons/rewards/eligibility/{playerId}` - Reward eligibility check

**Skills**
- `GET /skills/tree?playerId=X` - Skill tree
- `POST /skills/{nodeId}/unlock` - Unlock skill node
- `POST /skills/{nodeId}/use` - Use active skill
- `POST /skills/tree/respec` - Reset skill tree

**Game Events**
- `GET /game-events/upcoming` - Upcoming events
- `GET /game-events/{gameEventId}/status` - Event status
- `POST /game-events/enter` - Enter event
- `POST /game-events/revive` - Revive in event
- `GET /game-events/{gameEventId}/leaderboard` - Event leaderboard

**Guardians**
- `GET /guardians/{seasonId}/{tierNumber}` - List guardians
- `POST /guardians/challenge` - Challenge guardian
- `GET /guardians/my?playerId=X` - My guardian status

**Territory**
- `GET /territory/{seasonId}/{tierNumber}` - Territory board
- `POST /territory/duel` - Start territory duel
- `GET /territory/{seasonId}/{tierNumber}/dominance` - Dominance leaderboard
- `GET /territory/{seasonId}/{tierNumber}/multiplier?playerId=X` - Territory multiplier

**Economy/Mobile**
- `GET /mobile/economy/state?playerId=X` - Economy state
- `POST /mobile/economy/session/start` - Start economy session
- `POST /mobile/economy/daily-jackpot-ticket/claim` - Claim daily ticket
- `POST /mobile/economy/revive/quote` - Get revive quote
- `POST /mobile/economy/pity/report-loss` - Report loss
- `POST /mobile/economy/pity/report-win` - Report win
- `POST /mobile/matches/start` - Policy-enforced match start

**Personalization**
- `GET /personalization/{playerId}/profile` - Mind profile
- `GET /personalization/{playerId}/home` - Home personalization
- `POST /personalization/{playerId}/events` - Record behavior event
- `GET /personalization/{playerId}/recommendations` - Recommendations
- `POST /personalization/{playerId}/toggle` - Toggle personalization

**Coach**
- `GET /coach/{playerId}/daily-brief` - Daily coaching brief
- `POST /coach/{playerId}/feedback` - Coach feedback

**Experiments**
- `GET /experiments/player/{playerId}` - All assignments
- `GET /experiments/player/{playerId}/{experimentKey}` - Single assignment
- `POST /experiments/player/{playerId}/{experimentKey}/impression` - Record impression
- `POST /experiments/player/{playerId}/{experimentKey}/outcome` - Record outcome

**Powerups**
- `GET /powerups/state/{playerId}` - Powerup balances
- `POST /powerups/use` - Use powerup

**Store/Commerce**
- `GET /store/items?category=X` - Store catalog
- `POST /store/purchase` - Purchase item

**Social/Friends**
- `GET /users/{userId}/friends` - Friends list
- `POST /users/{userId}/friends/request` - Send request
- `POST /users/{userId}/friends/accept` - Accept request

**Party/Group**
- `POST /party` - Create party
- `GET /party/{partyId}` - Party roster
- `POST /party/{partyId}/invite` - Invite to party
- `POST /party/invites/{inviteId}/accept` - Accept invite
- `POST /party/invites/{inviteId}/decline` - Decline invite
- `POST /party/{partyId}/leave` - Leave party
- `GET /party/invites?playerId=X` - List invites
- `POST /party/{partyId}/enqueue` - Enqueue party
- `POST /party/{partyId}/queue/cancel` - Cancel party queue

**Votes**
- `POST /votes` - Cast vote
- `GET /votes/{topic}/results` - Vote results

**Admin**
- `GET /admin/stats` - Admin stats
- `POST /admin/users/{userId}/ban` - Ban user

**Analytics & Events**
- `POST /analytics/track` - Track event (fire-and-forget)

**Health**
- `GET /healthz` - Health check
- `GET /health/readiness` - Readiness probe
- `GET /health/liveness` - Liveness probe

#### ❌ POTENTIALLY MISSING OR INCOMPLETE ENDPOINTS

Based on Flutter client analysis, these areas need verification:

1. **Quiz/Questions**
   - `GET /questions/set?count=X` - Fetch quiz questions
   - `POST /questions/check-batch` - Batch check answers
   - Status: Backend may have these but endpoint naming/routing should be verified

2. **User Achievements** (Deprecated in client but still referenced)
   - `GET /users/{userId}/achievements` - List achievements
   - `POST /users/{userId}/achievements/{achievementId}` - Unlock
   - Status: May be obsolete; verify if still needed

3. **Match Join/Leave** (Older API, may be superseded by matchmaking queue)
   - `POST /matches/{matchId}/join` - Join match
   - `POST /matches/{matchId}/leave` - Leave match
   - Status: Likely deprecated in favor of `/matchmaking/queue`

---

## 3. Performance Optimization - Django Operator Dashboard

### Issues Identified

#### 3.1 **N+1 Query Problem in Admin Audit Views**
**Impact:** High  
**Severity:** Medium

```python
# CURRENT (Bad) - N+1 queries
def operator_audit_security_view(request):
    events = AdminSecurityAudit.objects.all()
    # Template loops through events, each access to event.user causes a query
    return render(request, '...', {'events': events})

# OPTIMIZED (Good)
def operator_audit_security_view(request):
    events = AdminSecurityAudit.objects.select_related('user').all()
    return render(request, '...', {'events': events})
```

**Fix Location:** `dashboard/views.py` - All admin list views
**Estimated Impact:** 50-80% query reduction on admin pages

---

#### 3.2 **Unoptimized Admin Auth Client**
**Impact:** Medium  
**Severity:** Low

**Current Issue:**
- Every view makes fresh auth calls to backend
- No client-side caching of token/profile
- Session data redundantly serialized/deserialized

**Solution:**
```python
# Add memoization decorator
from functools import lru_cache

@lru_cache(maxsize=1)
def get_cached_admin_profile(request):
    """Cached for request lifecycle"""
    return json.loads(request.session.get('admin_profile', '{}'))
```

**Fix Location:** `dashboard/services/admin_auth_client.py`
**Estimated Impact:** 20-30% faster page loads

---

#### 3.3 **Inefficient Template Rendering**
**Impact:** Medium  
**Severity:** Low

**Current Issue:**
- Tables render 100+ items without pagination
- Large JSON objects in template context
- No lazy loading of related data

**Solutions:**

a) **Add Pagination**
```python
# views.py
from django.core.paginator import Paginator

events = AdminSecurityAudit.objects.all()
paginator = Paginator(events, 50)  # 50 per page
page_obj = paginator.get_page(request.GET.get('page'))
```

b) **Lazy Load Related Data**
```python
# Don't load all matching audit logs immediately
events = AdminSecurityAudit.objects.all().only('id', 'action', 'created_at')
```

**Fix Locations:**
- `dashboard/templates/dashboard/audit_security.html`
- `dashboard/templates/dashboard/moderation_logs.html`
- `dashboard/templates/dashboard/store_catalog.html`

**Estimated Impact:** 40-60% faster initial render on large datasets

---

#### 3.4 **Missing Database Indexes**
**Impact:** High  
**Severity:** Medium

**Analysis:** Django ORM handles most indexing, but admin queries should verify:

```python
# Verify these indexes exist
class AdminSecurityAudit(models.Model):
    actor_email = models.EmailField(db_index=True)  # ✓ Good
    action = models.CharField(max_length=50, db_index=True)  # ✓ Good
    created_at = models.DateTimeField(db_index=True)  # ✓ Good
    
    class Meta:
        indexes = [
            models.Index(fields=['actor_email', 'created_at']),  # Composite for range queries
        ]
```

**Fix:** Review `dashboard/models.py` and add composite indexes
**Estimated Impact:** 20-40% faster filtered queries

---

#### 3.5 **Static Files Not Minified**
**Impact:** Low  
**Severity:** Low

**Current:** CSS/JS served uncompressed
**Solution:**
```bash
# Collect and compress static files
python manage.py collectstatic --no-input
python manage.py compress
```

**Fix:** Update `docker/compose.yml` operator-dashboard service
**Estimated Impact:** 30-50% smaller asset transfer size

---

### Django Performance Optimization Checklist

- [ ] Add `select_related()` / `prefetch_related()` to all view queries
- [ ] Implement pagination on list views (50-100 per page)
- [ ] Add database indexes on frequently filtered fields
- [ ] Cache admin profile in request object
- [ ] Use `only()` to select specific columns
- [ ] Minify CSS/JS in production
- [ ] Add response caching headers (for static views)
- [ ] Profile with Django Debug Toolbar in dev
- [ ] Monitor slow queries with `django-silk`

---

## 4. Missing Backend Endpoints - Implementation Plan

### Priority 1: CRITICAL (Week 1)

#### 4.1 Quiz/Questions Verification
**Endpoint:** `GET /questions/set`  
**Status:** May exist but needs verification  

```csharp
// Proposed endpoint location: Synaptix.Backend.Api/Features/Questions/QuestionsEndpoints.cs
public static void Map(RouteGroupBuilder group)
{
    group.MapGet("/set", GetQuestionSet)
        .WithName("GetQuestionSet")
        .WithOpenApi();
}

private static async Task<IResult> GetQuestionSet(
    int count = 10,
    string? category = null,
    string? difficulty = null,
    [FromServices] IQuestionService questionService,
    CancellationToken ct)
{
    var questions = await questionService.GetRandomQuestionsAsync(
        count, category, difficulty, ct);
    
    return Results.Ok(new { questions = questions.ToList() });
}
```

**Database Schema:**
```csharp
// Already exists - verify in Synaptix.Backend.Domain/Entities/Question.cs
public class Question : Entity
{
    public string Text { get; set; }
    public List<QuestionOption> Options { get; set; }
    public string Category { get; set; }
    public string Difficulty { get; set; }
    public string CorrectOptionId { get; set; }
}
```

**Implementation Time:** 2-4 hours

---

#### 4.2 Verify Match Join/Leave Endpoints
**Endpoints:**
- `POST /matches/{matchId}/join`
- `POST /matches/{matchId}/leave`

**Decision:**
- ✅ Keep if actively used
- ❌ Deprecate if superseded by `/matchmaking/queue`

**Recommendation:** Mark as deprecated with redirect to matchmaking queue

**Implementation Time:** 1-2 hours (documentation only)

---

### Priority 2: HIGH (Week 2-3)

#### 4.3 User Achievements System
**Status:** Deprecated in Flutter client but may be needed for future use

**To Implement:**
```csharp
// Achievement entity
public class Achievement : Entity
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string IconUrl { get; set; }
    public AchievementCategory Category { get; set; }
    public int Points { get; set; }
}

public class UserAchievement : Entity
{
    public Guid UserId { get; set; }
    public Guid AchievementId { get; set; }
    public DateTimeOffset UnlockedAt { get; set; }
}
```

**Endpoints Needed:**
```
GET /users/{userId}/achievements
POST /users/{userId}/achievements/{achievementId}/unlock
GET /users/{userId}/achievements/{achievementId}/progress
```

**Implementation Time:** 8-12 hours

---

### Priority 3: MEDIUM (Week 4)

#### 4.4 Enhanced Store Analytics
**Current:** Basic store endpoints exist  
**Missing:** Analytics breakdown by category, time range, user demographics

```csharp
// New admin endpoint for store analytics
GET /admin/store/analytics?startDate=X&endDate=Y&category=Z

Response:
{
    "totalRevenue": 15000,
    "totalPurchases": 234,
    "topItems": [...],
    "categoryBreakdown": {...},
    "playerDemographics": {...}
}
```

**Implementation Time:** 6-10 hours

---

#### 4.5 Enhanced Moderation Tools
**Current:** Basic ban/unban exists  
**Missing:**
- Temporary suspensions
- Appeal system
- Moderation appeal tracking

```csharp
public class ModerationAppeal : Entity
{
    public Guid UserId { get; set; }
    public string Reason { get; set; }
    public ModerationAppealStatus Status { get; set; }  // Pending, Approved, Rejected
    public string? ReviewerNotes { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
}

// Endpoints:
POST /admin/moderation/appeals
GET /admin/moderation/appeals/{appealId}
PATCH /admin/moderation/appeals/{appealId}
```

**Implementation Time:** 8-12 hours

---

## 5. Missing Backend Components

### 5.1 Achievement Service (Not Yet Implemented)
**Location:** `Synaptix.Backend.Application/Achievements/IAchievementService.cs`

```csharp
public interface IAchievementService
{
    Task<Achievement> GetAchievementAsync(Guid achievementId, CancellationToken ct);
    Task<List<Achievement>> GetUserAchievementsAsync(Guid userId, CancellationToken ct);
    Task UnlockAchievementAsync(Guid userId, Guid achievementId, CancellationToken ct);
    Task<AchievementProgress> GetAchievementProgressAsync(
        Guid userId, Guid achievementId, CancellationToken ct);
}
```

**Time to Implement:** 10-15 hours

---

### 5.2 Appeal Management Service (Not Yet Implemented)
**Location:** `Synaptix.Backend.Application/Moderation/IAppealService.cs`

```csharp
public interface IAppealService
{
    Task<ModerationAppeal> SubmitAppealAsync(
        Guid userId, string reason, CancellationToken ct);
    Task<ModerationAppeal> ReviewAppealAsync(
        Guid appealId, ModerationAppealStatus status, string notes, CancellationToken ct);
    Task<List<ModerationAppeal>> GetPendingAppealsAsync(CancellationToken ct);
}
```

**Time to Implement:** 8-12 hours

---

### 5.3 Enhanced Audit Logging (Partial)
**Status:** Basic audit logging exists  
**Enhancement:** Add audit trail for administrative actions

```csharp
public class AdminAuditLog : Entity
{
    public Guid AdminId { get; set; }
    public string Action { get; set; }  // BanUser, UnbanUser, ResetPassword, etc.
    public string ResourceType { get; set; }  // User, Question, StoreItem, etc.
    public Guid? ResourceId { get; set; }
    public Dictionary<string, object> ChangesBefore { get; set; }
    public Dictionary<string, object> ChangesAfter { get; set; }
    public string IpAddress { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
```

**Time to Implement:** 6-8 hours

---

### 5.4 Batch Operations Service (Not Yet Implemented)
**For:** Bulk player actions (ban, reset, reward distribution)

```csharp
public interface IBatchOperationService
{
    Task<BatchOperationResult> BulkBanPlayersAsync(
        List<Guid> playerIds, string reason, DateTime? until, CancellationToken ct);
    
    Task<BatchOperationResult> BulkRewardPlayersAsync(
        List<Guid> playerIds, Dictionary<string, int> rewards, CancellationToken ct);
    
    Task<BatchOperationResult> BulkResetProgressAsync(
        List<Guid> playerIds, string scope, CancellationToken ct);  // scope: skills, economy, etc.
}
```

**Time to Implement:** 10-15 hours

---

## 6. Implementation Priority Matrix

| Component | Difficulty | Impact | Urgency | Est. Hours | Priority |
|-----------|-----------|--------|---------|-----------|----------|
| Quiz Endpoints Verification | Low | High | High | 3 | P1 |
| Match Join/Leave Deprecation | Low | Low | Low | 1 | P1 |
| Django Performance Optimization | Low | High | Medium | 16 | P1 |
| Achievement System | Medium | Medium | Low | 12 | P2 |
| Appeal System | Medium | Medium | Medium | 10 | P2 |
| Enhanced Analytics | Medium | Medium | Medium | 8 | P3 |
| Batch Operations | High | High | Low | 12 | P3 |
| Admin Audit Logs | Medium | High | Medium | 7 | P2 |

**Total Estimated Work:** 79-85 hours across 4 weeks

---

## 7. Deployment Order

### Week 1 (Critical)
1. ✅ Password recovery (already done)
2. Deploy to staging
3. Fix Django performance issues (low risk)
4. Verify quiz endpoints exist

### Week 2-3 (High Priority)
1. Implement achievements
2. Implement appeals system
3. Add audit logging enhancements
4. Update operator dashboard to show appeals

### Week 4+ (Medium Priority)
1. Batch operations
2. Enhanced analytics
3. Additional store features

---

## 8. Testing Strategy

### Unit Tests Required:
```csharp
[TestClass]
public class AchievementServiceTests
{
    [TestMethod]
    public async Task UnlockAchievement_WithValidId_Success()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var achievementId = Guid.NewGuid();
        
        // Act
        var result = await _service.UnlockAchievementAsync(userId, achievementId, CancellationToken.None);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(achievementId, result.AchievementId);
    }
}
```

### Integration Tests:
- Test complete quiz flow: fetch → submit → grade
- Test achievement unlock with reward distribution
- Test appeal submission through to resolution

### Performance Tests:
- Load test leaderboard queries with 100k+ records
- Profile admin dashboard with large audit logs
- Benchmark before/after optimization changes

---

## 9. Success Criteria

✅ **Week 1:**
- Password recovery fully functional
- Django performance improved by 40%+
- Quiz endpoints verified working

✅ **Week 2-3:**
- Achievement system live
- Appeals system live
- All endpoints tested with Flutter client

✅ **Week 4:**
- Batch operations available
- Analytics enhanced
- Zero performance regressions in production

---

## 10. Risks & Mitigation

| Risk | Severity | Mitigation |
|------|----------|-----------|
| Quiz endpoint compatibility | Medium | Verify with Flutter client early |
| Database migration downtime | High | Run migrations during off-peak |
| Performance regression | High | Benchmark before/after each change |
| Third-party API failures (email, payments) | Medium | Implement fallback/retry logic |

---

## 11. Reference Links

- [Password Recovery Implementation](PASSWORD_RECOVERY_IMPLEMENTATION.md)
- [Flutter API Client](C:\Users\lmxbl\StudioProjects\trivia_tycoon\lib\core\networking\synaptix_api_client.dart)
- [Backend Program.cs](c:\Users\lmxbl\Documents\TycoonTycoon_Backend\Synaptix.Backend.Api\Program.cs)

---

## Next Steps

1. **Review this document** - Team alignment on priorities
2. **Schedule verification calls** - Confirm quiz endpoint availability
3. **Create sprint tasks** - Break down each component
4. **Set up monitoring** - Benchmark current performance
5. **Begin Week 1 work** - Start with high-impact, low-risk items
