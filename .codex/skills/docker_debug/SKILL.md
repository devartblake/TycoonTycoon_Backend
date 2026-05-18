# Skill: Docker Compose Debugging

Use this skill for local Docker, compose, healthcheck, networking, port, or service startup problems.

## Procedure

1. Read compose files, env examples, Dockerfiles, and health checks.
2. Identify the failing service and its dependencies.
3. Check port bindings, container DNS names, volumes, and startup order.
4. Prefer healthcheck-backed `depends_on` where supported.
5. Avoid hardcoded localhost inside containers.
6. Make defaults safe for local development.
7. Verify with a targeted `docker compose config`, startup, logs, or smoke check.

## Common risks

- Container uses `localhost` instead of service DNS.
- Secret env vars default to blank.
- Port conflicts with local services.
- DB not ready before migrations.
- MinIO bucket/object path mismatch.
