using Hangfire;
using Hangfire.InMemory;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Tycoon.Backend.Api.Tests.TestHost
{
    public class TycoonApiFactory : WebApplicationFactory<Program>
    {
        public const string TestAdminKey = "test-admin-ops-key";

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            // UseSetting is applied to the host configuration before Program.cs service-
            // registration code reads builder.Configuration, so these flags are visible in
            // time for conditional service registrations (in-memory DB path, schema gate off).
            //
            // NOTE: Hangfire:Enabled is intentionally left at the default (true) so that
            // AddHangfire() still runs and IBackgroundJobClient gets registered for handlers
            // that depend on it.  The PostgreSQL storage is replaced with in-memory storage
            // below via ConfigureServices, so no real database connection is made.
            builder.UseSetting("Testing:UseInMemoryDb", "true");
            // Disable the relational schema startup gate — it calls GetDbConnection() which
            // is not available on the EF Core in-memory provider used during tests.
            builder.UseSetting("SchemaGate:Enabled", "false");
            builder.UseSetting("SchemaGate:StartupGateEnabled", "false");

            builder.ConfigureAppConfiguration((ctx, cfg) =>
            {
                var dict = new Dictionary<string, string?>
                {
                    ["Testing:UseInMemoryDb"] = "true",
                    ["SchemaGate:Enabled"] = "false",
                    ["SchemaGate:StartupGateEnabled"] = "false",

                    // Admin ops gate for /admin/*
                    ["AdminOps:Enabled"] = "true",
                    ["AdminOps:Header"] = "X-Admin-Ops-Key",
                    ["AdminOps:Key"] = TestAdminKey,
                    ["AdminAuth:AllowTrustedBffPlainJson"] = "true",
                };

                cfg.AddInMemoryCollection(dict);
            });

            // ConfigureAppConfiguration overrides are applied during builder.Build(), which
            // is too late for Hangfire service registration.  Hangfire is registered with the
            // real PostgreSQL storage by Program.cs.  Replace every Hangfire descriptor with
            // an in-memory backend here so that the hosted server (BackgroundJobServer) and
            // UseHangfireDashboard start successfully without requiring a live database.
            builder.ConfigureServices(services =>
            {
                var hangfireDescriptors = services
                    .Where(s =>
                        (s.ServiceType.Assembly.GetName().Name?.StartsWith("Hangfire") == true) ||
                        (s.ImplementationType?.Assembly.GetName().Name?.StartsWith("Hangfire") == true))
                    .ToList();

                foreach (var d in hangfireDescriptors)
                    services.Remove(d);

                services.AddHangfire(cfg =>
                    cfg.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                       .UseSimpleAssemblyNameTypeSerializer()
                       .UseRecommendedSerializerSettings()
                       .UseInMemoryStorage());
                services.AddHangfireServer();
            });
        }
    }
}
