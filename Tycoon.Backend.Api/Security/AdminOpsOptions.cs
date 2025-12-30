namespace Tycoon.Backend.Api.Security
{
    public sealed class AdminOpsOptions
    {
        public string Header { get; set; } = "X-Admin-Ops-Key";
        public string Key { get; set; } = string.Empty;

        // Optional: allowlist CIDRs or exact IPs later
        public string[] AllowedIps { get; set; } = Array.Empty<string>();
    }
}
