namespace Tycoon.Shared.Contracts.Dtos
{
    public sealed record PartyEnqueueRequest(
        Guid LeaderPlayerId,
        string Mode,
        int Tier
    );

    public sealed record PartyQueueResultDto(
        string Status,          // Queued | Matched | Forbidden | PartyNotReady
        Guid? TicketId,
        Guid PartyId,
        Guid? OpponentPartyId,
        Guid? MatchId // Only set when Status == "Matched"
    );
}
