using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Security.Kms.Client.Abstractions;
using Synaptix.Security.Kms.Client.Exceptions;
using Synaptix.Security.Kms.Client.Models.Requests;
using Synaptix.Security.Kms.Client.Models.Responses;

namespace Synaptix.Backend.Api.Tests.Security;

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

    private static StringContent ValidEnvelope() =>
        new(JsonSerializer.Serialize(new
        {
            ciphertext = "dGVzdA",
            nonce = "bm9uY2U",
            mac = "dGFn",
            contentType = "application/json",
            encryptedAtUtc = DateTimeOffset.UtcNow
        }), Encoding.UTF8, "application/json");

    private static void AddSecureHeaders(HttpRequestMessage req, string sessionId, long sequence = 1, string replayNonce = "replay-nonce-1")
    {
        req.Headers.Add("X-Syn-Sec-Session", sessionId);
        req.Headers.Add("X-Syn-Sec-Seq", sequence.ToString(System.Globalization.CultureInfo.InvariantCulture));
        req.Headers.Add("X-Syn-Sec-Nonce", replayNonce);
    }

    // ─── Test 1: no session header ────────────────────────────────────────────

    [Fact]
    public async Task MissingSessionHeader_Returns400WithSecureSessionRequiredCode()
    {
        var resp = await _http.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = "any" });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await resp.Content.ReadAsStringAsync();
        body.Should().Contain("secure_session_required");
    }

    // ─── Test 2: malformed body ───────────────────────────────────────────────

    [Fact]
    public async Task MalformedBody_Returns400WithInvalidPayloadCode()
    {
        var sessionId = Guid.NewGuid().ToString();
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh")
        {
            Content = new StringContent(@"{""not_an_envelope"":true}", Encoding.UTF8, "application/json")
        };
        AddSecureHeaders(req, sessionId);

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
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh")
        {
            Content = ValidEnvelope()
        };
        AddSecureHeaders(req, sessionId);

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
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh")
        {
            Content = ValidEnvelope()
        };
        AddSecureHeaders(req, sessionId, 42, "nonce-roundtrip");

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

    [Fact]
    public async Task MissingSequenceHeader_Returns400WithSequenceRequiredCode()
    {
        var sessionId = Guid.NewGuid().ToString();
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh")
        {
            Content = ValidEnvelope()
        };
        req.Headers.Add("X-Syn-Sec-Session", sessionId);
        req.Headers.Add("X-Syn-Sec-Nonce", "nonce-without-sequence");

        var resp = await _http.SendAsync(req);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await resp.Content.ReadAsStringAsync();
        body.Should().Contain("secure_sequence_required");
    }

    [Fact]
    public async Task InvalidSequenceHeader_Returns400WithSequenceRequiredCode()
    {
        var sessionId = Guid.NewGuid().ToString();
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh")
        {
            Content = ValidEnvelope()
        };
        req.Headers.Add("X-Syn-Sec-Session", sessionId);
        req.Headers.Add("X-Syn-Sec-Seq", "0");
        req.Headers.Add("X-Syn-Sec-Nonce", "nonce-invalid-sequence");

        var resp = await _http.SendAsync(req);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await resp.Content.ReadAsStringAsync();
        body.Should().Contain("secure_sequence_required");
    }

    [Fact]
    public async Task MissingReplayNonceHeader_Returns400WithReplayNonceRequiredCode()
    {
        var sessionId = Guid.NewGuid().ToString();
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh")
        {
            Content = ValidEnvelope()
        };
        req.Headers.Add("X-Syn-Sec-Session", sessionId);
        req.Headers.Add("X-Syn-Sec-Seq", "7");

        var resp = await _http.SendAsync(req);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await resp.Content.ReadAsStringAsync();
        body.Should().Contain("secure_replay_nonce_required");
    }

    [Fact]
    public async Task ValidEncryptedRequest_PassesReplayMetadataAndAadToKms()
    {
        var sessionId = Guid.NewGuid();
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh")
        {
            Content = ValidEnvelope()
        };
        AddSecureHeaders(req, sessionId.ToString(), 99, "nonce-metadata");

        await _http.SendAsync(req);

        _factory.FakeKms.LastDecryptRequest.Should().NotBeNull();
        _factory.FakeKms.LastDecryptRequest!.SessionId.Should().Be(sessionId);
        _factory.FakeKms.LastDecryptRequest.SequenceNumber.Should().Be(99);
        _factory.FakeKms.LastDecryptRequest.ReplayNonce.Should().Be("nonce-metadata");
        _factory.FakeKms.LastDecryptRequest.Aad.Should().Contain("syn-sec-v1|request|POST|/api/v1/auth/refresh");
        _factory.FakeKms.LastEncryptRequest.Should().NotBeNull();
        _factory.FakeKms.LastEncryptRequest!.Aad.Should().Contain("syn-sec-v1|response|POST|/api/v1/auth/refresh");
    }

    // ─── Test 5: unprotected endpoint ignores the header ─────────────────────

    [Fact]
    public async Task UnprotectedEndpoint_WithSessionHeader_ProcessesNormally()
    {
        // /auth/login is NOT protected by secure channel (pre-auth)
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
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
                ["SecureChannel:AllowPlainJsonInTests"] = "false",
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IKmsPayloadClient>();
            services.RemoveAll<IKmsInternalClient>();
            services.AddSingleton<IKmsPayloadClient>(FakeKms);
            services.AddSingleton<IKmsInternalClient>(FakeKms);
        });
    }
}

/// Controllable test double for IKmsPayloadClient.
public sealed class FakeKmsPayloadClient : IKmsPayloadClient, IKmsInternalClient
{
    public Func<DecryptPayloadRequest, DecryptPayloadResponse>? DecryptResponse { get; set; }
    public Exception? DecryptException { get; set; }
    public DecryptPayloadRequest? LastDecryptRequest { get; private set; }
    public EncryptPayloadRequest? LastEncryptRequest { get; private set; }

    public Task<DecryptPayloadResponse> DecryptAsync(
        DecryptPayloadRequest request, CancellationToken ct = default)
    {
        LastDecryptRequest = request;
        if (DecryptException is not null) throw DecryptException;

        var response = DecryptResponse?.Invoke(request)
            ?? new DecryptPayloadResponse(
                Encoding.UTF8.GetBytes(@"{""refreshToken"":""test-token""}"),
                "application/json");

        return Task.FromResult(response);
    }

    public Task<EncryptPayloadResponse> EncryptAsync(
        EncryptPayloadRequest request, CancellationToken ct = default)
    {
        LastEncryptRequest = request;
        return Task.FromResult(new EncryptPayloadResponse(
            Convert.ToBase64String(request.Plaintext),
            "test-nonce-" + Guid.NewGuid().ToString("N")[..8],
            "test-mac",
            request.ContentType,
            DateTimeOffset.UtcNow));
    }

    public Task<GenerateDataKeyResponse> GenerateDataKeyAsync(
        GenerateDataKeyRequest request,
        CancellationToken ct = default)
        => Task.FromResult(new GenerateDataKeyResponse(
            new byte[32],
            "test-encrypted-key",
            "test-key-version"));
}
