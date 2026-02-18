using System.ComponentModel.DataAnnotations;

namespace Tycoon.Backend.Application.Auth
{
    public class JwtSettings
    {
        [Required(AllowEmptyStrings = false,
            ErrorMessage = "Jwt:Secret is required. Minimum 32 characters for HS256.")]
        [MinLength(32, ErrorMessage = "Jwt:Secret must be at least 32 characters for HS256.")]
        public string Secret { get; set; } = string.Empty;

        public string Issuer { get; set; } = "TycoonBackend";

        public string Audience { get; set; } = "TycoonBackend";

        [Range(1, 1440, ErrorMessage = "Jwt:AccessTokenExpirationMinutes must be between 1 and 1440.")]
        public int AccessTokenExpirationMinutes { get; set; } = 15;

        [Range(1, 365, ErrorMessage = "Jwt:RefreshTokenExpirationDays must be between 1 and 365.")]
        public int RefreshTokenExpirationDays { get; set; } = 30;
    }
}