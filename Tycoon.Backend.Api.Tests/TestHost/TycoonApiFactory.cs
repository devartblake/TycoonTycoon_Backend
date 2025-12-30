using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Tycoon.Backend.Api.Tests.TestHost
{
    public sealed class TycoonApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((ctx, cfg) =>
            {
                var dict = new Dictionary<string, string?>
                {
                    ["Testing:UseInMemoryDb"] = "true",
                    ["Hangfire:Enabled"] = "false",
                    ["Auth:JwtKey"] = "dev-test-only-dev-test-only-dev-test-only"
                };

                cfg.AddInMemoryCollection(dict);
            });
        }
    }
}
