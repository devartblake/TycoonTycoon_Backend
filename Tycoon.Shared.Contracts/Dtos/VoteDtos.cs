namespace Tycoon.Shared.Contracts.Dtos
{
    /// <summary>Request to cast a vote (e.g. !A, !B, !C).</summary>
    public sealed record CastVoteRequest(
        Guid PlayerId,
        string Option,
        string Topic
    );

    /// <summary>Confirmation returned after a vote is recorded.</summary>
    public sealed record CastVoteResponse(
        Guid VoteId,
        Guid PlayerId,
        string Option,
        string Topic,
        DateTimeOffset TimestampUtc
    );

    /// <summary>Aggregated vote tally for a single option within a topic.</summary>
    public sealed record VoteOptionResult(
        string Option,
        int Count,
        double Percentage
    );

    /// <summary>Full results for a topic, ordered by vote count descending.</summary>
    public sealed record VoteResultsResponse(
        string Topic,
        int TotalVotes,
        IReadOnlyList<VoteOptionResult> Results
    );
}
