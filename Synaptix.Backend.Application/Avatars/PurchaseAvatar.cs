using Mediator;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Shared.Contracts.Abstractions;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Avatars
{
    public sealed record PurchaseAvatar(Guid PlayerId, string AvatarId) : IRequest<PurchaseAvatarResult>;

    public sealed record PurchaseAvatarResult(
        bool Success,
        PurchaseAvatarResultDto? Dto,
        string? ErrorCode,
        string? ErrorMessage,
        object? ErrorDetails
    );

    public sealed class PurchaseAvatarHandler : IRequestHandler<PurchaseAvatar, PurchaseAvatarResult>
    {
        private readonly IAppDb _db;
        private readonly IPlayerTransactionService _txnService;

        public PurchaseAvatarHandler(IAppDb db, IPlayerTransactionService txnService)
        {
            _db = db;
            _txnService = txnService;
        }

        public async ValueTask<PurchaseAvatarResult> Handle(PurchaseAvatar request, CancellationToken ct)
        {
            var item = await _db.StoreItems
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Sku == request.AvatarId && i.IsActive && i.ItemType == "avatar", ct);

            if (item is null)
                return new PurchaseAvatarResult(false, null, "avatar_not_found",
                    $"Avatar {request.AvatarId} does not exist in the catalog.", null);

            var alreadyOwned = await _db.PlayerTransactions
                .AsNoTracking()
                .Where(t => t.Status == PlayerTransactionStatus.Applied
                            && t.Actors.Any(a => a.PlayerId == request.PlayerId))
                .SelectMany(t => t.ItemChanges)
                .Where(i => i.ItemType == item.Sku)
                .GroupBy(i => i.ItemType)
                .Select(g => g.Sum(i => i.Operation == ItemOperation.Revoke ? -i.Quantity : i.Quantity))
                .FirstOrDefaultAsync(ct);

            if (alreadyOwned > 0)
                return new PurchaseAvatarResult(false, null, "already_owned",
                    "Player already owns this avatar.", null);

            var txnReq = new CreatePlayerTransactionRequest(
                EventId: Guid.NewGuid(),
                Kind: "store-purchase",
                Actors: new[] { new PlayerTransactionActorDto(request.PlayerId, "buyer") },
                CurrencyChanges: new[]
                {
                    new PlayerTransactionCurrencyDto(
                        request.PlayerId,
                        new[] { new EconomyLineDto(CurrencyType.Coins, -item.PriceCoins) })
                },
                ItemChanges: new[]
                {
                    new PlayerTransactionItemDto(item.Sku, 1, "grant")
                },
                Note: $"Avatar purchase: {item.Name}"
            );

            var result = await _txnService.ExecuteAsync(txnReq, ct);

            if (result.Status == "InsufficientFunds")
            {
                var available = result.EconomyResults.FirstOrDefault()?.BalanceCoins ?? 0;
                return new PurchaseAvatarResult(false, null, "insufficient_funds",
                    "Not enough coins to purchase this avatar.",
                    new { required = item.PriceCoins, available });
            }

            if (result.Status != "Applied")
                return new PurchaseAvatarResult(false, null, "purchase_failed",
                    $"Purchase could not be completed: {result.Status}", null);

            var newBalance = result.EconomyResults.FirstOrDefault()?.BalanceCoins ?? 0;

            return new PurchaseAvatarResult(true,
                new PurchaseAvatarResultDto(true, request.AvatarId, item.PriceCoins, newBalance),
                null, null, null);
        }
    }
}
