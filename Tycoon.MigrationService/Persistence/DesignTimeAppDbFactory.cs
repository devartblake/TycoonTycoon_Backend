using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Tycoon.Backend.Infrastructure.Persistence;

namespace Tycoon.MigrationService.Persistence;

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

        var cs =
            cfg.GetConnectionString("tycoon-db")
            ?? cfg.GetConnectionString("db");

        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException(
                "Missing PostgreSQL connection string. Set ConnectionStrings:db (or tycoon-db).");

        var options = new DbContextOptionsBuilder<AppDb>()
            .UseNpgsql(cs, npgsql =>
            {
                // IMPORTANT: migrations live in Tycoon.Backend.Migrations
                npgsql.MigrationsAssembly("Tycoon.Backend.Migrations");
            })
            .Options;

        return new AppDb(options);
    }
}
