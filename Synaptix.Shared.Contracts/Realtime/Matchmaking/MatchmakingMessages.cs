namespace Synaptix.Shared.Contracts.Realtime.Matchmaking
{
    public sealed record MatchmakingQueuedMessage(
        Guid TicketId,
        string Mode,
        int Tier,
        DateTimeOffset QueuedAtUtc);

    public sealed record MatchmakingMatchedMessage(
        Guid TicketId,
        Guid OpponentId,
        string Mode,
        int Tier,
        DateTimeOffset MatchedAtUtc);

    public sealed record MatchmakingCancelledMessage(
        Guid TicketId,
        string Reason,
        DateTimeOffset CancelledAtUtc);
}
