using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Synaptix.Backend.Api.Features.AdminDashboard
{
    // Queries the Prometheus HTTP API for instant scalar values that back the
    // operator dashboard's system-metrics tiles. Deliberately best-effort: every
    // failure (unset URL, unreachable, malformed, non-numeric) yields 0 so the
    // dashboard degrades to zeros rather than erroring. Rich, durable metrics
    // still live in Prometheus/Grafana — this only surfaces instant values.
    public interface IPrometheusQueryClient
    {
        bool IsConfigured { get; }

        // Runs an instant PromQL query; returns the first result sample's scalar
        // value, or 0 on any failure.
        Task<double> QueryScalarAsync(string promql, CancellationToken ct);
    }

    public sealed class PrometheusQueryClient : IPrometheusQueryClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<PrometheusQueryClient> _logger;
        private readonly string? _baseUrl;

        public PrometheusQueryClient(HttpClient http, IConfiguration cfg, ILogger<PrometheusQueryClient> logger)
        {
            _http = http;
            _logger = logger;
            _baseUrl = cfg["Dashboard:PrometheusUrl"]?.TrimEnd('/');
            if (_http.Timeout > TimeSpan.FromSeconds(5))
                _http.Timeout = TimeSpan.FromSeconds(5);
        }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_baseUrl);

        public async Task<double> QueryScalarAsync(string promql, CancellationToken ct)
        {
            if (!IsConfigured || string.IsNullOrWhiteSpace(promql))
                return 0d;

            try
            {
                var url = $"{_baseUrl}/api/v1/query?query={Uri.EscapeDataString(promql)}";
                using var resp = await _http.GetAsync(url, ct);
                if (!resp.IsSuccessStatusCode)
                    return 0d;

                await using var stream = await resp.Content.ReadAsStreamAsync(ct);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
                return ParseInstantScalar(doc.RootElement);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Prometheus query failed for '{Query}'; returning 0.", promql);
                return 0d;
            }
        }

        // Shape: { status, data: { resultType, result: [ { value: [ <ts>, "<val>" ] } ] } }
        private static double ParseInstantScalar(JsonElement root)
        {
            if (!root.TryGetProperty("status", out var status) || status.GetString() != "success")
                return 0d;
            if (!root.TryGetProperty("data", out var data) || !data.TryGetProperty("result", out var result))
                return 0d;

            // Vector/matrix: take the first series' value; scalar: value is on data.
            if (result.ValueKind == JsonValueKind.Array && result.GetArrayLength() > 0)
            {
                var first = result[0];
                if (first.TryGetProperty("value", out var v) && v.ValueKind == JsonValueKind.Array && v.GetArrayLength() == 2)
                    return ToDouble(v[1]);
            }
            else if (data.TryGetProperty("resultType", out var rt) && rt.GetString() == "scalar"
                     && result.ValueKind == JsonValueKind.Array && result.GetArrayLength() == 2)
            {
                return ToDouble(result[1]);
            }

            return 0d;
        }

        private static double ToDouble(JsonElement raw)
        {
            var s = raw.ValueKind == JsonValueKind.String ? raw.GetString() : raw.GetRawText();
            return double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d)
                && !double.IsNaN(d) && !double.IsInfinity(d)
                ? d
                : 0d;
        }
    }
}
