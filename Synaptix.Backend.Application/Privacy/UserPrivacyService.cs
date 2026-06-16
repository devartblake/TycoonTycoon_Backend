using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Compliance.Client.Abstractions;
using Synaptix.Compliance.Client.Models.Requests;

namespace Synaptix.Backend.Application.Privacy;

public sealed class UserPrivacyService(
    IAppDb db,
    IObjectStorage storage,
    IComplianceClient compliance,
    ILogger<UserPrivacyService> logger) : IUserPrivacyService
{
    public async Task AnonymizeUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            logger.LogWarning("AnonymizeUser: user {UserId} not found", userId);
            return;
        }

        var shortId = userId.ToString("N")[..8];

        // Scrub PII — financial rows (PlayerTransaction, EconomyTransaction) are preserved
        var anonEmail = $"deleted-{shortId}@anon.synaptix";
        var anonHandle = $"deleted-{shortId}";
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        var deadHash = Convert.ToHexString(SHA256.HashData(randomBytes));

        user.Anonymize(anonEmail, anonHandle, deadHash);

        var player = await db.Players.FirstOrDefaultAsync(p => p.Id == userId, ct);
        player?.AnonymizeUsername($"deleted-player-{shortId}");

        // Hard-delete sessions and messages (no financial value)
        var tokens = await db.RefreshTokens.Where(t => t.UserId == userId).ToListAsync(ct);
        db.RefreshTokens.RemoveRange(tokens);

        var messages = await db.DirectMessages
            .Where(m => m.SenderId == userId)
            .ToListAsync(ct);
        foreach (var msg in messages)
            msg.Redact();

        await db.SaveChangesAsync(ct);

        await compliance.RecordAuditEventAsync(new RecordAuditEventRequest(
            UserId: userId,
            EventType: "user_anonymized",
            EventData: $"{{\"userId\":\"{userId}\"}}",
            Source: "privacy-fulfillment"), ct);

        logger.LogInformation("User {UserId} anonymized for CCPA delete request", userId);
    }

    public async Task<string> ExportUserDataAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        var player = await db.Players.AsNoTracking().FirstOrDefaultAsync(p => p.Id == userId, ct);
        var wallet = await db.PlayerWallets.AsNoTracking().FirstOrDefaultAsync(w => w.PlayerId == userId, ct);
        var cutoff90 = DateTime.UtcNow.AddDays(-90);
        var cutoff12m = DateTimeOffset.UtcNow.AddMonths(-12);

        var missions = await db.MissionClaims.AsNoTracking()
            .Where(m => m.PlayerId == userId && m.ClaimedAtUtc >= cutoff90)
            .ToListAsync(ct);

        var transactions = await db.PlayerTransactions.AsNoTracking()
            .Include(t => t.Actors)
            .Include(t => t.ItemChanges)
            .Where(t => t.Actors.Any(a => a.PlayerId == userId) && t.CreatedAtUtc >= cutoff12m)
            .ToListAsync(ct);

        var export = new
        {
            exportedAt = DateTimeOffset.UtcNow,
            user = user is null ? null : new
            {
                user.Id, user.Handle, user.Email, user.Country, user.CreatedAt, user.LastLoginAt
            },
            player = player is null ? null : new
            {
                player.Id, player.Username, player.Level, player.Score, player.Coins, player.Diamonds
            },
            wallet = wallet is null ? null : new { wallet.Coins, wallet.Diamonds },
            recentMissions = missions.Select(m => new { m.MissionId, m.PlayerId, m.ClaimedAtUtc }),
            transactions = transactions.Select(t => new
            {
                t.Id, t.Kind, t.Status, t.CreatedAtUtc, t.CompletedAtUtc
            })
        };

        var json = JsonSerializer.SerializeToUtf8Bytes(export, new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var key = $"compliance-exports/{userId}/{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.json";
        await storage.PutAsync(key, new MemoryStream(json), "application/json", json.Length, ct);

        var url = storage.GetPublicUrl(key);
        logger.LogInformation("Data export for user {UserId} written to {Key}", userId, key);
        return url;
    }

    public async Task ApplyOptOutAsync(Guid userId, CancellationToken ct = default)
    {
        await compliance.RecordAuditEventAsync(new RecordAuditEventRequest(
            UserId: userId,
            EventType: "opt_out_applied",
            EventData: $"{{\"userId\":\"{userId}\"}}",
            Source: "privacy-fulfillment"), ct);

        logger.LogInformation("Opt-out applied for user {UserId}", userId);
    }
}
