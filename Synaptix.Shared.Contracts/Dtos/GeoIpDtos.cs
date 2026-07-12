namespace Synaptix.Shared.Contracts.Dtos
{
    // Resolved location for a single IP (mirrors the Django operator dashboard's
    // geo-lookup fields). Lat/Lon are null when the IP could not be resolved.
    public sealed record GeoLocationDto(
        string Ip,
        string? Country,
        string? CountryCode,
        string? City,
        double? Lat,
        double? Lon,
        string? Isp,
        bool Proxy);

    // Batch geo-lookup request: the operator dashboard sends the distinct client
    // IPs it collected from audit events.
    public sealed record GeoLookupRequest(IReadOnlyList<string> Ips);
}
