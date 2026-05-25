using System.Security.Cryptography;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Players;

public sealed record AdminResolvePlayerLookup(string Query) : IRequest<AdminPlayerLookupResponse?>;

public sealed class AdminResolvePlayerLookupHandler(IAppDb db)
    : IRequestHandler<AdminResolvePlayerLookup, AdminPlayerLookupResponse?>
{
    private const string Alphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";

    public async Task<AdminPlayerLookupResponse?> Handle(AdminResolvePlayerLookup request, CancellationToken ct)
    {
        var query = (request.Query ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(query)) return null;

        var code = NormalizeShortCode(query);
        if (code is not null)
        {
            var existing = await db.PlayerLookupCodes.AsNoTracking()
                .FirstOrDefaultAsync(x => x.ShortCode == code, ct);
            if (existing is not null)
                return await MapAsync(existing.PlayerId, existing.UserId, existing.ShortCode, "shortCode", false, ct);
        }

        if (TryParseGuid(query, out var parsedId))
        {
            var lookup = await db.PlayerLookupCodes
                .FirstOrDefaultAsync(x => x.PlayerId == parsedId || x.UserId == parsedId, ct);
            if (lookup is not null)
                return await MapAsync(lookup.PlayerId, lookup.UserId, lookup.ShortCode, "uuid", false, ct);

            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == parsedId, ct);
            if (user is not null)
                return await EnsureAndMapAsync(user.Id, user.Id, "userUuid", ct);

            var player = await db.Players.AsNoTracking().FirstOrDefaultAsync(x => x.Id == parsedId, ct);
            if (player is not null)
                return await EnsureAndMapAsync(player.Id, null, "playerUuid", ct);
        }

        var lowered = query.ToLowerInvariant();
        var matchedUser = await db.Users.AsNoTracking()
            .Where(x => x.Email.ToLower() == lowered || x.Handle.ToLower() == lowered)
            .FirstOrDefaultAsync(ct);
        if (matchedUser is not null)
            return await EnsureAndMapAsync(matchedUser.Id, matchedUser.Id, "user", ct);

        var matchedPlayer = await db.Players.AsNoTracking()
            .Where(x => x.Username.ToLower() == lowered)
            .FirstOrDefaultAsync(ct);
        if (matchedPlayer is not null)
            return await EnsureAndMapAsync(matchedPlayer.Id, null, "player", ct);

        return null;
    }

    private async Task<AdminPlayerLookupResponse> EnsureAndMapAsync(
        Guid playerId,
        Guid? userId,
        string source,
        CancellationToken ct)
    {
        var lookup = await db.PlayerLookupCodes.FirstOrDefaultAsync(x => x.PlayerId == playerId, ct);
        var created = false;

        if (lookup is null)
        {
            lookup = new PlayerLookupCode(playerId, await GenerateUniqueShortCodeAsync(ct), userId);
            db.PlayerLookupCodes.Add(lookup);
            created = true;
        }
        else
        {
            lookup.LinkUser(userId);
        }

        await db.SaveChangesAsync(ct);
        return await MapAsync(playerId, userId, lookup.ShortCode, source, created, ct)
            ?? new AdminPlayerLookupResponse(ToContractId(playerId), ToUserContractId(userId), lookup.ShortCode, null, null, source, created);
    }

    private async Task<AdminPlayerLookupResponse?> MapAsync(
        Guid playerId,
        Guid? userId,
        string shortCode,
        string source,
        bool created,
        CancellationToken ct)
    {
        var user = userId.HasValue
            ? await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId.Value, ct)
            : null;
        var player = await db.Players.AsNoTracking().FirstOrDefaultAsync(x => x.Id == playerId, ct);

        return new AdminPlayerLookupResponse(
            PlayerId: ToContractId(playerId),
            UserId: ToUserContractId(userId),
            ShortCode: shortCode,
            Username: user?.Handle ?? player?.Username,
            Email: user?.Email,
            Source: source,
            Created: created);
    }

    private async Task<string> GenerateUniqueShortCodeAsync(CancellationToken ct)
    {
        for (var attempt = 0; attempt < 16; attempt++)
        {
            var code = GenerateShortCode();
            if (!await db.PlayerLookupCodes.AnyAsync(x => x.ShortCode == code, ct))
                return code;
        }

        throw new InvalidOperationException("Unable to allocate a unique player lookup code.");
    }

    private static string GenerateShortCode()
    {
        Span<char> chars = stackalloc char[6];
        for (var i = 0; i < chars.Length; i++)
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        return new string(chars);
    }

    private static string? NormalizeShortCode(string query)
    {
        var value = query.Trim().ToUpperInvariant();
        if (value.Length != 6) return null;
        return value.All(c => Alphabet.Contains(c)) ? value : null;
    }

    private static bool TryParseGuid(string value, out Guid id)
    {
        var raw = value.StartsWith("usr_", StringComparison.OrdinalIgnoreCase)
            ? value[4..]
            : value.StartsWith("ply_", StringComparison.OrdinalIgnoreCase)
                ? value[4..]
                : value;
        return Guid.TryParse(raw, out id);
    }

    private static string? ToContractId(Guid? id)
        => id.HasValue ? ToContractId(id.Value) : null;

    private static string ToContractId(Guid id) => $"ply_{id:N}";

    private static string? ToUserContractId(Guid? id)
        => id.HasValue ? $"usr_{id.Value:N}" : null;
}
