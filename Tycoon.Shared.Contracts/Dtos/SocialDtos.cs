namespace Tycoon.Shared.Contracts.Dtos
{
    // --- Friends ---

    public sealed record SendFriendRequestRequest(Guid ToPlayerId);

    public sealed record FriendRequestDto(
        Guid RequestId,
        Guid FromPlayerId,
        Guid ToPlayerId,
        string Status,              // Pending | Accepted | Declined | Cancelled
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? RespondedAtUtc
    );

    public sealed record FriendDto(
        Guid FriendPlayerId,
        DateTimeOffset SinceUtc
    );

    public sealed record FriendsListResponseDto(
        int Page,
        int PageSize,
        int Total,
        IReadOnlyList<FriendDto> Items
    );

    public sealed record FriendRequestsListResponseDto(
        int Page,
        int PageSize,
        int Total,
        IReadOnlyList<FriendRequestDto> Items
    );

    // --- Party ---

    public sealed record CreatePartyRequest(Guid LeaderPlayerId);

    public sealed record PartyInviteRequest(Guid ToPlayerId);

    public sealed record PartyInviteDto(
        Guid InviteId,
        Guid PartyId,
        Guid FromPlayerId,
        Guid ToPlayerId,
        string Status,              // Pending | Accepted | Declined | Cancelled
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? RespondedAtUtc
    );

    public sealed record PartyMemberDto(
        Guid PlayerId,
        DateTimeOffset JoinedAtUtc
    );

    public sealed record PartyRosterDto(
        Guid PartyId,
        Guid LeaderPlayerId,
        string Status,              // Open | Queued | Matched | Closed
        IReadOnlyList<PartyMemberDto> Members
    );

    // Grid-friendly wrapper (future pagination)
    public sealed record PartyInvitesListResponseDto(
        int Page,
        int PageSize,
        int Total,
        IReadOnlyList<PartyInviteDto> Items
    );
}
