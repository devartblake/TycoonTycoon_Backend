using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Tycoon.Backend.Application.Moderation;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.AdminModeration
{
    public static class AdminEscalationEndpoints
    {
        private const string AdminHeader = "X-Admin-User";

        public static void Map(RouteGroupBuilder admin)
        {
            var g = admin.MapGroup("/moderation/escalation").WithTags("Admin/Moderation");

            g.MapPost("/run", async (
                HttpContext ctx,
                [FromBody] RunEscalationRequest req,
                EscalationService svc,
                CancellationToken ct) =>
            {
                var adminUser = ctx.Request.Headers.TryGetValue(AdminHeader, out var h) ? h.ToString() : null;

                // Safety defaults if omitted/invalid
                var safeReq = req with
                {
                    WindowHours = Math.Clamp(req.WindowHours, 1, 168),
                    MaxPlayers = Math.Clamp(req.MaxPlayers, 1, 2000)
                };

                var res = await svc.RunAsync(safeReq, adminUser, ct);
                return Results.Ok(res);
            });
        }
    }
}
