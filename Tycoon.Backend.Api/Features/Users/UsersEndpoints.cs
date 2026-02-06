using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.Users
{
    public static class UsersEndpoints
    {
        public static void Map(WebApplication app)
        {
            var g = app.MapGroup("/users").WithTags("Users").RequireAuthorization();

            g.MapGet("/me", async (HttpContext ctx, IAppDb db, CancellationToken ct) =>
            {
                var userIdClaim = ctx.User.FindFirst("sub");
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                    return Results.Unauthorized();

                var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
                if (user == null) return Results.NotFound();

                return Results.Ok(new UserDto(user.Id, user.Email, user.Handle, user.Country, user.AvatarUrl, user.CreatedAt));
            });

            g.MapPatch("/me", async (
                [FromBody] UpdateProfileRequest req,
                HttpContext ctx,
                IAppDb db,
                CancellationToken ct) =>
            {
                var userIdClaim = ctx.User.FindFirst("sub");
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                    return Results.Unauthorized();

                var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
                if (user == null) return Results.NotFound();

                user.UpdateProfile(req.Handle, req.Country);
                await db.SaveChangesAsync(ct);

                return Results.Ok(new UserDto(user.Id, user.Email, user.Handle, user.Country, user.AvatarUrl, user.CreatedAt));
            });
        }
    }
}
