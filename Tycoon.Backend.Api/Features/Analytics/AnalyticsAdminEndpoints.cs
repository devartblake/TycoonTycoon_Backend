using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tycoon.Backend.Api.Security;
using Tycoon.Backend.Application.Analytics.Abstractions;

namespace Tycoon.Backend.Api.Features.Analytics
{
    public static class AnalyticsAdminEndpoints
    {
        public static void Map(WebApplication app)
        {
            var g = app.MapGroup("/admin/analytics")
                .WithTags("Analytics")
                .RequireAuthorization(AdminPolicies.AdminOnly)
                .WithMetadata(new RequireAdminOpsKeyAttribute());

            // POST /admin/analytics/rebuild-elastic-rollups?from=2025-01-01&to=2025-01-31
            g.MapPost("/rebuild-elastic-rollups", async (
                [FromQuery] DateOnly? fromUtcDate,
                [FromQuery] DateOnly? toUtcDate,
                IRollupRebuilder rebuilder,
                CancellationToken ct) =>
            {
                await rebuilder.RebuildElasticFromMongoAsync(fromUtcDate, toUtcDate, ct);
                return Results.Ok(new
                {
                    message = "Elastic rollups rebuild completed.",
                    fromUtcDate,
                    toUtcDate
                });
            });
        }
    }
}
