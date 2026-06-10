using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Synaptix.Compliance.Application.Abstractions;

namespace Synaptix.Compliance.Api.Features.AgeVerification;

public static class AgeVerificationEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/compliance/age-verification")
            .WithTags("AgeVerification")
            .RequireAuthorization();

        group.MapPost("/", HandleSubmit);
        group.MapGet("/me", HandleGetOwn);
    }

    private static async Task<IResult> HandleSubmit(
        [FromBody] SubmitAgeRequest body,
        ClaimsPrincipal user,
        IAgeVerificationService svc,
        HttpContext ctx,
        CancellationToken ct)
    {
        var userId = ParseUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        if (body.DeclaredAge is < 1 or > 120)
            return Results.BadRequest(new { error = "invalid_age", message = "DeclaredAge must be between 1 and 120." });

        var record = await svc.SubmitAsync(userId.Value, body.DeclaredAge, "declaration", ctx.Connection.RemoteIpAddress?.ToString(), ct);
        return Results.Ok(new { record.Id, record.IsMinor, record.VerifiedAt });
    }

    private static async Task<IResult> HandleGetOwn(
        ClaimsPrincipal user,
        IAgeVerificationService svc,
        CancellationToken ct)
    {
        var userId = ParseUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var record = await svc.GetLatestAsync(userId.Value, ct);
        if (record is null)
            return Results.NotFound(new { error = "not_found" });

        return Results.Ok(new { record.DeclaredAge, record.IsMinor, record.VerifiedAt });
    }

    private static Guid? ParseUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}

public sealed record SubmitAgeRequest(int DeclaredAge);
