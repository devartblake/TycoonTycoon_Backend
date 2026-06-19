using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Abstractions;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Wallet.Services
{
    public sealed class PlayerTransactionService : IPlayerTransactionService
    {
        private readonly IAppDb _db;
        private readonly IEconomyService _econ;

        public PlayerTransactionService(IAppDb db, IEconomyService econ)
        {
            _db = db;
            _econ = econ;
        }

        /// <summary>
        /// Execute a composite player transaction atomically.
        /// Wraps one or more EconomyTransaction ledger entries + optional inventory changes
        /// into a single business operation with all-or-nothing semantics.
        /// </summary>
        public async Task<PlayerTransactionResultDto> ExecuteAsync(CreatePlayerTransactionRequest req, CancellationToken ct)
        {
            // ── Idempotency check ──
            var dup = await _db.PlayerTransactions.AsNoTracking()
                .AnyAsync(x => x.EventId == req.EventId, ct);

            if (dup)
            {
                var existing = await _db.PlayerTransactions.AsNoTracking()
                    .Include(x => x.Actors)
                    .Include(x => x.ItemChanges)
                    .FirstAsync(x => x.EventId == req.EventId, ct);

                return ToResult(existing, "Duplicate", Array.Empty<EconomyTxnResultDto>());
            }

            // ── Create aggregate ──
            var ptxn = new PlayerTransaction(req.EventId, req.Kind, req.CorrelatedEventId, req.Receipt);

            // Actors
            if (req.Actors is { Count: > 0 })
            {
                foreach (var a in req.Actors)
                {
                    ptxn.AddActor(a.PlayerId, ParseRole(a.Role), a.AllocationPercent);
                }
            }

            // Item changes (tracked, applied after economy succeeds)
            if (req.ItemChanges is { Count: > 0 })
            {
                foreach (var item in req.ItemChanges)
                {
                    ptxn.AddItemChange(item.ItemType, item.Quantity, ParseOperation(item.Operation));
                }
            }

            _db.PlayerTransactions.Add(ptxn);

            // ── Economy ledger entries (all-or-nothing) ──
            var econResults = new List<EconomyTxnResultDto>();

            if (req.CurrencyChanges is { Count: > 0 })
            {
                foreach (var change in req.CurrencyChanges)
                {
                    // Deterministic per-player event ID derived from the parent EventId
                    var playerEventId = DeterministicGuid(req.EventId, change.PlayerId);

                    var econReq = new CreateEconomyTxnRequest(
                        EventId: playerEventId,
                        PlayerId: change.PlayerId,
                        Kind: req.Kind,
                        Lines: change.Lines,
                        Note: req.Note ?? (req.CorrelatedEventId.HasValue ? $"ptxn:{req.CorrelatedEventId}" : null)
                    );

                    var econRes = await _econ.ApplyAsync(econReq, ct);
                    econResults.Add(econRes);

                    if (econRes.Status == EconomyTxnStatus.InsufficientFunds)
                    {
                        ptxn.MarkFailed();
                        await SaveOrHandleDuplicate(ptxn, ct);
                        return ToResult(ptxn, "InsufficientFunds", econResults);
                    }

                    if (econRes.Status == EconomyTxnStatus.Invalid)
                    {
                        ptxn.MarkFailed();
                        await SaveOrHandleDuplicate(ptxn, ct);
                        return ToResult(ptxn, "Failed", econResults);
                    }

                    // Link the economy transaction to this aggregate
                    if (econRes.Status == EconomyTxnStatus.Applied)
                    {
                        var econTxn = await _db.EconomyTransactions
                            .FirstOrDefaultAsync(x => x.EventId == playerEventId, ct);

                        econTxn?.LinkToPlayerTransaction(ptxn.Id);
                    }
                }
            }

            // ── Apply inventory mutations ──
            if (req.ItemChanges is { Count: > 0 })
            {
                foreach (var item in req.ItemChanges)
                {
                    await ApplyItemChangeAsync(ptxn.Actors, item, ct);
                }
            }

            // ── Mark complete ──
            ptxn.MarkApplied();

            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // Race condition on EventId unique index
                return ToResult(ptxn, "Duplicate", econResults);
            }

            return ToResult(ptxn, "Applied", econResults);
        }

        /// <summary>
        /// Dispute a previously applied transaction.
        /// </summary>
        public async Task<PlayerTransactionResultDto> DisputeAsync(DisputePlayerTransactionRequest req, CancellationToken ct)
        {
            var ptxn = await _db.PlayerTransactions
                .Include(x => x.Actors)
                .Include(x => x.ItemChanges)
                .FirstOrDefaultAsync(x => x.Id == req.PlayerTransactionId, ct);

            if (ptxn is null)
                throw new InvalidOperationException("Player transaction not found.");

            ptxn.Dispute(req.Reason);
            await _db.SaveChangesAsync(ct);

            return ToResult(ptxn, ptxn.Status.ToString(), Array.Empty<EconomyTxnResultDto>());
        }

        /// <summary>
        /// Reverse a transaction: rolls back all child economy transactions and reverts item changes.
        /// </summary>
        public async Task<PlayerTransactionResultDto> ReverseAsync(ReversePlayerTransactionRequest req, CancellationToken ct)
        {
            var ptxn = await _db.PlayerTransactions
                .Include(x => x.Actors)
                .Include(x => x.ItemChanges)
                .Include(x => x.EconomyTransactions).ThenInclude(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == req.PlayerTransactionId, ct);

            if (ptxn is null)
                throw new InvalidOperationException("Player transaction not found.");

            if (ptxn.Status == PlayerTransactionStatus.Reversed)
                throw new InvalidOperationException("Transaction already reversed.");

            var econResults = new List<EconomyTxnResultDto>();

            // Rollback each child economy transaction
            foreach (var econTxn in ptxn.EconomyTransactions)
            {
                // Skip if already rolled back
                var alreadyRolledBack = await _db.EconomyTransactions.AsNoTracking()
                    .AnyAsync(x => x.ReversalOfTransactionId == econTxn.Id, ct);

                if (alreadyRolledBack) continue;

                try
                {
                    var result = await _econ.RollbackByEventIdAsync(econTxn.EventId, req.Reason, ct);
                    econResults.Add(result);
                }
                catch (InvalidOperationException)
                {
                    // Already rolled back or not found — continue
                }
            }

            // Revert item changes
            foreach (var item in ptxn.ItemChanges)
            {
                await RevertItemChangeAsync(ptxn.Actors, item, ct);
            }

            ptxn.MarkReversed();
            await _db.SaveChangesAsync(ct);

            return ToResult(ptxn, "Reversed", econResults);
        }

        /// <summary>
        /// Get paginated transaction history, optionally filtered by player or correlated event.
        /// </summary>
        public async Task<PlayerTransactionHistoryDto> GetHistoryAsync(
            Guid? playerId, Guid? correlatedEventId, int page, int pageSize, CancellationToken ct)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var q = _db.PlayerTransactions.AsNoTracking()
                .Include(x => x.Actors)
                .Include(x => x.ItemChanges)
                .Include(x => x.EconomyTransactions)
                .AsQueryable();

            if (playerId.HasValue)
                q = q.Where(x => x.Actors.Any(a => a.PlayerId == playerId.Value));

            if (correlatedEventId.HasValue)
                q = q.Where(x => x.CorrelatedEventId == correlatedEventId.Value);

            q = q.OrderByDescending(x => x.CreatedAtUtc);

            var total = await q.CountAsync(ct);

            var items = await q
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new PlayerTransactionListItemDto(
                    x.Id,
                    x.EventId,
                    x.CorrelatedEventId,
                    x.Kind,
                    x.Status.ToString(),
                    x.DisputeReason,
                    x.Actors.Count,
                    x.EconomyTransactions.Count,
                    x.ItemChanges.Count,
                    x.CreatedAtUtc,
                    x.CompletedAtUtc
                ))
                .ToListAsync(ct);

            return new PlayerTransactionHistoryDto(page, pageSize, total, items);
        }

        /// <summary>
        /// Get full detail of a single player transaction.
        /// </summary>
        public async Task<PlayerTransactionDetailDto?> GetDetailAsync(Guid id, CancellationToken ct)
        {
            var ptxn = await _db.PlayerTransactions.AsNoTracking()
                .Include(x => x.Actors)
                .Include(x => x.ItemChanges)
                .Include(x => x.EconomyTransactions).ThenInclude(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (ptxn is null) return null;

            return new PlayerTransactionDetailDto(
                ptxn.Id,
                ptxn.EventId,
                ptxn.CorrelatedEventId,
                ptxn.Kind,
                ptxn.Status.ToString(),
                ptxn.Receipt,
                ptxn.DisputeReason,
                ptxn.DisputeLinkedToTransactionId,
                ptxn.CreatedAtUtc,
                ptxn.CompletedAtUtc,
                ptxn.Actors.Select(a => new PlayerTransactionActorDto(a.PlayerId, a.Role.ToString(), a.AllocationPercent)).ToList(),
                ptxn.ItemChanges.Select(i => new PlayerTransactionItemDto(i.ItemType, i.Quantity, i.Operation.ToString())).ToList(),
                ptxn.EconomyTransactions.Select(e => new EconomyTxnListItemDto(
                    e.EventId,
                    e.Kind,
                    (e.Lines ?? Enumerable.Empty<EconomyTransactionLine>())
                        .Select(l => new EconomyLineDto(l.Currency, l.Delta)).ToList(),
                    e.CreatedAtUtc
                )).ToList()
            );
        }

        // ── Private helpers ──────────────────────────────────────────

        private async Task ApplyItemChangeAsync(
            List<PlayerTransactionActor> actors, PlayerTransactionItemDto item, CancellationToken ct)
        {
            // For powerup-type items, update PlayerPowerup inventory
            if (item.ItemType.StartsWith("powerup:", StringComparison.OrdinalIgnoreCase))
            {
                var powerupType = item.ItemType.Split(':')[1];

                // Apply to each actor with a recipient/buyer/system role
                var targetActors = actors
                    .Where(a => a.Role is PlayerTransactionActorRole.Recipient
                        or PlayerTransactionActorRole.Buyer
                        or PlayerTransactionActorRole.System)
                    .ToList();

                // If no specific target actors, fall back to first actor
                if (targetActors.Count == 0 && actors.Count > 0)
                    targetActors.Add(actors[0]);

                foreach (var actor in targetActors)
                {
                    if (Enum.TryParse<PowerupType>(powerupType, true, out var pt))
                    {
                        var pp = await _db.PlayerPowerups
                            .FirstOrDefaultAsync(x => x.PlayerId == actor.PlayerId && x.Type == pt, ct);

                        if (item.Operation == "grant")
                        {
                            if (pp is null)
                            {
                                pp = new PlayerPowerup(actor.PlayerId, pt);
                                _db.PlayerPowerups.Add(pp);
                            }
                            pp.Add(item.Quantity);
                        }
                        else if (item.Operation == "revoke" && pp is not null)
                        {
                            pp.Add(-item.Quantity);
                        }
                    }
                }
            }
        }

        private async Task RevertItemChangeAsync(
            List<PlayerTransactionActor> actors, PlayerTransactionItem item, CancellationToken ct)
        {
            if (item.ItemType.StartsWith("powerup:", StringComparison.OrdinalIgnoreCase))
            {
                var powerupType = item.ItemType.Split(':')[1];

                var targetActors = actors
                    .Where(a => a.Role is PlayerTransactionActorRole.Recipient
                        or PlayerTransactionActorRole.Buyer
                        or PlayerTransactionActorRole.System)
                    .ToList();

                if (targetActors.Count == 0 && actors.Count > 0)
                    targetActors.Add(actors[0]);

                foreach (var actor in targetActors)
                {
                    if (Enum.TryParse<PowerupType>(powerupType, true, out var pt))
                    {
                        var pp = await _db.PlayerPowerups
                            .FirstOrDefaultAsync(x => x.PlayerId == actor.PlayerId && x.Type == pt, ct);

                        if (pp is null) continue;

                        if (item.Operation == ItemOperation.Grant)
                            pp.Add(-item.Quantity);
                        else if (item.Operation == ItemOperation.Revoke)
                            pp.Add(item.Quantity);
                    }
                }
            }
        }

        private async Task SaveOrHandleDuplicate(PlayerTransaction ptxn, CancellationToken ct)
        {
            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // Race condition — ignore
            }
        }

        private static PlayerTransactionResultDto ToResult(
            PlayerTransaction ptxn, string status, IReadOnlyList<EconomyTxnResultDto> econResults)
        {
            return new PlayerTransactionResultDto(
                ptxn.Id,
                ptxn.EventId,
                ptxn.Kind,
                status,
                ptxn.CreatedAtUtc,
                ptxn.CompletedAtUtc,
                ptxn.Actors.Select(a => new PlayerTransactionActorDto(a.PlayerId, a.Role.ToString(), a.AllocationPercent)).ToList(),
                ptxn.ItemChanges.Select(i => new PlayerTransactionItemDto(i.ItemType, i.Quantity, i.Operation.ToString())).ToList(),
                econResults
            );
        }

        private static PlayerTransactionActorRole ParseRole(string role)
        {
            return role.ToLowerInvariant() switch
            {
                "system" => PlayerTransactionActorRole.System,
                "buyer" => PlayerTransactionActorRole.Buyer,
                "seller" => PlayerTransactionActorRole.Seller,
                "recipient" => PlayerTransactionActorRole.Recipient,
                "sender" => PlayerTransactionActorRole.Sender,
                _ => PlayerTransactionActorRole.System
            };
        }

        private static ItemOperation ParseOperation(string operation)
        {
            return operation.ToLowerInvariant() switch
            {
                "grant" => ItemOperation.Grant,
                "revoke" => ItemOperation.Revoke,
                "swap" => ItemOperation.Swap,
                _ => ItemOperation.Grant
            };
        }

        private static Guid DeterministicGuid(Guid a, Guid b)
        {
            Span<byte> bytes = stackalloc byte[32];
            a.TryWriteBytes(bytes[..16]);
            b.TryWriteBytes(bytes[16..]);

            Span<byte> folded = stackalloc byte[16];
            for (var i = 0; i < 16; i++)
                folded[i] = (byte)(bytes[i] ^ bytes[i + 16]);

            return new Guid(folded);
        }
    }
}
