using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Abstractions;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Wallet.Services
{
    public sealed class EconomyService : IEconomyService
    {
        private readonly IAppDb _db;

        public EconomyService(IAppDb db) => _db = db;

        public async Task<EconomyTxnResultDto> ApplyAsync(CreateEconomyTxnRequest req, CancellationToken ct)
        {
            var now = DateTimeOffset.UtcNow;

            // Duplicate guard (fast path)
            var dup = await _db.EconomyTransactions.AsNoTracking().AnyAsync(x => x.EventId == req.EventId, ct);
            if (dup)
            {
                var wallet = await EnsureWalletAsync(req.PlayerId, ct);
                return new EconomyTxnResultDto(req.EventId, req.PlayerId, EconomyTxnStatus.Duplicate,
                    req.Lines, wallet.Xp, wallet.Coins, wallet.Diamonds, now);
            }

            var lines = req.Lines ?? Array.Empty<EconomyLineDto>();
            if (lines.Count == 0)
            {
                var wallet = await EnsureWalletAsync(req.PlayerId, ct);
                return new EconomyTxnResultDto(req.EventId, req.PlayerId, EconomyTxnStatus.Invalid,
                    Array.Empty<EconomyLineDto>(), wallet.Xp, wallet.Coins, wallet.Diamonds, now);

                // Allow
            }

            var dxp = lines.Where(l => l.Currency == CurrencyType.Xp).Sum(l => l.Delta);
            var dcoins = lines.Where(l => l.Currency == CurrencyType.Coins).Sum(l => l.Delta);
            var ddiamonds = lines.Where(l => l.Currency == CurrencyType.Diamonds).Sum(l => l.Delta);

            var w = await _db.PlayerWallets.FirstOrDefaultAsync(x => x.PlayerId == req.PlayerId, ct);
            if (w is null)
            {
                w = new PlayerWallet(req.PlayerId);
                _db.PlayerWallets.Add(w);
                await _db.SaveChangesAsync(ct);
            }

            if (!w.CanApply(dxp, dcoins, ddiamonds))
            {
                return new EconomyTxnResultDto(req.EventId, req.PlayerId, EconomyTxnStatus.InsufficientFunds,
                    lines, w.Xp, w.Coins, w.Diamonds, now);
            }

            // Apply wallet changes
            w.Apply(dxp, dcoins, ddiamonds);

            // Persist transaction
            var txn = new EconomyTransaction(req.EventId, req.PlayerId, req.Kind, req.Note);
            txn.SetLines(lines);

            _db.EconomyTransactions.Add(txn);

            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // Race: treat as duplicate
                var wallet = await EnsureWalletAsync(req.PlayerId, ct);
                return new EconomyTxnResultDto(req.EventId, req.PlayerId, EconomyTxnStatus.Duplicate,
                    lines, wallet.Xp, wallet.Coins, wallet.Diamonds, now);
            }

            return new EconomyTxnResultDto(req.EventId, req.PlayerId, EconomyTxnStatus.Applied,
                lines, w.Xp, w.Coins, w.Diamonds, now);
        }

        public async Task<EconomyHistoryDto> GetHistoryAsync(Guid playerId, int page, int pageSize, CancellationToken ct)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var q = _db.EconomyTransactions.AsNoTracking()
                .Where(x => x.PlayerId == playerId)
                .OrderByDescending(x => x.CreatedAtUtc);

            var total = await q.CountAsync(ct);

            var raw = await q.Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(x => x.Lines)
                .ToListAsync(ct);

            var txns = raw.Select(x => new EconomyTxnListItemDto(
                x.EventId,
                x.Kind,
                (x.Lines ?? Enumerable.Empty<EconomyTransactionLine>())
                    .Select(l => new EconomyLineDto(l.Currency, l.Delta)).ToList(),
                x.CreatedAtUtc
            )).ToList();

            return new EconomyHistoryDto(playerId, page, pageSize, total, txns);
        }

        public async Task<EconomyTxnResultDto> RollbackByEventIdAsync(Guid eventId, string reason, CancellationToken ct)
        {
            var original = await _db.EconomyTransactions
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.EventId == eventId, ct);

            if (original is null)
                throw new InvalidOperationException("Original transaction not found.");

            var existingRollback = await _db.EconomyTransactions
                .AsNoTracking()
                .AnyAsync(x => x.ReversalOfTransactionId == original.Id, ct);

            if (existingRollback)
                throw new InvalidOperationException("Original transaction already rolled back.");

            var rollbackEventId = Guid.NewGuid();
            var rollbackLines = (original.Lines ?? [])
                .Select(l => new EconomyLineDto(l.Currency, -l.Delta))
                .ToArray();

            var result = await ApplyAsync(new CreateEconomyTxnRequest(
                EventId: rollbackEventId,
                PlayerId: original.PlayerId,
                Kind: $"rollback:{original.Kind}",
                Lines: rollbackLines,
                Note: reason
            ), ct);

            if (result.Status != EconomyTxnStatus.Applied)
                throw new InvalidOperationException($"Rollback failed with status '{result.Status}'.");

            var rollbackTxn = await _db.EconomyTransactions
                .FirstAsync(x => x.EventId == rollbackEventId, ct);
            rollbackTxn.MarkAsReversalOf(original.Id);
            await _db.SaveChangesAsync(ct);

            return result;
        }

        private async Task<PlayerWallet> EnsureWalletAsync(Guid playerId, CancellationToken ct)
        {
            var w = await _db.PlayerWallets.AsNoTracking().FirstOrDefaultAsync(x => x.PlayerId == playerId, ct);
            if (w is null)
            {
                w = new PlayerWallet(playerId);
                _db.PlayerWallets.Add(w);
                await _db.SaveChangesAsync(ct);
                return w;
            }
            return w;
        }
    }
}
