using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Features.AdminAnalytics
{
    // Resolves client IPs (captured on admin audit events) to map coordinates.
    // Behind an interface so a MaxMind/GeoLite2 implementation can replace the
    // ip-api.com one later without touching the endpoint or the React client.
    public interface IGeoIpResolver
    {
        Task<IReadOnlyList<GeoLocationDto>> ResolveAsync(IEnumerable<string> ips, CancellationToken ct);
    }

    // ip-api.com implementation: batch endpoint + in-memory cache. Public IPs
    // only (private/loopback/link-local are skipped). Best-effort — network
    // failures yield an empty result rather than throwing.
    public sealed class IpApiGeoIpResolver : IGeoIpResolver
    {
        private const int BatchLimit = 100; // ip-api.com batch cap
        private static readonly string[] Fields = { "status", "country", "countryCode", "city", "lat", "lon", "isp", "proxy", "query" };

        private readonly HttpClient _http;
        private readonly IMemoryCache _cache;
        private readonly ILogger<IpApiGeoIpResolver> _logger;
        private readonly string _baseUrl;
        private readonly TimeSpan _ttl;

        public IpApiGeoIpResolver(HttpClient http, IMemoryCache cache, IConfiguration cfg, ILogger<IpApiGeoIpResolver> logger)
        {
            _http = http;
            _cache = cache;
            _logger = logger;
            _baseUrl = (cfg["GeoIp:IpApiBaseUrl"] ?? "http://ip-api.com").TrimEnd('/');
            _ttl = TimeSpan.FromHours(cfg.GetValue("GeoIp:CacheHours", 24));
            if (_http.Timeout > TimeSpan.FromSeconds(6))
                _http.Timeout = TimeSpan.FromSeconds(6);
        }

        public async Task<IReadOnlyList<GeoLocationDto>> ResolveAsync(IEnumerable<string> ips, CancellationToken ct)
        {
            var distinct = (ips ?? Enumerable.Empty<string>())
                .Select(ip => ip?.Trim() ?? string.Empty)
                .Where(IsPublicIp)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (distinct.Count == 0)
                return Array.Empty<GeoLocationDto>();

            var results = new List<GeoLocationDto>(distinct.Count);
            var toFetch = new List<string>();
            foreach (var ip in distinct)
            {
                if (_cache.TryGetValue(CacheKey(ip), out GeoLocationDto? cached) && cached is not null)
                    results.Add(cached);
                else
                    toFetch.Add(ip);
            }

            foreach (var chunk in Chunk(toFetch, BatchLimit))
            {
                foreach (var dto in await FetchBatchAsync(chunk, ct))
                {
                    _cache.Set(CacheKey(dto.Ip), dto, _ttl);
                    results.Add(dto);
                }
            }

            return results;
        }

        private async Task<IReadOnlyList<GeoLocationDto>> FetchBatchAsync(IReadOnlyList<string> ips, CancellationToken ct)
        {
            try
            {
                var url = $"{_baseUrl}/batch?fields={string.Join(",", Fields)}";
                using var resp = await _http.PostAsJsonAsync(url, ips, ct);
                if (!resp.IsSuccessStatusCode)
                    return Array.Empty<GeoLocationDto>();

                await using var stream = await resp.Content.ReadAsStreamAsync(ct);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                    return Array.Empty<GeoLocationDto>();

                var list = new List<GeoLocationDto>(ips.Count);
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    var ip = Str(el, "query") ?? string.Empty;
                    if (string.IsNullOrEmpty(ip)) continue;
                    var ok = Str(el, "status") == "success";
                    list.Add(new GeoLocationDto(
                        ip,
                        ok ? Str(el, "country") : null,
                        ok ? Str(el, "countryCode") : null,
                        ok ? Str(el, "city") : null,
                        ok ? Num(el, "lat") : null,
                        ok ? Num(el, "lon") : null,
                        ok ? Str(el, "isp") : null,
                        ok && Bool(el, "proxy")));
                }
                return list;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "ip-api geo lookup failed for {Count} IPs; returning none for this batch.", ips.Count);
                return Array.Empty<GeoLocationDto>();
            }
        }

        private static string CacheKey(string ip) => $"geoip:{ip}";

        private static bool IsPublicIp(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip) || ip is "unknown" or "-")
                return false;
            if (!IPAddress.TryParse(ip, out var addr))
                return false;
            if (IPAddress.IsLoopback(addr))
                return false;

            if (addr.AddressFamily == AddressFamily.InterNetwork)
            {
                var b = addr.GetAddressBytes();
                if (b[0] == 10) return false;                          // 10.0.0.0/8
                if (b[0] == 172 && b[1] >= 16 && b[1] <= 31) return false; // 172.16.0.0/12
                if (b[0] == 192 && b[1] == 168) return false;          // 192.168.0.0/16
                if (b[0] == 169 && b[1] == 254) return false;          // link-local
                if (b[0] == 127) return false;
                return true;
            }

            // IPv6: skip link-local (fe80::/10) and unique-local (fc00::/7).
            if (addr.IsIPv6LinkLocal) return false;
            var first = addr.GetAddressBytes()[0];
            if ((first & 0xFE) == 0xFC) return false;
            return true;
        }

        private static IEnumerable<IReadOnlyList<string>> Chunk(IReadOnlyList<string> src, int size)
        {
            for (var i = 0; i < src.Count; i += size)
                yield return src.Skip(i).Take(size).ToList();
        }

        private static string? Str(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

        private static double? Num(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var d) ? d : null;

        private static bool Bool(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.True;
    }
}
