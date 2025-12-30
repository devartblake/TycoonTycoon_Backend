using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tycoon.Backend.Infrastructure.Persistence
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDb>
    {
        public AppDb CreateDbContext(string[] args)
        {
            var opts = new DbContextOptionsBuilder<AppDb>()
                .UseNpgsql("Host=localhost;Database=trivia;Username=postgres;Password=postgres")
                .Options;
            return new AppDb(opts);
        }
    }
}
