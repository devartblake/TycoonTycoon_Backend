using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Backend.Application.Auth;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Api.Tests.AdminSetup;

public sealed class AdminSetupEndpointsTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _http;

    public AdminSetupEndpointsTests(TycoonApiFactory factory)
    {
        _http = factory.CreateClient().WithAdminOpsKey();
    }

    [Theory]
    [InlineData("/admin/setup/status")]
    [InlineData("/admin/setup/readiness")]
    [InlineData("/admin/setup/services")]
    [InlineData("/admin/setup/seeds")]
    [InlineData("/admin/setup/validation")]
    [InlineData("/admin/setup/history")]
    public async Task ReadOnly_Setup_Endpoints_Return_Sanitized_Contracts(string path)
    {
        var response = await _http.GetAsync(path);

        response.IsSuccessStatusCode.Should().BeTrue();
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain(TycoonApiFactory.TestAdminKey);
        body.ToLowerInvariant().Should().NotContain("connectionstrings");
        body.ToLowerInvariant().Should().NotContain("password");
    }

    [Fact]
    public async Task Status_Declares_Live_ReadOnly_Durable_Source_When_Report_Store_Is_Available()
    {
        var response = await _http.GetFromJsonAsync<JsonElement>("/admin/setup/status");

        response.GetProperty("source").GetString().Should().StartWith("live-backend-diagnostics");
        response.GetProperty("readOnly").GetBoolean().Should().BeTrue();
        response.GetProperty("durableReportAvailable").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task History_Returns_Sanitized_Durable_Report_Summaries()
    {
        await _http.GetAsync("/admin/setup/status");

        var response = await _http.GetFromJsonAsync<JsonElement>("/admin/setup/history?limit=5");

        response.GetProperty("source").GetString().Should().Be("durable-setup-report-store");
        response.GetProperty("reports").GetArrayLength().Should().BeGreaterThan(0);

        var body = response.GetRawText();
        body.Should().NotContain(TycoonApiFactory.TestAdminKey);
        body.ToLowerInvariant().Should().NotContain("connectionstrings");
        body.ToLowerInvariant().Should().NotContain("password");
    }

    [Fact]
    public async Task Latest_History_Returns_Sanitized_Durable_Report_Detail()
    {
        await _http.GetAsync("/admin/setup/status");

        var response = await _http.GetFromJsonAsync<JsonElement>("/admin/setup/history/latest");

        response.GetProperty("source").GetString().Should().Be("live-backend-diagnostics");
        response.GetProperty("report").GetProperty("status").GetProperty("readOnly").GetBoolean().Should().BeTrue();

        var body = response.GetRawText();
        body.Should().NotContain(TycoonApiFactory.TestAdminKey);
        body.ToLowerInvariant().Should().NotContain("connectionstrings");
        body.ToLowerInvariant().Should().NotContain("password");
    }

    [Fact]
    public async Task Services_Return_Expected_Sanitized_Service_Names()
    {
        var response = await _http.GetFromJsonAsync<JsonElement>("/admin/setup/services");
        var names = response.GetProperty("services").EnumerateArray()
            .Select(x => x.GetProperty("name").GetString())
            .ToArray();

        names.Should().Contain(["postgresql", "mongodb", "redis", "rabbitmq", "minio", "elasticsearch", "kms"]);
    }

    [Fact]
    public void PermissionProfiles_Assign_SetupRead_To_Admins_Only()
    {
        AdminPermissionProfiles.ForRole(AdminRole.Admin).Permissions.Should().Contain("setup:read");
        AdminPermissionProfiles.ForRole(AdminRole.SuperAdmin).Permissions.Should().Contain("setup:read");
        AdminPermissionProfiles.ForRole(AdminRole.Moderator).Permissions.Should().NotContain("setup:read");
        AdminPermissionProfiles.ForRole(AdminRole.Viewer).Permissions.Should().NotContain("setup:read");
    }
}
