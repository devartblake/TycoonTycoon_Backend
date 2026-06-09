using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Synaptix.Compliance.Infrastructure.Persistence;

// Used only by `dotnet ef migrations add` — never called at runtime.
public sealed class ComplianceDbDesignTimeFactory : IDesignTimeDbContextFactory<ComplianceDb>
{
    public ComplianceDb CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<ComplianceDb>()
            .UseNpgsql("Host=localhost;Port=5432;Database=synaptix_compliance;Username=postgres;Password=postgres")
            .UseSnakeCaseNamingConventions()
            .Options;

        return new ComplianceDb(opts);
    }
}
