# Phase D Web Account Linking Backend Implementation Guide

**Status date:** 2026-05-14  
**Branch:** `claude/fix-hexagon-alignment-CrQVu`  
**Flutter commits:** `b31e256` (Phase D), `63c9d08` (Phase C mobile auth)  
**Audience:** Backend engineering team  
**Goal:** Enable a web browser session to authenticate by linking to a mobile account through QR code, Google Sign-In, or a one-time link code.  
**Sequencing note:** This is a future implementation guide. Complete the Operator Dashboard May cutover before starting this backend work.

The Flutter client has implemented three web account linking methods. Each method lets the web browser obtain an authenticated backend session for future API requests. The client calls these endpoints through `WebLinkService` in `lib/core/services/web_link_service.dart`.

## Shared Session Contract

All endpoints that directly authenticate a user must return the same session JSON shape as the existing `/auth/login` endpoint:

```json
{
  "accessToken": "string",
  "refreshToken": "string",
  "userId": "string (optional)",
  "expiresIn": 3600
}
```

The Flutter client parses this shape through `GoogleWebAuthResponse.fromJson()` for web linking flows and the existing `AuthApiClient._parseSession()` path for mobile-game login.

`GET /auth/link/qr/status/{qrToken}` is the exception in the current Flutter contract: when consumed, it returns a `sessionToken` field containing the web access token. The backend should still create that token through the same central session creation helper used by full session responses.

Implement session creation once and reuse it across these flows:

```text
createSessionForUser(userId: string) -> {
  accessToken,
  refreshToken,
  userId,
  expiresIn
}
```

Do not duplicate token issuance logic from `/auth/login`.

## Shared Backend Requirements

Use a fast TTL store, preferably Redis, for short-lived QR tokens and link codes.

| Store key | TTL | Contents |
|-----------|-----|----------|
| `qr:<token>` | 300 seconds, then extend about 30 seconds after consume | `{ status, sessionToken }` |
| `link_code:<CODE>` | 300 seconds | `{ userId }` |

Configure CORS so the web app origin can call these endpoints from a browser:

```http
Access-Control-Allow-Origin: https://your-web-app-domain.com
Access-Control-Allow-Headers: Content-Type, Authorization
Access-Control-Allow-Methods: GET, POST
Access-Control-Allow-Credentials: true
```

Add rate limiting and abuse monitoring for public credential endpoints:

- `POST /auth/link/qr/generate`
- `GET /auth/link/qr/status/{qrToken}`
- `POST /auth/google-web`
- `POST /auth/link/code/consume`

Recommended controls include per-IP request limits, short TTLs, generic error messages, and structured security logs for rejected or expired credentials.

## Method 1: QR Code Linking

The web browser requests a short-lived QR token, renders it as a QR code, and polls for status. A logged-in mobile user scans and consumes the QR token. The next browser poll receives a web access token in `sessionToken`.

```text
Web browser                    Backend                    Mobile app
    |                              |                           |
    | POST /auth/link/qr/generate  |                           |
    |----------------------------->|                           |
    |     { qrToken, expiresIn }   |                           |
    |<-----------------------------|                           |
    |                              |                           |
    | (display QR code)            |                           |
    | GET /auth/link/qr/status/... |                           |
    | (poll every 3 seconds)       |                           |
    |----------------------------->|                           |
    |     { status: "pending" }    |                           |
    |<-----------------------------|                           |
    |                              |                           |
    |                              | POST /auth/link/qr/consume|
    |                              |<--------------------------|
    |                              | Bearer: <access_token>    |
    |                              | { qrToken }               |
    |                              |                           |
    | GET /auth/link/qr/status/... |                           |
    |----------------------------->|                           |
    |   { status: "consumed",      |                           |
    |     sessionToken: "..." }    |                           |
    |<-----------------------------|                           |
    | (navigate to /home)          |                           |
```

### `POST /auth/link/qr/generate`

| Field | Value |
|-------|-------|
| Authentication | None, public |
| Purpose | Create a short-lived QR token for the web browser to display |
| Request body | None |

Response `200 OK`:

```json
{
  "qrToken": "a1b2c3d4e5f6...",
  "expiresIn": 300
}
```

Implementation notes:

- Generate a cryptographically random token, such as 32 random bytes encoded as hex, or a UUID with enough entropy.
- Store `qr:<token>` with value `{ "status": "pending", "sessionToken": null }`.
- Default `expiresIn` to 300 seconds.
- Guarantee uniqueness for each request.

### `GET /auth/link/qr/status/{qrToken}`

| Field | Value |
|-------|-------|
| Authentication | None, public |
| Purpose | Let the browser poll until the mobile app consumes the QR token |
| Path parameter | `qrToken`, returned by the generate endpoint |

Pending response `200 OK`:

```json
{
  "status": "pending",
  "sessionToken": null
}
```

Consumed response `200 OK`:

```json
{
  "status": "consumed",
  "sessionToken": "<access_token_for_web_session>"
}
```

Expired response `200 OK`:

```json
{
  "status": "expired",
  "sessionToken": null
}
```

Token-not-found response `404 Not Found`:

```json
{
  "status": "expired"
}
```

Implementation notes:

- Look up `qr:<token>` in Redis or the selected fast TTL store.
- Return `expired` if the token is missing or already cleaned up.
- Return `consumed` with the stored web access token if the token was consumed.
- Return `pending` if the token exists and has not been consumed.
- Design for high read throughput because Flutter polls every 3 seconds.
- After returning a consumed token, keep the record around for about 30 seconds so the final poll can be delivered before cleanup.

### `POST /auth/link/qr/consume`

| Field | Value |
|-------|-------|
| Authentication | Required Bearer access token for an already logged-in mobile user |
| Purpose | Let the mobile app bind the QR token to the authenticated mobile user and create a web session |

Request:

```http
Authorization: Bearer <mobile_user_access_token>
Content-Type: application/json
```

```json
{
  "qrToken": "a1b2c3d4e5f6..."
}
```

Response `200 OK`:

```json
{
  "message": "QR token consumed successfully"
}
```

Response `400 Bad Request`:

```json
{
  "error": "QR token not found or already consumed"
}
```

Implementation notes:

- Validate the Bearer token and identify the authenticated mobile user.
- Reject missing, expired, or already consumed QR tokens with `400`.
- Create a new web session for the authenticated user.
- Store the new access token as `qr:<token>.sessionToken`.
- Mark `qr:<token>.status` as `consumed`.
- Do not invalidate the mobile user's existing session.

## Method 2: Google Sign-In On Web

The web browser signs in through Google, then sends the Google ID token to the backend. The backend verifies the token and exchanges it for a backend session.

```text
Web browser              Google OAuth              Backend
    |                         |                       |
    | GoogleSignIn.signIn()   |                       |
    |------------------------>|                       |
    |    { idToken }          |                       |
    |<------------------------|                       |
    |                         |                       |
    | POST /auth/google-web   |                       |
    | { googleIdToken }       |                       |
    |--------------------------------->               |
    |                         | verify idToken        |
    |                         |<--------------------> |
    |       session JSON      |                       |
    |<---------------------------------               |
```

### `POST /auth/google-web`

| Field | Value |
|-------|-------|
| Authentication | None, public; the Google ID token is the credential |
| Purpose | Exchange a Google-issued ID token for a backend session |

Request:

```http
Content-Type: application/json
```

```json
{
  "googleIdToken": "eyJhbGciOiJSUzI1NiIs..."
}
```

Response `200 OK`:

```json
{
  "accessToken": "...",
  "refreshToken": "...",
  "userId": "...",
  "expiresIn": 3600
}
```

Response `401 Unauthorized`:

```json
{
  "error": "Invalid or expired Google ID token"
}
```

Response `409 Conflict`:

```json
{
  "error": "Email already registered with a different login method"
}
```

Implementation notes:

- Verify the Google ID token through a Google client library or Google's token info endpoint:

```text
GET https://oauth2.googleapis.com/tokeninfo?id_token=<googleIdToken>
```

- Validate `aud` against the configured Google OAuth Client ID.
- Validate `iss` as either `accounts.google.com` or `https://accounts.google.com`.
- Reject expired, tampered, or wrong-audience tokens with `401`.
- Extract verified claims:
  - `sub`: stable Google user ID.
  - `email`: Google email address.
  - `name` and `picture`: optional display fields.
- Account resolution policy:
  - If a user exists with matching `googleId`, return that user's session.
  - If no user has the `googleId` but the email matches an existing account, default to `409` unless product policy explicitly chooses automatic account linking.
  - If no user exists, create a new account with `googleId = sub`, then return a session.

Google OAuth setup:

- Create a Google OAuth 2.0 Client ID of type `Web application`.
- Configure the Flutter web app with the same client ID through `web/index.html`:

```html
<meta name="google-signin-client_id" content="YOUR_CLIENT_ID">
```

- Add the web app's authorized JavaScript origins and redirect URIs in Google Cloud Console.

## Method 3: One-Time Link Code

A logged-in mobile user generates a short code. The user types that code into the web browser, and the browser exchanges it for a standard backend session.

```text
Mobile app                    Backend                    Web browser
    |                              |                           |
    | POST /auth/link/code/generate|                           |
    | Bearer: <access_token>       |                           |
    |----------------------------->|                           |
    |   { code: "ABC123",          |                           |
    |     expiresIn: 300 }         |                           |
    |<-----------------------------|                           |
    |                              |                           |
    | (display code to user)       |                           |
    |                              |                           |
    |                 (user types code into web form)          |
    |                              |                           |
    |                              | POST /auth/link/code/consume
    |                              | { code: "ABC123" }        |
    |                              |<--------------------------|
    |                              | (session JSON)            |
    |                              |-------------------------->|
    |                              |                           |
    |                              |             (navigate to /home)
```

### `POST /auth/link/code/generate`

| Field | Value |
|-------|-------|
| Authentication | Required Bearer access token for an already logged-in mobile user |
| Purpose | Generate a short-lived 6-character alphanumeric code for the web browser |
| Request body | None |

Request:

```http
Authorization: Bearer <mobile_user_access_token>
Content-Type: application/json
```

Response `200 OK`:

```json
{
  "code": "ABC123",
  "expiresIn": 300
}
```

Implementation notes:

- Validate the Bearer token and identify the authenticated mobile user.
- Generate a 6-character Crockford Base32 code using uppercase alphanumeric characters and excluding visually ambiguous letters such as `I`, `L`, `O`, and `U`.
- Check active codes to guarantee uniqueness.
- Store `link_code:<CODE>` with value `{ "userId": "<authenticated_user_id>" }` and TTL 300 seconds.
- Invalidate any previously active code for the same user so each user has one active code at a time.
- Return the code in uppercase.

### `POST /auth/link/code/consume`

| Field | Value |
|-------|-------|
| Authentication | None, public; the code is the credential |
| Purpose | Exchange a valid one-time link code for a backend session |

Request:

```http
Content-Type: application/json
```

```json
{
  "code": "ABC123"
}
```

Response `200 OK`:

```json
{
  "accessToken": "...",
  "refreshToken": "...",
  "userId": "...",
  "expiresIn": 3600
}
```

Response `400 Bad Request`:

```json
{
  "error": "Invalid or expired code"
}
```

Implementation notes:

- Normalize the submitted code to uppercase before lookup.
- Look up `link_code:<CODE>` in the fast TTL store.
- Return `400` if the code is expired, missing, or never existed.
- Delete the code immediately before returning the session to enforce single use and prevent replay.
- Create a new web session for the linked user and return the standard session JSON.
- Do not invalidate the mobile user's existing session.

## Phase C Addendum: Mobile Game Platform Auth

The Flutter client also calls these Phase C endpoints through `AuthApiClient`.

### `POST /auth/mobile-game-login`

| Field | Value |
|-------|-------|
| Authentication | None, public; the platform token is the credential |
| Purpose | Authenticate through iOS Game Center or Android Play Games |

Request:

```json
{
  "platform": "ios",
  "playerId": "G:1234567890",
  "displayName": "PlayerName"
}
```

Response: standard session JSON.

Implementation notes:

- For iOS Game Center, verify player identity using Apple's Game Center identity verification signature API from `GKLocalPlayer.generateIdentityVerificationSignature`.
- For Android Play Games, use Google's server-side token exchange to verify the player ID.
- On success, look up or create a backend user linked to the `platform` and `playerId` combination, then return a session.

### `POST /auth/link-game-account`

| Field | Value |
|-------|-------|
| Authentication | Required Bearer access token |
| Purpose | Link a platform player ID to the authenticated backend account |

Request:

```json
{
  "platform": "ios",
  "playerId": "G:1234567890"
}
```

Response `200 OK`:

```json
{
  "message": "Game account linked successfully"
}
```

Implementation notes:

- Associate the platform player ID with the authenticated user's backend account.
- Allow future `/auth/mobile-game-login` requests with that `playerId` to resolve to the same backend account.
- Return `409` if the `playerId` is already linked to a different backend account.

## Flutter Client Reference

| File | Role |
|------|------|
| `lib/core/dto/web_link_dto.dart` | Request and response models for all web-link endpoints |
| `lib/core/services/web_link_service.dart` | HTTP client calling web-link endpoints |
| `lib/game/providers/web_link_providers.dart` | Riverpod provider wiring |
| `lib/screens/web_link/qr_link_widget.dart` | Web QR display widget and 3-second polling |
| `lib/screens/web_link/link_code_screen.dart` | Mobile code display screen and countdown |
| `lib/screens/login_screen.dart` | Web linking UI with Google button, code field, and QR toggle |
| `lib/core/services/game_platform_auth_service.dart` | Game Center and Play Games sign-in for Phase C |
| `lib/core/services/auth_api_client.dart` | Phase C mobile game login and link endpoints |

## Backend Testing Checklist

### QR Code Flow

- [ ] `POST /auth/link/qr/generate` returns a unique `qrToken` on each call.
- [ ] `GET /auth/link/qr/status/{token}` returns `pending` immediately after generation.
- [ ] `POST /auth/link/qr/consume` with authentication transitions status to `consumed`.
- [ ] The next status poll returns `consumed` with a valid `sessionToken`.
- [ ] Token auto-expires after 300 seconds and status returns `expired`.
- [ ] Consuming an already consumed token returns `400`.
- [ ] Consuming a non-existent token returns `400`.

### Google Sign-In Flow

- [ ] `POST /auth/google-web` rejects an invalid or tampered Google ID token with `401`.
- [ ] `POST /auth/google-web` rejects an expired Google ID token with `401`.
- [ ] First Google sign-in creates a new account when no existing account matches.
- [ ] Repeat Google sign-in returns the existing user's session.
- [ ] The Google token `aud` claim must match the configured OAuth Client ID.

### Link Code Flow

- [ ] `POST /auth/link/code/generate` requires a valid Bearer token and returns `401` without it.
- [ ] Generated codes are 6 uppercase alphanumeric characters.
- [ ] `POST /auth/link/code/consume` exchanges a valid code for a session.
- [ ] Codes are single-use; a second consume attempt returns `400`.
- [ ] Codes expire after 300 seconds and consume returns `400`.
- [ ] Generating a new code invalidates the user's previously active code.

### All Flows

- [ ] All returned session tokens are accepted by authenticated API endpoints.
- [ ] CORS headers are present on all browser-called endpoints.
- [ ] The mobile user's existing session is not invalidated by any web-linking operation.
- [ ] Public credential endpoints are rate limited and produce security logs for rejected attempts.
