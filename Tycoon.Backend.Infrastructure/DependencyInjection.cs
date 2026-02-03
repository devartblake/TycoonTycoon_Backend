using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Diagnostics;
using System.Reflection;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Application.Analytics.Abstractions;
using Tycoon.Backend.Application.Analytics.Rollups;
using Tycoon.Backend.Application.Analytics.Writers;
using Tycoon.Backend.Domain.Abstractions;
using Tycoon.Backend.Infrastructure.Analytics.Elastic;
using Tycoon.Backend.Infrastructure.Analytics.Mongo;
using Tycoon.Backend.Infrastructure.Events;
using Tycoon.Backend.Infrastructure.Persistence;
using Tycoon.Backend.Infrastructure.Services;
using Tycoon.Backend.Migrations;

namespace Tycoon.Backend.Infrastructure
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers infrastructure services (EF Core, Redis cache, system clock).
        /// Uses connection string fallback so it works with Aspire or plain appsettings.
        /// </summary>
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration cfg,
            string? serviceName = null,
            params Assembly[] mediatRAssemblies)
        {
            // ---------------------------
            // EF Core / Postgres (Option B)
            // ---------------------------
            var useInMemory = cfg.GetValue<bool>("Testing:UseInMemoryDb");

            if (useInMemory)
            {
                // Defaults (in-memory safe)
                services.RemoveAll<IAnalyticsEventWriter>();
                services.RemoveAll<IRollupStore>();
                services.TryAddScoped<IRollupStore, EfCoreRollupStore>();
                services.TryAddScoped<IAnalyticsEventWriter, PostgresAnalyticsEventWriter>();

                services.AddSingleton<IClock, SystemClock>();
                services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

                return services;
            }

            var postgres =
                cfg.GetConnectionString("tycoon-db")
                ?? cfg.GetConnectionString("db");

            if (string.IsNullOrWhiteSpace(postgres))
                throw new InvalidOperationException(
                    "Missing PostgreSQL connection string. Provide ConnectionStrings:tycoon-db (Aspire) or ConnectionStrings:db.");

            services.AddDbContext<AppDb>((sp, opt) =>
            {
                opt.UseNpgsql(postgres, npgsql =>
                {
                    // ✅ SINGLE SOURCE OF TRUTH
                    npgsql.MigrationsAssembly(typeof(AppDbModelSnapshot).Assembly.FullName);
                });

                // Optional but recommended
                opt.EnableDetailedErrors();
            });

            // Expose AppDb behind IAppDb
            services.TryAddScoped<IAppDb>(sp => sp.GetRequiredService<AppDb>());

            // ---------------------------
            // Analytics registrations
            // ---------------------------

            // Ensure infrastructure owns the analytics registrations.
            services.RemoveAll<IAnalyticsEventWriter>();
            services.RemoveAll<IRollupStore>();

            services.AddScoped<IRollupStore, EfCoreRollupStore>();
            services.AddScoped<IAnalyticsEventWriter, PostgresAnalyticsEventWriter>();

            // Redis cache
            var redis =
                cfg.GetConnectionString("cache")
                ?? cfg.GetConnectionString("redis");

            if (!string.IsNullOrWhiteSpace(redis))
                services.AddStackExchangeRedisCache(opt => opt.Configuration = redis);

            // Mongo (analytics)
            var analyticsEnabled = cfg.GetValue("Analytics:Enabled", true);
            var mongo = cfg.GetConnectionString("mongo") ?? cfg["Mongo:ConnectionString"];

            if (analyticsEnabled && !string.IsNullOrWhiteSpace(mongo))
            {
                services.Configure<MongoOptions>(o =>
                {
                    o.ConnectionString = mongo;
                    o.Database = cfg["Mongo:Database"] ?? "tycoon_analytics";
                });

                services.RemoveAll<IAnalyticsEventWriter>();
                services.AddScoped<IAnalyticsEventWriter, PostgresAnalyticsEventWriter>();

                services.AddSingleton<MongoClientFactory>();

                services.Replace(ServiceDescriptor.Singleton<IRollupStore, MongoRollupStore>());
                services.Replace(ServiceDescriptor.Singleton<IAnalyticsEventWriter, MongoAnalyticsEventWriter>());

                services.AddSingleton<MongoRollupReader>();
            }

            // Elasticsearch (rollup indexing + rebuild)
            var elastic =
                cfg["Elastic:Url"]
                ?? cfg.GetConnectionString("elasticsearch")
                ?? cfg.GetConnectionString("elastic");

            if (!string.IsNullOrWhiteSpace(elastic))
            {
                // ... keep your existing Elastic registration block ...
                services.AddSingleton<ElasticAdmin>();
                services.AddSingleton<ElasticIndexBootstrapper>();
                services.AddSingleton<IRollupIndexer, ElasticRollupIndexer>();
                services.AddSingleton<IRollupRebuilder, ElasticRollupRebuilder>();
            }

            // Common
            services.AddSingleton<TimeProvider>(TimeProvider.System);
            services.AddSingleton<ActivitySource>(_ => new ActivitySource("Tycoon.Backend"));

            services.AddSingleton<IClock, SystemClock>();
            services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

            return services;
        }
    }
}
