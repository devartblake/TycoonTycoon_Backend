using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Synaptix.Security.Kms.Application.Abstractions;
using Synaptix.Security.Kms.Application.Sessions;

namespace Synaptix.Security.Kms.Api.Features.Sessions;

public static class SessionEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/security/sessions")
            .WithTags("Sessions")
            .RequireAuthorization();

        group.MapPost("/start", HandleStart);
        group.MapPost("/renew", HandleRenew);
        group.MapPost("/revoke", HandleRevoke);
    }

    private static async Task<IResult> HandleStart(
        [FromBody] StartSessionRequest body,
        ClaimsPrincipal user,
        ISecureSessionService sessions,
        CancellationToken ct)
    {
        var subjectId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(subjectId))
            return Results.Unauthorized();

        try
        {
            var result = await sessions.StartAsync(
                subjectId,
                new StartSessionCommand(
                    body.DeviceId,
                    body.ClientNonce,
                    body.ClientPublicKey,
                    body.SupportedSuites),
                ct);

            return Results.Ok(result);
        }
        catch (CryptographicException ex)
        {
            return Results.BadRequest(new { error = "invalid_key_material", message = ex.Message });
        }
    }

    private static async Task<IResult> HandleRenew(
        [FromBody] RenewSessionRequest body,
        ClaimsPrincipal user,
        ISecureSessionService sessions,
        CancellationToken ct)
    {
        var subjectId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(subjectId))
            return Results.Unauthorized();

        try
        {
            var result = await sessions.RenewAsync(body.SessionId, subjectId, body.DeviceId, ct);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.NotFound(new { error = "session_not_found", message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
    }

    private static async Task<IResult> HandleRevoke(
        [FromBody] RevokeSessionRequest body,
        ISecureSessionService sessions,
        CancellationToken ct)
    {
        await sessions.RevokeAsync(body.SessionId, body.Reason, ct);
        return Results.NoContent();
    }
}

public sealed record StartSessionRequest(
    string DeviceId,
    string ClientNonce,
    string ClientPublicKey,
    IReadOnlyList<string> SupportedSuites);

public sealed record RenewSessionRequest(Guid SessionId, string DeviceId);
public sealed record RevokeSessionRequest(Guid SessionId, string Reason);
