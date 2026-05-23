namespace Synaptix.Shared.Contracts.Dtos;

public sealed record ReviewAntiCheatFlagRequestDto(
    string ReviewedBy,
    string? Note
);
