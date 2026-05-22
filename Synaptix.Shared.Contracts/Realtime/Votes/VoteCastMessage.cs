namespace Synaptix.Shared.Contracts.Realtime.Votes
{
    public sealed record VoteCastMessage(
        Guid VoteId,
        Guid PlayerId,
        string Option,
        string Topic,
        DateTime CastAtUtc
    );
}
