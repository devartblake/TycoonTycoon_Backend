namespace Synaptix.Shared.Contracts.Dtos
{
    public record PlayerDto(Guid Id, string Username, string CountryCode, int Level, double Xp);
    public record CreatePlayerRequest(string Username, string CountryCode);

    public sealed record PlayerCareerStatsDto(
        Guid PlayerId,
        int TotalMatches,
        int Wins,
        int Losses,
        double WinRate,
        int TotalCorrect,
        int TotalWrong,
        double AvgScore,
        double AvgAnswerTimeMs
    );
}
