using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Tycoon.Backend.Api.Observability;

public static class AdminSecurityMetrics
{
    private static readonly Meter Meter = new("Tycoon.Backend.Api.AdminSecurity", "1.0.0");

    private static readonly Counter<long> AuthEvents = Meter.CreateCounter<long>("admin_auth_events_total");
    private static readonly Counter<long> NotificationEvents = Meter.CreateCounter<long>("admin_notification_events_total");
    private static readonly Counter<long> AuditEvents = Meter.CreateCounter<long>("admin_audit_events_total");
    private static readonly Counter<long> RateLimitRejected = Meter.CreateCounter<long>("admin_rate_limit_rejected_total");
    private static readonly Histogram<double> RequestLatencyMs = Meter.CreateHistogram<double>("admin_request_latency_ms");

    public static void RecordAuth(string action, string outcome, Stopwatch sw)
    {
        AuthEvents.Add(1,
            new KeyValuePair<string, object?>("action", action),
            new KeyValuePair<string, object?>("outcome", outcome));
        RequestLatencyMs.Record(sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("area", "auth"),
            new KeyValuePair<string, object?>("action", action),
            new KeyValuePair<string, object?>("outcome", outcome));
    }

    public static void RecordNotification(string action, string outcome, Stopwatch sw)
    {
        NotificationEvents.Add(1,
            new KeyValuePair<string, object?>("action", action),
            new KeyValuePair<string, object?>("outcome", outcome));
        RequestLatencyMs.Record(sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("area", "notifications"),
            new KeyValuePair<string, object?>("action", action),
            new KeyValuePair<string, object?>("outcome", outcome));
    }

    public static void RecordAuditWrite(string action)
    {
        AuditEvents.Add(1, new KeyValuePair<string, object?>("action", action));
    }

    public static void RecordRateLimitReject(string path)
    {
        RateLimitRejected.Add(1, new KeyValuePair<string, object?>("path", path));
    }
}
