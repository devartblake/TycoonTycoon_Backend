# Docker Debug Prompt

Diagnose and patch local Docker issue.

## Prompt Template

Problem:
`<paste error/log>`

Constraints:
- Local Docker is official integration target.
- Do not hardcode host-only paths.
- Prefer service DNS inside containers.
- Keep env vars documented.
- Verify compose config and affected service startup if possible.

Required output:
- Root cause.
- Patch summary.
- Verification commands.
- Remaining risks.
