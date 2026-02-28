using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Tests.AdminAuth;

public sealed class AdminOpsKeyContractTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;

    public AdminOpsKeyContractTests(TycoonApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AdminLogin_WithoutOpsKey_Returns401()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/admin/auth/login", new AdminLoginRequest("x@example.com", "badpass"));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await resp.HasErrorCodeAsync("UNAUTHORIZED");
    }

    [Fact]
    public async Task AdminLogin_WithWrongOpsKey_Returns403()
    {
        var client = _factory.CreateClient().WithAdminOpsKey("wrong-key");

        var resp = await client.PostAsJsonAsync("/admin/auth/login", new AdminLoginRequest("x@example.com", "badpass"));
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await resp.HasErrorCodeAsync("FORBIDDEN");
    }

    [Fact]
    public async Task AdminMe_WithMalformedBearer_Returns401()
    {
        var client = _factory.CreateClient().WithAdminOpsKey();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "not-a-jwt");

        var resp = await client.GetAsync("/admin/auth/me");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await resp.HasErrorCodeAsync("UNAUTHORIZED");
    }

    [Fact]
    public async Task NotificationSend_WithMalformedBearer_Returns401()
    {
        var client = _factory.CreateClient().WithAdminOpsKey();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "not-a-jwt");

        var resp = await client.PostAsJsonAsync("/admin/notifications/send",
            new AdminNotificationSendRequest("Title", "Body", "admin_basic", new Dictionary<string, object> { ["segment"] = "all" }, null));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await resp.HasErrorCodeAsync("UNAUTHORIZED");
    }
}
