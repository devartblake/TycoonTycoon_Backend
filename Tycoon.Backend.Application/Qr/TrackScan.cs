using MediatR;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Qr
{
    public sealed record TrackScan(TrackScanRequest Req) : IRequest<TrackScanResultDto>;

    public sealed class TrackScanHandler(IAppDb db)
        : IRequestHandler<TrackScan, TrackScanResultDto>
    {
        public async Task<TrackScanResultDto> Handle(TrackScan r, CancellationToken ct)
        {
            var now = DateTimeOffset.UtcNow;

            var eventId = r.Req.EventId ?? DeterministicEventId(r.Req.PlayerId, r.Req.Value, r.Req.OccurredAtUtc, r.Req.Type);

            var dup = await db.QrScanEvents.AsNoTracking().AnyAsync(x => x.EventId == eventId, ct);
            if (dup)
                return new TrackScanResultDto(eventId, r.Req.PlayerId, "Duplicate", now);

            db.QrScanEvents.Add(new QrScanEvent(eventId, r.Req.PlayerId, r.Req.Value, r.Req.OccurredAtUtc, r.Req.Type));

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                return new TrackScanResultDto(eventId, r.Req.PlayerId, "Duplicate", now);
            }

            return new TrackScanResultDto(eventId, r.Req.PlayerId, "Tracked", now);
        }

        private static Guid DeterministicEventId(Guid playerId, string value, DateTimeOffset occurredAtUtc, QrScanType type)
        {
            // Stable deterministic GUID from a SHA256 hash (no external deps)
            var raw = $"{playerId:N}|{type}|{occurredAtUtc.UtcTicks}|{value}";
            var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw));
            Span<byte> g = stackalloc byte[16];
            bytes.AsSpan(0, 16).CopyTo(g);
            return new Guid(g);
        }
    }
}
