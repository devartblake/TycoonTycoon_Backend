namespace Tycoon.Shared.Contracts.Dtos
{
    public enum QrScanType
    {
        Unknown = 0,
        Profile = 1,
        Referral = 2,
        Mission = 3,
        Promo = 4
    }

    public sealed record CreateReferralCodeRequest(Guid OwnerPlayerId);

    public sealed record ReferralCodeDto(
        Guid Id,
        string Code,
        Guid OwnerPlayerId,
        DateTimeOffset CreatedAtUtc,
        int TotalRedemptions
    );

    public sealed record RedeemReferralRequest(
        Guid EventId,
        Guid RedeemerPlayerId
    );

    public sealed record RedeemReferralResultDto(
        string Code,
        Guid OwnerPlayerId,
        Guid RedeemerPlayerId,
        int AwardXpToOwner,
        int AwardCoinsToOwner,
        int AwardXpToRedeemer,
        int AwardCoinsToRedeemer,
        string Status, // "Redeemed" | "Duplicate" | "Invalid" | "SelfRedeemNotAllowed"
        DateTimeOffset ProcessedAtUtc
    );

    public sealed record TrackScanRequest(
        Guid? EventId,
        Guid PlayerId,
        string Value,
        DateTimeOffset OccurredAtUtc,
        QrScanType Type
    );

    public sealed record TrackScanResultDto(
        Guid EventId,
        Guid PlayerId,
        string Status, // "Tracked" | "Duplicate"
        DateTimeOffset StoredAtUtc
    );

    public sealed record SyncScansRequest(
        Guid PlayerId,
        IReadOnlyList<TrackScanRequest> Scans
    );

    public sealed record SyncScansResultDto(
        Guid PlayerId,
        int Received,
        int Tracked,
        int Duplicates
    );

    public sealed record ScanHistoryItemDto(
        Guid EventId,
        Guid PlayerId,
        string Value,
        DateTimeOffset OccurredAtUtc,
        QrScanType Type
    );

    public sealed record ScanHistoryDto(
        Guid PlayerId,
        int Page,
        int PageSize,
        int Total,
        IReadOnlyList<ScanHistoryItemDto> Items
    );
}
