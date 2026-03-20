using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Tycoon.OperatorDashboard.Services;

public sealed class AdminAuthService(
    AdminApiClient api,
    TokenStore tokens,
    IHttpContextAccessor httpCtx)
{
    public async Task<bool> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var result = await api.LoginAsync(email, password, ct);
        if (result is null) return false;

        var userId = email; // use email as stable key; swap for JWT sub if available
        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(result.ExpiresIn);
        tokens.Set(userId, result.AccessToken, result.RefreshToken, expiresAt);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, email),
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, "admin")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await httpCtx.HttpContext!.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = expiresAt });

        return true;
    }

    public async Task LogoutAsync()
    {
        var userId = httpCtx.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is not null) tokens.Remove(userId);
        api.ClearToken();
        await httpCtx.HttpContext!.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Attaches the stored token to the AdminApiClient for the current user.
    /// Proactively refreshes the access token if it expires within 5 minutes.
    /// Call this at the top of every Blazor component that makes API calls.
    /// </summary>
    public async Task<bool> TryAttachTokenAsync(CancellationToken ct = default)
    {
        var userId = httpCtx.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return false;

        var entry = tokens.Get(userId);
        if (entry is null) return false;

        // Proactively refresh when the access token expires within 5 minutes.
        if (entry.ExpiresAt <= DateTimeOffset.UtcNow.AddMinutes(5))
        {
            var refreshed = await api.RefreshAsync(entry.RefreshToken, ct);
            if (refreshed is null)
            {
                tokens.Remove(userId);
                return false;
            }
            var newExpiry = DateTimeOffset.UtcNow.AddSeconds(refreshed.ExpiresIn);
            tokens.Set(userId, refreshed.AccessToken, entry.RefreshToken, newExpiry);
            entry = new TokenStore.TokenEntry(refreshed.AccessToken, entry.RefreshToken, newExpiry);
        }

        api.SetToken(entry.AccessToken);
        return true;
    }
}
