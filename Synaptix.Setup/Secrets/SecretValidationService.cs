using Microsoft.Extensions.Configuration;
using Serilog;

namespace Synaptix.Setup.Secrets;

public sealed class SecretValidationService
{
    private static readonly string[] WeakPasswordFragments =
    [
        "password_123", "change-me", "changeme", "ChangeMe123", "CHANGE_ME",
        "<generated-by-synaptix-setup>", "your-super-secret", "admin_password",
        "synaptix_password", "synaptix_mongo_password", "synaptix_redis_password",
        "synaptix_elastic_password", "synaptix_rabbitmq_password", "synaptix_minio_password",
        "synaptix_app_password",
    ];

    private static readonly (string Key, string Description, bool IsSecret)[] SecretKeys =
    [
        ("POSTGRES_PASSWORD",            "PostgreSQL password",             true),
        ("MONGO_INITDB_ROOT_PASSWORD",   "MongoDB root password",           true),
        ("MONGO_APP_PASSWORD",           "MongoDB app user password",       true),
        ("REDIS_PASSWORD",               "Redis password",                  true),
        ("ELASTIC_PASSWORD",             "Elasticsearch password",          true),
        ("RABBITMQ_PASSWORD",            "RabbitMQ password",               true),
        ("MINIO_ROOT_PASSWORD",          "MinIO root password",             true),
        ("JWT_SECRET_KEY",               "JWT signing key",                 true),
        ("ADMIN_OPS_KEY",                "Admin ops API key",               true),
        ("KMS_SERVICE_TOKEN",            "KMS service token",               false),
        ("SUPER_ADMIN_PASSWORD",         "Super admin password",            true),
        ("GRAFANA_PASSWORD",             "Grafana admin password",          false),
        ("PGADMIN_DEFAULT_PASSWORD",     "pgAdmin password",                false),
        ("MONGO_EXPRESS_PASSWORD",       "Mongo Express password",          false),
    ];

    public ValidationReport Validate(IConfiguration cfg, bool isLocal, bool strict)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        foreach (var (key, desc, isRequired) in SecretKeys)
        {
            var value = cfg[key] ?? Environment.GetEnvironmentVariable(key);

            if (string.IsNullOrWhiteSpace(value))
            {
                if (isRequired)
                    errors.Add($"{key} ({desc}) is not set.");
                continue;
            }

            if (IsPlaceholder(value))
            {
                errors.Add($"{key} ({desc}) still has a placeholder value. Run: dotnet run --project Synaptix.Setup -- init-local");
                continue;
            }

            if (IsWeak(value))
            {
                if (!isLocal && isRequired)
                    errors.Add($"{key} ({desc}) uses a known weak default. Generate a strong secret.");
                else
                    warnings.Add($"{key} ({desc}) uses a known weak value. Consider regenerating.");
            }
        }

        // Environment-specific guards
        if (!isLocal)
        {
            var migrReset = cfg["MIGRATION_RESET_DATABASE"] ?? "false";
            if (migrReset.Equals("true", StringComparison.OrdinalIgnoreCase))
                errors.Add("MIGRATION_RESET_DATABASE=true is not allowed outside local environment.");

            var ensureCreated = cfg["MIGRATION_ALLOW_ENSURE_CREATED"] ?? "false";
            if (ensureCreated.Equals("true", StringComparison.OrdinalIgnoreCase))
                errors.Add("MIGRATION_ALLOW_ENSURE_CREATED=true is not allowed outside local environment.");
        }

        return new ValidationReport(errors, warnings, errors.Count == 0 && (!strict || warnings.Count == 0));
    }

    private static bool IsPlaceholder(string value) =>
        value.Contains("<generated-by-synaptix-setup>", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("<", StringComparison.Ordinal) && value.EndsWith(">", StringComparison.Ordinal);

    private static bool IsWeak(string value) =>
        WeakPasswordFragments.Any(fragment =>
            value.Contains(fragment, StringComparison.OrdinalIgnoreCase));
}

public sealed record ValidationReport(
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings,
    bool Passed);
