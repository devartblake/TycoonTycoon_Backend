# Synaptix.Setup UI + CLI Architecture Recommendation

This file is retained as a compatibility pointer.

The canonical architecture, implementation status, security boundaries, route conventions, and read-only delivery roadmap are documented in:

- [`Synaptix_Setup_UI_CLI_Architecture_Handoff.md`](Synaptix_Setup_UI_CLI_Architecture_Handoff.md)

Current decision:

- `Synaptix.Setup` remains the offline CLI and one-shot provisioning engine.
- `Synaptix.Backend.Api` may expose sanitized read-only setup endpoints in a future phase.
- `Synaptix.OperatorDashboard.Django` is the canonical setup UI/BFF target.
- Initial UI/API delivery is read-only; mutating and destructive operations remain CLI-only.
