namespace Tycoon.Shared.Contracts.Dtos
{
    public record PlayerDto(Guid Id, string Username, string CountryCode, int Level, double Xp);
    public record CreatePlayerRequest(string Username, string CountryCode);
}
