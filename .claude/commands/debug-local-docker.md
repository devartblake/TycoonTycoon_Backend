---
project: TycoonTycoon_Backend / Synaptix
mode: Claude Code
priority: Alpha/Beta before June 1, without damaging long-term platform architecture
---

# Command: Debug Local Docker

Use this command when local Docker, service startup, networking, health checks, or env config fails.

## Instructions

1. Inspect compose config.
2. Inspect failing service logs.
3. Check health status.
4. Check env variables.
5. Check network DNS and ports.
6. Check volumes.
7. Propose smallest safe fix.
8. Provide verification commands.

## Output

```md
# Docker Debug Report

## Symptom
-

## Failing Service
-

## Likely Root Cause
-

## Fix
-

## Commands
```bash
docker compose ps
docker compose logs --tail=200 <service>
docker compose up -d
```

## Verification
-
```
