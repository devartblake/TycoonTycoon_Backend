using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.AdminConfig;

public static class AdminConfigEndpoints
{
    public static void Map(RouteGroupBuilder admin)
    {
        var g = admin.MapGroup("/config").WithTags("Admin/Config").WithOpenApi();

        g.MapGet("", async (IAppDb db, CancellationToken ct) =>
        {
            var config = await GetOrCreate(db, ct);
            return Results.Ok(ToDto(config));
        });

        g.MapPatch("", async ([FromBody] UpdateAdminAppConfigRequest request, IAppDb db, CancellationToken ct) =>
        {
            var config = await GetOrCreate(db, ct);
            config.Update(request.EnableLogging, request.FeatureFlags is null ? null : JsonSerializer.Serialize(request.FeatureFlags));
            await db.SaveChangesAsync(ct);
            return Results.Ok(new UpdateAdminAppConfigResponse(config.UpdatedAt));
        });
    }

    private static async Task<AdminAppConfig> GetOrCreate(IAppDb db, CancellationToken ct)
    {
        var existing = await db.AdminAppConfigs.FirstOrDefaultAsync(x => x.Id == "default", ct);
        if (existing is not null) return existing;

        var created = new AdminAppConfig("https://api.example.com", false, JsonSerializer.Serialize(new Dictionary<string, bool>
        {
            ["adminEventUpload"] = true
        }));

        db.AdminAppConfigs.Add(created);
        await db.SaveChangesAsync(ct);
        return created;
    }

    private static AdminAppConfigDto ToDto(AdminAppConfig config)
    {
        Dictionary<string, bool> flags;
        try { flags = JsonSerializer.Deserialize<Dictionary<string, bool>>(config.FeatureFlagsJson) ?? []; }
        catch { flags = []; }

        return new AdminAppConfigDto(config.ApiBaseUrl, config.EnableLogging, flags);
    }
}
