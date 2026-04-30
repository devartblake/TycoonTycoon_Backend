using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tycoon.Backend.Application.Qr;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.Qr
{
    public static class QrEndpoints
    {
        public static void Map(WebApplication app)
        {
            var g = app.MapGroup("/qr").WithTags("QR");

            g.MapPost("/track-scan", async ([FromBody] TrackScanRequest req, IMediator mediator, CancellationToken ct) =>
            {
                var res = await mediator.Send(new TrackScan(req), ct);
                return Results.Ok(res);
            });

            g.MapPost("/sync", async ([FromBody] SyncScansRequest req, IMediator mediator, CancellationToken ct) =>
            {
                var res = await mediator.Send(new SyncScans(req), ct);
                return Results.Ok(res);
            });

            g.MapGet("/history/{playerId:guid}", async (
                [FromRoute] Guid playerId,
                [FromQuery] QrScanType? type,
                [FromQuery] DateTimeOffset? fromUtc,
                [FromQuery] DateTimeOffset? toUtc,
                [FromQuery] int page,
                [FromQuery] int pageSize,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var dto = await mediator.Send(new GetScanHistory(playerId, type, fromUtc, toUtc, page, pageSize), ct);
                return Results.Ok(dto);
            });
        }
    }
}
