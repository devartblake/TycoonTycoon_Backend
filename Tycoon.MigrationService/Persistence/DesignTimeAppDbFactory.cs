using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Tycoon.Backend.Infrastructure.Persistence;

namespace Tycoon.MigrationService.Persistence;

/// <summary>
/// Used by `dotnet ef migrations add` and `dotnet ef database update` at design time.
/// Resolves the PostgreSQL connection string using the same multi-key fallback chain
/// as the runtime Infrastructure DI, so it works regardless of which environment
/// convention is in use (Aspire, docker-compose, local appsettings).
/// </summary>
public sealed class DesignTimeAppDbFactory : IDesignTimeDbContextFactory<AppDb>
{
    public AppDb CreateDbContext(string[] args)
    {
        var env =
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "Development";

        var cfg = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{env}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var cs = ResolveConnectionString(cfg);

        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException(
                "Missing PostgreSQL connection string for design-time migrations. " +
                "Provide one of: ConnectionStrings:tycoon-db, ConnectionStrings:tycoon_db, " +
                "ConnectionStrings:db, ConnectionStrings:PostgreSQL, " +
                "or Postgres:ConnectionString.");

        var options = new DbContextOptionsBuilder<AppDb>()
            .UseNpgsql(cs, npgsql =>
            {
                // Migrations always live in Tycoon.Backend.Migrations — single source of truth.
                npgsql.MigrationsAssembly("Tycoon.Backend.Migrations");
            })
            .Options;

        return new AppDb(options);
    }

    /// <summary>
    /// Mirrors the fallback chain in Infrastructure.DependencyInjection.ResolvePostgresConnectionString
    /// so design-time tooling and runtime use the same resolution order.
    /// </summary>
    private static string? ResolveConnectionString(IConfiguration cfg) =>
        cfg.GetConnectionString("tycoon-db")          // Aspire convention
        ?? cfg.GetConnectionString("tycoon_db")        // compose/appsettings convention
        ?? cfg.GetConnectionString("db")               // common compose shorthand
        ?? cfg.GetConnectionString("PostgreSQL")       // legacy key
        ?? cfg["Postgres:ConnectionString"]            // explicit section
        ?? cfg["ConnectionStrings:tycoon_db"]          // env-var flattened form
        ?? cfg["ConnectionStrings:db"];                // env-var flattened shorthand
}