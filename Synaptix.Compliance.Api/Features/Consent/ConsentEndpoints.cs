using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Synaptix.Compliance.Application.Abstractions;
using Synaptix.Compliance.Contracts.Models;

namespace Synaptix.Compliance.Api.Features.Consent;

public static class ConsentEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/compliance/consent")
            .WithTags("Consent")
            .RequireAuthorization();

        group.MapPost("/", HandleRecord);
        group.MapGet("/me", HandleGetCurrent);
    }

    private static async Task<IResult> HandleRecord(
        [FromBody] RecordConsentRequest body,
        ClaimsPrincipal user,
        IConsentService svc,
        HttpContext ctx,
        CancellationToken ct)
    {
        var userId = ParseUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        if (!Enum.TryParse<ConsentType>(body.ConsentType, ignoreCase: true, out var type))
            return Results.BadRequest(new
            {
                error = "invalid_consent_type",
                message = $"Valid types: {string.Join(", ", Enum.GetNames<ConsentType>())}"
            });

        var record = await svc.RecordAsync(
            userId.Value, type, body.ConsentGiven, body.PolicyVersion,
            ctx.Connection.RemoteIpAddress?.ToString(),
            ctx.Request.Headers.UserAgent.ToString(),
            ct);

        return Results.Ok(new { record.Id, record.ConsentType, record.ConsentGiven, record.RecordedAt });
    }

    private static async Task<IResult> HandleGetCurrent(
        ClaimsPrincipal user,
        IConsentService svc,
        CancellationToken ct)
    {
        var userId = ParseUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var records = await svc.GetCurrentAsync(userId.Value, ct);
        return Results.Ok(records.Select(r => new
        {
            r.ConsentType,
            r.ConsentGiven,
            r.PolicyVersion,
            r.RecordedAt
        }));
    }

    private static Guid? ParseUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}

public sealed record RecordConsentRequest(string ConsentType, bool ConsentGiven, string PolicyVersion);
