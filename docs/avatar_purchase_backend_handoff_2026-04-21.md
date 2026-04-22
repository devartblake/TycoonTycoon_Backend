# 3D Avatar Purchase Path — Backend Handoff

> **Audience:** Backend team
> **Date:** 2026-04-21
> **Base URL:** `http(s)://<host>:5000`
> **v1 prefix (MinIO asset routes):** `/v1`
> **OpenAPI docs:** `/swagger` (dev only)

---

## Overview

The Flutter premium store now ships a full 3D avatar purchase loop:
**Browse → Buy (coins) → Download (GLB archive) → Render (flutter\_3d\_controller)**.

Three backend endpoints are required to close this loop. The Flutter service layer is complete and waiting on these endpoints. This document provides the exact request/response contracts the Flutter client expects.

---

## Error Envelope

All error responses must use the existing nested envelope format:

```json
{
  "error": {
    "code": "error_code_snake_case",
    "message": "Human-readable message.",
    "details": {}
  }
}
```

Do **not** use a flat `{ "error": "string" }` format — the Flutter `ApiService` parses the nested structure.

---

## Authentication

All three endpoints require a valid bearer token in the `Authorization` header:

```
Authorization: Bearer <jwt>
```

The backend should reject unauthenticated requests with `401 Unauthorized` using the standard error envelope.

---

## Endpoint 1 — Avatar Catalog

### `GET /store/catalog`

This endpoint **already exists** and powers the main store screen. It needs to support filtering by `category=avatar` to return 3D avatar items.

#### Query Parameters

| Parameter  | Type   | Required | Description                              |
|------------|--------|----------|------------------------------------------|
| `category` | string | no       | When `avatar`, return only avatar items  |

#### Flutter call site

`lib/core/services/store/avatar_store_remote_source.dart`:

```dart
final response = await _apiService.get(
  '/store/catalog',
  queryParameters: <String, dynamic>{'category': 'avatar'},
);
final rawItems = response['items'] as List<dynamic>? ?? const <dynamic>[];
```

The Flutter parser reads `response['items']` — the response **must** be wrapped in `{"items": [...]}`.

#### Response `200`

```json
{
  "items": [
    {
      "id": "avatar-cartoon-hero",
      "sku": "avatar:cartoon-hero:v1",
      "name": "Cartoon Hero",
      "description": "A bold 3D cartoon character avatar",
      "price": 1200,
      "currency": "coins",
      "category": "avatar",
      "type": "cosmetic",
      "mediaKey": "avatars/cartoon-hero-v1",
      "thumbnailUrl": "https://<cdn>/avatars/cartoon-hero-v1/thumb.png",
      "owned": false,
      "isFeatured": true,
      "version": "1.0.0"
    }
  ]
}
```

#### Item field reference

| Field         | Type    | Required | Notes                                                       |
|---------------|---------|----------|-------------------------------------------------------------|
| `sku`         | string  | yes      | Used as the avatar package `id` in Flutter. Must be stable. |
| `name`        | string  | yes      | Display name shown in store card.                           |
| `price`       | integer | yes      | Coin price (e.g. `1200`).                                   |
| `currency`    | string  | yes      | Must be `"coins"` for in-app coin purchases.                |
| `thumbnailUrl`| string  | no       | CDN URL to a preview image. Shown before install.           |
| `owned`       | boolean | yes      | `true` if the authenticated player already purchased this.  |
| `version`     | string  | no       | Semver string (e.g. `"1.0.0"`). Defaults to `"1.0.0"`.     |
| `mediaKey`    | string  | no       | MinIO object key prefix for the archive (used by Endpoint 3).|
| `id`          | string  | no       | Internal DB id. Flutter falls back to `sku` if absent.      |

#### Flutter field mapping (`AvatarPackageMetadata`)

```
sku          → AvatarPackageMetadata.id
name         → AvatarPackageMetadata.name
version      → AvatarPackageMetadata.version
thumbnailUrl → AvatarPackageMetadata.thumbnailUrl
archiveUrl   → left null (filled on-demand by Endpoint 3)
render.kind  → AvatarPackageType.depthCard (hardcoded for category=avatar)
```

The `owned` flag on the catalog item controls the **Buy vs Install** button state. After a successful purchase (Endpoint 2), the client invalidates its provider cache and re-fetches this endpoint — the item must now return `"owned": true`.

---

## Endpoint 2 — Avatar Purchase

### `POST /store/avatars/{avatarId}/purchase`

Deducts coins from the player's wallet and records ownership of the avatar in the player's inventory.

#### Path Parameters

| Parameter  | Type   | Description                                          |
|------------|--------|------------------------------------------------------|
| `avatarId` | string | The avatar SKU (e.g. `avatar:cartoon-hero:v1`).      |

#### Request Body

```json
{
  "playerId": "00000000-0000-0000-0000-000000000000"
}
```

| Field      | Type   | Required | Description                     |
|------------|--------|----------|---------------------------------|
| `playerId` | UUID   | yes      | Authenticated player's UUID.    |

The backend must validate that `playerId` matches the JWT subject to prevent purchasing on behalf of another player.

#### Flutter call site

`lib/core/services/store/store_service.dart`:

```dart
Future<Map<String, dynamic>> purchaseAvatar({
  required String playerId,
  required String avatarId,
}) {
  return apiService.post(
    '/store/avatars/$avatarId/purchase',
    body: <String, dynamic>{'playerId': playerId},
  );
}
```

`lib/screens/store/widgets/try_now_widget.dart`:

```dart
final response = await ref.read(storeServiceProvider).purchaseAvatar(
  playerId: playerId,
  avatarId: meta.id,
);
final newBalance = (response['newBalance'] as num?)?.toInt();
if (newBalance != null) {
  await ref.read(coinBalanceProvider.notifier).set(newBalance);
}
ref.invalidate(serverAvatarPackagesProvider);
```

After a successful purchase, the client:
1. Updates the displayed coin balance from `newBalance`
2. Refetches the avatar catalog (Endpoint 1) — the item must now show `owned: true`

#### Response `200` — success

```json
{
  "success": true,
  "avatarId": "avatar:cartoon-hero:v1",
  "coinsDeducted": 1200,
  "newBalance": 840
}
```

| Field           | Type    | Required | Description                                  |
|-----------------|---------|----------|----------------------------------------------|
| `success`       | boolean | yes      | Always `true` on 200.                        |
| `avatarId`      | string  | yes      | Echo of the purchased avatar SKU.            |
| `coinsDeducted` | integer | yes      | Amount deducted from the player's wallet.    |
| `newBalance`    | integer | yes      | Player's coin balance after the transaction. |

#### Response `409` — insufficient funds

```json
{
  "error": {
    "code": "insufficient_funds",
    "message": "Not enough coins to purchase this avatar.",
    "details": {
      "required": 1200,
      "available": 400
    }
  }
}
```

#### Response `409` — already owned

```json
{
  "error": {
    "code": "already_owned",
    "message": "Player already owns this avatar.",
    "details": {}
  }
}
```

#### Response `404` — avatar not found

```json
{
  "error": {
    "code": "avatar_not_found",
    "message": "Avatar avatar:cartoon-hero:v1 does not exist in the catalog.",
    "details": {}
  }
}
```

#### Backend implementation notes

- Deduct coins via the existing `PlayerWallet` / `PlayerTransaction` table pattern used by other coin purchases.
- Record ownership in a `PlayerInventory` (or equivalent) table with `playerId`, `sku`, `purchasedAtUtc`.
- The purchase must be atomic — if coin deduction fails, inventory must not be updated.
- Idempotency: if the player already owns the avatar, return `409 already_owned` rather than charging again.

---

## Endpoint 3 — MinIO Presigned Download URL

### `GET /v1/assets/avatars/{avatarId}`

Returns a short-lived presigned URL for the avatar's `.zip` archive stored in MinIO. This mirrors the existing audio asset endpoint at `GET /v1/assets/audio/{category}/{filename}`.

The Flutter client calls this **only** when the player taps "Install" (i.e. already owns the avatar). The presigned URL is then passed to `AvatarPackageService.downloadAndInstall()` which handles download, extraction to device storage, and manifest writing.

#### Path Parameters

| Parameter  | Type   | Description                                         |
|------------|--------|-----------------------------------------------------|
| `avatarId` | string | The avatar SKU (e.g. `avatar:cartoon-hero:v1`).     |

#### Flutter call site

`lib/core/services/store/avatar_asset_service.dart`:

```dart
// GET /v1/assets/avatars/{avatarId}
final json = await _client.getJson('/v1/assets/avatars/$avatarId');
final response = AvatarAssetResponse.fromJson(json);
```

`AvatarAssetResponse.fromJson` reads exactly these fields:

```dart
factory AvatarAssetResponse.fromJson(Map<String, dynamic> json) {
  return AvatarAssetResponse(
    presignedUrl:   json['presignedUrl'] as String,
    thumbnailUrl:   json['thumbnailUrl'] as String?,
    expiresAt:      DateTime.parse(json['expiresAt'] as String),
    sha256:         json['sha256'] as String?,
    archiveFormat:  (json['archiveFormat'] as String?) ?? 'zip',
  );
}
```

The client maintains a 2-minute expiry buffer cache — if the URL expires within 2 minutes of the current time, a fresh request is made. Set `expiresAt` to at least 10 minutes in the future to allow for slow connections.

#### Response `200`

```json
{
  "presignedUrl": "https://minio.<host>/trivia-tycoon/avatars/cartoon-hero-v1.zip?X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=...&X-Amz-Signature=...",
  "thumbnailUrl": "https://minio.<host>/trivia-tycoon/avatars/cartoon-hero-v1/thumb.png",
  "expiresAt": "2026-04-21T12:00:00Z",
  "contentType": "application/zip",
  "archiveFormat": "zip",
  "sha256": "a1b2c3d4e5f6..."
}
```

| Field           | Type   | Required | Description                                                    |
|-----------------|--------|----------|----------------------------------------------------------------|
| `presignedUrl`  | string | yes      | Short-lived MinIO presigned URL to the `.zip` archive.         |
| `thumbnailUrl`  | string | no       | CDN or presigned URL to thumbnail image. May match catalog URL.|
| `expiresAt`     | string | yes      | ISO 8601 UTC datetime when `presignedUrl` expires.             |
| `contentType`   | string | no       | `"application/zip"` (informational).                           |
| `archiveFormat` | string | no       | `"zip"` (default). Flutter supports zip/tar.gz/tgz.           |
| `sha256`        | string | no       | Hex SHA-256 of the archive for integrity checking.             |

#### Backend must validate ownership

Before generating a presigned URL, verify the authenticated player owns the avatar (check `PlayerInventory`). Return `403 Forbidden` if they do not:

```json
{
  "error": {
    "code": "not_owned",
    "message": "Player does not own this avatar.",
    "details": {}
  }
}
```

#### MinIO archive structure

The `.zip` archive must contain the following layout for the Flutter installer to resolve files correctly:

```
cartoon-hero-v1.zip
├── manifest.json          ← required: package manifest
├── models/
│   └── avatar.glb         ← required: primary 3D model (GLB format)
├── images/                ← optional: 2D preview images
│   └── preview.png
└── previews/
    └── cover.png          ← optional: additional preview assets
```

`manifest.json` format (must match `AvatarPackageManifest`):

```json
{
  "id": "avatar:cartoon-hero:v1",
  "name": "Cartoon Hero",
  "version": "1.0.0",
  "description": "A bold 3D cartoon character avatar",
  "kind": "model3d",
  "thumbnail": "previews/cover.png",
  "tags": ["3d", "cartoon", "hero"],
  "imagesDir": "images",
  "modelsDir": "models"
}
```

The Flutter installer reads `installDir/models/avatar.glb` to render via `Flutter3DViewer`. The path is built as:

```dart
String _glbPath(AvatarPackageInstall install) {
  final sep = install.installDir.endsWith('/') ? '' : '/';
  return '${install.installDir}${sep}models/avatar.glb';
}
```

If the model file is named differently, the Flutter renderer will silently fail. **The file must be `models/avatar.glb` inside the archive.**

#### MinIO object naming convention

Recommended object key pattern (mirrors audio pattern):

```
trivia-tycoon/
  avatars/
    cartoon-hero-v1.zip       ← archive
    cartoon-hero-v1/
      thumb.png               ← thumbnail
```

The `mediaKey` field in Endpoint 1 can carry the key prefix (`avatars/cartoon-hero-v1`) to help the backend locate the object without parsing the SKU.

---

## Flutter Integration — Full Flow Diagram

```
Player taps "Buy"
  └─ POST /store/avatars/{avatarId}/purchase
       ├─ 200 → coinBalanceProvider updated
       │         serverAvatarPackagesProvider invalidated
       │         catalog re-fetched → item.owned = true → button shows "Install"
       └─ 409 → snackbar error displayed

Player taps "Install"
  └─ GET /v1/assets/avatars/{avatarId}
       └─ 200 → presignedUrl passed to AvatarPackageService.downloadAndInstall()
                  ├─ HTTP GET presignedUrl → downloads archive from MinIO
                  ├─ extracts to device storage
                  └─ writes local manifest.json
                     installedAvatarPackagesProvider invalidated
                     button shows "Equip"

Player taps "Equip"
  └─ navigates to /avatar-select (local, no network call)
       └─ Flutter3DViewer renders installDir/models/avatar.glb
```

---

## Implementation Order

1. **Endpoint 3 first** — `GET /v1/assets/avatars/{avatarId}`. This is the simplest new endpoint (pure MinIO presigning). Validate by uploading a test `.zip` and confirming the presigned URL works end-to-end.
2. **Endpoint 1 filter** — Add `category=avatar` filtering to the existing `/store/catalog`. Upload at least one avatar item to the catalog DB with the correct field set (including `sku`, `owned`, `thumbnailUrl`).
3. **Endpoint 2** — `POST /store/avatars/{avatarId}/purchase`. Add the wallet deduction + inventory record + ownership flag update logic.
4. **Ownership gate on Endpoint 3** — Once Endpoint 2 is live, add the ownership check to Endpoint 3.

---

## Verification Checklist

| Check | Expected result |
|---|---|
| `GET /store/catalog?category=avatar` | Returns `{"items": [...]}` with avatar items; `sku` present on each |
| Item `owned: false` before purchase | Flutter shows "Buy — Coins" button |
| `POST /store/avatars/{avatarId}/purchase` with sufficient coins | `200 {success, newBalance}`; coin badge in app bar updates |
| `GET /store/catalog?category=avatar` after purchase | Same item now has `owned: true`; Flutter shows "Install" button |
| `POST /store/avatars/{avatarId}/purchase` when already owned | `409 already_owned` |
| `POST /store/avatars/{avatarId}/purchase` with insufficient coins | `409 insufficient_funds` |
| `GET /v1/assets/avatars/{avatarId}` as owner | `200 {presignedUrl, expiresAt, archiveFormat: "zip"}` |
| `GET /v1/assets/avatars/{avatarId}` as non-owner | `403 not_owned` |
| Presigned URL download | Returns a valid `.zip` with `models/avatar.glb` inside |
| Flutter install flow | `Flutter3DViewer` renders avatar after extraction |

---

## Related Flutter Files

| File | Role |
|---|---|
| `lib/core/services/store/avatar_store_remote_source.dart` | Calls Endpoint 1; maps items to `AvatarPackageMetadata` |
| `lib/core/services/store/avatar_asset_service.dart` | Calls Endpoint 3; caches presigned URLs with 2-min expiry buffer |
| `lib/core/services/store/store_service.dart` | `purchaseAvatar()` calls Endpoint 2 |
| `lib/game/providers/avatar_package_providers.dart` | `serverAvatarPackagesProvider`, `installedAvatarPackagesProvider`, `avatarAssetServiceProvider` |
| `lib/screens/store/widgets/try_now_widget.dart` | Full buy/install/equip state machine; renders `Flutter3DViewer` |
| `lib/game/services/avatar_package_service.dart` | Download + extract + install pipeline; reads presigned URL from metadata |
| `lib/game/models/avatar_package_models.dart` | `AvatarPackageMetadata`, `AvatarPackageManifest`, `AvatarPackageInstall` |
