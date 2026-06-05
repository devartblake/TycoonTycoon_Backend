using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Synaptix.Setup.Services;

// Uses the Elasticsearch REST API via HttpClient to avoid version-pinned client dependency.
public sealed class ElasticsearchSetupTask : ISetupTask
{
    public string Name => "Elasticsearch";

    public async Task<SetupResult> RunAsync(IConfiguration cfg, CancellationToken ct = default)
    {
        var enabled = !(bool.TryParse(cfg["Elastic:Enabled"] ?? cfg["ANALYTICS_ENABLED"] ?? "true", out var e) && !e);
        if (!enabled)
            return SetupResult.Skip("Elasticsearch disabled via config (Elastic:Enabled=false).");

        var url      = cfg["Elastic:Url"] ?? cfg.GetConnectionString("elasticsearch") ?? cfg["ConnectionStrings:elasticsearch"] ?? "http://localhost:9200";
        var username = cfg["Elastic:Username"] ?? "elastic";
        var password = cfg["ELASTIC_PASSWORD"] ?? cfg["Elastic:Password"];

        if (string.IsNullOrWhiteSpace(password))
            return SetupResult.Skip("ELASTIC_PASSWORD not set — skipping Elasticsearch validation.");

        using var client = CreateHttpClient(url, username, password);

        try
        {
            var health = await client.GetAsync("_cluster/health", ct);
            if (!health.IsSuccessStatusCode)
                return SetupResult.Fail($"Elasticsearch returned {(int)health.StatusCode}.");

            var body = await health.Content.ReadAsStringAsync(ct);
            Log.Information("Elasticsearch cluster health: {Body}", body[..Math.Min(200, body.Length)]);

            // Ensure configured write targets exist. The MigrationService/Backend
            // bootstrapper creates these idempotently when Elasticsearch is enabled.
            var requiredIndices = new[]
            {
                cfg["Elastic:DailyWriteAlias"] ?? "synaptix-daily-rollups-write",
                cfg["Elastic:PlayerDailyWriteAlias"] ?? "synaptix-player-daily-rollups-write",
            };

            var warnings = new List<string>();
            foreach (var index in requiredIndices)
            {
                var headResp = await client.SendAsync(
                    new HttpRequestMessage(HttpMethod.Head, index), ct);
                if (!headResp.IsSuccessStatusCode)
                    warnings.Add($"Index '{index}' not found — will be created by MigrationService on first run.");
            }

            return SetupResult.Ok("Elasticsearch connection validated.", warnings);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Elasticsearch validation failed (non-fatal).");
            return SetupResult.Ok("Elasticsearch unreachable — continuing (non-critical).",
                [$"Elasticsearch unreachable: {ex.Message}"]);
        }
    }

    private static HttpClient CreateHttpClient(string baseUrl, string user, string pass)
    {
        var client = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"), Timeout = TimeSpan.FromSeconds(10) };
        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{user}:{pass}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        return client;
    }
}
