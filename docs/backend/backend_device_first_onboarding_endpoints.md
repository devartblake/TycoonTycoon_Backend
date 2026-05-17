# Backend Contract: Device-First Onboarding And Account Rewards

## Goal

The mobile app now starts with a lightweight player identity instead of requiring login before onboarding. The backend must become the source of truth for identity, onboarding state, wallet balances, account links, and reward claims. Hive/local storage should only cache backend-confirmed state.

## Required Backend Behavior

1. A fresh mobile install can create or resume a playable identity with no email/password.
2. iOS Game Center and Android Play Games identities can be attached during bootstrap when available.
3. If platform auth is unavailable, a device guest identity is created from the app device id.
4. Onboarding can complete for guest/platform users.
5. Account creation upgrades the existing guest/platform player instead of creating a disconnected new player.
6. Reward claims are idempotent and server-authoritative.
7. Backend responses include enough account/wallet/reward state for the app to overwrite local Hive cache.

## Identity States

The app expects these account states:

- `anonymousDevice`: backend player exists, tied to device identity only.
- `platformLinked`: backend player exists and has Game Center or Play Games identity.
- `fullAccount`: backend player has email/web/social account credentials.

Suggested database fields:

```text
players
- id
- display_name
- onboarding_completed_at
- active_profile_id
- account_state: anonymousDevice | platformLinked | fullAccount
- created_at
- updated_at

player_devices
- id
- player_id
- device_id
- device_type
- first_seen_at
- last_seen_at

player_platform_identities
- id
- player_id
- platform: ios | android
- platform_player_id
- display_name
- verified_at
- created_at

player_accounts
- id
- player_id
- email
- password_hash
- created_at
- upgraded_at

player_reward_claims
- id
- player_id
- reward_key
- claimed_at
- source
- unique(player_id, reward_key)

player_wallets
- player_id
- credits
- neural_xp
- synapse_shards
- updated_at
```

## Shared Response Shape

Where possible, return this shape after auth/bootstrap/link/reward operations:

```json
{
  "accessToken": "jwt-access-token",
  "refreshToken": "jwt-refresh-token",
  "expiresIn": 3600,
  "playerId": "player_123",
  "accountState": "anonymousDevice",
  "profile": {
    "activeProfileId": "profile_123",
    "displayName": "Player",
    "onboardingCompleted": false
  },
  "wallet": {
    "credits": 250,
    "neuralXp": 50,
    "synapseShards": 0,
    "updatedAtUtc": "2026-05-17T18:00:00Z"
  },
  "claimedRewardKeys": [
    "onboarding_complete"
  ]
}
```

The app already understands both camelCase and some legacy auth token fields, but camelCase is preferred for new endpoints.

## Endpoints

## `POST /auth/device/bootstrap`

Creates or resumes a playable mobile identity.

Request:

```json
{
  "deviceId": "device_uuid_or_install_id",
  "device_id": "device_uuid_or_install_id",
  "deviceType": "ios",
  "device_type": "ios",
  "platform": "ios",
  "platformPlayerId": "game_center_player_id",
  "displayName": "Game Center Name"
}
```

Rules:

- `deviceId` and `deviceType` are required.
- `platform`, `platformPlayerId`, and `displayName` are optional.
- If a verified platform identity already exists, return that player.
- If only the device exists, return the player attached to the device.
- If neither exists, create a new player, device row, wallet row, and default profile if desired.
- If platform identity is provided, verify it server-side before trusting it.
- If platform verification fails, fall back to device guest or return a recoverable auth error.

Response:

```json
{
  "accessToken": "jwt-access-token",
  "refreshToken": "jwt-refresh-token",
  "expiresIn": 3600,
  "playerId": "player_123",
  "accountState": "platformLinked",
  "wallet": {
    "credits": 0,
    "neuralXp": 0,
    "synapseShards": 0,
    "updatedAtUtc": "2026-05-17T18:00:00Z"
  },
  "claimedRewardKeys": []
}
```

## `POST /auth/account/upgrade`

Upgrades the current guest/platform player into a full account.

Auth:

```text
Authorization: Bearer <guest-or-platform-access-token>
```

Request:

```json
{
  "email": "player@example.com",
  "password": "plain-text-password-over-https",
  "username": "TriviaPlayer",
  "handle": "TriviaPlayer",
  "country": "US",
  "deviceId": "device_uuid_or_install_id",
  "deviceType": "ios"
}
```

Rules:

- Do not create a new disconnected player.
- Attach credentials to the authenticated player.
- If email already exists, return `409` with a clear error code.
- Rotate/return new access and refresh tokens after upgrade.
- Set account state to `fullAccount`.
- Consider claiming `website_account_linked` if this upgrade represents linking to a web/email account.

Response:

```json
{
  "accessToken": "new-jwt-access-token",
  "refreshToken": "new-jwt-refresh-token",
  "expiresIn": 3600,
  "playerId": "player_123",
  "accountState": "fullAccount",
  "claimedRewardKeys": [
    "onboarding_complete",
    "website_account_linked"
  ],
  "wallet": {
    "credits": 750,
    "neuralXp": 150,
    "synapseShards": 0,
    "updatedAtUtc": "2026-05-17T18:05:00Z"
  }
}
```

## `GET /account/rewards/status`

Returns server-owned reward claim state and current wallet.

Auth:

```text
Authorization: Bearer <access-token>
```

Response:

```json
{
  "claimedRewardKeys": [
    "onboarding_complete"
  ],
  "availableRewardKeys": [
    "website_account_linked",
    "phone_or_qr_linked",
    "discord_connected",
    "twitch_connected",
    "x_connected"
  ],
  "wallet": {
    "credits": 250,
    "neuralXp": 50,
    "synapseShards": 0,
    "updatedAtUtc": "2026-05-17T18:00:00Z"
  }
}
```

Rules:

- This response should replace local Hive reward claim state.
- If the local app has extra claimed keys that the server does not return, the app should discard the local extras.

## `POST /account/rewards/claim`

Claims a one-time reward.

Auth:

```text
Authorization: Bearer <access-token>
```

Request:

```json
{
  "rewardKey": "onboarding_complete",
  "playerId": "player_123"
}
```

Reward config for v1:

```json
{
  "onboarding_complete": {
    "credits": 250,
    "neuralXp": 50,
    "items": [{ "key": "hint", "quantity": 1 }]
  },
  "website_account_linked": {
    "credits": 500,
    "neuralXp": 100,
    "badges": ["account_linked"]
  },
  "phone_or_qr_linked": {
    "credits": 250,
    "items": [{ "key": "revive", "quantity": 1 }]
  },
  "discord_connected": {
    "credits": 300,
    "items": [{ "key": "freeze", "quantity": 1 }]
  },
  "twitch_connected": {
    "credits": 300,
    "boosts": [{ "key": "double_xp", "quantity": 1 }]
  },
  "x_connected": {
    "credits": 300,
    "cosmetics": ["profile_flair_x"]
  }
}
```

Rules:

- Use a unique constraint on `(player_id, reward_key)`.
- If the reward was already claimed, return `200` with current state, not a duplicate grant.
- Grant wallet/items/badges in the same transaction as inserting the claim.
- Reward amounts should come from server config, not the app.
- Validate prerequisites for link/social rewards:
  - `website_account_linked` requires full account or linked web account.
  - `phone_or_qr_linked` requires completed QR/code link.
  - `discord_connected` requires verified Discord OAuth link.
  - `twitch_connected` requires verified Twitch OAuth link.
  - `x_connected` requires verified X OAuth link.

Response:

```json
{
  "rewardKey": "onboarding_complete",
  "alreadyClaimed": false,
  "claimedRewardKeys": [
    "onboarding_complete"
  ],
  "wallet": {
    "credits": 250,
    "neuralXp": 50,
    "synapseShards": 0,
    "updatedAtUtc": "2026-05-17T18:00:00Z"
  }
}
```

## `GET /users/me/wallet`

This endpoint already appears to be expected by the Flutter app.

Response:

```json
{
  "playerId": "player_123",
  "credits": 250,
  "neuralXp": 50,
  "synapseShards": 0,
  "updatedAtUtc": "2026-05-17T18:00:00Z"
}
```

Rules:

- The wallet endpoint should always return the authoritative wallet.
- The app mirrors this response into Hive/local providers.

## QR And Code Linking

Existing app code references:

- `POST /auth/link/qr/generate`
- `GET /auth/link/qr/status/{qrToken}`
- `POST /auth/link/qr/consume`
- `POST /auth/link/code/generate`
- `POST /auth/link/code/consume`

After successful QR/code linking:

1. Attach the mobile player to the web account or browser session.
2. Claim or enable `phone_or_qr_linked`.
3. Return updated wallet and `claimedRewardKeys`.

## Social Linking

Recommended endpoints:

```text
GET  /auth/social/{provider}/start
POST /auth/social/{provider}/callback
GET  /auth/social/links
DELETE /auth/social/{provider}
```

Supported providers for v1:

- `discord`
- `twitch`
- `x`

After successful provider verification:

1. Store social identity with provider user id.
2. Claim or enable the matching reward key.
3. Return updated wallet and reward state.

## Server-Authoritative Hive Sync

The app should treat Hive as a cache only. Backend should send authoritative snapshots after important operations.

Recommended sync moments:

- After `/auth/device/bootstrap`
- After `/auth/account/upgrade`
- After onboarding completion reward claim
- After QR/code/social linking
- After app resume or startup via `/account/rewards/status` and `/users/me/wallet`

Client behavior:

```text
server response received
-> parse wallet/profile/reward state
-> overwrite Hive cache
-> update Riverpod providers
-> render UI from confirmed state
```

Security rules:

- Never trust client-submitted wallet balances, XP, items, or claimed rewards.
- Client may request a reward claim, but server decides eligibility.
- Client may display cached Hive state while offline, but production grants must be confirmed by server.
- Consider adding server-side audit records for every grant/spend.

## Error Codes

Use stable error codes in JSON responses:

```json
{
  "error": {
    "code": "reward_not_eligible",
    "message": "Discord must be connected before claiming this reward."
  }
}
```

Suggested codes:

- `invalid_device_identity`
- `platform_verification_failed`
- `email_already_registered`
- `session_required`
- `reward_unknown`
- `reward_not_eligible`
- `reward_already_claimed`
- `link_code_expired`
- `qr_token_expired`
- `social_provider_error`

## Production Checklist

- Add migrations for player devices, platform identities, reward claims, and wallet records.
- Add unique constraints for platform ids and reward claims.
- Verify Game Center and Play Games identities server-side.
- Make reward claim operation transactional.
- Return updated wallet and claim state after every reward/link operation.
- Add rate limiting for bootstrap, link code generation, QR consume, and reward claim.
- Add integration tests for duplicate reward claims.
- Add tests for guest-to-full-account upgrade preserving the same `playerId`.
- Add monitoring for failed platform verification and suspicious repeated bootstrap attempts.

