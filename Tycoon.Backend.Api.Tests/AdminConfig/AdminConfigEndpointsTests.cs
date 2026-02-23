using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Api.Tests.AdminConfig;

public sealed class AdminConfigEndpointsTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _http;

    public AdminConfigEndpointsTests(TycoonApiFactory factory)
    {
        _http = factory.CreateClient().WithAdminOpsKey();
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
    }
}
