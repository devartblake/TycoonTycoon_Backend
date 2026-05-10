using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Security.Kms.Client.Abstractions;
using Tycoon.Security.Kms.Client.Exceptions;
using Tycoon.Security.Kms.Client.Models.Requests;
using Tycoon.Security.Kms.Client.Models.Responses;

namespace Tycoon.Backend.Api.Tests.Security;

/// Integration tests for SecureChannelFilter behaviour, using /auth/refresh as the
/// test endpoint (no auth requirement, only RequireSecureChannel).
public sealed class SecureChannelFilterTests : IClassFixture<SecureChannelApiFactory>
{
    private readonly SecureChannelApiFactory _factory;
    private readonly HttpClient _http;

    public SecureChannelFilterTests(SecureChannelApiFactory factory)
    {
        _factory = factory;
        _http = factory.CreateClient();
    }

    private static StringContent ValidEnvelope(string sessionId) =>
        new(JsonSerializer.Serialize(new
        {
            ciphertext = "dGVzdA",
            nonce = "bm9uY2U",
            mac = "dGFn",
            contentType = "application/json",
            encryptedAtUtc = DateTimeOffset.UtcNow
        }), Encoding.UTF8, "application/json");

    // ─── Test 1: no session header ────────────────────────────────────────────

    [Fact]
    public async Task MissingSessionHeader_Returns400WithSecureSessionRequiredCode()
    {
        var resp = await _http.PostAsJsonAsync("/auth/refresh", new { refreshToken = "any" });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await resp.Content.ReadAsStringAsync();
        body.Should().Contain("secure_session_required");
    }

    // ─── Test 2: malformed body ───────────────────────────────────────────────

    [Fact]
    public async Task MalformedBody_Returns400WithInvalidPayloadCode()
    {
        var sessionId = Guid.NewGuid().ToString();
        var req = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh")
        {
            Content = new StringContent(@"{""not_an_envelope"":true}", Encoding.UTF8, "application/json")
        };
        req.Headers.Add("X-Syn-Sec-Session", sessionId);

        var resp = await _http.SendAsync(req);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await resp.Content.ReadAsStringAsync();
        body.Should().Contain("invalid_encrypted_payload");
    }

    // ─── Test 3: KMS returns 401 ─────────────────────────────────────────────

    [Fact]
    public async Task KmsDecryptReturns401_FilterReturns401WithSecureSessionInvalidCode()
    {
        _factory.FakeKms.DecryptException =
            new KmsClientException("session not found", statusCode: 401);

        var sessionId = Guid.NewGuid().ToString();
        var req = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh")
        {
            Content = ValidEnvelope(sessionId)
        };
        req.Headers.Add("X-Syn-Sec-Session", sessionId);

        var resp = await _http.SendAsync(req);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await resp.Content.ReadAsStringAsync();
        body.Should().Contain("secure_session_invalid");

        _factory.FakeKms.DecryptException = null; // reset
    }

    // ─── Test 4: full roundtrip — response is encrypted ─────────────────────

    [Fact]
    public async Task ValidEncryptedRequest_ResponseBodyIsEncryptedEnvelope()
    {
        // FakeKms decrypts to a valid (but invalid-credentials) RefreshRequest
        _factory.FakeKms.DecryptResponse = _ => new DecryptPayloadResponse(
            Encoding.UTF8.GetBytes(@"{""refreshToken"":""bad-token""}"),
            "application/json");

        var sessionId = Guid.NewGuid().ToString();
        var req = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh")
        {
            Content = ValidEnvelope(sessionId)
        };
        req.Headers.Add("X-Syn-Sec-Session", sessionId);

        var resp = await _http.SendAsync(req);

        // Status code is preserved from the inner handler (401 for invalid token)
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Response body must be an encrypted envelope, not the raw error JSON
        var body = await resp.Content.ReadAsStringAsync();
        var envelope = JsonSerializer.Deserialize<JsonElement>(body);
        envelope.TryGetProperty("ciphertext", out _).Should().BeTrue();
        envelope.TryGetProperty("nonce", out _).Should().BeTrue();
        envelope.TryGetProperty("mac", out _).Should().BeTrue();

        _factory.FakeKms.DecryptResponse = null; // reset
    }

    // ─── Test 5: unprotected endpoint ignores the header ─────────────────────

    [Fact]
    public async Task UnprotectedEndpoint_WithSessionHeader_ProcessesNormally()
    {
        // /auth/login is NOT protected by secure channel (pre-auth)
        var req = new HttpRequestMessage(HttpMethod.Post, "/auth/login")
        {
            Content = new StringContent(
                @"{""email"":""x@x.com"",""password"":""bad"",""deviceId"":""dev1""}",
                Encoding.UTF8, "application/json")
        };
        req.Headers.Add("X-Syn-Sec-Session", Guid.NewGuid().ToString());

        var resp = await _http.SendAsync(req);

        // The header is ignored on unprotected endpoints — should NOT return 400 secure_session_required
        // (will return 401 for invalid credentials, which is the handler's own response)
        resp.StatusCode.Should().NotBe(HttpStatusCode.BadRequest);
        var body = await resp.Content.ReadAsStringAsync();
        body.Should().NotContain("secure_session_required");
    }
}

/// Test factory that replaces IKmsPayloadClient with a controllable fake.
public sealed class SecureChannelApiFactory : TycoonApiFactory
{
    public FakeKmsPayloadClient FakeKms { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Satisfy KmsClientOptions binding; the real HTTP client is replaced below
                ["KmsClient:BaseUrl"] = "http://test-kms-not-used",
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IKmsPayloadClient>();
            services.AddSingleton<IKmsPayloadClient>(FakeKms);
        });
    }
}

/// Controllable test double for IKmsPayloadClient.
public sealed class FakeKmsPayloadClient : IKmsPayloadClient
{
    public Func<DecryptPayloadRequest, DecryptPayloadResponse>? DecryptResponse { get; set; }
    public Exception? DecryptException { get; set; }

    public Task<DecryptPayloadResponse> DecryptAsync(
        DecryptPayloadRequest request, CancellationToken ct = default)
    {
        if (DecryptException is not null) throw DecryptException;

        var response = DecryptResponse?.Invoke(request)
            ?? new DecryptPayloadResponse(
                Encoding.UTF8.GetBytes(@"{""refreshToken"":""test-token""}"),
                "application/json");

        return Task.FromResult(response);
    }

    public Task<EncryptPayloadResponse> EncryptAsync(
        EncryptPayloadRequest request, CancellationToken ct = default)
        => Task.FromResult(new EncryptPayloadResponse(
            Convert.ToBase64String(request.Plaintext),
            "test-nonce-" + Guid.NewGuid().ToString("N")[..8],
            "test-mac",
            request.ContentType,
            DateTimeOffset.UtcNow));
}
