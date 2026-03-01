using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Tests.AdminNotifications;

public sealed class AdminNotificationsSecurityContractTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _http;

    public AdminNotificationsSecurityContractTests(TycoonApiFactory factory)
    {
        _http = factory.CreateClient().WithAdminOpsKey();
    }


    [Fact]
    public async Task Send_WithWrongOpsKey_Returns403()
    {
        var wrongKey = new TycoonApiFactory().CreateClient().WithAdminOpsKey("wrong-key");

        var resp = await wrongKey.PostAsJsonAsync("/admin/notifications/send",
            new AdminNotificationSendRequest("Title", "Body", "admin_basic", new Dictionary<string, object>{{"segment", "all"}}, null));

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await resp.HasErrorCodeAsync("FORBIDDEN");
    }

    [Fact]
    public async Task Channels_WithWrongOpsKey_Returns403()
    {
        var wrongKey = new TycoonApiFactory().CreateClient().WithAdminOpsKey("wrong-key");

        var resp = await wrongKey.GetAsync("/admin/notifications/channels");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await resp.HasErrorCodeAsync("FORBIDDEN");
    }

    [Fact]
    public async Task Send_WithoutBearer_Returns401()
    {
        var resp = await _http.PostAsJsonAsync("/admin/notifications/send",
            new AdminNotificationSendRequest("Title", "Body", "admin_basic", new Dictionary<string, object>{{"segment", "all"}}, null));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await resp.HasErrorCodeAsync("UNAUTHORIZED");
    }

    [Fact]
    public async Task Send_WithUserBearer_Returns403()
    {
        var userToken = await SignupAndGetUserTokenAsync();

        using var req = new HttpRequestMessage(HttpMethod.Post, "/admin/notifications/send")
        {
            Content = JsonContent.Create(new AdminNotificationSendRequest("Title", "Body", "admin_basic", new Dictionary<string, object>{{"segment", "all"}}, null))
        };
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken);

        var resp = await _http.SendAsync(req);
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await resp.HasErrorCodeAsync("FORBIDDEN");
    }

    [Fact]
    public async Task Send_WithAdminBearer_ReturnsAccepted()
    {
        var adminToken = await SignupAndGetAdminTokenAsync();

        // seed default channel (admin_basic)
        using (var seedReq = new HttpRequestMessage(HttpMethod.Get, "/admin/notifications/channels"))
        {
            seedReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
            var seedResp = await _http.SendAsync(seedReq);
            seedResp.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using var req = new HttpRequestMessage(HttpMethod.Post, "/admin/notifications/send")
        {
            Content = JsonContent.Create(new AdminNotificationSendRequest("Title", "Body", "admin_basic", new Dictionary<string, object>{{"segment", "all"}}, null))
        };
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        var resp = await _http.SendAsync(req);
        resp.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }



    [Fact]
    public async Task Channels_WithoutBearer_Returns401()
    {
        var resp = await _http.GetAsync("/admin/notifications/channels");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await resp.HasErrorCodeAsync("UNAUTHORIZED");
    }


    [Fact]
    public async Task History_WithWrongOpsKey_Returns403()
    {
        var wrongKey = new TycoonApiFactory().CreateClient().WithAdminOpsKey("wrong-key");

        var resp = await wrongKey.GetAsync("/admin/notifications/history?page=1&pageSize=25");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await resp.HasErrorCodeAsync("FORBIDDEN");
    }

    [Fact]
    public async Task History_WithUserBearer_Returns403()
    {
        var userToken = await SignupAndGetUserTokenAsync();

        using var req = new HttpRequestMessage(HttpMethod.Get, "/admin/notifications/history?page=1&pageSize=25");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken);

        var resp = await _http.SendAsync(req);
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await resp.HasErrorCodeAsync("FORBIDDEN");
    }


    [Fact]
    public async Task DeadLetterList_WithWrongOpsKey_Returns403()
    {
        var wrongKey = new TycoonApiFactory().CreateClient().WithAdminOpsKey("wrong-key");

        var resp = await wrongKey.GetAsync("/admin/notifications/dead-letter?page=1&pageSize=25");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await resp.HasErrorCodeAsync("FORBIDDEN");
    }

    [Fact]
    public async Task DeadLetterReplay_WithWrongOpsKey_Returns403()
    {
        var wrongKey = new TycoonApiFactory().CreateClient().WithAdminOpsKey("wrong-key");

        var resp = await wrongKey.PostAsync("/admin/notifications/dead-letter/nonexistent/replay", content: null);
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await resp.HasErrorCodeAsync("FORBIDDEN");
    }

    [Fact]
    public async Task DeadLetterReplay_WithoutBearer_Returns401()
    {
        var resp = await _http.PostAsync("/admin/notifications/dead-letter/nonexistent/replay", content: null);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await resp.HasErrorCodeAsync("UNAUTHORIZED");
    }

    [Fact]
    public async Task DeadLetterReplay_WithUserBearer_Returns403()
    {
        var userToken = await SignupAndGetUserTokenAsync();

        using var req = new HttpRequestMessage(HttpMethod.Post, "/admin/notifications/dead-letter/nonexistent/replay");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken);

        var resp = await _http.SendAsync(req);
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await resp.HasErrorCodeAsync("FORBIDDEN");
    }



    [Fact]
    public async Task TemplatesCreate_WithWrongOpsKey_Returns403()
    {
        var wrongKey = new TycoonApiFactory().CreateClient().WithAdminOpsKey("wrong-key");

        var resp = await wrongKey.PostAsJsonAsync("/admin/notifications/templates",
            new AdminNotificationTemplateRequest("promo", "T", "B", "admin_basic", new[] { "v" }));
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await resp.HasErrorCodeAsync("FORBIDDEN");
    }

    [Fact]
    public async Task TemplatesCreate_WithoutBearer_Returns401()
    {
        var resp = await _http.PostAsJsonAsync("/admin/notifications/templates",
            new AdminNotificationTemplateRequest("promo", "T", "B", "admin_basic", new[] { "v" }));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await resp.HasErrorCodeAsync("UNAUTHORIZED");
    }

    [Fact]
    public async Task TemplatesCreate_WithUserBearer_Returns403()
    {
        var userToken = await SignupAndGetUserTokenAsync();

        using var req = new HttpRequestMessage(HttpMethod.Post, "/admin/notifications/templates")
        {
            Content = JsonContent.Create(new AdminNotificationTemplateRequest("promo", "T", "B", "admin_basic", new[] { "v" }))
        };
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken);

        var resp = await _http.SendAsync(req);
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await resp.HasErrorCodeAsync("FORBIDDEN");
    }


    [Fact]
    public async Task DeadLetterList_WithoutBearer_Returns401()
    {
        var resp = await _http.GetAsync("/admin/notifications/dead-letter?page=1&pageSize=25");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await resp.HasErrorCodeAsync("UNAUTHORIZED");
    }

    [Fact]
    public async Task DeadLetterList_WithUserBearer_Returns403()
    {
        var userToken = await SignupAndGetUserTokenAsync();

        using var req = new HttpRequestMessage(HttpMethod.Get, "/admin/notifications/dead-letter?page=1&pageSize=25");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken);

        var resp = await _http.SendAsync(req);
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await resp.HasErrorCodeAsync("FORBIDDEN");
    }

    private async Task<string> SignupAndGetUserTokenAsync()
    {
        var email = $"user_{Guid.NewGuid():N}@example.com";
        var password = "Passw0rd!123";
        var deviceId = $"dev-{Guid.NewGuid():N}";

        var signupResp = await _http.PostAsJsonAsync("/auth/signup",
            new SignupRequest(email, password, deviceId, Username: $"u_{Guid.NewGuid():N}"));

        signupResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var signup = await signupResp.Content.ReadFromJsonAsync<SignupResponse>();
        signup.Should().NotBeNull();
        signup!.AccessToken.Should().NotBeNullOrWhiteSpace();

        return signup.AccessToken;
    }

    private async Task<string> SignupAndGetAdminTokenAsync()
    {
        var email = $"admin_{Guid.NewGuid():N}@example.com";
        var password = "Passw0rd!123";
        var deviceId = $"dev-{Guid.NewGuid():N}";

        var signupResp = await _http.PostAsJsonAsync("/auth/signup",
            new SignupRequest(email, password, deviceId, Username: $"adm_{Guid.NewGuid():N}"));
        signupResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var adminLoginResp = await _http.PostAsJsonAsync("/admin/auth/login", new AdminLoginRequest(email, password));
        adminLoginResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var login = await adminLoginResp.Content.ReadFromJsonAsync<AdminLoginResponse>();
        login.Should().NotBeNull();
        login!.AccessToken.Should().NotBeNullOrWhiteSpace();

        return login.AccessToken;
    }
}
