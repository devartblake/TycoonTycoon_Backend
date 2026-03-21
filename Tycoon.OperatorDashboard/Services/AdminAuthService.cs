using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;

namespace Tycoon.OperatorDashboard.Services;

public sealed class AdminAuthService(
    AdminApiClient api,
    TokenStore tokens,
    BearerTokenStore bearerTokenStore,
    IHttpContextAccessor httpCtx,
    AuthenticationStateProvider authStateProvider)
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

        // Store per-admin permissions as cookie claims so Blazor components can gate write actions
        foreach (var perm in result.Admin?.Permissions ?? [])
            claims.Add(new Claim("permission", perm));

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
        bearerTokenStore.AccessToken = null;
        await httpCtx.HttpContext!.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Attaches the stored token to the AdminApiClient for the current user.
    /// Proactively refreshes the access token if it expires within 5 minutes.
    /// Uses AuthenticationStateProvider as fallback when IHttpContextAccessor.HttpContext
    /// is null (Blazor Server SignalR circuit context).
    /// </summary>
    public async Task<bool> TryAttachTokenAsync(CancellationToken ct = default)
    {
        // IHttpContextAccessor.HttpContext is null on the Blazor SignalR circuit.
        // Use it when available (Razor Pages / SSR), fall back to AuthenticationStateProvider
        // which is circuit-aware and always has the correct ClaimsPrincipal.
        var httpContextUser = httpCtx.HttpContext?.User;
        ClaimsPrincipal user;

        if (httpContextUser?.Identity?.IsAuthenticated == true)
        {
            user = httpContextUser;
        }
        else
        {
            var authState = await authStateProvider.GetAuthenticationStateAsync();
            user = authState.User;
        }

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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

        bearerTokenStore.AccessToken = entry.AccessToken;
        api.SetToken(entry.AccessToken);
        return true;
    }
}
