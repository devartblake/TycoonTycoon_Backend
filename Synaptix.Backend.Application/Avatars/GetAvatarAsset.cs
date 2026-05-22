using MediatR;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Avatars
{
    public sealed record GetAvatarAsset(Guid PlayerId, string AvatarId) : IRequest<GetAvatarAssetResult>;

    public sealed record GetAvatarAssetResult(bool Found, bool Owned, AvatarAssetResponseDto? Dto);

    public sealed class GetAvatarAssetHandler : IRequestHandler<GetAvatarAsset, GetAvatarAssetResult>
    {
        private readonly IAppDb _db;
        private readonly IObjectStorage _storage;
        private static readonly TimeSpan PresignExpiry = TimeSpan.FromMinutes(15);

        public GetAvatarAssetHandler(IAppDb db, IObjectStorage storage)
        {
            _db = db;
            _storage = storage;
        }

        public async Task<GetAvatarAssetResult> Handle(GetAvatarAsset request, CancellationToken ct)
        {
            var item = await _db.StoreItems
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Sku == request.AvatarId && i.IsActive && i.ItemType == "avatar", ct);

            if (item is null)
                return new GetAvatarAssetResult(false, false, null);

            var owned = await _db.PlayerTransactions
                .AsNoTracking()
                .Where(t => t.Status == PlayerTransactionStatus.Applied
                            && t.Actors.Any(a => a.PlayerId == request.PlayerId))
                .SelectMany(t => t.ItemChanges)
                .Where(i => i.ItemType == item.Sku)
                .GroupBy(i => i.ItemType)
                .Select(g => g.Sum(i => i.Operation == ItemOperation.Revoke ? -i.Quantity : i.Quantity))
                .FirstOrDefaultAsync(ct);

            if (owned <= 0)
                return new GetAvatarAssetResult(true, false, null);

            if (_storage is not IPresignedStorage presigned)
                throw new InvalidOperationException("Storage backend does not support presigned GET URLs.");

            var archiveKey = string.IsNullOrWhiteSpace(item.MediaKey)
                ? $"avatars/{request.AvatarId}.zip"
                : $"{item.MediaKey}.zip";

            var expiresAt = DateTimeOffset.UtcNow.Add(PresignExpiry);
            var presignedUrl = await presigned.GetPresignedGetUrlAsync(archiveKey, PresignExpiry, ct);

            return new GetAvatarAssetResult(true, true,
                new AvatarAssetResponseDto(presignedUrl, item.ThumbnailUrl, expiresAt, "application/zip", "zip", null));
        }
    }
}
