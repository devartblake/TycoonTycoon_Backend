namespace Synaptix.Backend.Domain.Entities;

public sealed class SetupReport
{
    private SetupReport() { }

    public SetupReport(
        string status,
        string source,
        int warningCount,
        DateTimeOffset generatedAtUtc,
        string reportJson)
    {
        Id = Guid.NewGuid();
        Status = status;
        Source = source;
        WarningCount = warningCount;
        GeneratedAtUtc = generatedAtUtc;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        ReportJson = reportJson;
    }

    public Guid Id { get; private set; }
    public string Status { get; private set; } = "unknown";
    public string Source { get; private set; } = "live-backend-diagnostics";
    public int WarningCount { get; private set; }
    public DateTimeOffset GeneratedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public string ReportJson { get; private set; } = "{}";
}
