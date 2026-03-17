using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.OperatorDashboard.Services;

/// <summary>
/// Typed HttpClient wrapping all admin REST endpoints on tycoon-api.
/// Attach the caller's JWT before each call via SetToken().
/// </summary>
public sealed class AdminApiClient(HttpClient http, IConfiguration config)
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // ── Auth ──────────────────────────────────────────────────────────────

    public async Task<AdminLoginResult?> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var body = JsonContent.Create(new { email, password });
        var resp = await http.PostAsync("/admin/auth/login", body, ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<AdminLoginResult>(Json, ct);
    }

    public async Task<AdminRefreshResult?> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var body = JsonContent.Create(new { refreshToken });
        var resp = await http.PostAsync("/admin/auth/refresh", body, ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<AdminRefreshResult>(Json, ct);
    }

    // ── Config / Feature Flags ─────────────────────────────────────────────

    public async Task<AdminAppConfigDto?> GetConfigAsync(CancellationToken ct = default)
    {
        var resp = await http.GetAsync("/admin/config", ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<AdminAppConfigDto>(Json, ct);
    }

    public async Task<bool> PatchFlagsAsync(Dictionary<string, bool> flags, CancellationToken ct = default)
    {
        var body = JsonContent.Create(new { featureFlags = flags });
        var resp = await http.PatchAsync("/admin/config", body, ct);
        return resp.IsSuccessStatusCode;
    }

    // ── Seasons ────────────────────────────────────────────────────────────

    public async Task<PagedResult<SeasonDto>?> ListSeasonsAsync(int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var resp = await http.GetAsync($"/admin/seasons?page={page}&pageSize={pageSize}", ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<PagedResult<SeasonDto>>(Json, ct);
    }

    public async Task<SeasonDto?> ActivateSeasonAsync(Guid seasonId, CancellationToken ct = default)
    {
        var body = JsonContent.Create(new { seasonId });
        var resp = await http.PostAsync("/admin/seasons/activate", body, ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<SeasonDto>(Json, ct);
    }

    public async Task<bool> CloseSeasonAsync(Guid seasonId, bool carryOverPoints = false, CancellationToken ct = default)
    {
        var body = JsonContent.Create(new { seasonId, carryOverPoints });
        var resp = await http.PostAsync("/admin/seasons/close", body, ct);
        return resp.IsSuccessStatusCode;
    }

    // ── Game Event Queue ───────────────────────────────────────────────────

    public async Task<JsonDocument?> ListEventQueueAsync(int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var resp = await http.GetAsync($"/admin/event-queue?page={page}&pageSize={pageSize}", ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
    }

    public async Task<bool> OpenEventAsync(Guid eventId, CancellationToken ct = default)
    {
        var resp = await http.PostAsync($"/admin/event-queue/{eventId}/open", null, ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> CloseEventAsync(Guid eventId, CancellationToken ct = default)
    {
        var resp = await http.PostAsync($"/admin/event-queue/{eventId}/close", null, ct);
        return resp.IsSuccessStatusCode;
    }

    // ── Users ──────────────────────────────────────────────────────────────

    public async Task<JsonDocument?> ListUsersAsync(string? query, bool? isBanned, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var qs = $"page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(query)) qs += $"&q={Uri.EscapeDataString(query)}";
        if (isBanned.HasValue) qs += $"&isBanned={isBanned.Value}";
        var resp = await http.GetAsync($"/admin/users?{qs}", ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
    }

    public async Task<bool> BanUserAsync(string userId, string reason, CancellationToken ct = default)
    {
        var body = JsonContent.Create(new { reason });
        var resp = await http.PostAsync($"/admin/users/{userId}/ban", body, ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> UnbanUserAsync(string userId, CancellationToken ct = default)
    {
        var resp = await http.PostAsync($"/admin/users/{userId}/unban", null, ct);
        return resp.IsSuccessStatusCode;
    }

    // ── Moderation ─────────────────────────────────────────────────────────

    public async Task<JsonDocument?> ListEscalationsAsync(string? status, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var qs = $"page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(status)) qs += $"&status={status}";
        var resp = await http.GetAsync($"/admin/moderation/escalations?{qs}", ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
    }

    // ── Economy ────────────────────────────────────────────────────────────

    public async Task<JsonDocument?> GetEconomyOverviewAsync(CancellationToken ct = default)
    {
        var resp = await http.GetAsync("/admin/economy", ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
    }

    // ── Token attachment ───────────────────────────────────────────────────

    public void SetToken(string accessToken)
    {
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var opsKey = config["AdminOps:Key"] ?? config["ADMIN_OPS_KEY"] ?? string.Empty;
        if (!string.IsNullOrEmpty(opsKey))
            http.DefaultRequestHeaders.TryAddWithoutValidation("X-Admin-Ops-Key", opsKey);
    }
}

// ── Local response types ──────────────────────────────────────────────────────

public sealed record AdminLoginResult(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType);

public sealed record AdminRefreshResult(
    string AccessToken,
    int ExpiresIn,
    string TokenType);

public sealed record PagedResult<T>(int Page, int PageSize, int Total, List<T> Items);
