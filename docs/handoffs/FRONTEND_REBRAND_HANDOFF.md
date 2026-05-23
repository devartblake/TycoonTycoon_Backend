# Frontend Rebrand Handoff — Synaptix Rename

**Date:** 2026-05-22  
**Prepared by:** Backend Team  
**Priority:** HIGH — required before next backend deployment

---

## What Changed on the Backend

The backend has completed a full identifier rename from `Tycoon` to `Synaptix` across all layers. This affects two things the Flutter app must update:

1. **JWT token `iss` and `aud` claims** — the values embedded in every auth token have changed
2. **Any hardcoded service/package name strings** that reference old identifiers

---

## 1. JWT Issuer and Audience

This is the breaking change. Every token issued by the backend will now carry new `iss` and `aud` values. If the Flutter app validates or reads these claims, it must be updated.

| Claim | Old value | New value |
|---|---|---|
| `iss` (issuer) | `TycoonBackendApi` | `SynaptixBackendApi` |
| `aud` (audience) | `TycoonFrontendApp` | `SynaptixFrontendApp` |

**Action required:**  
Search your Flutter/Dart codebase for any of these strings and replace them:

```bash
# Search for affected strings
grep -r "TycoonBackendApi\|TycoonFrontendApp\|TycoonBackend\|TycoonClient" lib/
```

Common locations to check:
- JWT decode/verify utilities (`dart_jsonwebtoken`, `jose`, or custom)
- Any `AuthConfig`, `ApiConfig`, or constants file where issuer/audience are hardcoded
- Mock/stub data in tests that include JWT payloads

If your app does **not** independently validate `iss`/`aud` claims (i.e. it trusts the backend entirely and only reads the token payload), no code change is needed — but you should still remove any hardcoded string references for correctness.

**Important:** Tokens issued before this backend deployment will be rejected immediately after cutover, since the issuer will no longer match. All users will need to log in again after the backend is updated. Plan for a forced re-authentication event or a short overlap window.

---

## 2. API Base URLs

No API routes changed. All endpoints remain at the same paths. No action required here.

---

## 3. Package / Bundle ID

The Flutter package name (`com.tycoon.app.dev`, `com.tycoon.app`) and iOS bundle ID are **not changing in this release**. That is a separate store/legal coordination item. No action needed.

---

## 4. Environment / Config Constants

If your Flutter app has any hardcoded references to these old backend identifiers, update them:

| Old | New |
|---|---|
| `TycoonBackendApi` | `SynaptixBackendApi` |
| `TycoonFrontendApp` | `SynaptixFrontendApp` |
| `TycoonBackend` | `SynaptixBackend` |
| `TycoonClient` | `SynaptixClient` |

These may appear in:
- `lib/core/config/api_config.dart` or equivalent
- `lib/core/auth/` — token validation, token storage keys
- Test fixtures or golden files containing JWT payloads

---

## 5. Secure Channel

No changes to the Secure Channel protocol. Key exchange, nonce handling, and payload encryption are unchanged. The `EncryptedApiClient` integration work can continue without modification.

---

## 6. Deployment Coordination

| Step | Owner | Notes |
|---|---|---|
| Update JWT issuer/audience constants in Flutter | Frontend | Before next backend deploy |
| QA re-login flow on staging | Both | Existing tokens will be invalid after cutover |
| Coordinate backend deploy window | DevOps | Notify frontend of exact deploy time so re-auth is expected |

---

## Questions?

Raise in the `#backend-frontend-sync` channel or tag the backend lead in the relevant PR.
