---
project: TycoonTycoon_Backend / Synaptix
mode: Claude Code
priority: Alpha/Beta before June 1, without damaging long-term platform architecture
---

# KMS Security Skill

Use this skill when working on KMS, secrets, encryption, auth, token security, or sensitive data.

## Principles

- Use standard cryptographic primitives.
- Do not invent encryption algorithms.
- Separate key management from gameplay.
- Never log secrets.
- Never commit credentials.
- Minimize secret exposure to clients.
- Prefer envelope encryption for protected payloads.
- Maintain clear trust boundaries.

## Review Checklist

- What secret/key/data is protected?
- Who can access it?
- Where is it stored?
- Is rotation possible?
- Are logs safe?
- Is local dev clearly separated from production?
- Are APIs authenticated and authorized?
