using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Synaptix.Backend.Api.Contracts;
using Synaptix.Backend.Application.Email;
using Synaptix.Compliance.Client.Abstractions;
using Synaptix.Compliance.Client.Models.Requests;

namespace Synaptix.Backend.Api.Features.ParentalConsent;

public static class ParentalConsentEndpoints
{
    public static void Map(WebApplication app)
    {
        var g = app.MapGroup("/users/me/parental-consent")
            .WithTags("ParentalConsent")
            .RequireAuthorization();

        g.MapPost("/request", RequestParentalConsent);
    }

    private static async Task<IResult> RequestParentalConsent(
        [FromBody] ParentalConsentRequest body,
        HttpContext httpContext,
        IComplianceClient compliance,
        IEmailService email,
        IConfiguration configuration,
        CancellationToken ct)
    {
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
            ?? httpContext.User.FindFirst("sub");

        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

        if (string.IsNullOrWhiteSpace(body.ParentEmail))
            return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "parentEmail is required.");

        var result = await compliance.InitiateParentalConsentAsync(
            new InitiateParentalConsentRequest(userId, body.ParentEmail.Trim()), ct);

        var verifyUrl = configuration["Compliance:ConsentVerifyUrl"]
            ?? "https://app.synaptix.gg/parental-consent";

        var link = $"{verifyUrl}?token={Uri.EscapeDataString(result.ConsentToken)}";
        var expiresHours = (int)Math.Ceiling((result.ExpiresAt - DateTimeOffset.UtcNow).TotalHours);

        await email.SendAsync(
            body.ParentEmail.Trim(),
            "Action required: Parental consent for your child's Synaptix account",
            BuildConsentEmailHtml(link, expiresHours),
            ct);

        return Results.Accepted(value: new { consentId = result.Id, expiresAt = result.ExpiresAt });
    }

    private static string BuildConsentEmailHtml(string link, int expiresHours) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:24px;color:#1a1a1a">
          <h2>Parental Consent Request — Synaptix</h2>
          <p>A Synaptix account linked to your email address requires your parental consent before
             certain features can be unlocked for your child.</p>
          <p>Synaptix is a competitive trivia and learning platform. Under COPPA (Children's Online
             Privacy Protection Act), we require verifiable parental consent before allowing players
             under 13 to access social or purchase features.</p>
          <p style="margin:24px 0">
            <a href="{link}"
               style="background:#0F766E;color:#fff;padding:12px 24px;border-radius:6px;
                      text-decoration:none;font-weight:bold">
              Review &amp; Grant Consent
            </a>
          </p>
          <p style="color:#6b7280;font-size:13px">
            This link expires in approximately {expiresHours} hours.<br/>
            If you did not request this or do not recognise this account, you can safely ignore
            this email.
          </p>
          <hr style="border:none;border-top:1px solid #e5e7eb;margin:24px 0"/>
          <p style="color:#9ca3af;font-size:12px">Synaptix &mdash; no-reply@synaptix.gg</p>
        </body>
        </html>
        """;
}

internal sealed record ParentalConsentRequest(string ParentEmail);
