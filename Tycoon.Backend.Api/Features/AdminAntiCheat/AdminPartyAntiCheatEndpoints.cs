using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.AdminAntiCheat
{
    public static class AdminPartyAntiCheatEndpoints
    {
        public static void Map(IEndpointRouteBuilder admin)
        {
            var g = admin.MapGroup("/anti-cheat/party")
                .WithTags("Admin AntiCheat - Party").WithOpenApi();

            // GET /admin/anti-cheat/party/flags?sinceUtc=...&ruleKeyPrefix=party-&page=1&pageSize=50
            g.MapGet("/flags", async (
                IAppDb db,
                DateTimeOffset? sinceUtc,
                string? ruleKeyPrefix,
                int page,
                int pageSize,
                CancellationToken ct) =>
            {
                page = page <= 0 ? 1 : page;
                pageSize = pageSize <= 0 ? 50 : Math.Min(pageSize, 200);

                var since = sinceUtc ?? DateTimeOffset.UtcNow.AddDays(-7);
                var prefix = string.IsNullOrWhiteSpace(ruleKeyPrefix) ? "party-" : ruleKeyPrefix;

                var q = db.AntiCheatFlags.AsNoTracking()
                    .Where(f => f.CreatedAtUtc >= since)
                    .Where(f => f.RuleKey.StartsWith(prefix));

                var total = await q.CountAsync(ct);

                var flags = await q
                    .OrderByDescending(f => f.CreatedAtUtc)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(f => new
                    {
                        f.Id,
                        f.CreatedAtUtc,
                        f.MatchId,
                        f.PlayerId,
                        f.RuleKey,
                        f.Severity,
                        f.Action,
                        f.Message,
                        f.EvidenceJson
                    })
                    .ToListAsync(ct);

                var items = flags.Select(f =>
                {
                    Guid? partyId = TryExtractPartyId(f.EvidenceJson);
                    return new PartyAntiCheatFlagDto(
                        Id: f.Id,
                        CreatedAtUtc: f.CreatedAtUtc,
                        MatchId: f.MatchId,
                        PlayerId: f.PlayerId,
                        RuleKey: f.RuleKey,
                        Severity: f.Severity.ToString(),
                        Action: f.Action.ToString(),
                        Message: f.Message,
                        PartyId: partyId,
                        EvidenceJson: f.EvidenceJson
                    );
                }).ToList();

                return Results.Ok(new PartyAntiCheatFlagsResponseDto(
                    Page: page,
                    PageSize: pageSize,
                    Total: total,
                    Items: items
                ));
            });

            // GET /admin/anti-cheat/party/summary?sinceUtc=...
            g.MapGet("/summary", async (
                IAppDb db,
                DateTimeOffset? sinceUtc,
                CancellationToken ct) =>
            {
                var since = sinceUtc ?? DateTimeOffset.UtcNow.AddDays(-14);

                // Pull last N flags then group in-memory (simple & safe). Optimize later if needed.
                var recent = await db.AntiCheatFlags.AsNoTracking()
                    .Where(f => f.CreatedAtUtc >= since)
                    .Where(f => f.RuleKey.StartsWith("party-"))
                    .OrderByDescending(f => f.CreatedAtUtc)
                    .Take(2000)
                    .Select(f => new
                    {
                        f.CreatedAtUtc,
                        f.MatchId,
                        f.PlayerId,
                        f.RuleKey,
                        f.EvidenceJson
                    })
                    .ToListAsync(ct);

                var grouped = recent
                    .Select(f => new
                    {
                        f.PlayerId,
                        PartyId = TryExtractPartyId(f.EvidenceJson),
                        f.CreatedAtUtc,
                        f.MatchId
                    })
                    .GroupBy(x => new { x.PlayerId, x.PartyId })
                    .Select(g => new PartyAntiCheatSummaryItemDto(
                        PlayerId: g.Key.PlayerId,
                        PartyId: g.Key.PartyId,
                        Count: g.Count(),
                        LastSeenUtc: g.Max(x => x.CreatedAtUtc),
                        RecentMatchIds: g.Select(x => x.MatchId).Distinct().Take(10).ToList()
                    ))
                    .OrderByDescending(x => x.Count)
                    .ThenByDescending(x => x.LastSeenUtc)
                    .Take(200)
                    .ToList();

                return Results.Ok(new PartyAntiCheatSummaryResponseDto(
                    SinceUtc: since,
                    TotalFlags: recent.Count,
                    Items: grouped
                ));
            });

            // PUT /admin/anti-cheat/party/flags/{id:guid}/review
            g.MapPut("/flags/{id:guid}/review", async (
                Guid id,
                ReviewAntiCheatFlagRequestDto body,
                IAppDb db,
                CancellationToken ct) =>
            {
                var flag = await db.AntiCheatFlags.FirstOrDefaultAsync(x => x.Id == id, ct);
                if (flag is null)
                    return Results.NotFound();

                flag.MarkReviewed(body.ReviewedBy, body.Note);
                await db.SaveChangesAsync(ct);

                return Results.NoContent();
            });

        }

        private static Guid? TryExtractPartyId(string? evidenceJson)
        {
            if (string.IsNullOrWhiteSpace(evidenceJson))
                return null;

            try
            {
                using var doc = JsonDocument.Parse(evidenceJson);
                if (doc.RootElement.TryGetProperty("partyId", out var p))
                {
                    if (p.ValueKind == JsonValueKind.String && Guid.TryParse(p.GetString(), out var g))
                        return g;
                    if (p.ValueKind == JsonValueKind.Undefined || p.ValueKind == JsonValueKind.Null)
                        return null;
                }
            }
            catch
            {
                // ignore malformed evidence
            }

            return null;
        }
    }
}
