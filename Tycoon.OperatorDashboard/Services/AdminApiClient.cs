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
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
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

    // ── Questions ──────────────────────────────────────────────────────────

    public async Task<JsonDocument?> ListQuestionsAsync(string? q, string? category, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var qs = $"page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(q)) qs += $"&q={Uri.EscapeDataString(q)}";
        if (!string.IsNullOrWhiteSpace(category)) qs += $"&category={Uri.EscapeDataString(category)}";
        var resp = await http.GetAsync($"/admin/questions?{qs}", ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
    }

    public async Task<bool> CreateQuestionAsync(object body, CancellationToken ct = default)
    {
        var resp = await http.PostAsync("/admin/questions", JsonContent.Create(body), ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateQuestionAsync(Guid id, object body, CancellationToken ct = default)
    {
        var resp = await http.PatchAsync($"/admin/questions/{id}", JsonContent.Create(body), ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteQuestionAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await http.DeleteAsync($"/admin/questions/{id}", ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<JsonDocument?> BulkImportQuestionsAsync(object body, CancellationToken ct = default)
    {
        var resp = await http.PostAsync("/admin/questions/bulk", JsonContent.Create(body), ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
    }

    public async Task<JsonDocument?> BulkDeleteQuestionsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var body = JsonContent.Create(new { ids });
        var resp = await http.PostAsync("/admin/questions/bulk-delete", body, ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
    }

    // ── Notifications ──────────────────────────────────────────────────────

    public async Task<JsonDocument?> ListChannelsAsync(CancellationToken ct = default)
    {
        var resp = await http.GetAsync("/admin/notifications/channels", ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
    }

    public async Task<bool> SendNotificationAsync(object body, CancellationToken ct = default)
    {
        var resp = await http.PostAsync("/admin/notifications/send", JsonContent.Create(body), ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> ScheduleNotificationAsync(object body, CancellationToken ct = default)
    {
        var resp = await http.PostAsync("/admin/notifications/schedule", JsonContent.Create(body), ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<JsonDocument?> ListScheduledAsync(int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var resp = await http.GetAsync($"/admin/notifications/scheduled?page={page}&pageSize={pageSize}", ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
    }

    public async Task<bool> CancelScheduledAsync(Guid scheduleId, CancellationToken ct = default)
    {
        var resp = await http.DeleteAsync($"/admin/notifications/scheduled/{scheduleId}", ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<JsonDocument?> GetDeadLetterAsync(int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var resp = await http.GetAsync($"/admin/notifications/dead-letter?page={page}&pageSize={pageSize}", ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
    }

    public async Task<bool> ReplayDeadLetterAsync(Guid scheduleId, CancellationToken ct = default)
    {
        var resp = await http.PostAsync($"/admin/notifications/dead-letter/{scheduleId}/replay", null, ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<JsonDocument?> ListTemplatesAsync(CancellationToken ct = default)
    {
        var resp = await http.GetAsync("/admin/notifications/templates", ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
    }

    public async Task<bool> CreateTemplateAsync(object body, CancellationToken ct = default)
    {
        var resp = await http.PostAsync("/admin/notifications/templates", JsonContent.Create(body), ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteTemplateAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await http.DeleteAsync($"/admin/notifications/templates/{id}", ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<JsonDocument?> GetNotificationHistoryAsync(string? channelKey, string? status, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var qs = $"page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(channelKey)) qs += $"&channelKey={channelKey}";
        if (!string.IsNullOrWhiteSpace(status)) qs += $"&status={status}";
        var resp = await http.GetAsync($"/admin/notifications/history?{qs}", ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
    }

    // ── Anti-Cheat ─────────────────────────────────────────────────────────

    public async Task<JsonDocument?> ListAnticheatFlagsAsync(bool unreviewedOnly = true, string? severity = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var qs = $"page={page}&pageSize={pageSize}&unreviewedOnly={unreviewedOnly}";
        if (!string.IsNullOrWhiteSpace(severity)) qs += $"&severity={severity}";
        var resp = await http.GetAsync($"/admin/anti-cheat/flags?{qs}", ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
    }

    public async Task<bool> ReviewAnticheatFlagAsync(Guid id, string reviewedBy, string note, CancellationToken ct = default)
    {
        var body = JsonContent.Create(new { reviewedBy, note });
        var resp = await http.PutAsync($"/admin/anti-cheat/flags/{id}/review", body, ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<JsonDocument?> GetAnticheatSummaryAsync(int windowHours = 24, CancellationToken ct = default)
    {
        var resp = await http.GetAsync($"/admin/anti-cheat/analytics/summary?windowHours={windowHours}", ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
    }

    public async Task<JsonDocument?> ListPartyFlagsAsync(bool unreviewedOnly = true, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var resp = await http.GetAsync($"/admin/anti-cheat/party/flags?page={page}&pageSize={pageSize}&unreviewedOnly={unreviewedOnly}", ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
    }

    public async Task<bool> ReviewPartyFlagAsync(Guid id, string reviewedBy, string note, CancellationToken ct = default)
    {
        var body = JsonContent.Create(new { reviewedBy, note });
        var resp = await http.PutAsync($"/admin/anti-cheat/party/flags/{id}/review", body, ct);
        return resp.IsSuccessStatusCode;
    }

    // ── Moderation (enhanced) ──────────────────────────────────────────────

    public async Task<JsonDocument?> GetModerationProfileAsync(Guid playerId, CancellationToken ct = default)
    {
        var resp = await http.GetAsync($"/admin/moderation/profile/{playerId}", ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
    }

    public async Task<bool> SetModerationStatusAsync(object body, CancellationToken ct = default)
    {
        var resp = await http.PostAsync("/admin/moderation/set-status", JsonContent.Create(body), ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<JsonDocument?> GetModerationLogsAsync(Guid? playerId, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var qs = $"page={page}&pageSize={pageSize}";
        if (playerId.HasValue) qs += $"&playerId={playerId.Value}";
        var resp = await http.GetAsync($"/admin/moderation/logs?{qs}", ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
    }

    // ── Economy (enhanced) ─────────────────────────────────────────────────

    public async Task<JsonDocument?> GetPlayerEconomyHistoryAsync(Guid playerId, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var resp = await http.GetAsync($"/admin/economy/history/{playerId}?page={page}&pageSize={pageSize}", ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
    }

    public async Task<bool> CreateTransactionAsync(object body, CancellationToken ct = default)
    {
        var resp = await http.PostAsync("/admin/economy/transactions", JsonContent.Create(body), ct);
        return resp.IsSuccessStatusCode;
    }

    // ── Security Audit ─────────────────────────────────────────────────────

    public async Task<JsonDocument?> GetSecurityAuditAsync(DateTimeOffset? from, DateTimeOffset? to, string? status, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var qs = $"page={page}&pageSize={pageSize}";
        if (from.HasValue) qs += $"&from={Uri.EscapeDataString(from.Value.ToString("o"))}";
        if (to.HasValue) qs += $"&to={Uri.EscapeDataString(to.Value.ToString("o"))}";
        if (!string.IsNullOrWhiteSpace(status)) qs += $"&status={status}";
        var resp = await http.GetAsync($"/admin/audit/security?{qs}", ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
    }

    // ── Matches ────────────────────────────────────────────────────────────

    public async Task<JsonDocument?> ListMatchesAsync(int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var resp = await http.GetAsync($"/admin/matches?page={page}&pageSize={pageSize}", ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
    }

    // ── Season Rewards ─────────────────────────────────────────────────────

    public async Task<JsonDocument?> GetRewardClaimsAsync(Guid? seasonId, Guid? playerId, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var qs = $"page={page}&pageSize={pageSize}";
        if (seasonId.HasValue) qs += $"&seasonId={seasonId.Value}";
        if (playerId.HasValue) qs += $"&playerId={playerId.Value}";
        var resp = await http.GetAsync($"/admin/seasons/rewards/claims?{qs}", ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
    }

    public async Task<bool> ForceRecomputeAsync(Guid seasonId, CancellationToken ct = default)
    {
        var resp = await http.PostAsync($"/admin/seasons/rewards/recompute/{seasonId}", null, ct);
        return resp.IsSuccessStatusCode;
    }

    // ── Media ──────────────────────────────────────────────────────────────

    public async Task<JsonDocument?> GetMediaIntentAsync(CancellationToken ct = default)
    {
        var resp = await http.GetAsync("/admin/media/intent", ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
    }

    public async Task<JsonDocument?> UploadMediaAsync(string assetKey, Stream content, string contentType, CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();
        var streamContent = new StreamContent(content);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(streamContent, "file", assetKey);
        var resp = await http.PostAsync($"/admin/media/upload/{Uri.EscapeDataString(assetKey)}", form, ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
    }

    // ── Powerups ───────────────────────────────────────────────────────────

    public async Task<JsonDocument?> GetPlayerPowerupsAsync(Guid playerId, CancellationToken ct = default)
    {
        var resp = await http.GetAsync($"/admin/players/{playerId}/powerups", ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
    }

    public async Task<bool> GrantPowerupAsync(Guid playerId, object body, CancellationToken ct = default)
    {
        var resp = await http.PostAsync($"/admin/players/{playerId}/powerups", JsonContent.Create(body), ct);
        return resp.IsSuccessStatusCode;
    }

    // ── Skills ─────────────────────────────────────────────────────────────

    public async Task<JsonDocument?> SeedSkillsAsync(object body, CancellationToken ct = default)
    {
        var resp = await http.PostAsync("/admin/skills/seed", JsonContent.Create(body), ct);
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

    public void ClearToken()
    {
        http.DefaultRequestHeaders.Authorization = null;
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
