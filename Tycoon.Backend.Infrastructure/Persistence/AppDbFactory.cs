using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Tycoon.Backend.Infrastructure.Persistence
{
    /// <summary>
    /// Enables "dotnet ef migrations ..." without requiring the API host to run.
    /// </summary>
    public sealed class AppDbFactory : IDesignTimeDbContextFactory<AppDb>
    {
        public AppDb CreateDbContext(string[] args)
        {
            // base path = where dotnet-ef is invoked from
            var basePath = Directory.GetCurrentDirectory();

            var cfg = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                // fallback: Infrastructure appsettings (when invoking dotnet-ef from solution root)
                .AddJsonFile(Path.Combine(basePath, "Tycoon.Backend.Infrastructure", "appsettings.json"), optional: true)
                .AddEnvironmentVariables()
                .Build();

            var cs = cfg.GetConnectionString("tycoon-db")
                     ?? cfg.GetConnectionString("db")
                     ?? "Host=localhost;Port=5432;Database=tycoon;Username=postgres;Password=postgres";

            var opt = new DbContextOptionsBuilder<AppDb>()
                .UseNpgsql(cs);

            return new AppDb(opt.Options);
        }
    }
}
