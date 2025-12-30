using MediatR;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Matches
{
    public record StartMatch(Guid HostPlayerId, string Mode) : IRequest<StartMatchResponse>;

    public sealed class StartMatchHandler(IAppDb db)
        : IRequestHandler<StartMatch, StartMatchResponse>
    {
        public async Task<StartMatchResponse> Handle(StartMatch r, CancellationToken ct)
        {
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
