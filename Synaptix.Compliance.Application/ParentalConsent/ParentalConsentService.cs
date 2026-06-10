using Microsoft.EntityFrameworkCore;
using Synaptix.Compliance.Application.Abstractions;
using Synaptix.Compliance.Application.Entities;
using Synaptix.Compliance.Contracts.Models;
using System.Security.Cryptography;

namespace Synaptix.Compliance.Application.ParentalConsent;

internal sealed class ParentalConsentService(IComplianceDb db) : IParentalConsentService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(72);

    public async Task<(Entities.ParentalConsent Record, string RawToken)> InitiateAsync(
        Guid userId, string parentEmail, string? ip, CancellationToken ct)
    {
        // Expire any existing pending consent for this user
        var existing = await db.ParentalConsents
            .Where(c => c.UserId == userId && c.Status == ParentalConsentStatus.Pending)
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
        {
            existing.Status = ParentalConsentStatus.Expired;
        }

        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

        var record = new Entities.ParentalConsent
        {
            UserId = userId,
            ParentEmailHash = HashValue(parentEmail.ToLowerInvariant()),
            TokenHash = HashValue(rawToken),
            Status = ParentalConsentStatus.Pending,
            ExpiresAt = DateTimeOffset.UtcNow.Add(TokenLifetime)
        };

        db.ParentalConsents.Add(record);
        await db.SaveChangesAsync(ct);
        return (record, rawToken);
    }

    public async Task<Entities.ParentalConsent> VerifyAsync(string rawToken, CancellationToken ct)
    {
        var tokenHash = HashValue(rawToken);
        var record = await db.ParentalConsents
            .Where(c => c.TokenHash == tokenHash && c.Status == ParentalConsentStatus.Pending)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("consent_token_invalid");

        if (record.ExpiresAt < DateTimeOffset.UtcNow)
        {
            record.Status = ParentalConsentStatus.Expired;
            await db.SaveChangesAsync(ct);
            throw new InvalidOperationException("consent_token_expired");
        }

        record.Status = ParentalConsentStatus.Granted;
        record.GrantedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return record;
    }

    public Task<Entities.ParentalConsent?> GetStatusAsync(Guid userId, CancellationToken ct)
        => db.ParentalConsents
             .Where(c => c.UserId == userId)
             .OrderByDescending(c => c.RequestedAt)
             .FirstOrDefaultAsync(ct);

    public async Task RevokeAsync(Guid userId, CancellationToken ct)
    {
        var record = await db.ParentalConsents
            .Where(c => c.UserId == userId && c.Status == ParentalConsentStatus.Granted)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("consent_not_found");

        record.Status = ParentalConsentStatus.Revoked;
        record.RevokedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task<ParentalConsentStatus> GetEffectiveStatusAsync(
        Guid userId, bool isMinor, CancellationToken ct)
    {
        if (!isMinor)
            return ParentalConsentStatus.NotRequired;

        var record = await GetStatusAsync(userId, ct);
        if (record is null)
            return ParentalConsentStatus.Pending;

        if (record.Status == ParentalConsentStatus.Pending && record.ExpiresAt < DateTimeOffset.UtcNow)
            return ParentalConsentStatus.Expired;

        return record.Status;
    }

    private static string HashValue(string value)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
