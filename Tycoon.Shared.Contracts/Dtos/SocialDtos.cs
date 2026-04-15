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

    // Enriched request DTO — includes sender profile for display in the UI
    public sealed record FriendRequestDetailDto(
        Guid RequestId,
        Guid FromPlayerId,
        string SenderDisplayName,
        string SenderUsername,
        string? SenderAvatarUrl,
        Guid ToPlayerId,
        string Status,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? RespondedAtUtc
    );

    public sealed record FriendDto(
        Guid FriendPlayerId,
        string DisplayName,
        string Username,
        string? AvatarUrl,
        bool IsOnline,
        DateTimeOffset? LastSeenUtc,
        DateTimeOffset SinceUtc
    );

    public sealed record FriendSuggestionDto(
        Guid Id,
        string DisplayName,
        string Username,
        string? AvatarUrl,
        int MutualFriendCount,
        string Reason
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

    public sealed record FriendRequestsDetailListResponseDto(
        int Page,
        int PageSize,
        int Total,
        IReadOnlyList<FriendRequestDetailDto> Items
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
        string Role,
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
