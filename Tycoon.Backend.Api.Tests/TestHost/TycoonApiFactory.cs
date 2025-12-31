using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Tycoon.Backend.Api.Tests.TestHost
{
    public sealed class TycoonApiFactory : WebApplicationFactory<Program>
    {
        public const string TestAdminKey = "test-admin-ops-key";

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((ctx, cfg) =>
            {
                var dict = new Dictionary<string, string?>
                {
                    ["Testing:UseInMemoryDb"] = "true",
                    ["Hangfire:Enabled"] = "false",

                    // Admin ops gate for /admin/*
                    ["AdminOps:Enabled"] = "true",
                    ["AdminOps:Header"] = "X-Admin-Ops-Key",
                    ["AdminOps:Key"] = TestAdminKey,
                };

                cfg.AddInMemoryCollection(dict);
            });
        }
    }
}
