namespace Tycoon.Shared.Contracts.Dtos;

public sealed record ReviewAntiCheatFlagRequestDto(
    string ReviewedBy,
    string? Note
);
