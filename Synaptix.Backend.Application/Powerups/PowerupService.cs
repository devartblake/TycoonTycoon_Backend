using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Abstractions;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Powerups
{
    public sealed class PowerupService
    {
        private readonly IAppDb _db;
        private readonly IEconomyService _econ;
        private readonly IPlayerTransactionService _ptxnSvc;

        public PowerupService(IAppDb db, IEconomyService econ, IPlayerTransactionService ptxnSvc)
        {
            _db = db;
            _econ = econ;
            _ptxnSvc = ptxnSvc;
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

        public async Task<PlayerTransactionResultDto> GrantAsync(GrantPowerupRequest req, CancellationToken ct)
        {
            // Use PlayerTransaction for atomic idempotent grant (no more 0-delta hack).
            var result = await _ptxnSvc.ExecuteAsync(new CreatePlayerTransactionRequest(
                EventId: req.EventId,
                Kind: "powerup-grant",
                Actors: new[] { new PlayerTransactionActorDto(req.PlayerId, "recipient") },
                ItemChanges: new[] { new PlayerTransactionItemDto($"powerup:{req.Type}", req.Quantity, "grant") },
                Note: req.Reason
            ), ct);

            return result;
        }

        public async Task<UsePowerupResultDto> UseAsync(UsePowerupRequest req, CancellationToken ct)
        {
            var now = DateTimeOffset.UtcNow;

            // Idempotency via PlayerTransaction (replaces the 0-delta economy hack)
            var dupCheck = await _db.PlayerTransactions.AsNoTracking()
                .AnyAsync(x => x.EventId == req.EventId, ct);

            if (dupCheck)
            {
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

            // Record the use via PlayerTransaction for audit trail
            await _ptxnSvc.ExecuteAsync(new CreatePlayerTransactionRequest(
                EventId: req.EventId,
                Kind: "powerup-use",
                Actors: new[] { new PlayerTransactionActorDto(req.PlayerId, "system") },
                ItemChanges: new[] { new PlayerTransactionItemDto($"powerup:{req.Type}", 1, "revoke") },
                Note: req.Type.ToString()
            ), ct);

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
