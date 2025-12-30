using MediatR;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Qr
{
    public sealed record GetScanHistory(
        Guid PlayerId,
        QrScanType? Type,
        DateTimeOffset? FromUtc,
        DateTimeOffset? ToUtc,
        int Page = 1,
        int PageSize = 50
    ) : IRequest<ScanHistoryDto>;

    public sealed class GetScanHistoryHandler(IAppDb db)
        : IRequestHandler<GetScanHistory, ScanHistoryDto>
    {
        public async Task<ScanHistoryDto> Handle(GetScanHistory r, CancellationToken ct)
        {
            var page = Math.Max(1, r.Page);
            var pageSize = Math.Clamp(r.PageSize, 1, 100);

            var q = db.QrScanEvents.AsNoTracking()
                .Where(x => x.PlayerId == r.PlayerId);

            if (r.Type.HasValue)
                q = q.Where(x => x.Type == r.Type.Value);

            if (r.FromUtc.HasValue)
                q = q.Where(x => x.OccurredAtUtc >= r.FromUtc.Value);

            if (r.ToUtc.HasValue)
                q = q.Where(x => x.OccurredAtUtc <= r.ToUtc.Value);

            q = q.OrderByDescending(x => x.OccurredAtUtc);

            var total = await q.CountAsync(ct);

            var items = await q
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new ScanHistoryItemDto(
                    x.EventId,
                    x.PlayerId,
                    x.Value,
                    x.OccurredAtUtc,
                    x.Type))
                .ToListAsync(ct);

            return new ScanHistoryDto(r.PlayerId, page, pageSize, total, items);
        }
    }
}
