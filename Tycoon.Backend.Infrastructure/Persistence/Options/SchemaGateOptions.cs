using System.ComponentModel.DataAnnotations;

namespace Tycoon.Backend.Infrastructure.Persistence.Options;

/// <summary>
/// Configuration for schema gating / validation.
///
/// appsettings.json:
/// "SchemaGate": {
///   "Enabled": true,
///   "StartupGateEnabled": true,
///   "HealthCheckEnabled": true,
///   "FailStartupIfInvalid": true,
///   "RequireMigrationsHistoryTable": true,
///   "Schema": "public",
///   "CriticalTables": [ "Tiers", "Missions" ],
///   "MigrationsHistoryTable": "__EFMigrationsHistory",
///   "TimeoutSeconds": 10,
///   "LogOnly": false
/// }
/// </summary>
public sealed class SchemaGateOptions
{
    public const string SectionName = "SchemaGate";

    /// <summary>Master switch.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>If enabled, run the startup gate (hosted) validation.</summary>
    public bool StartupGateEnabled { get; set; } = true;

    /// <summary>If enabled, register schema health check.</summary>
    public bool HealthCheckEnabled { get; set; } = true;

    /// <summary>
    /// If true, startup gate throws and prevents host from starting when schema is invalid.
    /// If false, startup gate logs and allows host to start.
    /// </summary>
    public bool FailStartupIfInvalid { get; set; } = true;

    /// <summary>If true, requires __EFMigrationsHistory to exist.</summary>
    public bool RequireMigrationsHistoryTable { get; set; } = true;

    /// <summary>Postgres schema name used for table checks.</summary>
    [Required]
    public string Schema { get; set; } = "public";

    /// <summary>Tables that must exist for the app to be considered schema-ready.</summary>
    public string[] CriticalTables { get; set; } = new[] { "Tiers", "Missions" };

    /// <summary>Name of the EF migrations history table.</summary>
    public string MigrationsHistoryTable { get; set; } = "__EFMigrationsHistory";

    /// <summary>
    /// If true, always log instead of throwing, regardless of FailStartupIfInvalid.
    /// Useful in dev.
    /// </summary>
    public bool LogOnly { get; set; } = false;

    /// <summary>Timeout for startup gate validation (seconds).</summary>
    [Range(1, 600)]
    public int TimeoutSeconds { get; set; } = 10;
}
