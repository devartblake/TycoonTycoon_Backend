using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Api.Tests.AdminUsers;

public sealed class AdminUsersEndpointsTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _http;

    public AdminUsersEndpointsTests(TycoonApiFactory factory)
    {
        _http = factory.CreateClient().WithAdminOpsKey();
    }

    [Fact]
    public async Task Crud_And_Activity_Flow_Works()
    {
        var create = new AdminCreateUserRequest(
            Username: $"user_{Guid.NewGuid():N}"[..12],
            Email: $"{Guid.NewGuid():N}@example.com",
            Role: "user",
            AgeGroup: "adult",
            IsVerified: false,
            TemporaryPassword: "TempPass123!"
        );

        var createdResp = await _http.PostAsJsonAsync("/admin/users", create);
        createdResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createdResp.Content.ReadFromJsonAsync<AdminCreateUserResponse>();
        created.Should().NotBeNull();
        created!.Id.Should().StartWith("usr_");

        var getResp = await _http.GetAsync($"/admin/users/{created.Id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await getResp.Content.ReadFromJsonAsync<AdminUserDetailDto>();
        detail.Should().NotBeNull();
        detail!.Email.Should().Be(create.Email.ToLowerInvariant());

        var patchResp = await _http.PatchAsJsonAsync($"/admin/users/{created.Id}", new AdminUpdateUserRequest(Username: "updated_name"));
        patchResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var banResp = await _http.PostAsJsonAsync($"/admin/users/{created.Id}/ban", new AdminBanUserRequest("abuse", DateTimeOffset.UtcNow.AddDays(7)));
        banResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var ban = await banResp.Content.ReadFromJsonAsync<AdminBanUserResponse>();
        ban!.IsBanned.Should().BeTrue();

        var listResp = await _http.GetAsync("/admin/users?page=1&pageSize=25&isBanned=true");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await listResp.Content.ReadFromJsonAsync<AdminUsersListResponse>();
        list.Should().NotBeNull();
        list!.Items.Should().Contain(x => x.Id == created.Id);

        var unbanResp = await _http.PostAsync($"/admin/users/{created.Id}/unban", JsonContent.Create(new { }));
        unbanResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var activityResp = await _http.GetAsync($"/admin/users/{created.Id}/activity?page=1&pageSize=50");
        activityResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var activity = await activityResp.Content.ReadFromJsonAsync<AdminUserActivityResponse>();
        activity.Should().NotBeNull();
        activity!.Items.Should().NotBeEmpty();

        var deleteResp = await _http.DeleteAsync($"/admin/users/{created.Id}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var missingResp = await _http.GetAsync($"/admin/users/{created.Id}");
        missingResp.StatusCode.Should().Be(HttpStatusCode.NotFound);

        await missingResp.HasErrorCodeAsync("NOT_FOUND");
    }

    [Fact]
    public async Task AdminRoutes_Reject_Wrong_OpsKey()
    {
        using var wrongKey = new TycoonApiFactory().CreateClient().WithAdminOpsKey("wrong-key");
        var r = await wrongKey.GetAsync("/admin/users");
        r.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await r.HasErrorCodeAsync("FORBIDDEN");
    }

    [Fact]
    public async Task AdminRoutes_Require_OpsKey()
    {
        using var noKey = new TycoonApiFactory().CreateClient();
        var r = await noKey.GetAsync("/admin/users");
        r.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await r.HasErrorCodeAsync("UNAUTHORIZED");
    }
}
