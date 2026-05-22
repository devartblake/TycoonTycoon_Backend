using MediatR;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Avatars
{
    public sealed record GetAvatarCatalog(Guid? PlayerId) : IRequest<AvatarCatalogDto>;

    public sealed class GetAvatarCatalogHandler : IRequestHandler<GetAvatarCatalog, AvatarCatalogDto>
    {
        private readonly IAppDb _db;

        public GetAvatarCatalogHandler(IAppDb db) => _db = db;

        public async Task<AvatarCatalogDto> Handle(GetAvatarCatalog request, CancellationToken ct)
        {
            var items = await _db.StoreItems
                .AsNoTracking()
                .Where(i => i.IsActive && i.ItemType == "avatar")
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.Name)
                .ToListAsync(ct);

            var ownedSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (request.PlayerId.HasValue)
            {
                var playerId = request.PlayerId.Value;
                var avatarSkus = items.Select(i => i.Sku).ToList();
                var owned = await _db.PlayerTransactions
                    .AsNoTracking()
                    .Where(t => t.Status == PlayerTransactionStatus.Applied
                                && t.Actors.Any(a => a.PlayerId == playerId))
                    .SelectMany(t => t.ItemChanges)
                    .Where(i => avatarSkus.Contains(i.ItemType))
                    .GroupBy(i => i.ItemType)
                    .Select(g => new
                    {
                        g.Key,
                        Qty = g.Sum(i => i.Operation == ItemOperation.Revoke ? -i.Quantity : i.Quantity)
                    })
                    .Where(x => x.Qty > 0)
                    .Select(x => x.Key)
                    .ToListAsync(ct);

                ownedSkus = new HashSet<string>(owned, StringComparer.OrdinalIgnoreCase);
            }

            var dtos = items.Select(i => new AvatarCatalogItemDto(
                i.Id.ToString(),
                i.Sku,
                i.Name,
                i.Description,
                i.PriceCoins,
                "coins",
                "avatar",
                "cosmetic",
                i.MediaKey,
                i.ThumbnailUrl,
                ownedSkus.Contains(i.Sku),
                i.IsFeatured,
                i.Version ?? "1.0.0"
            )).ToList();

            return new AvatarCatalogDto(dtos);
        }
    }
}
