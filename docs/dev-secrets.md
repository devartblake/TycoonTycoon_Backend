# Local development credentials

The dev `appsettings.{Development,Local}.json` files carry throwaway passwords
(`synaptix_*_123`, `postgres`, etc.) so a fresh clone runs against the local Docker stack with
zero setup. These files are git-ignored for local overrides and the values are **not** secrets —
they only ever reach the loopback containers in `docker/compose.yml`.

## Prefer user-secrets for anything real

Never put a real credential in a tracked `appsettings*.json`. For per-developer secrets that must
not land in git, use .NET user-secrets (stored outside the repo, under your user profile):

```bash
cd Synaptix.Backend.Api
dotnet user-secrets init          # once per project
dotnet user-secrets set "Elastic:Password" "<your-value>"
dotnet user-secrets set "ConnectionStrings:db" "Host=...;Password=<your-value>"
```

User-secrets override `appsettings*.json` in Development and are read through the same
`IConfiguration` keys the app already binds — no code change required.

## Connection strings: keep passwords out of the URL

Source credentials from dedicated keys, not embedded in a connection-string URL. URLs get logged
(request traces, health-probe output, error envelopes), so an embedded password is a leak vector.
Elasticsearch is the reference pattern — `Elastic:Username` / `Elastic:Password` feed
`BasicAuthentication` in `Synaptix.Backend.Infrastructure/DependencyInjection.cs`, and the URL stays
password-free:

```
Elastic__Url:      "http://elasticsearch:9200"
Elastic__Username: "elastic"
Elastic__Password: "${ELASTIC_PASSWORD}"   # from the environment / secret store, never the URL
```
