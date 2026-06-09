using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Synaptix.Compliance.Application.Abstractions;
using Synaptix.Compliance.Contracts.Models;

namespace Synaptix.Compliance.Api.Features.PrivacyRequests;

public static class PrivacyRequestEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/compliance/privacy-requests")
            .WithTags("PrivacyRequests")
            .RequireAuthorization();

        group.MapPost("/", HandleSubmit);
        group.MapGet("/{requestId:guid}", HandleGetStatus);
    }

    private static async Task<IResult> HandleSubmit(
        [FromBody] SubmitPrivacyRequest body,
        ClaimsPrincipal user,
        IPrivacyRequestService svc,
        HttpContext ctx,
        CancellationToken ct)
    {
        var userId = ParseUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        if (!Enum.TryParse<PrivacyRequestType>(body.RequestType, ignoreCase: true, out var type))
            return Results.BadRequest(new
            {
                error = "invalid_request_type",
                message = $"Valid types: {string.Join(", ", Enum.GetNames<PrivacyRequestType>())}"
            });

        var record = await svc.SubmitAsync(userId.Value, type, ctx.Connection.RemoteIpAddress?.ToString(), ct);
        return Results.Created($"/compliance/privacy-requests/{record.Id}", new
        {
            record.Id,
            record.RequestType,
            record.Status,
            record.SubmittedAt
        });
    }

    private static async Task<IResult> HandleGetStatus(
        Guid requestId,
        ClaimsPrincipal user,
        IPrivacyRequestService svc,
        CancellationToken ct)
    {
        var userId = ParseUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var record = await svc.GetAsync(requestId, ct);
        if (record is null || record.UserId != userId.Value)
            return Results.NotFound(new { error = "not_found" });

        return Results.Ok(new
        {
            record.Id,
            record.RequestType,
            record.Status,
            record.SubmittedAt,
            record.CompletedAt
        });
    }

    private static Guid? ParseUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}

public sealed record SubmitPrivacyRequest(string RequestType);
