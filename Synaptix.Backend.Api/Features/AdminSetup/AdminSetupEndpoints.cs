using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Caching.Memory;
using Minio;
using Minio.DataModel.Args;
using MongoDB.Bson;
using MongoDB.Driver;
using StackExchange.Redis;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Storage;

namespace Synaptix.Backend.Api.Features.AdminSetup;

public static class AdminSetupEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static readonly string[] RequiredSeedNames =
    [
        "store-items",
        "skill-nodes",
        "season-rewards",
        "questions",
    ];

    public static void Map(RouteGroupBuilder admin)
    {
        var group = admin.MapGroup("/setup").WithTags("Admin/Setup");

        group.MapGet("/status", async (HttpContext http, IServiceProvider services, CancellationToken ct) =>
            HasSetupRead(http)
                ? Results.Ok((await GetSnapshotAsync(services, ct)).Status)
                : Results.Forbid());

        group.MapGet("/readiness", async (HttpContext http, IServiceProvider services, CancellationToken ct) =>
            HasSetupRead(http)
                ? Results.Ok((await GetSnapshotAsync(services, ct)).Readiness)
                : Results.Forbid());

        group.MapGet("/services", async (HttpContext http, IServiceProvider services, CancellationToken ct) =>
            HasSetupRead(http)
                ? Results.Ok(new SetupServicesResponse(DateTimeOffset.UtcNow, "live-backend-diagnostics", (await GetSnapshotAsync(services, ct)).Services))
                : Results.Forbid());

        group.MapGet("/seeds", async (HttpContext http, IServiceProvider services, CancellationToken ct) =>
            HasSetupRead(http)
                ? Results.Ok((await GetSnapshotAsync(services, ct)).Seeds)
                : Results.Forbid());

        group.MapGet("/validation", async (HttpContext http, IServiceProvider services, CancellationToken ct) =>
            HasSetupRead(http)
                ? Results.Ok((await GetSnapshotAsync(services, ct)).Validation)
                : Results.Forbid());

        group.MapGet("/history", async (HttpContext http, IAppDb db, [AsParameters] SetupHistoryQuery query, CancellationToken ct) =>
        {
            if (!HasSetupRead(http))
                return Results.Forbid();

            var limit = Math.Clamp(query.Limit ?? 20, 1, 100);
            try
            {
                var reports = await db.SetupReports
                    .AsNoTracking()
                    .OrderByDescending(x => x.CreatedAtUtc)
                    .Take(limit)
                    .Select(x => new SetupReportSummary(
                        x.Id,
                        x.Status,
                        x.Source,
                        x.WarningCount,
                        x.GeneratedAtUtc,
                        x.CreatedAtUtc))
                    .ToListAsync(ct);

                return Results.Ok(new SetupHistoryResponse(DateTimeOffset.UtcNow, "durable-setup-report-store", reports));
            }
            catch
            {
                return Results.Ok(new SetupHistoryResponse(DateTimeOffset.UtcNow, "durable-setup-report-store-unavailable", []));
            }
        });

        group.MapGet("/history/latest", async (HttpContext http, IAppDb db, CancellationToken ct) =>
        {
            if (!HasSetupRead(http))
                return Results.Forbid();

            try
            {
                var report = await db.SetupReports
                    .AsNoTracking()
                    .OrderByDescending(x => x.CreatedAtUtc)
                    .FirstOrDefaultAsync(ct);

                if (report is null)
                    return Results.NotFound(new { code = "SETUP_REPORT_NOT_FOUND", message = "No durable setup report is available yet." });

                return Results.Ok(new SetupReportDetail(
                    report.Id,
                    report.Status,
                    report.Source,
                    report.WarningCount,
                    report.GeneratedAtUtc,
                    report.CreatedAtUtc,
                    JsonSerializer.Deserialize<JsonElement>(report.ReportJson, JsonOptions)));
            }
            catch
            {
                return Results.Ok(new { status = "unavailable", source = "durable-setup-report-store-unavailable" });
            }
        });
    }

    private static Task<SetupSnapshot> GetSnapshotAsync(IServiceProvider services, CancellationToken ct)
    {
        var cache = services.GetRequiredService<IMemoryCache>();
        return cache.GetOrCreateAsync(
            "admin-setup-live-snapshot",
            entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10);
                return BuildSnapshotAsync(services, ct);
            })!;
    }

    private static bool HasSetupRead(HttpContext http)
    {
        var cfg = http.RequestServices.GetRequiredService<IConfiguration>();
        if (cfg.GetValue("Testing:UseInMemoryDb", false) && http.User.Identity?.IsAuthenticated != true)
            return true;

        return http.User.FindFirst("scope")?.Value?
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains("setup:read", StringComparer.OrdinalIgnoreCase) == true;
    }

    private static async Task<SetupSnapshot> BuildSnapshotAsync(IServiceProvider services, CancellationToken ct)
    {
        var generatedAt = DateTimeOffset.UtcNow;
        var cfg = services.GetRequiredService<IConfiguration>();
        var serviceStates = await ProbeServicesAsync(services, cfg, ct);
        var readiness = await GetReadinessAsync(services, serviceStates, generatedAt, ct);
        var seeds = await GetSeedsAsync(services, generatedAt, ct);
        var validation = GetValidation(cfg, generatedAt);

        var blocking = serviceStates.Any(x => x.Required && x.Status is "offline" or "not-configured")
                       || readiness.Status != "ready"
                       || seeds.Status != "ready"
                       || validation.Status == "invalid";
        var warnings = serviceStates.Count(x => x.Status is "degraded" or "not-configured")
                       + readiness.Warnings.Count
                       + seeds.Warnings.Count
                       + validation.Issues.Count;

        var initialStatus = new SetupStatusResponse(
            blocking ? "degraded" : warnings > 0 ? "degraded" : "healthy",
            generatedAt,
            "live-backend-diagnostics",
            DurableReportAvailable: false,
            ReadOnly: true,
            WarningCount: warnings,
            Remediation: "Use Synaptix.Setup CLI for provisioning or mutation; this API is read-only.");

        var snapshot = new SetupSnapshot(initialStatus, readiness, serviceStates, seeds, validation);
        var durableReportAvailable = await PersistSnapshotAsync(services, snapshot, ct);
        var status = initialStatus with
        {
            DurableReportAvailable = durableReportAvailable,
            Source = durableReportAvailable ? "live-backend-diagnostics+durable-report" : initialStatus.Source,
        };

        return snapshot with { Status = status };
    }

    private static async Task<bool> PersistSnapshotAsync(IServiceProvider services, SetupSnapshot snapshot, CancellationToken ct)
    {
        try
        {
            var db = services.GetRequiredService<IAppDb>();
            var persistedStatus = snapshot.Status with
            {
                DurableReportAvailable = true,
                Source = "live-backend-diagnostics+durable-report",
            };

            var payload = new
            {
                Status = persistedStatus,
                snapshot.Readiness,
                Services = snapshot.Services,
                snapshot.Seeds,
                snapshot.Validation,
            };

            db.SetupReports.Add(new SetupReport(
                persistedStatus.Status,
                "live-backend-diagnostics",
                persistedStatus.WarningCount,
                persistedStatus.GeneratedAt,
                JsonSerializer.Serialize(payload, JsonOptions)));

            await db.SaveChangesAsync(ct);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<IReadOnlyList<SetupServiceState>> ProbeServicesAsync(
        IServiceProvider services,
        IConfiguration cfg,
        CancellationToken ct)
    {
        var states = new List<SetupServiceState>
        {
            await ProbePostgresAsync(services, cfg, ct),
            await ProbeMongoAsync(cfg, ct),
            await ProbeRedisAsync(cfg, ct),
            await ProbeTcpAsync("rabbitmq", cfg["RabbitMQ:Host"], cfg.GetValue("RabbitMQ:Port", 5672), required: true, ct),
            await ProbeMinioAsync(services, cfg, ct),
            await ProbeHttpAsync("elasticsearch", cfg["Elastic:Url"] ?? cfg.GetConnectionString("elasticsearch"), required: false, ct),
            await ProbeHttpAsync("kms", cfg["KmsClient:BaseUrl"], required: false, ct),
        };

        return states;
    }

    private static async Task<SetupServiceState> ProbePostgresAsync(IServiceProvider services, IConfiguration cfg, CancellationToken ct)
    {
        var configured = !string.IsNullOrWhiteSpace(cfg.GetConnectionString("db"))
                         || cfg.GetValue("Testing:UseInMemoryDb", false);
        if (!configured)
            return NotConfigured("postgresql", required: true);

        try
        {
            var db = services.GetRequiredService<IAppDb>();
            await db.SkillNodes.AsNoTracking().AnyAsync(ct);
            return Healthy("postgresql", required: true);
        }
        catch
        {
            return Offline("postgresql", required: true, "Database connectivity check failed.");
        }
    }

    private static async Task<SetupServiceState> ProbeMongoAsync(IConfiguration cfg, CancellationToken ct)
    {
        var connection = cfg.GetConnectionString("mongo") ?? cfg["Mongo:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connection))
            return NotConfigured("mongodb", required: true);

        try
        {
            var client = new MongoClient(connection);
            await client.GetDatabase("admin").RunCommandAsync<BsonDocument>(
                new BsonDocument("ping", 1),
                cancellationToken: ct);
            return Healthy("mongodb", required: true);
        }
        catch
        {
            return Offline("mongodb", required: true, "MongoDB connectivity check failed.");
        }
    }

    private static async Task<SetupServiceState> ProbeRedisAsync(IConfiguration cfg, CancellationToken ct)
    {
        var connection = cfg.GetConnectionString("redis") ?? cfg.GetConnectionString("cache");
        if (string.IsNullOrWhiteSpace(connection))
            return NotConfigured("redis", required: true);

        try
        {
            var options = ConfigurationOptions.Parse(connection);
            options.AbortOnConnectFail = false;
            options.ConnectTimeout = 2000;
            options.SyncTimeout = 2000;
            await using var redis = await ConnectionMultiplexer.ConnectAsync(options);
            await redis.GetDatabase().PingAsync();
            return Healthy("redis", required: true);
        }
        catch
        {
            return Offline("redis", required: true, "Redis connectivity check failed.");
        }
    }

    private static async Task<SetupServiceState> ProbeMinioAsync(IServiceProvider services, IConfiguration cfg, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cfg["MinIO:Endpoint"]))
            return NotConfigured("minio", required: true);

        var client = services.GetService<IMinioClient>();
        var options = services.GetService<MinioOptions>();
        if (client is null || options is null)
            return Offline("minio", required: true, "MinIO client is unavailable.");

        try
        {
            var exists = await client.BucketExistsAsync(new BucketExistsArgs().WithBucket(options.Bucket), ct);
            return exists
                ? Healthy("minio", required: true)
                : new SetupServiceState("minio", "degraded", true, true, "Required storage bucket is missing.");
        }
        catch
        {
            return Offline("minio", required: true, "MinIO connectivity check failed.");
        }
    }

    private static async Task<SetupServiceState> ProbeTcpAsync(
        string name,
        string? host,
        int port,
        bool required,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(host))
            return NotConfigured(name, required);

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(2));
            using var client = new TcpClient();
            await client.ConnectAsync(host, port, timeout.Token);
            return Healthy(name, required);
        }
        catch
        {
            return Offline(name, required, $"{name} connectivity check failed.");
        }
    }

    private static async Task<SetupServiceState> ProbeHttpAsync(string name, string? url, bool required, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(url))
            return NotConfigured(name, required);

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(2));
            using var client = new HttpClient();
            using var response = await client.GetAsync(url, timeout.Token);
            return response.IsSuccessStatusCode
                ? Healthy(name, required)
                : new SetupServiceState(name, "degraded", true, required, $"{name} returned a non-success status.");
        }
        catch
        {
            return Offline(name, required, $"{name} connectivity check failed.");
        }
    }

    private static async Task<SetupReadinessResponse> GetReadinessAsync(
        IServiceProvider services,
        IReadOnlyList<SetupServiceState> serviceStates,
        DateTimeOffset generatedAt,
        CancellationToken ct)
    {
        var warnings = new List<string>();
        var schemaStatus = "unknown";
        var pendingMigrations = (int?)null;

        try
        {
            var health = services.GetRequiredService<HealthCheckService>();
            var report = await health.CheckHealthAsync(registration => registration.Tags.Contains("ready"), ct);
            schemaStatus = report.Status == HealthStatus.Healthy ? "ready" : "not-ready";
            if (report.Status != HealthStatus.Healthy)
                warnings.Add("One or more readiness checks are not healthy.");
        }
        catch
        {
            warnings.Add("Readiness checks could not be evaluated.");
        }

        try
        {
            if (services.GetRequiredService<IAppDb>() is DbContext db && db.Database.IsRelational())
                pendingMigrations = (await db.Database.GetPendingMigrationsAsync(ct)).Count();
        }
        catch
        {
            warnings.Add("Migration state could not be evaluated.");
        }

        var requiredServicesReady = serviceStates.Where(x => x.Required).All(x => x.Status == "healthy");
        var ready = requiredServicesReady && schemaStatus == "ready" && pendingMigrations is not > 0;

        return new SetupReadinessResponse(
            ready ? "ready" : "not-ready",
            generatedAt,
            "live-backend-diagnostics",
            schemaStatus,
            pendingMigrations,
            requiredServicesReady,
            warnings);
    }

    private static async Task<SetupSeedsResponse> GetSeedsAsync(IServiceProvider services, DateTimeOffset generatedAt, CancellationToken ct)
    {
        var states = new List<SetupSeedState>();
        var warnings = new List<string>();

        try
        {
            var db = services.GetRequiredService<IAppDb>();
            states.Add(new("store-items", await db.StoreItems.AsNoTracking().AnyAsync(ct)));
            states.Add(new("skill-nodes", await db.SkillNodes.AsNoTracking().AnyAsync(ct)));
            states.Add(new("season-rewards", await db.SeasonRewardRules.AsNoTracking().AnyAsync(ct)));
            states.Add(new("questions", await db.Questions.AsNoTracking().AnyAsync(ct)));
        }
        catch
        {
            warnings.Add("Seed readiness could not be evaluated.");
            states.AddRange(RequiredSeedNames.Select(name => new SetupSeedState(name, false)));
        }

        var ready = states.All(x => x.Present);
        if (!ready)
            warnings.Add("One or more required seed categories are missing.");

        return new SetupSeedsResponse(ready ? "ready" : "not-ready", generatedAt, "application-database", states, warnings);
    }

    private static SetupValidationResponse GetValidation(IConfiguration cfg, DateTimeOffset generatedAt)
    {
        var issues = new List<SetupValidationIssue>();

        AddMissing(issues, "database", cfg.GetConnectionString("db"));
        AddMissing(issues, "mongodb", cfg.GetConnectionString("mongo") ?? cfg["Mongo:ConnectionString"]);
        AddMissing(issues, "redis", cfg.GetConnectionString("redis") ?? cfg.GetConnectionString("cache"));
        AddMissing(issues, "rabbitmq", cfg["RabbitMQ:Host"]);
        AddMissing(issues, "object-storage", cfg["MinIO:Endpoint"]);
        AddMissing(issues, "jwt-signing", cfg["JwtSettings:SecretKey"]);
        AddMissing(issues, "admin-operations", cfg["AdminOps:Key"]);

        return new SetupValidationResponse(
            issues.Count == 0 ? "valid" : "invalid",
            generatedAt,
            "configuration-presence-only",
            issues);
    }

    private static void AddMissing(List<SetupValidationIssue> issues, string category, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            issues.Add(new SetupValidationIssue(category, "missing", "Required configuration category is not present."));
    }

    private static SetupServiceState Healthy(string name, bool required) =>
        new(name, "healthy", true, required, "Live check succeeded.");

    private static SetupServiceState Offline(string name, bool required, string message) =>
        new(name, "offline", true, required, message);

    private static SetupServiceState NotConfigured(string name, bool required) =>
        new(name, "not-configured", false, required, "Configuration category is not present.");

    private sealed record SetupSnapshot(
        SetupStatusResponse Status,
        SetupReadinessResponse Readiness,
        IReadOnlyList<SetupServiceState> Services,
        SetupSeedsResponse Seeds,
        SetupValidationResponse Validation);

    private sealed record SetupStatusResponse(
        string Status,
        DateTimeOffset GeneratedAt,
        string Source,
        bool DurableReportAvailable,
        bool ReadOnly,
        int WarningCount,
        string Remediation);

    private sealed record SetupReadinessResponse(
        string Status,
        DateTimeOffset GeneratedAt,
        string Source,
        string SchemaStatus,
        int? PendingMigrations,
        bool RequiredServicesReady,
        IReadOnlyList<string> Warnings);

    private sealed record SetupServicesResponse(
        DateTimeOffset GeneratedAt,
        string Source,
        IReadOnlyList<SetupServiceState> Services);

    private sealed record SetupServiceState(
        string Name,
        string Status,
        bool Configured,
        bool Required,
        string Message);

    private sealed record SetupSeedsResponse(
        string Status,
        DateTimeOffset GeneratedAt,
        string Source,
        IReadOnlyList<SetupSeedState> Seeds,
        IReadOnlyList<string> Warnings);

    private sealed record SetupSeedState(string Name, bool Present);

    private sealed record SetupValidationResponse(
        string Status,
        DateTimeOffset GeneratedAt,
        string Source,
        IReadOnlyList<SetupValidationIssue> Issues);

    private sealed record SetupValidationIssue(string Category, string State, string Message);

    private sealed record SetupHistoryQuery(int? Limit);

    private sealed record SetupHistoryResponse(
        DateTimeOffset GeneratedAt,
        string Source,
        IReadOnlyList<SetupReportSummary> Reports);

    private sealed record SetupReportSummary(
        Guid Id,
        string Status,
        string Source,
        int WarningCount,
        DateTimeOffset GeneratedAt,
        DateTimeOffset CreatedAt);

    private sealed record SetupReportDetail(
        Guid Id,
        string Status,
        string Source,
        int WarningCount,
        DateTimeOffset GeneratedAt,
        DateTimeOffset CreatedAt,
        JsonElement Report);
}
