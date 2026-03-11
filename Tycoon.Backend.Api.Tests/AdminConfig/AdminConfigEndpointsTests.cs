using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Api.Tests.AdminConfig;

public sealed class AdminConfigEndpointsTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;
    private readonly HttpClient _http;

    public AdminConfigEndpointsTests(TycoonApiFactory factory)
    {
        _factory = factory;
        _http = factory.CreateClient().WithAdminOpsKey();
    }



    [Fact]
    public async Task Config_Routes_Reject_Wrong_OpsKey()
    {
        using var wrongKey = _factory.CreateClient().WithAdminOpsKey("wrong-key");

        var getResp = await wrongKey.GetAsync("/admin/config");
        getResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await getResp.HasErrorCodeAsync("FORBIDDEN");

        var patchResp = await wrongKey.PatchAsJsonAsync("/admin/config",
            new UpdateAdminAppConfigRequest(EnableLogging: true, FeatureFlags: new Dictionary<string, bool>{{"adminEventUpload", true}}));
        patchResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await patchResp.HasErrorCodeAsync("FORBIDDEN");
    }

    [Fact]
    public async Task Config_Routes_Require_OpsKey()
    {
        using var noKey = _factory.CreateClient();

        var getResp = await noKey.GetAsync("/admin/config");
        getResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await getResp.HasErrorCodeAsync("UNAUTHORIZED");

        var patchResp = await noKey.PatchAsJsonAsync("/admin/config",
            new UpdateAdminAppConfigRequest(EnableLogging: true, FeatureFlags: new Dictionary<string, bool>{{"adminEventUpload", true}}));
        patchResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await patchResp.HasErrorCodeAsync("UNAUTHORIZED");
    }

    [Fact]
    public async Task Get_And_Patch_Config_Works()
    {
        var getResp = await _http.GetAsync("/admin/config");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var cfg = await getResp.Content.ReadFromJsonAsync<AdminAppConfigDto>();
        cfg.Should().NotBeNull();

        var patchResp = await _http.PatchAsJsonAsync("/admin/config",
            new UpdateAdminAppConfigRequest(EnableLogging: true, FeatureFlags: new Dictionary<string, bool>{{"adminEventUpload", true}}));
        patchResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var patchBody = await patchResp.Content.ReadFromJsonAsync<UpdateAdminAppConfigResponse>();
        patchBody.Should().NotBeNull();

        var verifyResp = await _http.GetAsync("/admin/config");
        verifyResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var verifyCfg = await verifyResp.Content.ReadFromJsonAsync<AdminAppConfigDto>();
        verifyCfg!.EnableLogging.Should().BeTrue();
    }
}
