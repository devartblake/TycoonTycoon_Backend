namespace Tycoon.Shared.Contracts.Dtos
{
    public record PlayerPreferencesDto(
        string SynaptixMode,
        string PreferredSurface,
        bool ReducedMotion,
        string TonePreference);

    public record UpdatePlayerPreferencesRequest(
        string? SynaptixMode,
        string? PreferredSurface,
        bool? ReducedMotion,
        string? TonePreference);

    public record PlayerLoadoutDto(
        string? AvatarItemType,
        IReadOnlyList<string> EquippedCosmeticItemTypes);

    public record UpdatePlayerLoadoutRequest(
        string? AvatarItemType,
        IReadOnlyList<string>? EquippedCosmeticItemTypes);
}
