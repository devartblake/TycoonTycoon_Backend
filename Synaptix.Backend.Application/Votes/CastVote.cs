using Mediator;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Votes
{
    /// <summary>
    /// Valid vote options accepted by the system.
    /// </summary>
    public static class VoteOptions
    {
        public static readonly IReadOnlySet<string> Valid =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "!A", "!B", "!C", "!D", "!True", "!False" };
    }

    public record CastVote(Guid PlayerId, string Option, string Topic) : IRequest<CastVoteResult>;

    public enum CastVoteStatus { Recorded, InvalidOption, DuplicateVote }

    public record CastVoteResult(CastVoteStatus Status, CastVoteResponse? Vote = null);

    public sealed class CastVoteHandler(IAppDb db) : IRequestHandler<CastVote, CastVoteResult>
    {
        public async ValueTask<CastVoteResult> Handle(CastVote r, CancellationToken ct)
        {
            if (!VoteOptions.Valid.Contains(r.Option))
                return new CastVoteResult(CastVoteStatus.InvalidOption);

            // One vote per player per topic — reject duplicates.
            var alreadyVoted = await db.Votes
                .AsNoTracking()
                .AnyAsync(v => v.PlayerId == r.PlayerId && v.Topic == r.Topic, ct);

            if (alreadyVoted)
                return new CastVoteResult(CastVoteStatus.DuplicateVote);

            var vote = new Vote(r.PlayerId, r.Option, r.Topic);
            db.Votes.Add(vote);
            await db.SaveChangesAsync(ct);

            return new CastVoteResult(
                CastVoteStatus.Recorded,
                new CastVoteResponse(vote.Id, vote.PlayerId, vote.Option, vote.Topic, vote.TimestampUtc)
            );
        }
    }
}
