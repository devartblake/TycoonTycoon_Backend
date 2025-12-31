using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Application.Economy;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Skills
{
    public sealed class SkillTreeService
    {
        private readonly IAppDb _db;
        private readonly EconomyService _econ;

        private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

        public SkillTreeService(IAppDb db, EconomyService econ)
        {
            _db = db;
            _econ = econ;
        }

        public async Task<SkillTreeCatalogDto> GetCatalogAsync(CancellationToken ct)
        {
            var nodes = await _db.SkillNodes.AsNoTracking()
                .OrderBy(x => x.Branch).ThenBy(x => x.Tier).ThenBy(x => x.Key)
                .ToListAsync(ct);

            return new SkillTreeCatalogDto(nodes.Select(ToDto).ToList());
        }

        public async Task<PlayerSkillStateDto> GetStateAsync(Guid playerId, CancellationToken ct)
        {
            var unlocked = await _db.PlayerSkillUnlocks.AsNoTracking()
                .Where(x => x.PlayerId == playerId)
                .OrderBy(x => x.NodeKey)
                .Select(x => x.NodeKey)
                .ToListAsync(ct);

            return new PlayerSkillStateDto(playerId, unlocked);
        }

        public async Task<UnlockSkillResultDto> UnlockAsync(UnlockSkillRequest req, CancellationToken ct)
        {
            var node = await _db.SkillNodes.AsNoTracking().FirstOrDefaultAsync(x => x.Key == req.NodeKey, ct);
            if (node is null)
                return await StateResult(req, "NotFound", ct);

            // already unlocked?
            var already = await _db.PlayerSkillUnlocks.AsNoTracking()
                .AnyAsync(x => x.PlayerId == req.PlayerId && x.NodeKey == req.NodeKey, ct);
            if (already)
                return await StateResult(req, "Duplicate", ct);

            // prereqs
            var prereqs = JsonSerializer.Deserialize<List<string>>(node.PrereqKeysJson, JsonOpts) ?? new();
            if (prereqs.Count > 0)
            {
                var unlocked = await _db.PlayerSkillUnlocks.AsNoTracking()
                    .Where(x => x.PlayerId == req.PlayerId)
                    .Select(x => x.NodeKey)
                    .ToListAsync(ct);

                if (prereqs.Any(p => !unlocked.Contains(p, StringComparer.OrdinalIgnoreCase)))
                    return await StateResult(req, "MissingPrereq", ct);
            }

            // costs -> ledger spend
            var costs = JsonSerializer.Deserialize<List<SkillCostDto>>(node.CostsJson, JsonOpts) ?? new();
            var lines = costs.Select(c =>
                new EconomyLineDto(
                    c.Currency,
                    -Math.Abs(c.Amount)
                )).ToList();

            var econRes = await _econ.ApplyAsync(
                new CreateEconomyTxnRequest(req.EventId, req.PlayerId, "skill-unlock", lines, req.NodeKey),
                ct);

            if (econRes.Status == EconomyTxnStatus.Duplicate)
                return await StateResult(req, "Duplicate", ct);

            if (econRes.Status == EconomyTxnStatus.InsufficientFunds)
                return await StateResult(req, "InsufficientFunds", ct);

            if (econRes.Status != EconomyTxnStatus.Applied)
                return await StateResult(req, "NotFound", ct);

            // persist unlock
            _db.PlayerSkillUnlocks.Add(new PlayerSkillUnlock(req.PlayerId, req.NodeKey));
            await _db.SaveChangesAsync(ct);

            return await StateResult(req, "Unlocked", ct);
        }

        public async Task<RespecSkillsResultDto> RespecAsync(RespecSkillsRequest req, CancellationToken ct)
        {
            var already = await _db.EconomyTransactions.AsNoTracking()
                .AnyAsync(x => x.EventId == req.EventId, ct);
            if (already)
            {
                var state = await GetStateAsync(req.PlayerId, ct);
                return new RespecSkillsResultDto(req.EventId, req.PlayerId, "Duplicate", 0, 0, state.UnlockedKeys);
            }

            var pct = Math.Clamp(req.RefundPercent, 0, 100);

            // Determine what the player spent on skill unlocks by reading economy txns
            var skillTxns = await _db.EconomyTransactions.AsNoTracking()
                .Where(x => x.PlayerId == req.PlayerId && x.Kind == "skill-unlock")
                .SelectMany(x => x.Lines)
                .ToListAsync(ct);

            var spentCoins = -skillTxns.Where(l => l.Currency == CurrencyType.Coins).Sum(l => l.Delta);       // deltas were negative spends
            var spentDiamonds = -skillTxns.Where(l => l.Currency == CurrencyType.Diamonds).Sum(l => l.Delta);

            var refundCoins = (int)Math.Floor(spentCoins * (pct / 100.0));
            var refundDiamonds = (int)Math.Floor(spentDiamonds * (pct / 100.0));

            var refundLines = new List<EconomyLineDto>();
            if (refundCoins > 0) refundLines.Add(new EconomyLineDto(CurrencyType.Coins, refundCoins));
            if (refundDiamonds > 0) refundLines.Add(new EconomyLineDto(CurrencyType.Diamonds, refundDiamonds));

            // Ledger tx for respec (audit-only allowed; if no refund lines, still fine)
            await _econ.ApplyAsync(
                new CreateEconomyTxnRequest(req.EventId, req.PlayerId, "skill-respec", refundLines, $"refund:{pct}%"),
                ct);

            // Remove unlocks
            var unlocks = await _db.PlayerSkillUnlocks.Where(x => x.PlayerId == req.PlayerId).ToListAsync(ct);
            _db.PlayerSkillUnlocks.RemoveRange(unlocks);
            await _db.SaveChangesAsync(ct);

            var stateAfter = await GetStateAsync(req.PlayerId, ct);
            return new RespecSkillsResultDto(req.EventId, req.PlayerId, "Respecced", refundCoins, refundDiamonds, stateAfter.UnlockedKeys);
        }

        // Admin seeding (idempotent by key)
        public async Task<int> UpsertNodesAsync(IEnumerable<SkillNodeDto> nodes, CancellationToken ct)
        {
            var count = 0;

            foreach (var n in nodes)
            {
                var existing = await _db.SkillNodes.FirstOrDefaultAsync(x => x.Key == n.Key, ct);
                var prereqJson = JsonSerializer.Serialize(n.PrereqKeys ?? Array.Empty<string>(), JsonOpts);
                var costsJson = JsonSerializer.Serialize(n.Costs ?? Array.Empty<SkillCostDto>(), JsonOpts);
                var effectsJson = JsonSerializer.Serialize(n.Effects ?? new Dictionary<string, double>(), JsonOpts);

                if (existing is null)
                {
                    _db.SkillNodes.Add(new SkillNode(
                        n.Key, n.Branch, n.Tier, n.Title, n.Description, prereqJson, costsJson, effectsJson));
                    count++;
                }
                else
                {
                    // Minimal update policy: overwrite content
                    // (Keep it simple; you can add diff logic later)
                    typeof(SkillNode).GetProperty(nameof(SkillNode.PrereqKeysJson))!.SetValue(existing, prereqJson);
                    typeof(SkillNode).GetProperty(nameof(SkillNode.CostsJson))!.SetValue(existing, costsJson);
                    typeof(SkillNode).GetProperty(nameof(SkillNode.EffectsJson))!.SetValue(existing, effectsJson);
                    typeof(SkillNode).GetProperty(nameof(SkillNode.Title))!.SetValue(existing, n.Title);
                    typeof(SkillNode).GetProperty(nameof(SkillNode.Description))!.SetValue(existing, n.Description);
                    typeof(SkillNode).GetProperty(nameof(SkillNode.Branch))!.SetValue(existing, n.Branch);
                    typeof(SkillNode).GetProperty(nameof(SkillNode.Tier))!.SetValue(existing, n.Tier);
                    existing.Touch();
                }
            }

            await _db.SaveChangesAsync(ct);
            return count;
        }

        private static SkillNodeDto ToDto(SkillNode n)
        {
            var prereqs = JsonSerializer.Deserialize<List<string>>(n.PrereqKeysJson, JsonOpts) ?? new();
            var costs = JsonSerializer.Deserialize<List<SkillCostDto>>(n.CostsJson, JsonOpts) ?? new();
            var effects = JsonSerializer.Deserialize<Dictionary<string, double>>(n.EffectsJson, JsonOpts) ?? new();

            return new SkillNodeDto(n.Key, n.Branch, n.Tier, n.Title, n.Description, prereqs, costs, effects);
        }

        private async Task<UnlockSkillResultDto> StateResult(UnlockSkillRequest req, string status, CancellationToken ct)
        {
            var state = await GetStateAsync(req.PlayerId, ct);
            return new UnlockSkillResultDto(req.EventId, req.PlayerId, req.NodeKey, status, state.UnlockedKeys);
        }
    }
}
