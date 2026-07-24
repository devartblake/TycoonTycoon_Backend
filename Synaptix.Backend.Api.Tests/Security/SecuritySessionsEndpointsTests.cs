using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Security.Kms.Client.Abstractions;
using Synaptix.Security.Kms.Client.Exceptions;
using Synaptix.Security.Kms.Client.Models.Requests;
using Synaptix.Security.Kms.Client.Models.Responses;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Tests.Security;

/// <summary>
/// The secure-session handshake proxies to the KMS service. When that dependency
/// is unreachable (transport failure / timeout / open circuit) the proxy must not
/// leak an opaque 500 — it should return a clean, retryable 503. An upstream KMS
/// 5xx should surface as 502, not a misleading 400.
/// </summary>
public sealed class SecuritySessionsEndpointsTests : IClassFixture<SynaptixApiFactory>
{
    private readonly SynaptixApiFactory _factory;

    public SecuritySessionsEndpointsTests(SynaptixApiFactory factory) => _factory = factory;

    private static readonly object StartBody = new
    {
        deviceId = "ios-sim",
        clientNonce = "AAAAAAAAAAAAAAAAAAAAAA",
        clientPublicKey = "AAAAAAAAAAAAAAAAAAAAAA",
        supportedSuites = new[] { "X25519-HKDF-SHA256-AES256GCM" },
    };

    [Fact]
    public async Task Start_WhenKmsUnreachable_Returns503_NotOpaque500()
    {
        using var factory = CreateFactory(
            new FakeKmsSessionClient(() => new HttpRequestException("connection refused")));
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, "secsess-unreachable");

        var response = await client.PostAsJsonAsync(
            "/api/v1/security/sessions/start", StartBody);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Start_WhenKmsTimesOut_Returns503()
    {
        using var factory = CreateFactory(
            new FakeKmsSessionClient(() => new TaskCanceledException("timed out")));
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, "secsess-timeout");

        var response = await client.PostAsJsonAsync(
            "/api/v1/security/sessions/start", StartBody);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Start_WhenKmsReturns500_Returns502_NotBadRequest()
    {
        using var factory = CreateFactory(
            new FakeKmsSessionClient(() =>
                new KmsClientException("KMS session/start failed with 500: boom", 500)));
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, "secsess-upstream500");

        var response = await client.PostAsJsonAsync(
            "/api/v1/security/sessions/start", StartBody);

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
    }

    private WebApplicationFactory<Program> CreateFactory(IKmsSessionClient fake)
        => _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IKmsSessionClient>();
                services.AddSingleton(fake);
            });
        });

    private static async Task AuthenticateAsync(HttpClient client, string prefix)
    {
        var signupResp = await client.PostAsJsonAsync(
            "/api/v1/auth/signup",
            new SignupRequest(
                Email: $"{prefix}-{Guid.NewGuid():N}@example.com",
                Password: "Passw0rd!",
                DeviceId: "ios-sim",
                Username: $"{prefix}_{Guid.NewGuid():N}"));

        signupResp.EnsureSuccessStatusCode();
        var signup = await signupResp.Content.ReadFromJsonAsync<SignupResponse>();
        signup.Should().NotBeNull();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", signup!.AccessToken);
    }

    private sealed class FakeKmsSessionClient(Func<Exception> exceptionFactory) : IKmsSessionClient
    {
        public Task<StartSecureSessionResponse> StartAsync(
            StartSecureSessionRequest request, CancellationToken ct)
            => throw exceptionFactory();

        public Task<RenewSecureSessionResponse> RenewAsync(
            RenewSecureSessionRequest request, CancellationToken ct)
            => throw exceptionFactory();

        public Task RevokeAsync(RevokeSecureSessionRequest request, CancellationToken ct)
            => throw exceptionFactory();
    }
}
