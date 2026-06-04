# Synaptix.Setup UI + CLI Architecture Recommendation

This document recommends a hybrid architecture where Synaptix.Setup remains the authoritative setup engine, Synaptix.Backend.Api exposes protected setup endpoints, and Synaptix.OperatorDashboard provides a secure administrative UI.

Key recommendations:
- Keep setup logic in Synaptix.Setup.
- Expose safe setup operations through backend APIs.
- Use OperatorDashboard for setup status, validation, seed management, service health, and admin management.
- Keep destructive operations CLI-only.
- Integrate with Synaptix.Security.Kms for secret protection.

Final Architecture:

Synaptix.Setup = Setup Engine
Synaptix.Backend.Api = Secure Setup API Layer
Synaptix.OperatorDashboard = Administrative UI Layer
