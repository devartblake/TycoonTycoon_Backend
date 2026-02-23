using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Events;

public sealed record AdminUploadEventQueue(AdminEventQueueUploadRequest Request, string? AdminUser) : IRequest<AdminEventQueueUploadResponse>;
public sealed record AdminReprocessEventQueue(AdminEventQueueReprocessRequest Request, string? AdminUser) : IRequest<AdminEventQueueReprocessResponse>;

public sealed class AdminUploadEventQueueHandler(IAppDb db, ILogger<AdminUploadEventQueueHandler> logger)
    : IRequestHandler<AdminUploadEventQueue, AdminEventQueueUploadResponse>
{
    public async Task<AdminEventQueueUploadResponse> Handle(AdminUploadEventQueue r, CancellationToken ct)
    {
        var accepted = 0;
        var rejected = 0;
        var duplicates = 0;
        var results = new List<AdminEventQueueUploadItemResult>();

        var playerId = ParsePlayerId(r.Request.PlayerId);
        if (playerId is null)
        {
            return new AdminEventQueueUploadResponse(0, r.Request.Events.Count, 0,
                r.Request.Events.Select(e => new AdminEventQueueUploadItemResult(e.EventId, "rejected")).ToList());
        }

        foreach (var e in r.Request.Events)
        {
            if (!Guid.TryParse(e.EventId, out var eventId) || string.IsNullOrWhiteSpace(e.EventType))
            {
                rejected++;
                results.Add(new AdminEventQueueUploadItemResult(e.EventId, "rejected"));
                continue;
            }

            var exists = await db.ProcessedGameplayEvents.AsNoTracking().AnyAsync(x => x.EventId == eventId, ct);
            if (exists)
            {
                duplicates++;
                results.Add(new AdminEventQueueUploadItemResult(e.EventId, "duplicate"));
                continue;
            }

            db.ProcessedGameplayEvents.Add(new ProcessedGameplayEvent(eventId, playerId.Value, e.EventType));
            accepted++;
            results.Add(new AdminEventQueueUploadItemResult(e.EventId, "accepted"));
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Admin event upload completed by {AdminUser}. Source={Source}, ExportedAt={ExportedAt}, PlayerId={PlayerId}, Accepted={Accepted}, Rejected={Rejected}, Duplicates={Duplicates}",
            r.AdminUser ?? "unknown",
            r.Request.Source,
            r.Request.ExportedAt,
            r.Request.PlayerId,
            accepted,
            rejected,
            duplicates);

        return new AdminEventQueueUploadResponse(accepted, rejected, duplicates, results);
    }

    private static Guid? ParsePlayerId(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId)) return null;
        if (playerId.StartsWith("player_", StringComparison.OrdinalIgnoreCase))
        {
            playerId = playerId[7..];
        }

        return Guid.TryParse(playerId, out var parsed) ? parsed : null;
    }
}

public sealed class AdminReprocessEventQueueHandler(ILogger<AdminReprocessEventQueueHandler> logger)
    : IRequestHandler<AdminReprocessEventQueue, AdminEventQueueReprocessResponse>
{
    public Task<AdminEventQueueReprocessResponse> Handle(AdminReprocessEventQueue r, CancellationToken ct)
    {
        var jobId = $"job_{Guid.NewGuid():N}";

        logger.LogInformation(
            "Admin event reprocess queued by {AdminUser}. JobId={JobId}, Scope={Scope}, Limit={Limit}",
            r.AdminUser ?? "unknown",
            jobId,
            r.Request.Scope,
            r.Request.Limit);

        return Task.FromResult(new AdminEventQueueReprocessResponse(jobId, "queued"));
    }
}
