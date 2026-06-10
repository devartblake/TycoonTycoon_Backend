using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Synaptix.Compliance.Application.Abstractions;
using Synaptix.Compliance.Contracts.Models;

namespace Synaptix.Compliance.Api.Features.ParentalConsent;

public static class ParentalConsentEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/compliance/parental-consent")
            .WithTags("ParentalConsent")
            .RequireAuthorization();

        group.MapPost("/initiate", HandleInitiate);
        group.MapPost("/verify", HandleVerify).AllowAnonymous();
        group.MapGet("/me", HandleGetOwn);
        group.MapDelete("/me", HandleRevoke);
    }

    // Called by the authenticated minor's session to start consent flow.
    // Returns the raw token which the main backend should email to the parent.
    private static async Task<IResult> HandleInitiate(
        [FromBody] InitiateConsentRequest body,
        ClaimsPrincipal user,
        IParentalConsentService svc,
        HttpContext ctx,
        CancellationToken ct)
    {
        var userId = ParseUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var (record, rawToken) = await svc.InitiateAsync(
            userId.Value, body.ParentEmail, ctx.Connection.RemoteIpAddress?.ToString(), ct);

        // The main backend uses this token to build the consent URL for the email.
        return Results.Ok(new
        {
            record.Id,
            record.Status,
            record.ExpiresAt,
            ConsentToken = rawToken
        });
    }

    // Called by the main backend after the parent clicks the verification link.
    // Does NOT require user JWT — uses the token from the email link.
    private static async Task<IResult> HandleVerify(
        [FromBody] VerifyConsentRequest body,
        IParentalConsentService svc,
        CancellationToken ct)
    {
        try
        {
            var record = await svc.VerifyAsync(body.Token, ct);
            return Results.Ok(new { record.UserId, record.Status, record.GrantedAt });
        }
        catch (InvalidOperationException ex) when (ex.Message == "consent_token_invalid")
        {
            return Results.NotFound(new { error = "consent_token_invalid" });
        }
        catch (InvalidOperationException ex) when (ex.Message == "consent_token_expired")
        {
            return Results.BadRequest(new { error = "consent_token_expired" });
        }
    }

    private static async Task<IResult> HandleGetOwn(
        ClaimsPrincipal user,
        IParentalConsentService svc,
        IAgeVerificationService ageSvc,
        CancellationToken ct)
    {
        var userId = ParseUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var ageRecord = await ageSvc.GetLatestAsync(userId.Value, ct);
        var status = await svc.GetEffectiveStatusAsync(userId.Value, ageRecord?.IsMinor ?? false, ct);
        return Results.Ok(new { Status = status.ToString() });
    }

    private static async Task<IResult> HandleRevoke(
        ClaimsPrincipal user,
        IParentalConsentService svc,
        CancellationToken ct)
    {
        var userId = ParseUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        try
        {
            await svc.RevokeAsync(userId.Value, ct);
            return Results.NoContent();
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound(new { error = "consent_not_found" });
        }
    }

    private static Guid? ParseUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}

public sealed record InitiateConsentRequest(string ParentEmail);
public sealed record VerifyConsentRequest(string Token);
