# Spin & Earn Backend API Handoff — .NET 10

**Project:** Trivia Tycoon / Synaptix Arcade Spin & Earn  
**Frontend consumer:** Flutter `WheelScreen` / `SpinWheelApiService`  
**Backend target:** .NET 10 Web API / Minimal API style  
**Status:** Ready for backend implementation

---

## 1. Purpose

The Flutter `WheelScreen` already contains integration hooks for a backend-powered Spin & Earn system. The backend needs to provide two initial endpoints:

```http
GET  /arcade/spin/segments
POST /arcade/spin/claim
```

The current frontend expects:

- `GET /arcade/spin/segments` to return a JSON array of `WheelSegment` objects.
- `POST /arcade/spin/claim` to accept `{ playerId, segmentId, spinId }` and return `{ success, coinsGranted, newBalance, message }`.

The backend should own reward validation, idempotency, coin balance updates, and claim history.

---

## 2. Current Frontend Expectations

### Flutter service path

```dart
lib/core/services/arcade/spin_wheel_api_service.dart
```

### Existing Flutter service behavior

```dart
Future<List<WheelSegment>> fetchSegments({String? playerId})
```

Calls:

```http
GET /arcade/spin/segments
```

Optional query:

```http
?playerId=<playerId>
```

```dart
Future<SpinClaimResponse> claimReward({
  required String playerId,
  required String segmentId,
  required String spinId,
})
```

Calls:

```http
POST /arcade/spin/claim
```

Body:

```json
{
  "playerId": "player_123",
  "segmentId": "gold_chest",
  "spinId": "spin_1710000000000"
}
```

---

## 3. API Contract

## 3.1 GET `/arcade/spin/segments`

Returns the current wheel configuration.

### Request

```http
GET /arcade/spin/segments
```

Optional:

```http
GET /arcade/spin/segments?playerId=player_123
```

### Response `200 OK`

```json
[
  {
    "id": "gold_chest",
    "label": "Gold Chest",
    "color": "#FFD700",
    "imagePath": "assets/images/rewards/gold.png",
    "rewardType": "coins",
    "reward": 250,
    "isExclusive": false,
    "requiredStreak": 0,
    "requiredCurrency": 0,
    "description": "Win 250 coins.",
    "probability": 1.0,
    "metadata": {},
    "isEnabled": true,
    "enabledUntil": null
  },
  {
    "id": "exclusive_diamond",
    "label": "Exclusive Diamond",
    "color": "#B9F2FF",
    "imagePath": "assets/images/rewards/diamond.png",
    "rewardType": "coins",
    "reward": 500,
    "isExclusive": true,
    "requiredStreak": 3,
    "requiredCurrency": 100,
    "description": "Exclusive high-value reward.",
    "probability": 0.25,
    "metadata": {
      "requiredStreak": 3,
      "requiredCurrency": 100
    },
    "isEnabled": true,
    "enabledUntil": null
  }
]
```

### Required fields

| Field | Type | Notes |
|---|---:|---|
| `id` | string | Required. Must be stable and unique. Used as `segmentId` during claim. |
| `label` | string | Display label. |
| `color` | string | Hex color. Flutter accepts `#RRGGBB` or `#AARRGGBB`. |
| `imagePath` | string? | Flutter asset path or remote asset URL later. |
| `rewardType` | string | Recommended values: `coins`, `gems`, `powerup`, `jackpot`, `bonus`. |
| `reward` | int | Reward amount. For this first version, treat as coin amount. |
| `isExclusive` | bool | Whether frontend should apply unlock rules. |
| `requiredStreak` | int | Required win/spin streak for exclusive segment. |
| `requiredCurrency` | int | Required premium/exclusive currency for exclusive segment. |
| `description` | string? | Optional display text. |
| `probability` | double | Future server-side weighting support. |
| `metadata` | object? | Optional extensible field. |
| `isEnabled` | bool | Disabled segments should not be claimable. |
| `enabledUntil` | datetime? | Optional expiration timestamp. |

---

## 3.2 POST `/arcade/spin/claim`

Claims the selected segment reward after the spin animation completes.

### Request

```http
POST /arcade/spin/claim
Content-Type: application/json
```

```json
{
  "playerId": "player_123",
  "segmentId": "gold_chest",
  "spinId": "spin_1710000000000"
}
```

### Response `200 OK`

```json
{
  "success": true,
  "coinsGranted": 250,
  "newBalance": 1450,
  "message": "Reward claimed."
}
```

### Error responses

#### `400 Bad Request`

Invalid request or invalid segment.

```json
{
  "success": false,
  "message": "playerId, segmentId, and spinId are required."
}
```

#### `409 Conflict`

Duplicate `spinId` claim.

```json
{
  "success": false,
  "message": "Spin reward already claimed."
}
```

#### `429 Too Many Requests`

Cooldown or daily limit violation.

```json
{
  "success": false,
  "message": "Spin cooldown has not expired."
}
```

---

## 4. Important Implementation Rules

### 4.1 `spinId` must be idempotent

The backend must enforce a unique constraint on `spinId`.

Reason:

- Prevents duplicate reward claims from retries.
- Protects against double taps, network retries, and replay attempts.

### 4.2 `segmentId` must be validated server-side

Do not trust the client-provided `segmentId` blindly.

The backend must verify:

- Segment exists.
- Segment is enabled.
- Segment has not expired.
- Player is eligible if the segment is exclusive.
- Reward amount comes from backend catalog/database, not from client request.

### 4.3 Reward value must come from backend

The request body should never include `reward` or `coinsGranted`.

Correct:

```json
{
  "playerId": "player_123",
  "segmentId": "gold_chest",
  "spinId": "spin_1710000000000"
}
```

Incorrect:

```json
{
  "playerId": "player_123",
  "segmentId": "gold_chest",
  "reward": 999999
}
```

### 4.4 Recommended future improvement

The current frontend decides where the wheel lands, then submits the segment to claim. For MVP this is acceptable, but the secure long-term version should add:

```http
POST /arcade/spin/start
```

That endpoint should:

1. Validate cooldown.
2. Generate `spinId` server-side.
3. Select winning segment server-side using weighted probability.
4. Return `{ spinId, winningSegmentId, segments }` or a signed spin result token.
5. Let the frontend animate to that result.
6. Then claim using `spinId` only or a signed result token.

---

# 5. .NET 10 Implementation

The following implementation uses .NET 10 Minimal API conventions and C# record/class syntax.

Recommended folder:

```text
src/Tycoon.Api/Features/ArcadeSpin/
  ArcadeSpinEndpoints.cs
  SpinSegmentDto.cs
  SpinClaimRequest.cs
  SpinClaimResponse.cs
  SpinClaim.cs
  PlayerWallet.cs
  SpinRewardCatalog.cs
  ArcadeSpinDbContextExtensions.cs
```

Adjust namespaces to match the backend repo.

---

## 5.1 `SpinSegmentDto.cs`

```csharp
namespace Tycoon.Api.Features.ArcadeSpin;

public sealed record SpinSegmentDto(
    string Id,
    string Label,
    string Color,
    string? ImagePath,
    string RewardType,
    int Reward,
    bool IsExclusive,
    int RequiredStreak,
    int RequiredCurrency,
    string? Description,
    double Probability,
    Dictionary<string, object>? Metadata,
    bool IsEnabled,
    DateTimeOffset? EnabledUntil
);
```

---

## 5.2 `SpinClaimRequest.cs`

```csharp
namespace Tycoon.Api.Features.ArcadeSpin;

public sealed record SpinClaimRequest(
    string PlayerId,
    string SegmentId,
    string SpinId
);
```

---

## 5.3 `SpinClaimResponse.cs`

```csharp
namespace Tycoon.Api.Features.ArcadeSpin;

public sealed record SpinClaimResponse(
    bool Success,
    int CoinsGranted,
    int NewBalance,
    string? Message
);
```

---

## 5.4 `SpinClaim.cs`

```csharp
namespace Tycoon.Api.Features.ArcadeSpin;

public sealed class SpinClaim
{
    public Guid Id { get; set; }

    public string PlayerId { get; set; } = string.Empty;

    public string SegmentId { get; set; } = string.Empty;

    public string SpinId { get; set; } = string.Empty;

    public int CoinsGranted { get; set; }

    public DateTimeOffset ClaimedAtUtc { get; set; }
}
```

---

## 5.5 `PlayerWallet.cs`

```csharp
namespace Tycoon.Api.Features.ArcadeSpin;

public sealed class PlayerWallet
{
    public string PlayerId { get; set; } = string.Empty;

    public int CoinBalance { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
```

---

## 5.6 `SpinRewardCatalog.cs`

For MVP, this can be an in-memory catalog. Later, move these segments into PostgreSQL with admin controls.

```csharp
namespace Tycoon.Api.Features.ArcadeSpin;

public static class SpinRewardCatalog
{
    private static readonly IReadOnlyList<SpinSegmentDto> Segments =
    [
        new(
            Id: "gold_chest",
            Label: "Gold Chest",
            Color: "#FFD700",
            ImagePath: "assets/images/rewards/gold.png",
            RewardType: "coins",
            Reward: 250,
            IsExclusive: false,
            RequiredStreak: 0,
            RequiredCurrency: 0,
            Description: "Win 250 coins.",
            Probability: 1.0,
            Metadata: new Dictionary<string, object>(),
            IsEnabled: true,
            EnabledUntil: null
        ),
        new(
            Id: "exclusive_diamond",
            Label: "Exclusive Diamond",
            Color: "#B9F2FF",
            ImagePath: "assets/images/rewards/diamond.png",
            RewardType: "coins",
            Reward: 500,
            IsExclusive: true,
            RequiredStreak: 3,
            RequiredCurrency: 100,
            Description: "Exclusive high-value reward.",
            Probability: 0.25,
            Metadata: new Dictionary<string, object>
            {
                ["requiredStreak"] = 3,
                ["requiredCurrency"] = 100
            },
            IsEnabled: true,
            EnabledUntil: null
        )
    ];

    public static IReadOnlyList<SpinSegmentDto> GetEnabledSegments()
    {
        var now = DateTimeOffset.UtcNow;

        return Segments
            .Where(segment =>
                segment.IsEnabled &&
                (segment.EnabledUntil is null || segment.EnabledUntil > now))
            .ToList();
    }

    public static SpinSegmentDto? Find(string segmentId)
    {
        return Segments.FirstOrDefault(segment =>
            string.Equals(segment.Id, segmentId, StringComparison.OrdinalIgnoreCase));
    }
}
```

---

## 5.7 `ArcadeSpinEndpoints.cs`

```csharp
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Tycoon.Api.Features.ArcadeSpin;

public static class ArcadeSpinEndpoints
{
    public static IEndpointRouteBuilder MapArcadeSpinEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/arcade/spin")
            .WithTags("Arcade Spin")
            .WithOpenApi();

        group.MapGet("/segments", GetSegments)
            .WithName("GetSpinSegments")
            .Produces<IReadOnlyList<SpinSegmentDto>>(StatusCodes.Status200OK);

        group.MapPost("/claim", ClaimReward)
            .WithName("ClaimSpinReward")
            .Produces<SpinClaimResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status429TooManyRequests);

        return app;
    }

    private static Ok<IReadOnlyList<SpinSegmentDto>> GetSegments(string? playerId = null)
    {
        // TODO: Use playerId for personalized segment filtering later.
        var segments = SpinRewardCatalog.GetEnabledSegments();
        return TypedResults.Ok(segments);
    }

    private static async Task<IResult> ClaimReward(
        SpinClaimRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PlayerId) ||
            string.IsNullOrWhiteSpace(request.SegmentId) ||
            string.IsNullOrWhiteSpace(request.SpinId))
        {
            return TypedResults.BadRequest(new
            {
                success = false,
                message = "playerId, segmentId, and spinId are required."
            });
        }

        var alreadyClaimed = await db.Set<SpinClaim>()
            .AnyAsync(claim => claim.SpinId == request.SpinId, cancellationToken);

        if (alreadyClaimed)
        {
            return TypedResults.Conflict(new
            {
                success = false,
                message = "Spin reward already claimed."
            });
        }

        var segment = SpinRewardCatalog.Find(request.SegmentId);

        if (segment is null)
        {
            return TypedResults.BadRequest(new
            {
                success = false,
                message = "Invalid spin segment."
            });
        }

        if (!segment.IsEnabled ||
            segment.EnabledUntil is not null && segment.EnabledUntil <= DateTimeOffset.UtcNow)
        {
            return TypedResults.BadRequest(new
            {
                success = false,
                message = "Spin segment is disabled or expired."
            });
        }

        // TODO: Add cooldown validation here.
        // TODO: Add exclusive segment eligibility validation here.
        // TODO: Add anti-cheat/enforcement checks here.

        var wallet = await db.Set<PlayerWallet>()
            .FirstOrDefaultAsync(wallet => wallet.PlayerId == request.PlayerId, cancellationToken);

        if (wallet is null)
        {
            wallet = new PlayerWallet
            {
                PlayerId = request.PlayerId,
                CoinBalance = 0,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            };

            db.Set<PlayerWallet>().Add(wallet);
        }

        wallet.CoinBalance += segment.Reward;
        wallet.UpdatedAtUtc = DateTimeOffset.UtcNow;

        var claim = new SpinClaim
        {
            Id = Guid.NewGuid(),
            PlayerId = request.PlayerId,
            SegmentId = request.SegmentId,
            SpinId = request.SpinId,
            CoinsGranted = segment.Reward,
            ClaimedAtUtc = DateTimeOffset.UtcNow
        };

        db.Set<SpinClaim>().Add(claim);

        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new SpinClaimResponse(
            Success: true,
            CoinsGranted: segment.Reward,
            NewBalance: wallet.CoinBalance,
            Message: "Reward claimed."
        ));
    }
}
```

---

## 5.8 `ArcadeSpinDbContextExtensions.cs`

If the backend uses centralized EF configuration, place this inside the relevant entity configuration files instead.

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tycoon.Api.Features.ArcadeSpin;

public static class ArcadeSpinDbContextExtensions
{
    public static ModelBuilder ApplyArcadeSpinConfiguration(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SpinClaim>(ConfigureSpinClaim);
        modelBuilder.Entity<PlayerWallet>(ConfigurePlayerWallet);

        return modelBuilder;
    }

    private static void ConfigureSpinClaim(EntityTypeBuilder<SpinClaim> entity)
    {
        entity.ToTable("arcade_spin_claims");

        entity.HasKey(claim => claim.Id);

        entity.HasIndex(claim => claim.SpinId)
            .IsUnique();

        entity.HasIndex(claim => new
        {
            claim.PlayerId,
            claim.ClaimedAtUtc
        });

        entity.Property(claim => claim.PlayerId)
            .HasMaxLength(100)
            .IsRequired();

        entity.Property(claim => claim.SegmentId)
            .HasMaxLength(100)
            .IsRequired();

        entity.Property(claim => claim.SpinId)
            .HasMaxLength(100)
            .IsRequired();

        entity.Property(claim => claim.CoinsGranted)
            .IsRequired();

        entity.Property(claim => claim.ClaimedAtUtc)
            .IsRequired();
    }

    private static void ConfigurePlayerWallet(EntityTypeBuilder<PlayerWallet> entity)
    {
        entity.ToTable("player_wallets");

        entity.HasKey(wallet => wallet.PlayerId);

        entity.Property(wallet => wallet.PlayerId)
            .HasMaxLength(100)
            .IsRequired();

        entity.Property(wallet => wallet.CoinBalance)
            .IsRequired();

        entity.Property(wallet => wallet.UpdatedAtUtc)
            .IsRequired();
    }
}
```

---

## 5.9 `AppDbContext` integration

Add DbSets if the backend uses explicit DbSet properties:

```csharp
public DbSet<SpinClaim> SpinClaims => Set<SpinClaim>();
public DbSet<PlayerWallet> PlayerWallets => Set<PlayerWallet>();
```

Inside `OnModelCreating`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    modelBuilder.ApplyArcadeSpinConfiguration();
}
```

---

## 5.10 `Program.cs` integration

Add the endpoint registration after middleware setup and before `app.Run()`:

```csharp
app.MapArcadeSpinEndpoints();
```

If the API uses versioned grouping, mount it accordingly:

```csharp
var api = app.MapGroup("/api/v1");
api.MapArcadeSpinEndpoints();
```

If mounted under `/api/v1`, the Flutter base URL must already include `/api/v1`, or the service path must be adjusted.

---

# 6. EF Core Migration

Generate migration:

```bash
dotnet ef migrations add AddArcadeSpinClaimsAndWallets \
  --project src/Tycoon.Infrastructure \
  --startup-project src/Tycoon.Api
```

Apply migration:

```bash
dotnet ef database update \
  --project src/Tycoon.Infrastructure \
  --startup-project src/Tycoon.Api
```

Adjust project paths to match the backend repository.

---

# 7. Frontend Compatibility Patch

The Flutter `ApiService` currently protects several paths for token injection, but `/arcade` should also be protected.

Patch:

```dart
bool _isProtectedPath(String path) {
  if (path == '/admin' || path.startsWith('/admin/')) return true;
  if (path == '/store' || path.startsWith('/store/')) return true;
  if (path == '/crypto' || path.startsWith('/crypto/')) return true;

  // Add this line:
  if (path == '/arcade' || path.startsWith('/arcade/')) return true;

  if (path == '/users/me' || path.startsWith('/users/me/')) return true;
  if (path == '/users/search' || path.startsWith('/users/search/')) return true;
  if (path == '/friends' || path.startsWith('/friends/')) return true;
  if (path == '/profile' || path.startsWith('/profile/')) return true;
  if (path == '/auth/profile' || path.startsWith('/auth/profile/')) return true;
  if (path == '/user/profile' || path.startsWith('/user/profile/')) return true;

  return false;
}
```

---

# 8. Recommended Backend Tests

## 8.1 `GET /arcade/spin/segments`

Verify:

- Returns `200 OK`.
- Returns JSON array.
- Every item has non-empty `id`.
- Every item has non-empty `label`.
- Every enabled item has `reward >= 0`.

## 8.2 `POST /arcade/spin/claim`

Verify:

- Valid claim returns `success: true`.
- Coin balance increases by backend segment reward.
- Duplicate `spinId` returns `409 Conflict`.
- Missing `playerId`, `segmentId`, or `spinId` returns `400 Bad Request`.
- Invalid `segmentId` returns `400 Bad Request`.
- Disabled segment cannot be claimed.

---

# 9. Suggested GitHub Issues

## Issue 1 — Add Arcade Spin segment endpoint

**Title:** Add `GET /arcade/spin/segments` endpoint  
**Labels:** `backend`, `arcade`, `api`, `spin-wheel`  
**Acceptance criteria:**

- Returns Flutter-compatible `WheelSegment[]`.
- Every segment includes stable `id`.
- Disabled/expired segments are excluded or marked correctly.
- Endpoint is documented in OpenAPI.

## Issue 2 — Add Arcade Spin reward claim endpoint

**Title:** Add `POST /arcade/spin/claim` endpoint  
**Labels:** `backend`, `arcade`, `economy`, `api`  
**Acceptance criteria:**

- Accepts `{ playerId, segmentId, spinId }`.
- Validates segment server-side.
- Grants coins using backend reward value.
- Returns `{ success, coinsGranted, newBalance, message }`.
- Duplicate `spinId` is rejected.

## Issue 3 — Add spin claim persistence and wallet updates

**Title:** Persist spin claims and update player wallet balance  
**Labels:** `backend`, `database`, `ef-core`, `economy`  
**Acceptance criteria:**

- Adds `arcade_spin_claims` table.
- Adds or reuses player wallet/economy balance table.
- Adds unique index on `spinId`.
- Adds EF migration.

## Issue 4 — Add cooldown and anti-cheat validation

**Title:** Add cooldown validation and anti-cheat guardrails for spin claims  
**Labels:** `backend`, `anti-cheat`, `arcade`, `security`  
**Acceptance criteria:**

- Prevents claiming before cooldown expires.
- Enforces daily spin limits.
- Records suspicious duplicate/replay attempts.
- Returns `429` for cooldown violations.

## Issue 5 — Backend-controlled spin result

**Title:** Add secure server-controlled spin start flow  
**Labels:** `backend`, `security`, `arcade`, `future`  
**Acceptance criteria:**

- Adds `POST /arcade/spin/start`.
- Backend generates `spinId`.
- Backend selects result using weighted probability.
- Frontend receives winning segment for animation.
- Claim endpoint no longer trusts client-selected `segmentId`.

---

# 10. MVP Definition of Done

The backend work is complete when:

- `GET /arcade/spin/segments` returns valid Flutter-compatible segments.
- `POST /arcade/spin/claim` grants coins and returns the new balance.
- Duplicate `spinId` claims are blocked.
- Invalid or disabled segments are blocked.
- Claim data is persisted.
- Wallet balance is persisted.
- Swagger/OpenAPI shows both endpoints.
- Flutter `WheelScreen` can spin, claim, and update coin balance without local-only reward logic.

