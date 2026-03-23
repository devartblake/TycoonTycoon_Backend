using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.OperatorDashboard.Services;

/// <summary>
/// Typed HttpClient wrapping all admin REST endpoints on tycoon-api.
/// Attach the caller's JWT before each call via SetToken().
/// Accepts IHttpClientFactory so DI can construct this directly (scoped lifetime).
/// </summary>
public sealed class AdminApiClient
{
    private readonly HttpClient http;
    private readonly IConfiguration config;

    public AdminApiClient(IHttpClientFactory httpFactory, IConfiguration config)
    {
        this.http = httpFactory.CreateClient("tycoon-api");
        this.config = config;
    }
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    // ── Auth ──────────────────────────────────────────────────────────────

    public async Task<AdminLoginResponse?> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var body = JsonContent.Create(new { email, password });
        var resp = await http.PostAsync("/admin/auth/login", body, ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<AdminLoginResponse>(Json, ct);
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

    // ── Game Events ────────────────────────────────────────────────────────

    public async Task<JsonDocument?> ListGameEventsAsync(string? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var qs = $"page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(status)) qs += $"&status={status}";
        var resp = await http.GetAsync($"/admin/game-events?{qs}", ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
    }

    public async Task<JsonDocument?> CreateGameEventAsync(object body, CancellationToken ct = default)
    {
        var resp = await http.PostAsync("/admin/game-events/", JsonContent.Create(body), ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
    }

    public async Task<bool> OpenGameEventAsync(Guid eventId, CancellationToken ct = default)
    {
        var resp = await http.PostAsync($"/admin/game-events/{eventId}/open", null, ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> StartGameEventAsync(Guid eventId, CancellationToken ct = default)
    {
        var resp = await http.PostAsync($"/admin/game-events/{eventId}/start", null, ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> CloseGameEventAsync(Guid eventId, CancellationToken ct = default)
    {
        var resp = await http.PostAsync($"/admin/game-events/{eventId}/close", null, ct);
        return resp.IsSuccessStatusCode;
    }

    // ── Game Event Queue (dead-letter upload/reprocess) ────────────────────

    public async Task<JsonDocument?> ListEventQueueAsync(int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var resp = await http.GetAsync($"/admin/event-queue?page={page}&pageSize={pageSize}", ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
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
        // Backend currently exposes /admin/moderation/logs (not /escalations).
        var qs = $"page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(status)) qs += $"&status={Uri.EscapeDataString(status)}";
        var resp = await http.GetAsync($"/admin/moderation/logs?{qs}", ct);
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

    public async Task<GameBalanceConfigDto?> GetGameBalanceConfigAsync(CancellationToken ct = default)
    {
        var resp = await http.GetAsync("/admin/economy/balance", ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<GameBalanceConfigDto>(Json, ct);
    }

    public sealed record ApiCallResult<T>(T? Value, HttpStatusCode StatusCode, string? ErrorCode, string? ErrorMessage)
    {
        public bool Success => Value is not null;
    }

    public async Task<ApiCallResult<GameBalanceConfigDto>> GetGameBalanceConfigDetailedAsync(CancellationToken ct = default)
    {
        var resp = await http.GetAsync("/admin/economy/balance", ct);
        if (resp.IsSuccessStatusCode)
        {
            var cfg = await resp.Content.ReadFromJsonAsync<GameBalanceConfigDto>(Json, ct);
            return new ApiCallResult<GameBalanceConfigDto>(cfg, resp.StatusCode, null, null);
        }

        try
        {
            using var json = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var error = json.RootElement.GetProperty("error");
            var code = error.TryGetProperty("code", out var c) ? c.GetString() : null;
            var message = error.TryGetProperty("message", out var m) ? m.GetString() : null;
            return new ApiCallResult<GameBalanceConfigDto>(null, resp.StatusCode, code, message);
        }
        catch
        {
            return new ApiCallResult<GameBalanceConfigDto>(null, resp.StatusCode, null, null);
        }
    }

    public async Task<GameBalanceConfigDto?> PatchGameBalanceConfigAsync(UpdateGameBalanceConfigRequest req, CancellationToken ct = default)
    {
        var resp = await http.PatchAsync("/admin/economy/balance", JsonContent.Create(req), ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<GameBalanceConfigDto>(Json, ct);
    }

    public sealed record BalancePatchResult(GameBalanceConfigDto? Config, IReadOnlyList<string> Errors, string? Message)
    {
        public bool Success => Config is not null;
    }

    public async Task<BalancePatchResult> PatchGameBalanceConfigDetailedAsync(UpdateGameBalanceConfigRequest req, CancellationToken ct = default)
    {
        var resp = await http.PatchAsync("/admin/economy/balance", JsonContent.Create(req), ct);
        if (resp.IsSuccessStatusCode)
        {
            var cfg = await resp.Content.ReadFromJsonAsync<GameBalanceConfigDto>(Json, ct);
            return new BalancePatchResult(cfg, [], null);
        }

        try
        {
            using var json = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var error = json.RootElement.GetProperty("error");
            var msg = error.TryGetProperty("message", out var m) ? m.GetString() : null;
            var errors = new List<string>();
            if (error.TryGetProperty("details", out var details)
                && details.TryGetProperty("errors", out var detailErrors)
                && detailErrors.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in detailErrors.EnumerateArray())
                {
                    var s = entry.GetString();
                    if (!string.IsNullOrWhiteSpace(s)) errors.Add(s);
                }
            }
            if (errors.Count == 0 && !string.IsNullOrWhiteSpace(msg))
                errors.Add(msg);
            return new BalancePatchResult(null, errors, msg);
        }
        catch
        {
            return new BalancePatchResult(null, [], null);
        }
    }

    public async Task<EconomySimulationResponse?> SimulateEconomyAsync(EconomySimulationRequest req, CancellationToken ct = default)
    {
        var resp = await http.PostAsync("/admin/economy/simulate", JsonContent.Create(req), ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<EconomySimulationResponse>(Json, ct);
    }

    public sealed record SidecarRebalanceAuditItem(
        string? AuditId,
        string? Status,
        string? ApprovedBy,
        string? Reason,
        DateTimeOffset? CreatedAtUtc,
        int? BackendStatus);

    public sealed record SidecarRebalanceRecommendation(
        int? MaxEnergy,
        int? RegenMinutesPerEnergy,
        int? DailyFreeEnergy,
        IReadOnlyList<ModeBalanceRuleDto> Modes);

    public sealed record SidecarRebalanceMetrics(
        int TotalApplyAttempts,
        int BlockedCount,
        int SuccessCount,
        int ErrorCount,
        DateTimeOffset? LastAttemptAtUtc,
        DateTimeOffset? LastSuccessAtUtc,
        DateTimeOffset? LastErrorAtUtc);

    public sealed record SidecarRebalanceAlert(string Severity, string Code, string Message);
    public sealed record SidecarRebalanceMetricsSnapshot(
        DateTimeOffset? CapturedAtUtc,
        int TotalApplyAttempts,
        int BlockedCount,
        int SuccessCount,
        int ErrorCount);
    public sealed record SidecarRolloutValidationCheck(
        string Name,
        bool Ok,
        string? Detail,
        string? CapturedAtUtc,
        string? GeneratedAtUtc,
        int? LocalAlertCount,
        int? SinkAlertCount);
    public sealed record SidecarRolloutValidationReport(
        bool Passed,
        DateTimeOffset? GeneratedAtUtc,
        string? Runbook,
        IReadOnlyList<SidecarRolloutValidationCheck> Checks);

    private string? BuildSidecarUrl(string relativePathAndQuery)
    {
        var sidecarBaseUrl = config["Sidecar:BaseUrl"] ?? config["SidecarBaseUrl"];
        if (string.IsNullOrWhiteSpace(sidecarBaseUrl)) return null;
        return $"{sidecarBaseUrl.TrimEnd('/')}{relativePathAndQuery}";
    }

    public async Task<IReadOnlyList<SidecarRebalanceAuditItem>?> GetSidecarRebalanceAuditHistoryAsync(int limit = 25, CancellationToken ct = default)
    {
        var url = BuildSidecarUrl($"/utilities/economy/rebalance/audit?limit={Math.Clamp(limit, 1, 200)}");
        if (url is null) return null;
        var resp = await http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode) return null;

        try
        {
            using var json = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            if (!json.RootElement.TryGetProperty("items", out var itemsEl) || itemsEl.ValueKind != JsonValueKind.Array)
                return [];

            var items = new List<SidecarRebalanceAuditItem>();
            foreach (var item in itemsEl.EnumerateArray())
            {
                var createdAt = item.TryGetProperty("createdAtUtc", out var c) && c.ValueKind == JsonValueKind.String
                    && DateTimeOffset.TryParse(c.GetString(), out var parsed)
                    ? parsed
                    : (DateTimeOffset?)null;
                var backendStatus = item.TryGetProperty("backendStatus", out var bs) && bs.ValueKind == JsonValueKind.Number
                    ? bs.GetInt32()
                    : (int?)null;

                items.Add(new SidecarRebalanceAuditItem(
                    AuditId: item.TryGetProperty("auditId", out var aid) ? aid.GetString() : null,
                    Status: item.TryGetProperty("status", out var s) ? s.GetString() : null,
                    ApprovedBy: item.TryGetProperty("approvedBy", out var ab) ? ab.GetString() : null,
                    Reason: item.TryGetProperty("reason", out var r) ? r.GetString() : null,
                    CreatedAtUtc: createdAt,
                    BackendStatus: backendStatus));
            }

            return items;
        }
        catch
        {
            return null;
        }
    }

    public async Task<SidecarRebalanceRecommendation?> GetSidecarRebalanceRecommendationAsync(CancellationToken ct = default)
    {
        var url = BuildSidecarUrl("/utilities/economy/rebalance/recommend");
        if (url is null) return null;
        var resp = await http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode) return null;

        try
        {
            using var json = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            if (!json.RootElement.TryGetProperty("recommendation", out var rec) || rec.ValueKind != JsonValueKind.Object)
                return null;

            var modes = new List<ModeBalanceRuleDto>();
            if (rec.TryGetProperty("modes", out var modesEl) && modesEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var m in modesEl.EnumerateArray())
                {
                    var mode = m.TryGetProperty("mode", out var n) ? n.GetString() ?? string.Empty : string.Empty;
                    var energyCost = m.TryGetProperty("energyCost", out var ec) && ec.ValueKind == JsonValueKind.Number ? ec.GetInt32() : 0;
                    int? lives = m.TryGetProperty("lives", out var lv) && lv.ValueKind == JsonValueKind.Number ? lv.GetInt32() : null;
                    var requiresTicket = m.TryGetProperty("requiresTicket", out var rt) && rt.ValueKind == JsonValueKind.True;
                    var tierPointsWeight = m.TryGetProperty("tierPointsWeight", out var tp) && tp.ValueKind == JsonValueKind.Number ? tp.GetInt32() : 0;
                    modes.Add(new ModeBalanceRuleDto(mode, energyCost, lives, requiresTicket, tierPointsWeight));
                }
            }

            return new SidecarRebalanceRecommendation(
                MaxEnergy: rec.TryGetProperty("maxEnergy", out var max) && max.ValueKind == JsonValueKind.Number ? max.GetInt32() : null,
                RegenMinutesPerEnergy: rec.TryGetProperty("regenMinutesPerEnergy", out var regen) && regen.ValueKind == JsonValueKind.Number ? regen.GetInt32() : null,
                DailyFreeEnergy: rec.TryGetProperty("dailyFreeEnergy", out var daily) && daily.ValueKind == JsonValueKind.Number ? daily.GetInt32() : null,
                Modes: modes);
        }
        catch
        {
            return null;
        }
    }

    public async Task<SidecarRebalanceMetrics?> GetSidecarRebalanceMetricsAsync(CancellationToken ct = default)
    {
        var url = BuildSidecarUrl("/utilities/economy/rebalance/metrics");
        if (url is null) return null;
        var resp = await http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode) return null;

        try
        {
            using var json = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            if (!json.RootElement.TryGetProperty("metrics", out var metrics) || metrics.ValueKind != JsonValueKind.Object)
                return null;

            DateTimeOffset? ParseDate(string key)
            {
                if (!metrics.TryGetProperty(key, out var value) || value.ValueKind != JsonValueKind.String)
                    return null;
                return DateTimeOffset.TryParse(value.GetString(), out var parsed) ? parsed : null;
            }

            return new SidecarRebalanceMetrics(
                TotalApplyAttempts: metrics.TryGetProperty("totalApplyAttempts", out var t) && t.ValueKind == JsonValueKind.Number ? t.GetInt32() : 0,
                BlockedCount: metrics.TryGetProperty("blockedCount", out var b) && b.ValueKind == JsonValueKind.Number ? b.GetInt32() : 0,
                SuccessCount: metrics.TryGetProperty("successCount", out var s) && s.ValueKind == JsonValueKind.Number ? s.GetInt32() : 0,
                ErrorCount: metrics.TryGetProperty("errorCount", out var e) && e.ValueKind == JsonValueKind.Number ? e.GetInt32() : 0,
                LastAttemptAtUtc: ParseDate("lastAttemptAtUtc"),
                LastSuccessAtUtc: ParseDate("lastSuccessAtUtc"),
                LastErrorAtUtc: ParseDate("lastErrorAtUtc"));
        }
        catch
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<SidecarRebalanceAlert>?> GetSidecarRebalanceAlertsAsync(CancellationToken ct = default)
    {
        var url = BuildSidecarUrl("/utilities/economy/rebalance/alerts");
        if (url is null) return null;
        var resp = await http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode) return null;

        try
        {
            using var json = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            if (!json.RootElement.TryGetProperty("alerts", out var alertsEl) || alertsEl.ValueKind != JsonValueKind.Array)
                return [];

            var alerts = new List<SidecarRebalanceAlert>();
            foreach (var a in alertsEl.EnumerateArray())
            {
                alerts.Add(new SidecarRebalanceAlert(
                    Severity: a.TryGetProperty("severity", out var s) ? s.GetString() ?? "info" : "info",
                    Code: a.TryGetProperty("code", out var c) ? c.GetString() ?? "UNKNOWN" : "UNKNOWN",
                    Message: a.TryGetProperty("message", out var m) ? m.GetString() ?? string.Empty : string.Empty));
            }
            return alerts;
        }
        catch
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<SidecarRebalanceMetricsSnapshot>?> GetSidecarRebalanceMetricsHistoryAsync(int limit = 50, CancellationToken ct = default)
    {
        var url = BuildSidecarUrl($"/utilities/economy/rebalance/metrics/history?limit={Math.Clamp(limit, 1, 500)}");
        if (url is null) return null;
        var resp = await http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode) return null;

        try
        {
            using var json = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            if (!json.RootElement.TryGetProperty("items", out var itemsEl) || itemsEl.ValueKind != JsonValueKind.Array)
                return [];

            var items = new List<SidecarRebalanceMetricsSnapshot>();
            foreach (var i in itemsEl.EnumerateArray())
            {
                DateTimeOffset? captured = null;
                if (i.TryGetProperty("capturedAtUtc", out var cap) && cap.ValueKind == JsonValueKind.String
                    && DateTimeOffset.TryParse(cap.GetString(), out var parsed))
                    captured = parsed;

                items.Add(new SidecarRebalanceMetricsSnapshot(
                    CapturedAtUtc: captured,
                    TotalApplyAttempts: i.TryGetProperty("totalApplyAttempts", out var t) && t.ValueKind == JsonValueKind.Number ? t.GetInt32() : 0,
                    BlockedCount: i.TryGetProperty("blockedCount", out var b) && b.ValueKind == JsonValueKind.Number ? b.GetInt32() : 0,
                    SuccessCount: i.TryGetProperty("successCount", out var s) && s.ValueKind == JsonValueKind.Number ? s.GetInt32() : 0,
                    ErrorCount: i.TryGetProperty("errorCount", out var e) && e.ValueKind == JsonValueKind.Number ? e.GetInt32() : 0));
            }

            return items;
        }
        catch
        {
            return null;
        }
    }

    public async Task<SidecarRolloutValidationReport?> GetSidecarRolloutValidationReportAsync(CancellationToken ct = default)
    {
        var url = BuildSidecarUrl("/utilities/economy/rebalance/rollout-validation-report");
        if (url is null) return null;

        var resp = await http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode) return null;

        try
        {
            using var json = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var root = json.RootElement;
            if (!root.TryGetProperty("checks", out var checksEl) || checksEl.ValueKind != JsonValueKind.Array)
                return null;

            DateTimeOffset? generatedAt = null;
            if (root.TryGetProperty("generatedAtUtc", out var generatedEl)
                && generatedEl.ValueKind == JsonValueKind.String
                && DateTimeOffset.TryParse(generatedEl.GetString(), out var parsedGenerated))
            {
                generatedAt = parsedGenerated;
            }

            var checks = new List<SidecarRolloutValidationCheck>();
            foreach (var c in checksEl.EnumerateArray())
            {
                checks.Add(new SidecarRolloutValidationCheck(
                    Name: c.TryGetProperty("name", out var name) ? name.GetString() ?? "unknown" : "unknown",
                    Ok: c.TryGetProperty("ok", out var ok) && ok.ValueKind == JsonValueKind.True,
                    Detail: c.TryGetProperty("detail", out var detail) ? detail.GetString() : null,
                    CapturedAtUtc: c.TryGetProperty("capturedAtUtc", out var captured) ? captured.GetString() : null,
                    GeneratedAtUtc: c.TryGetProperty("generatedAtUtc", out var generated) ? generated.GetString() : null,
                    LocalAlertCount: c.TryGetProperty("localAlertCount", out var local) && local.ValueKind == JsonValueKind.Number ? local.GetInt32() : null,
                    SinkAlertCount: c.TryGetProperty("sinkAlertCount", out var sink) && sink.ValueKind == JsonValueKind.Number ? sink.GetInt32() : null));
            }

            return new SidecarRolloutValidationReport(
                Passed: root.TryGetProperty("passed", out var passed) && passed.ValueKind == JsonValueKind.True,
                GeneratedAtUtc: generatedAt,
                Runbook: root.TryGetProperty("runbook", out var runbook) ? runbook.GetString() : null,
                Checks: checks);
        }
        catch
        {
            return null;
        }
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

    public async Task<JsonDocument?> GetMediaIntentAsync(string fileName, string contentType, long sizeBytes, CancellationToken ct = default)
    {
        var body = JsonContent.Create(new { fileName, contentType, sizeBytes });
        var resp = await http.PostAsync("/admin/media/intent", body, ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
    }

    public async Task<JsonDocument?> UploadMediaAsync(string uploadUrl, Stream content, string contentType, string fileName, CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();
        var streamContent = new StreamContent(content);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(streamContent, "file", fileName);
        var resp = await http.PostAsync(uploadUrl, form, ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
    }

    // ── Powerups ───────────────────────────────────────────────────────────

    public async Task<JsonDocument?> GetPlayerPowerupsAsync(Guid playerId, CancellationToken ct = default)
    {
        var resp = await http.GetAsync($"/admin/powerups/state/{playerId}", ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
    }

    public async Task<bool> GrantPowerupAsync(object body, CancellationToken ct = default)
    {
        var resp = await http.PostAsync("/admin/powerups/grant", JsonContent.Create(body), ct);
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
        var headerName = config["AdminOps:Header"] ?? config["ADMIN_OPS_HEADER"] ?? "X-Admin-Ops-Key";
        var opsKey = config["AdminOps:Key"] ?? config["AdminOps__Key"] ?? config["ADMIN_OPS_KEY"] ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(opsKey))
        {
            http.DefaultRequestHeaders.Remove(headerName);
            http.DefaultRequestHeaders.TryAddWithoutValidation(headerName, opsKey);
        }
    }

    public void ClearToken()
    {
        http.DefaultRequestHeaders.Authorization = null;
    }
}

// ── Local response types ──────────────────────────────────────────────────────

public sealed record AdminRefreshResult(
    string AccessToken,
    int ExpiresIn,
    string TokenType);

public sealed record PagedResult<T>(int Page, int PageSize, int Total, List<T> Items);
