# Tycoon.Security.Kms.Client — Implementation Plan

Source documents:
- `docs/Synaptix_Backend_Security_KMS_Handoff.md`
- `docs/Synaptix_Security_Architecture_Recommendation.md`

This plan describes every file, interface, model, and wiring step needed to add `Tycoon.Security.Kms.Client` as a typed HTTP client library that any other backend project can reference to talk to the `Synaptix.Security.Kms.Api` service.

---

## 1. What this project is

`Tycoon.Security.Kms.Client` is a `.NET 10` class library (no `OutputType: Exe`). It provides:

- Typed HTTP clients for every KMS API surface defined in the handoff.
- Strongly-typed request/response models that mirror the Contracts layer.
- An `IServiceCollection` extension to register everything with one call.
- Resilience policies (retry, circuit breaker) via `Microsoft.Extensions.Http.Resilience`.
- Custom exception types so callers never have to parse raw `HttpResponseMessage`.

Other Tycoon backend projects add a project reference and call `services.AddKmsClient(configuration)`. They never import `System.Net.Http` directly for KMS calls.

---

## 2. Solution placement

Add to `TycoonTycoon_Backend.slnx` under the existing `/Services/Synaptix.Security.Kms/` folder:

```xml
<Folder Name="/Services/Synaptix.Security.Kms/">
  <Project Path="Synaptix.Security.Kms.Api/Synaptix.Security.Kms.Api.csproj" />
  <Project Path="Synaptix.Security.Kms.Application/Synaptix.Security.Kms.Application.csproj" />
  <Project Path="Synaptix.Security.Kms.Infrastructure/Synaptix.Security.Kms.Infrastructure.csproj" />
  <!-- NEW -->
  <Project Path="Tycoon.Security.Kms.Client/Tycoon.Security.Kms.Client.csproj" />
</Folder>
```

---

## 3. Project file

**`Tycoon.Security.Kms.Client/Tycoon.Security.Kms.Client.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <!-- class library — no OutputType needed -->
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Http" />
    <PackageReference Include="Microsoft.Extensions.Http.Resilience" />
    <PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions" />
    <PackageReference Include="Microsoft.Extensions.ServiceDiscovery" />
    <PackageReference Include="System.Net.Http.Json" />
  </ItemGroup>

  <ItemGroup>
    <!-- Shared contracts: EncryptedPayload, SecureSession, WrappedDataKey -->
    <ProjectReference Include="..\Synaptix.Security.Kms.Contracts\Synaptix.Security.Kms.Contracts.csproj" />
  </ItemGroup>
</Project>
```

All `PackageVersion` entries already exist in `Directory.Packages.props`. No new central versions needed.

---

## 4. Full directory tree

```
Tycoon.Security.Kms.Client/
├── Tycoon.Security.Kms.Client.csproj
│
├── Abstractions/
│   ├── IKmsSessionClient.cs         ← session start / renew / revoke
│   ├── IKmsPayloadClient.cs         ← encrypt / decrypt payloads
│   ├── IKmsKeyClient.cs             ← key rotation (admin/internal)
│   └── IKmsInternalClient.cs        ← service-to-service datakey generation
│
├── Models/
│   ├── Requests/
│   │   ├── StartSecureSessionRequest.cs
│   │   ├── RenewSecureSessionRequest.cs
│   │   ├── RevokeSecureSessionRequest.cs
│   │   ├── EncryptPayloadRequest.cs
│   │   ├── DecryptPayloadRequest.cs
│   │   ├── GenerateDataKeyRequest.cs
│   │   └── RotateKeyRequest.cs
│   └── Responses/
│       ├── StartSecureSessionResponse.cs
│       ├── RenewSecureSessionResponse.cs
│       ├── EncryptPayloadResponse.cs
│       ├── DecryptPayloadResponse.cs
│       ├── GenerateDataKeyResponse.cs
│       └── RotateKeyResponse.cs
│
├── Http/
│   ├── KmsSessionClient.cs          ← implements IKmsSessionClient
│   ├── KmsPayloadClient.cs          ← implements IKmsPayloadClient
│   ├── KmsKeyClient.cs              ← implements IKmsKeyClient
│   └── KmsInternalClient.cs         ← implements IKmsInternalClient
│
├── Options/
│   └── KmsClientOptions.cs
│
├── Exceptions/
│   ├── KmsClientException.cs
│   └── KmsUnavailableException.cs
│
└── Extensions/
    └── ServiceCollectionExtensions.cs
```

---

## 5. Options

**`Options/KmsClientOptions.cs`**

Maps 1-to-1 with the `VaultOptions` pattern from the handoff; covers only the transport side for callers.

```csharp
namespace Tycoon.Security.Kms.Client.Options;

public sealed class KmsClientOptions
{
    public const string SectionName = "KmsClient";

    /// Base URL of Synaptix.Security.Kms.Api, e.g. "https://kms-api"
    public required string BaseUrl { get; init; }

    /// Shared internal service token for service-to-service calls.
    public string? ServiceToken { get; init; }

    /// Total timeout per request in seconds. Default 10.
    public int TimeoutSeconds { get; init; } = 10;

    /// Max retry attempts on transient failures. Default 3.
    public int MaxRetryAttempts { get; init; } = 3;

    /// Whether the KMS service is required for startup. Default true.
    /// Matches VaultOptions.Required from the handoff.
    public bool Required { get; init; } = true;
}
```

Configuration binding key: `"KmsClient"` in `appsettings.json` / environment.

---

## 6. Exceptions

### `Exceptions/KmsClientException.cs`

```csharp
namespace Tycoon.Security.Kms.Client.Exceptions;

public class KmsClientException : Exception
{
    public int? StatusCode { get; }

    public KmsClientException(string message, int? statusCode = null)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public KmsClientException(string message, Exception inner, int? statusCode = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
    }
}
```

### `Exceptions/KmsUnavailableException.cs`

```csharp
namespace Tycoon.Security.Kms.Client.Exceptions;

/// Thrown when KmsClientOptions.Required = true and the KMS API cannot be reached.
public sealed class KmsUnavailableException : KmsClientException
{
    public KmsUnavailableException(string message, Exception? inner = null)
        : base(message, inner) { }
}
```

---

## 7. Models — one-to-one mapping from the handoff

### Requests

**`Models/Requests/StartSecureSessionRequest.cs`**

Direct mapping from handoff section "POST /security/sessions/start":

```csharp
namespace Tycoon.Security.Kms.Client.Models.Requests;

public sealed record StartSecureSessionRequest(
    string DeviceId,
    string ClientNonce,
    string ClientPublicKey,
    IReadOnlyList<string> SupportedSuites);
```

`SupportedSuites` values come from the `SecureSuites` constants (defined in Contracts):
- `"X25519-HKDF-SHA256-AES256GCM"` (ClassicalV1 — initial implementation)
- `"X25519-MLKEM768-HKDF-SHA256-AES256GCM"` (HybridPqV1 — behind feature flag later)

**`Models/Requests/RenewSecureSessionRequest.cs`**

```csharp
namespace Tycoon.Security.Kms.Client.Models.Requests;

public sealed record RenewSecureSessionRequest(
    Guid SessionId,
    string DeviceId);
```

**`Models/Requests/RevokeSecureSessionRequest.cs`**

```csharp
namespace Tycoon.Security.Kms.Client.Models.Requests;

public sealed record RevokeSecureSessionRequest(
    Guid SessionId,
    string Reason);
```

**`Models/Requests/EncryptPayloadRequest.cs`**

Mapping from handoff "POST /security/payload/encrypt":

```csharp
namespace Tycoon.Security.Kms.Client.Models.Requests;

public sealed record EncryptPayloadRequest(
    Guid SessionId,
    byte[] Plaintext,
    string ContentType = "application/json");
```

**`Models/Requests/DecryptPayloadRequest.cs`**

Mapping from handoff "POST /security/payload/decrypt":

```csharp
namespace Tycoon.Security.Kms.Client.Models.Requests;

public sealed record DecryptPayloadRequest(
    Guid SessionId,
    string Ciphertext,
    string Nonce,
    string Mac,
    string ContentType,
    DateTimeOffset EncryptedAtUtc);
```

**`Models/Requests/GenerateDataKeyRequest.cs`**

Mapping from handoff `IKeyWrappingService.GenerateDataKeyAsync` / REST `POST /internal/security/datakey`:

```csharp
namespace Tycoon.Security.Kms.Client.Models.Requests;

public sealed record GenerateDataKeyRequest(string KeyName);
```

**`Models/Requests/RotateKeyRequest.cs`**

Mapping from handoff "POST /security/keys/rotate":

```csharp
namespace Tycoon.Security.Kms.Client.Models.Requests;

public sealed record RotateKeyRequest(string KeyName);
```

### Responses

**`Models/Responses/StartSecureSessionResponse.cs`**

Direct mapping from handoff `StartSecureSessionResponse`:

```csharp
namespace Tycoon.Security.Kms.Client.Models.Responses;

public sealed record StartSecureSessionResponse(
    Guid SessionId,
    string ProtocolVersion,
    string SelectedSuite,
    string ServerPublicKey,
    string ServerNonce,
    DateTimeOffset ExpiresAtUtc,
    string ServerSignature);
```

**`Models/Responses/RenewSecureSessionResponse.cs`**

```csharp
namespace Tycoon.Security.Kms.Client.Models.Responses;

public sealed record RenewSecureSessionResponse(
    Guid SessionId,
    DateTimeOffset ExpiresAtUtc);
```

**`Models/Responses/EncryptPayloadResponse.cs`**

Wraps the `EncryptedPayload` model from the handoff:

```csharp
namespace Tycoon.Security.Kms.Client.Models.Responses;

public sealed record EncryptPayloadResponse(
    string Ciphertext,
    string Nonce,
    string Mac,
    string ContentType,
    DateTimeOffset EncryptedAtUtc);
```

**`Models/Responses/DecryptPayloadResponse.cs`**

```csharp
namespace Tycoon.Security.Kms.Client.Models.Responses;

public sealed record DecryptPayloadResponse(
    byte[] Plaintext,
    string ContentType);
```

**`Models/Responses/GenerateDataKeyResponse.cs`**

Mapping from handoff `WrappedDataKey`:

```csharp
namespace Tycoon.Security.Kms.Client.Models.Responses;

public sealed record GenerateDataKeyResponse(
    byte[] PlaintextKey,
    string EncryptedKey,
    string KeyVersion);
```

**`Models/Responses/RotateKeyResponse.cs`**

```csharp
namespace Tycoon.Security.Kms.Client.Models.Responses;

public sealed record RotateKeyResponse(
    string KeyName,
    string NewKeyVersion,
    DateTimeOffset RotatedAtUtc);
```

---

## 8. Abstractions — one-to-one with handoff interfaces

### `Abstractions/IKmsSessionClient.cs`

Mapping from handoff `ISecureSessionService`, exposed over HTTP:

```csharp
namespace Tycoon.Security.Kms.Client.Abstractions;

public interface IKmsSessionClient
{
    /// POST /security/sessions/start
    Task<StartSecureSessionResponse> StartAsync(
        StartSecureSessionRequest request,
        CancellationToken ct = default);

    /// POST /security/sessions/renew
    Task<RenewSecureSessionResponse> RenewAsync(
        RenewSecureSessionRequest request,
        CancellationToken ct = default);

    /// POST /security/sessions/revoke  (maps to ISecureSessionService.RevokeAsync)
    Task RevokeAsync(
        RevokeSecureSessionRequest request,
        CancellationToken ct = default);
}
```

### `Abstractions/IKmsPayloadClient.cs`

Mapping from handoff `ISecurePayloadProtector`, exposed over HTTP:

```csharp
namespace Tycoon.Security.Kms.Client.Abstractions;

public interface IKmsPayloadClient
{
    /// POST /security/payload/encrypt
    Task<EncryptPayloadResponse> EncryptAsync(
        EncryptPayloadRequest request,
        CancellationToken ct = default);

    /// POST /security/payload/decrypt
    Task<DecryptPayloadResponse> DecryptAsync(
        DecryptPayloadRequest request,
        CancellationToken ct = default);
}
```

### `Abstractions/IKmsKeyClient.cs`

Mapping from handoff "POST /security/keys/rotate":

```csharp
namespace Tycoon.Security.Kms.Client.Abstractions;

public interface IKmsKeyClient
{
    /// POST /security/keys/rotate
    Task<RotateKeyResponse> RotateAsync(
        RotateKeyRequest request,
        CancellationToken ct = default);
}
```

### `Abstractions/IKmsInternalClient.cs`

Mapping from handoff `IKeyWrappingService` and internal REST endpoints
(`POST /internal/security/datakey`, `/internal/security/encrypt`, `/internal/security/decrypt`):

```csharp
namespace Tycoon.Security.Kms.Client.Abstractions;

public interface IKmsInternalClient
{
    /// POST /internal/security/datakey  (maps to IKeyWrappingService.GenerateDataKeyAsync)
    Task<GenerateDataKeyResponse> GenerateDataKeyAsync(
        GenerateDataKeyRequest request,
        CancellationToken ct = default);

    /// POST /internal/security/encrypt
    Task<EncryptPayloadResponse> EncryptAsync(
        EncryptPayloadRequest request,
        CancellationToken ct = default);

    /// POST /internal/security/decrypt
    Task<DecryptPayloadResponse> DecryptAsync(
        DecryptPayloadRequest request,
        CancellationToken ct = default);
}
```

---

## 9. HTTP implementations

Each client is a thin wrapper: build the request, POST, handle non-success status codes by throwing `KmsClientException`, deserialize the response via `System.Net.Http.Json`.

### `Http/KmsSessionClient.cs`

```csharp
namespace Tycoon.Security.Kms.Client.Http;

internal sealed class KmsSessionClient(HttpClient http) : IKmsSessionClient
{
    public async Task<StartSecureSessionResponse> StartAsync(
        StartSecureSessionRequest request, CancellationToken ct)
    {
        var response = await http.PostAsJsonAsync("/security/sessions/start", request, ct);
        await EnsureSuccessAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<StartSecureSessionResponse>(ct))!;
    }

    public async Task<RenewSecureSessionResponse> RenewAsync(
        RenewSecureSessionRequest request, CancellationToken ct)
    {
        var response = await http.PostAsJsonAsync("/security/sessions/renew", request, ct);
        await EnsureSuccessAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<RenewSecureSessionResponse>(ct))!;
    }

    public async Task RevokeAsync(RevokeSecureSessionRequest request, CancellationToken ct)
    {
        var response = await http.PostAsJsonAsync("/security/sessions/revoke", request, ct);
        await EnsureSuccessAsync(response, ct);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(ct);
        throw new KmsClientException(
            $"KMS session call failed: {(int)response.StatusCode} — {body}",
            (int)response.StatusCode);
    }
}
```

`KmsPayloadClient`, `KmsKeyClient`, and `KmsInternalClient` follow the same pattern using their respective endpoints.

---

## 10. Service registration

**`Extensions/ServiceCollectionExtensions.cs`**

```csharp
namespace Tycoon.Security.Kms.Client.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKmsClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<KmsClientOptions>()
            .Bind(configuration.GetSection(KmsClientOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddHttpClient<IKmsSessionClient, KmsSessionClient>()
            .ConfigureKmsHttpClient(configuration)
            .AddStandardResilienceHandler();

        services
            .AddHttpClient<IKmsPayloadClient, KmsPayloadClient>()
            .ConfigureKmsHttpClient(configuration)
            .AddStandardResilienceHandler();

        services
            .AddHttpClient<IKmsKeyClient, KmsKeyClient>()
            .ConfigureKmsHttpClient(configuration)
            .AddStandardResilienceHandler();

        services
            .AddHttpClient<IKmsInternalClient, KmsInternalClient>()
            .ConfigureKmsHttpClient(configuration)
            .AddStandardResilienceHandler();

        return services;
    }

    private static IHttpClientBuilder ConfigureKmsHttpClient(
        this IHttpClientBuilder builder,
        IConfiguration configuration)
    {
        return builder.ConfigureHttpClient((sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<KmsClientOptions>>().Value;
            client.BaseAddress = new Uri(opts.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);

            if (!string.IsNullOrEmpty(opts.ServiceToken))
                client.DefaultRequestHeaders.Add("X-Service-Token", opts.ServiceToken);
        });
    }
}
```

`AddStandardResilienceHandler()` from `Microsoft.Extensions.Http.Resilience` applies retry + circuit breaker automatically; no custom Polly pipeline needed.

---

## 11. Headers — X-Syn-Sec-* passthrough

The architecture doc defines these headers for encrypted requests:

```
X-Syn-Sec-Session: <sessionId>
X-Syn-Sec-Seq:     <sequence number>
X-Syn-Sec-Nonce:   <base64url nonce>
X-Syn-Sec-Key-Version: v1
X-Syn-Sec-AAD:     <base64url aad hash>
```

The client library does **not** set these headers itself — that is the responsibility of the service-side `EncryptedPayloadEndpointFilter` in `Synaptix.Security.Kms.Api`. The client library only sends the JSON body as specified in the request models above.

---

## 12. appsettings.json configuration block

Every consuming project adds this section:

```json
"KmsClient": {
  "BaseUrl": "https://kms-api",
  "ServiceToken": "",
  "TimeoutSeconds": 10,
  "MaxRetryAttempts": 3,
  "Required": true
}
```

For Aspire local dev, `BaseUrl` is resolved via service discovery:
`"BaseUrl": "https+http://synaptix-kms-api"` — the `AddServiceDiscovery()` in
`Tycoon.ServiceDefaults` resolves the name.

---

## 13. Consuming example

```csharp
// In Tycoon.Backend.Api/Program.cs
builder.Services.AddKmsClient(builder.Configuration);

// In a feature handler
public sealed class ClaimRewardHandler(IKmsPayloadClient kmsPayload)
{
    public async Task HandleAsync(EncryptedClaimRequest req, CancellationToken ct)
    {
        var plain = await kmsPayload.DecryptAsync(new DecryptPayloadRequest(
            SessionId: req.SessionId,
            Ciphertext: req.Ciphertext,
            Nonce: req.Nonce,
            Mac: req.Mac,
            ContentType: "application/json",
            EncryptedAtUtc: req.EncryptedAtUtc), ct);

        // work with plain.Plaintext ...
    }
}
```

---

## 14. Tests project

Add `Tycoon.Security.Kms.Client.Tests` alongside the other test projects:

```
Tycoon.Security.Kms.Client.Tests/
├── Tycoon.Security.Kms.Client.Tests.csproj
├── Http/
│   ├── KmsSessionClientTests.cs   ← WireMock stubs for start / renew / revoke
│   ├── KmsPayloadClientTests.cs   ← encrypt / decrypt round-trip
│   └── KmsInternalClientTests.cs  ← datakey generation
└── Extensions/
    └── ServiceCollectionExtensionsTests.cs
```

Use `WireMock.Net` (already in `Directory.Packages.props`) to stub the KMS API responses.

---

## 15. Milestones — mapped to handoff milestones

| Client milestone | Handoff milestone |
|---|---|
| M1 — `csproj`, `KmsClientOptions`, exceptions, DI registration skeleton | Milestone 1: Security hardening — `VaultOptions`, config validation |
| M2 — `IKmsSessionClient` + `KmsSessionClient` + session models | Milestone 2: Secure session API |
| M3 — `IKmsPayloadClient` + `KmsPayloadClient` + payload models | Milestone 3: Payload protection |
| M4 — `IKmsInternalClient` + `KmsInternalClient` + datakey models | Milestone 4: Vault envelope encryption |
| M5 — `IKmsKeyClient` + `KmsKeyClient` + key rotation models | Milestone 5: Internal security gateway |
| M6 — `SupportedSuites` field in `StartSecureSessionRequest` wired to PQ suite | Milestone 6: PQ/hybrid experiment |

---

## 16. Items that stay in Contracts, not Client

These types are defined **once** in `Synaptix.Security.Kms.Contracts` and referenced by the client:

| Type | From handoff |
|---|---|
| `EncryptedPayload` | Models section — `Ciphertext`, `Nonce`, `Mac`, `ContentType`, `EncryptedAtUtc` |
| `SecureSession` | Models section — full session state record |
| `WrappedDataKey` | Models section — `PlaintextKey`, `EncryptedKey`, `KeyVersion` |
| `SecureSuites` | Post-quantum readiness — `ClassicalV1`, `HybridPqV1` constants |

The client models in section 7 are the **wire DTOs** (what is sent over HTTP). They are intentionally separate from the domain records in Contracts so the API serialization contract can evolve independently.

---

## 17. What this project does NOT contain

- No cryptographic primitives. AEAD encryption/decryption stays inside `Synaptix.Security.Kms.Application` and `Synaptix.Security.Kms.Infrastructure`.
- No Redis replay store. That is `IReplayProtectionStore` in `Synaptix.Security.Kms.Infrastructure`.
- No Vault Transit client. That is `VaultEnvelopeEncryptionService` in `Synaptix.Security.Kms.Infrastructure`.
- No `EncryptedPayloadEndpointFilter`. That stays in `Synaptix.Security.Kms.Api` or `Tycoon.Backend.Api`.
- No JWT validation. Callers keep their own JWT middleware.
