using Mediator;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.Config;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Matches
{
    public sealed class ModeEntryDeniedException(string reasonCode, string message) : InvalidOperationException(message)
    {
        public string ReasonCode { get; } = reasonCode;
    }

    public record StartMatch(Guid HostPlayerId, string Mode) : IRequest<StartMatchResponse>;

    public sealed class StartMatchHandler(IAppDb db, IGameBalancePolicyService policy)
        : IRequestHandler<StartMatch, StartMatchResponse>
    {
        public async ValueTask<StartMatchResponse> Handle(StartMatch r, CancellationToken ct)
        {
            // Return existing active match idempotently (no energy charge).
            var existing = await db.Matches
                .AsNoTracking()
                .OrderByDescending(m => m.StartedAt)
                .FirstOrDefaultAsync(m =>
                    m.HostPlayerId == r.HostPlayerId &&
                    m.FinishedAt == null, ct);

            if (existing is not null)
                return new StartMatchResponse(existing.Id, existing.StartedAt);

            // Check energy/ticket policy — consumes resources if allowed.
            var decision = await policy.TryEnterModeAsync(r.HostPlayerId, r.Mode, ct);
            if (!decision.Allowed)
                throw new ModeEntryDeniedException(decision.ReasonCode, decision.Message);

            var match = new Match(
                r.HostPlayerId,
                string.IsNullOrWhiteSpace(r.Mode) ? "solo" : r.Mode
            );

            db.Matches.Add(match);
            await db.SaveChangesAsync(ct);

            return new StartMatchResponse(match.Id, match.StartedAt);
        }
    }
}
