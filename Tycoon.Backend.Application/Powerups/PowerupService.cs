using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Application.Economy;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Powerups
{
    public sealed class PowerupService
    {
        private readonly IAppDb _db;
        private readonly EconomyService _econ;

        public PowerupService(IAppDb db, EconomyService econ)
        {
            _db = db;
            _econ = econ;
        }

        public async Task<PowerupStateDto> GetStateAsync(Guid playerId, CancellationToken ct)
        {
            var list = await _db.PlayerPowerups.AsNoTracking()
                .Where(x => x.PlayerId == playerId)
                .OrderBy(x => x.Type)
                .Select(x => new PowerupBalanceDto(x.Type, x.Quantity, x.CooldownUntilUtc))
                .ToListAsync(ct);

            return new PowerupStateDto(playerId, list);
        }

        public async Task<EconomyTxnResultDto> GrantAsync(GrantPowerupRequest req, CancellationToken ct)
        {
            // Idempotency is handled at the ledger layer using EventId.
            // We still update inventory as part of the same unit-of-work (same DbContext).
            // Approach: apply ledger txn (0 currency delta allowed? If not, add a tiny non-monetary line later).
            // For now: ledger entry with 0 lines is "Invalid". We'll include a 0-coin line? No.
            // Better: treat grants as out-of-band inventory change with its own idempotency.
            // We'll implement idempotency using ProcessedGameplayEvent table if you kept it,
            // but since Step 5 establishes the ledger, we’ll just do a small coins delta of 0 is invalid.
            // So: store a ledger transaction with a note and a 0-coin line is still invalid in our validation.
            // We'll instead record inventory idempotency with EconomyTransactions by adding a 0 delta line is allowed.
            // Update: allow 0 deltas by not rejecting empty/non-zero; easiest fix is to accept 0 line(s) in EconomyService.
            // To avoid touching that now, we do "coins +0" as a single line.
            var econReq = new CreateEconomyTxnRequest(
                req.EventId,
                req.PlayerId,
                "powerup-grant",
                new[] { new EconomyLineDto(CurrencyType.Coins, 0) },
                req.Reason);

            var econRes = await _econ.ApplyAsync(econReq, ct);
            if (econRes.Status is EconomyTxnStatus.Duplicate)
                return econRes;

            // Apply inventory
            var p = await _db.PlayerPowerups.FirstOrDefaultAsync(x => x.PlayerId == req.PlayerId && x.Type == req.Type, ct);
            if (p is null)
            {
                p = new PlayerPowerup(req.PlayerId, req.Type);
                _db.PlayerPowerups.Add(p);
            }
            p.Add(req.Quantity);

            await _db.SaveChangesAsync(ct);
            return econRes;
        }

        public async Task<UsePowerupResultDto> UseAsync(UsePowerupRequest req, CancellationToken ct)
        {
            var now = DateTimeOffset.UtcNow;

            // Idempotency via economy ledger (0 delta line)
            var econReq = new CreateEconomyTxnRequest(
                req.EventId,
                req.PlayerId,
                "powerup-use",
                new[] { new EconomyLineDto(CurrencyType.Coins, 0) },
                req.Type.ToString());

            var econRes = await _econ.ApplyAsync(econReq, ct);
            if (econRes.Status == EconomyTxnStatus.Duplicate)
            {
                // Best-effort state read
                var dupState = await _db.PlayerPowerups.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.PlayerId == req.PlayerId && x.Type == req.Type, ct);

                return new UsePowerupResultDto(req.EventId, req.PlayerId, req.Type, "Duplicate",
                    dupState?.Quantity ?? 0, dupState?.CooldownUntilUtc);
            }

            var p = await _db.PlayerPowerups.FirstOrDefaultAsync(x => x.PlayerId == req.PlayerId && x.Type == req.Type, ct);
            if (p is null)
                return new UsePowerupResultDto(req.EventId, req.PlayerId, req.Type, "Insufficient", 0, null);

            if (!p.CanUse(now, out var reason))
            {
                return new UsePowerupResultDto(req.EventId, req.PlayerId, req.Type,
                    reason == "Cooldown" ? "Cooldown" : "Insufficient",
                    p.Quantity,
                    p.CooldownUntilUtc);
            }

            // Cooldown policy (tune later)
            var cooldown = req.Type switch
            {
                PowerupType.FiftyFifty => TimeSpan.FromSeconds(15),
                PowerupType.Skip => TimeSpan.FromSeconds(10),
                PowerupType.DoublePoints => TimeSpan.FromSeconds(20),
                PowerupType.ExtraTime => TimeSpan.FromSeconds(30),
                _ => TimeSpan.FromSeconds(10)
            };

            p.Use(now, cooldown);
            await _db.SaveChangesAsync(ct);

            return new UsePowerupResultDto(req.EventId, req.PlayerId, req.Type, "Used", p.Quantity, p.CooldownUntilUtc);
        }
    }
}
