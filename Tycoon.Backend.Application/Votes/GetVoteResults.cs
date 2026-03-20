using MediatR;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Votes
{
    public record GetVoteResults(string Topic) : IRequest<VoteResultsResponse>;

    public sealed class GetVoteResultsHandler(IAppDb db) : IRequestHandler<GetVoteResults, VoteResultsResponse>
    {
        public async Task<VoteResultsResponse> Handle(GetVoteResults r, CancellationToken ct)
        {
            var tally = await db.Votes
                .AsNoTracking()
                .Where(v => v.Topic == r.Topic)
                .GroupBy(v => v.Option)
                .Select(g => new { Option = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var total = tally.Sum(t => t.Count);

            var results = tally
                .OrderByDescending(t => t.Count)
                .Select(t => new VoteOptionResult(
                    t.Option,
                    t.Count,
                    total == 0 ? 0 : Math.Round((double)t.Count / total * 100, 2)
                ))
                .ToList();

            return new VoteResultsResponse(r.Topic, total, results);
        }
    }
}
