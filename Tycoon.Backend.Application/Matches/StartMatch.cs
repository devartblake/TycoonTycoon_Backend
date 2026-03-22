using MediatR;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Application.Config;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Matches
{
    public sealed class ModeEntryDeniedException(string reasonCode, string message) : InvalidOperationException(message)
    {
        public string ReasonCode { get; } = reasonCode;
    }

    public record StartMatch(Guid HostPlayerId, string Mode) : IRequest<StartMatchResponse>;

    public sealed class StartMatchHandler(IAppDb db, IGameBalancePolicyService policy)
        : IRequestHandler<StartMatch, StartMatchResponse>
    {
        public async Task<StartMatchResponse> Handle(StartMatch r, CancellationToken ct)
        {
            var decision = await policy.TryEnterModeAsync(r.HostPlayerId, r.Mode, ct);
            if (!decision.Allowed)
                throw new InvalidOperationException(decision.Message);

            // Prevent duplicate active matches for the same host.
            // "Active" == FinishedAt is null in your domain model.
            var existing = await db.Matches
                .AsNoTracking()
                .OrderByDescending(m => m.StartedAt)
                .FirstOrDefaultAsync(m =>
                    m.HostPlayerId == r.HostPlayerId &&
                    m.FinishedAt == null, ct);

            if (existing is not null)
            {
                // Return existing match instead of creating a new one.
                return new StartMatchResponse(existing.Id, existing.StartedAt);
            }

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
