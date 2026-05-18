---
project: TycoonTycoon_Backend / Synaptix
mode: Claude Code
priority: Alpha/Beta before June 1, without damaging long-term platform architecture
---

# Docker Compose Debugging Skill

Use this skill when Docker, local infra, service startup, or integration environment fails.

## Debug Order

1. Confirm compose file and env file.
2. Check container status.
3. Check service logs.
4. Check health checks.
5. Check network DNS/service names.
6. Check ports.
7. Check volumes.
8. Check credentials.
9. Check app configuration.

## Common Commands

```bash
docker compose ps
docker compose logs --tail=200 <service>
docker compose config
docker compose down
docker compose up -d
docker inspect <container>
```

## Alpha Bias

Local Docker must be reliable enough to act as the official integration target before staging.
