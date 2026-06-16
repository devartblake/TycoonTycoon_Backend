using Microsoft.AspNetCore.Mvc;
using Synaptix.Compliance.Api.Security;
using Synaptix.Compliance.Application.Abstractions;
using Synaptix.Compliance.Contracts.Models;

namespace Synaptix.Compliance.Api.Features.Internal;

/// Internal service-to-service endpoints — protected by X-Service-Token, not JWT.
public static class InternalEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/internal/compliance")
            .WithTags("Internal")
            .AddEndpointFilter<ServiceTokenFilter>();

        group.MapGet("/users/{userId:guid}/restrictions", HandleGetRestrictions);
        group.MapGet("/users/{userId:guid}/consent-status", HandleGetConsentStatus);
        group.MapPost("/parental-consent/initiate", HandleInitiateParentalConsent);
        group.MapGet("/privacy-requests/pending", HandleGetPending);
        group.MapPatch("/privacy-requests/{requestId:guid}", HandleUpdatePrivacyRequest);
        group.MapPost("/audit", HandleRecordAudit);
        group.MapGet("/audit/{userId:guid}", HandleGetAudit);
    }

    // Server-initiated parental consent — called by the main backend (which sends the email).
    private static async Task<IResult> HandleInitiateParentalConsent(
        [FromBody] InitiateParentalConsentInternalRequest body,
        IParentalConsentService svc,
        CancellationToken ct)
    {
        var (record, rawToken) = await svc.InitiateAsync(body.UserId, body.ParentEmail, null, ct);
        return Results.Ok(new
        {
            record.Id,
            record.Status,
            record.ExpiresAt,
            ConsentToken = rawToken
        });
    }

    /// Returns the set of feature restrictions that apply to this user (empty = no restrictions).
    /// The main backend calls this before allowing features like DMs, personalization, etc.
    private static async Task<IResult> HandleGetRestrictions(
        Guid userId,
        IAgeVerificationService ageSvc,
        IParentalConsentService consentSvc,
        CancellationToken ct)
    {
        var ageRecord = await ageSvc.GetLatestAsync(userId, ct);
        if (ageRecord is null || !ageRecord.IsMinor)
            return Results.Ok(new { UserId = userId, Restrictions = Array.Empty<string>() });

        var consentStatus = await consentSvc.GetEffectiveStatusAsync(userId, isMinor: true, ct);

        // COPPA: minors without granted parental consent get all restrictions
        var restrictions = consentStatus == ParentalConsentStatus.Granted
            ? new[] { nameof(UserRestriction.NoThirdPartySharing), nameof(UserRestriction.NoBehavioralProfiling) }
            : Enum.GetNames<UserRestriction>();

        return Results.Ok(new { UserId = userId, Restrictions = restrictions });
    }

    private static async Task<IResult> HandleGetConsentStatus(
        Guid userId,
        IAgeVerificationService ageSvc,
        IParentalConsentService parentalSvc,
        IConsentService consentSvc,
        CancellationToken ct)
    {
        var ageRecord = await ageSvc.GetLatestAsync(userId, ct);
        var parentalStatus = await parentalSvc.GetEffectiveStatusAsync(userId, ageRecord?.IsMinor ?? false, ct);
        var consentRecords = await consentSvc.GetCurrentAsync(userId, ct);

        return Results.Ok(new
        {
            UserId = userId,
            IsMinor = ageRecord?.IsMinor ?? false,
            ParentalConsent = parentalStatus.ToString(),
            Consents = consentRecords.Select(r => new
            {
                r.ConsentType,
                r.ConsentGiven,
                r.PolicyVersion,
                r.RecordedAt
            })
        });
    }

    private static async Task<IResult> HandleGetPending(
        [FromQuery] int limit,
        IPrivacyRequestService svc,
        CancellationToken ct)
    {
        if (limit is <= 0 or > 200)
            limit = 50;

        var requests = await svc.GetPendingAsync(limit, ct);
        return Results.Ok(requests.Select(r => new
        {
            r.Id,
            r.UserId,
            RequestType = r.RequestType.ToString(),
            Status = r.Status.ToString(),
            r.SubmittedAt
        }));
    }

    private static async Task<IResult> HandleUpdatePrivacyRequest(
        Guid requestId,
        [FromBody] UpdatePrivacyRequestBody body,
        IPrivacyRequestService svc,
        CancellationToken ct)
    {
        if (!Enum.TryParse<PrivacyRequestStatus>(body.Status, ignoreCase: true, out var status))
            return Results.BadRequest(new { error = "invalid_status" });

        try
        {
            var record = await svc.UpdateStatusAsync(requestId, status, body.Notes, ct);
            return Results.Ok(new { record.Id, record.Status, record.CompletedAt });
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound(new { error = "not_found" });
        }
    }

    private static async Task<IResult> HandleRecordAudit(
        [FromBody] RecordAuditRequest body,
        IComplianceAuditService svc,
        CancellationToken ct)
    {
        await svc.RecordAsync(body.UserId, body.EventType, body.Source, body.EventData, body.IpAddress, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> HandleGetAudit(
        Guid userId,
        [FromQuery] int limit,
        IComplianceAuditService svc,
        CancellationToken ct)
    {
        if (limit is <= 0 or > 500)
            limit = 100;

        var events = await svc.GetForUserAsync(userId, limit, ct);
        return Results.Ok(events.Select(e => new
        {
            e.Id,
            e.EventType,
            e.Source,
            e.OccurredAt,
            e.IpAddress
        }));
    }
}

public sealed record InitiateParentalConsentInternalRequest(Guid UserId, string ParentEmail);
public sealed record UpdatePrivacyRequestBody(string Status, string? Notes);
public sealed record RecordAuditRequest(
    Guid? UserId,
    string EventType,
    string Source,
    string? EventData,
    string? IpAddress);
