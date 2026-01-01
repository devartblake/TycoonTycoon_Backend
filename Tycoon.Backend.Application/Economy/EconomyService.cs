using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Economy
{
    public sealed class EconomyService
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

            var dxp = req.Lines.Where(l => l.Currency == CurrencyType.Xp).Sum(l => l.Delta);
            var dcoins = req.Lines.Where(l => l.Currency == CurrencyType.Coins).Sum(l => l.Delta);
            var ddiamonds = req.Lines.Where(l => l.Currency == CurrencyType.Diamonds).Sum(l => l.Delta);

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
                    req.Lines, w.Xp, w.Coins, w.Diamonds, now);
            }

            // Apply wallet changes
            w.Apply(dxp, dcoins, ddiamonds);

            // Persist transaction
            var txn = new EconomyTransaction(req.EventId, req.PlayerId, req.Kind, req.Note);
            txn.SetLines(req.Lines);

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
                    req.Lines, wallet.Xp, wallet.Coins, wallet.Diamonds, now);
            }

            return new EconomyTxnResultDto(req.EventId, req.PlayerId, EconomyTxnStatus.Applied,
                req.Lines, w.Xp, w.Coins, w.Diamonds, now);
        }

        public async Task<EconomyTxnResultDto> RollbackAsync(
            Guid originalEventId,
            string reason,
            CancellationToken ct)
        {
            var original = await _db.EconomyTransactions
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.EventId == originalEventId, ct);

            if (original is null)
                throw new InvalidOperationException("Original transaction not found.");

            // Prevent double rollback
            var exists = await _db.EconomyTransactions
                .AnyAsync(x => x.ReversalOfTransactionId == original.Id, ct);

            if (exists)
                throw new InvalidOperationException("Transaction already rolled back.");

            var reversalLines = original.Lines
                .Select(l => new EconomyLineDto(l.Currency, -l.Delta))
                .ToList();

            return await ApplyAsync(new CreateEconomyTxnRequest(
                EventId: Guid.NewGuid(),
                PlayerId: original.PlayerId,
                Kind: "rollback",
                Note: $"Rollback: {reason}",
                Lines: reversalLines
            ), ct);
        }

        public async Task<EconomyTxnResultDto> RollbackByEventIdAsync(
            Guid originalEventId,
            string reason,
            CancellationToken ct)
        {
            var original = await _db.EconomyTransactions
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.EventId == originalEventId, ct);

            if (original is null)
                throw new InvalidOperationException("Original transaction not found.");

            // Prevent double roolback
            var already = await _db.EconomyTransactions
                .AnyAsync(x => x.ReversalOfTransactionId == original.Id, ct);

            if (already)
                throw new InvalidOperationException("Transaction already rolled back.");

            // Create inverted lines
            var invertedLines = original.Lines
                .Select(l => new EconomyLineDto(l.Currency, -l.Delta))
                .ToList();

            // Use your existing ApplyAsync/Create flow so wallet updates remain consistent
            var res = await ApplyAsync(new CreateEconomyTxnRequest(
                EventId: Guid.NewGuid(),
                PlayerId: original.PlayerId,
                Kind: "rollback",
                Note: $"Rollback of {original.EventId}: {reason}",
                Lines: invertedLines
            ), ct);

            // IMPORTANT: link reversal txn back to original
            var reversalTxn = await _db.EconomyTransactions
                .FirstAsync(x => x.EventId == res.EventId, ct);

            reversalTxn.MarkAsReversalOf(original.Id);

            await _db.SaveChangesAsync(ct);

            return res;
        }

        public async Task<EconomyHistoryDto> GetHistoryAsync(Guid playerId, int page, int pageSize, CancellationToken ct)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var q = _db.EconomyTransactions.AsNoTracking()
                .Where(x => x.PlayerId == playerId)
                .OrderByDescending(x => x.CreatedAtUtc);

            var total = await q.CountAsync(ct);

            var txns = await q.Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new EconomyTxnListItemDto(
                    x.EventId,
                    x.Kind,
                    x.Lines.Select(l => new EconomyLineDto(l.Currency, l.Delta)).ToList(),
                    x.CreatedAtUtc
                ))
                .ToListAsync(ct);

            return new EconomyHistoryDto(playerId, page, pageSize, total, txns);
        }

        private async Task<PlayerWallet> EnsureWalletAsync(Guid playerId, CancellationToken ct)
        {
            var w = await _db.PlayerWallets.AsNoTracking().FirstOrDefaultAsync(x => x.PlayerId == playerId, ct);
            return w ?? new PlayerWallet(playerId);
        }
    }
}
