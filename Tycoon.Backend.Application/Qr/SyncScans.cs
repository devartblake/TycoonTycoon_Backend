using MediatR;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Qr
{
    public sealed record SyncScans(SyncScansRequest Req) : IRequest<SyncScansResultDto>;

    public sealed class SyncScansHandler(IMediator mediator)
        : IRequestHandler<SyncScans, SyncScansResultDto>
    {
        public async Task<SyncScansResultDto> Handle(SyncScans r, CancellationToken ct)
        {
            var tracked = 0;
            var dup = 0;

            foreach (var scan in r.Req.Scans)
            {
                var res = await mediator.Send(new TrackScan(scan), ct);
                if (res.Status == "Tracked") tracked++;
                else dup++;
            }

            return new SyncScansResultDto(r.Req.PlayerId, r.Req.Scans.Count, tracked, dup);
        }
    }
}
