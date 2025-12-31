namespace Tycoon.Shared.Contracts.Dtos
{
    public enum PowerupType
    {
        FiftyFifty = 1,
        Skip = 2,
        DoublePoints = 3,
        ExtraTime = 4
    }

    public sealed record PowerupBalanceDto(PowerupType Type, int Quantity, DateTimeOffset? CooldownUntilUtc);

    public sealed record PowerupStateDto(
        Guid PlayerId,
        IReadOnlyList<PowerupBalanceDto> Powerups
    );

    public sealed record GrantPowerupRequest(Guid EventId, Guid PlayerId, PowerupType Type, int Quantity, string Reason);
    public sealed record UsePowerupRequest(Guid EventId, Guid PlayerId, PowerupType Type);

    public sealed record UsePowerupResultDto(
        Guid EventId,
        Guid PlayerId,
        PowerupType Type,
        string Status, // "Used" | "Duplicate" | "Insufficient" | "Cooldown"
        int Remaining,
        DateTimeOffset? CooldownUntilUtc
    );
}
