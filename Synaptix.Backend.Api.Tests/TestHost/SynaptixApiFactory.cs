using Hangfire;
using Hangfire.InMemory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Tests.TestHost
{
    public class SynaptixApiFactory : WebApplicationFactory<Program>
    {
        public const string TestAdminKey = "test-admin-ops-key";
        private readonly string _inMemoryDatabaseName = $"synaptix-tests-{Guid.NewGuid():N}";

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
            // InMemoryDbName must be set via UseSetting (host config) so it is visible
            // when AddInfrastructure() calls cfg["Testing:InMemoryDbName"] during service
            // registration — ConfigureAppConfiguration additions arrive too late.
            builder.UseSetting("Testing:InMemoryDbName", _inMemoryDatabaseName);
            // Disable the relational schema startup gate — it calls GetDbConnection() which
            // is not available on the EF Core in-memory provider used during tests.
            builder.UseSetting("SchemaGate:Enabled", "false");
            builder.UseSetting("SchemaGate:StartupGateEnabled", "false");

            builder.ConfigureAppConfiguration((ctx, cfg) =>
            {
                var dict = new Dictionary<string, string?>
                {
                    ["Testing:UseInMemoryDb"] = "true",
                    ["Testing:InMemoryDbName"] = _inMemoryDatabaseName,
                    ["SchemaGate:Enabled"] = "false",
                    ["SchemaGate:StartupGateEnabled"] = "false",

                    // Admin ops gate for /admin/*
                    ["AdminOps:Enabled"] = "true",
                    ["AdminOps:Header"] = "X-Admin-Ops-Key",
                    ["AdminOps:Key"] = TestAdminKey,
                    ["AdminAuth:AllowTrustedBffPlainJson"] = "true",
                    ["SecureChannel:AllowPlainJsonInTests"] = "true",
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
                services.AddHostedService<TestBaselineDataSeeder>();

                // #405 adds a global authenticated-user FallbackPolicy (deny-by-default)
                // for production. Disable ONLY the fallback in the test host so the
                // integration tests exercise each endpoint's explicit auth posture
                // (endpoints with .RequireAuthorization() still return 401 without a
                // token; endpoints protected only by the fallback are reachable as they
                // were pre-#405). The fallback remains active in production.
                services.PostConfigure<AuthorizationOptions>(o => o.FallbackPolicy = null);
            });
        }
    }

    internal sealed class TestBaselineDataSeeder(IServiceProvider services) : IHostedService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await using var scope = services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();

            if (!await db.SkillNodes.AnyAsync(cancellationToken))
            {
                db.SkillNodes.AddRange(
                    Skill("str.steady_timer", SkillBranch.Strategy, 1, "Steady Timer", "Improve timer control.", [], [new SkillCostDto(CurrencyType.Coins, 100)]),
                    Skill("str.combo_master", SkillBranch.Strategy, 2, "Combo Master", "Improve combo scoring.", ["str.steady_timer"], [new SkillCostDto(CurrencyType.Coins, 100)]),
                    Skill("know.quick_learner", SkillBranch.Knowledge, 1, "Quick Learner", "Improve knowledge gains.", [], [new SkillCostDto(CurrencyType.Coins, 100)]));
            }

            if (!await db.SeasonRewardRules.AnyAsync(cancellationToken))
            {
                db.SeasonRewardRules.AddRange(
                    new SeasonRewardRule(tier: 1, maxTierRank: 100, xp: 100, coins: 50),
                    new SeasonRewardRule(tier: 2, maxTierRank: 100, xp: 200, coins: 100),
                    new SeasonRewardRule(tier: 3, maxTierRank: 100, xp: 300, coins: 150));
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private static SkillNode Skill(
            string key,
            SkillBranch branch,
            int tier,
            string title,
            string description,
            IReadOnlyList<string> prereqs,
            IReadOnlyList<SkillCostDto> costs) =>
            new(
                key,
                branch,
                tier,
                title,
                description,
                JsonSerializer.Serialize(prereqs, JsonOptions),
                JsonSerializer.Serialize(costs, JsonOptions),
                JsonSerializer.Serialize(new Dictionary<string, double>(), JsonOptions));
    }
}
