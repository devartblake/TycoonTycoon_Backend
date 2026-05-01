using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Tycoon.Backend.Infrastructure.Persistence;

namespace Tycoon.Backend.Migrations;

/// <summary>
/// Design-time DbContext factory scoped to the migrations assembly.
/// This allows `dotnet ef` to create <see cref="AppDb"/> when the migrations
/// project is used as startup/target during script-based workflows.
/// </summary>
public sealed class AppDbDesignTimeFactory : IDesignTimeDbContextFactory<AppDb>
{
    public AppDb CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();

        var cfg = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddJsonFile(Path.Combine(basePath, "Tycoon.Backend.Api", "appsettings.json"), optional: true)
            .AddJsonFile(Path.Combine(basePath, "Tycoon.Backend.Infrastructure", "appsettings.json"), optional: true)
            .AddEnvironmentVariables()
            .Build();

        var cs = cfg.GetConnectionString("tycoon-db")
                 ?? cfg.GetConnectionString("db")
                 ?? "Host=localhost;Port=5432;Database=tycoon;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<AppDb>()
            .UseNpgsql(cs)
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        return new AppDb(options);
    }
}
