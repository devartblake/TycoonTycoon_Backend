# Operator Dashboard Authenticated Preview

Use this workflow to review authenticated `Tycoon.OperatorDashboard.Django` pages locally. A
standalone Django `runserver` can render `/login` and `/healthz`, but it cannot authenticate
dashboard pages unless it can reach a live backend API with matching admin auth configuration.

## Canonical Docker Preview

1. Ensure `docker/.env` exists:

   ```bash
   cp docker/.env.example docker/.env
   ```

   Skip this command if `docker/.env` already exists and contains local secrets you want to keep.

2. Confirm the local preview values:

   ```env
   ADMIN_OPS_KEY=CHANGE_ME_IN_PRODUCTION
   ADMIN_AUTH_ALLOW_TRUSTED_BFF_PLAIN_JSON=true
   ADMIN_AUTH_TRANSPORT=auto
   SUPER_ADMIN_EMAIL=admin@tycoon.local
   SUPER_ADMIN_PASSWORD=ChangeMe123!
   MIGRATION_DASHBOARD_READINESS_ENABLED=true
   MIGRATION_DASHBOARD_READINESS_STRICT=true
   ```

   For an existing local database that you do not want to wipe, also confirm
   `MIGRATION_RESET_DATABASE=false` before starting compose. Use `true` only for an intentional
   fresh dev reseed.

3. Validate compose configuration:

   ```bash
   docker compose -f docker/compose.yml config
   ```

4. Start the stack:

   ```bash
   docker compose -f docker/compose.yml up -d --build
   ```

5. Confirm the one-shot migration/bootstrap job succeeds:

   ```bash
   docker compose -f docker/compose.yml logs migration
   docker compose -f docker/compose.yml ps
   ```

   The migration logs should show EF migrations applied, seed data loaded, the super-admin seeded,
   the matching `AdminEmailAcl` row created or updated, and dashboard readiness passing.

6. Open the dashboard:

   - URL: `http://localhost:8200/login`
   - Email: `admin@tycoon.local`
   - Password: `ChangeMe123!`

## Pages To Review

After login, review:

- `/`
- `/users`
- `/users/{userId}/investigation`
- `/personalization`
- `/store/player-stock`

Use a real player UUID for player-specific pages. The investigation workbench accepts
`?playerId=<uuid>` when the user ID is not the player UUID used by store/personalization APIs.

## Common Failures

- `401` or bad credentials: the super-admin was not seeded, the password differs from
  `SUPER_ADMIN_PASSWORD`, or the backend is using a different database.
- `403` allowlist denial: the `AdminEmailAcl` row was not seeded for the configured
  `SUPER_ADMIN_EMAIL`.
- `secure_session_required`: local/dev backend has trusted BFF plain JSON disabled, or Django is
  configured for plain auth against a production-like backend.
- backend unavailable: `operator-dashboard` cannot reach `DOTNET_API_BASE_URL=http://backend-api:5000`
  from inside compose.
- missing dashboard data: rerun `migration` and check readiness logs for tiers, missions, questions,
  store items, skill nodes, and season rewards.

## Standalone Django Runserver

Use standalone Django only for quick template/static checks:

```bash
cd Tycoon.OperatorDashboard.Django
python manage.py runserver 127.0.0.1:8300
```

Authenticated pages still require a live backend API, matching `ADMIN_OPS_KEY`, and a seeded admin
account. Docker is the supported local authenticated preview environment.
