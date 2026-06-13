using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Synaptix.Backend.Application.Auth;

namespace Synaptix.Backend.Api.Tests.TestHost
{
    /// <summary>
    /// Mints player access tokens for tests that hit <c>RequireAuthorization</c>
    /// endpoints. Mirrors <c>AuthService.CreateJwtToken</c> exactly — a mobile-app
    /// token whose subject is the <c>sub</c> claim — so tests exercise the same
    /// token shape real clients send (notably: no ClaimTypes.NameIdentifier claim).
    /// </summary>
    public static class TestAuth
    {
        public static string MintPlayerToken(WebApplicationFactory<Program> factory, Guid playerId)
        {
            var settings = factory.Services.GetRequiredService<IOptions<JwtSettings>>().Value;

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, playerId.ToString()),
                new(JwtRegisteredClaimNames.Email, $"{playerId:N}@test.local"),
                new("handle", $"p{playerId:N}"[..12]),
                new("role", "user"),
                new("scope", "profile:read profile:write gameplay:read gameplay:write"),
                new("client_type", "user"),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: settings.Issuer,
                audience: "mobile-app",
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(settings.AccessTokenExpirationMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Sets the client's Bearer header to a freshly-minted token for
        /// <paramref name="playerId"/> and returns the same client for chaining.
        /// </summary>
        public static HttpClient AuthenticateAsPlayer(
            this HttpClient client, WebApplicationFactory<Program> factory, Guid playerId)
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", MintPlayerToken(factory, playerId));
            return client;
        }
    }
}
