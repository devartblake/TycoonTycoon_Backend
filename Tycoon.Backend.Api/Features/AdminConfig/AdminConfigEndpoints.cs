using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.AdminConfig;

public static class AdminConfigEndpoints
{
    private static AdminAppConfigDto _config = new(
        ApiBaseUrl: "https://api.example.com",
        EnableLogging: false,
        FeatureFlags: new Dictionary<string, bool>
        {
            ["adminEventUpload"] = true
        });

    public static void Map(RouteGroupBuilder admin)
    {
        var g = admin.MapGroup("/config").WithTags("Admin/Config").WithOpenApi();

        g.MapGet("", () => Results.Ok(_config));

        g.MapPatch("", ([FromBody] UpdateAdminAppConfigRequest request) =>
        {
            _config = _config with
            {
                EnableLogging = request.EnableLogging ?? _config.EnableLogging,
                FeatureFlags = request.FeatureFlags ?? _config.FeatureFlags
            };

            return Results.Ok(new UpdateAdminAppConfigResponse(DateTimeOffset.UtcNow));
        });
    }
}
