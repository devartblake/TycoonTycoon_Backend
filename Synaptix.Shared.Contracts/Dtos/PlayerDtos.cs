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

    /// <summary>
    /// Complete user profile response for GET /users/me
    /// Combines User, Player, and Wallet data with preferences
    /// Matches Flutter's loadCompleteProfile() requirements
    /// </summary>
    public sealed record CurrentUserProfileDto(
        // Identity
        Guid UserId,
        string Username,
        string Email,

        // Profile
        string? DisplayName,           // Alias for Username
        string? Country,
        string? AvatarUrl,

        // Roles & Permissions
        string? UserRole,              // Primary system role
        IReadOnlyList<string>? UserRoles, // All assigned roles

        // Player Progression
        Guid? PlayerId,
        int PlayerLevel,
        double PlayerXp,
        int PlayerScore,
        string? CurrentTierId,

        // Economy
        int Coins,
        int Diamonds,
        double CumulativeXp,

        // Preferences & Settings
        bool IsPremium,
        string? AgeGroup,
        string? SynaptixMode,
        string? PreferredHomeSurface,
        bool ReducedMotion,
        string? TonePreference,

        // Account State
        bool IsAnonymous,
        DateTimeOffset CreatedAt,
        DateTimeOffset? LastLoginAt
    );

    /// <summary>
    /// Request to migrate guest account to full account
    /// </summary>
    public record AccountMigrationRequest(
        string Email,
        string Password,
        string DeviceId,
        string? Username = null,
        string? Handle = null,
        string? Country = null,
        bool DeleteGuestAccount = true  // Whether to delete the guest account after migration
    );

    /// <summary>
    /// Response confirming guest account migration
    /// </summary>
    public record AccountMigrationResponse(
        bool Success,
        string Message,
        string AccessToken,
        string RefreshToken,
        int ExpiresIn,
        CurrentUserProfileDto Profile,
        bool GuestAccountDeleted
    );
}
