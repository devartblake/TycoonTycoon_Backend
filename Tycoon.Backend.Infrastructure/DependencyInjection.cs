using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
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

            services.AddDbContext<AppDb>((sp, opt) =>
            {
                var connectionString = ResolvePostgresConnectionString(cfg);

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new InvalidOperationException(
                        "Missing PostgreSQL connection string. Provide one of:\n" +
                        "  - ConnectionStrings:tycoon_db\n" +
                        "  - ConnectionStrings:tycoon-db (Aspire)\n" +
                        "  - ConnectionStrings:db (docker-compose common)\n" +
                        "  - ConnectionStrings:PostgreSQL\n");
                }

                opt.UseNpgsql(connectionString, npgsql =>
                {
                    // ✅ SINGLE SOURCE OF TRUTH: migrations live here
                    npgsql.MigrationsAssembly("Tycoon.Backend.Migrations");

                    npgsql.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null);
                });

                // Suppress pending model changes warning if configured
                var suppressWarnings = cfg.GetValue<bool>("MigrationService:SuppressPendingModelWarnings");
                if (suppressWarnings)
                {
                    opt.ConfigureWarnings(warnings =>
                        warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
                }

                // Enable sensitive data logging in development (or when explicitly enabled)
                if (cfg.GetValue<bool>("Logging:EnableSensitiveDataLogging"))
                {
                    opt.EnableSensitiveDataLogging();
                }

                // Enable detailed errors in development (or when explicitly enabled)
                if (cfg.GetValue<bool>("Logging:EnableDetailedErrors"))
                {
                    opt.EnableDetailedErrors();
                }
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
                services.RemoveAll<IRollupStore>();

                services.AddSingleton<MongoClientFactory>();

                // MongoRollupStore and MongoAnalyticsEventWriter only depend on MongoClientFactory
                // (Singleton), so registering them as Singleton is safe and avoids scoped-in-singleton issues.
                services.AddSingleton<IRollupStore, MongoRollupStore>();
                services.AddSingleton<IAnalyticsEventWriter, MongoAnalyticsEventWriter>();

                services.AddSingleton<MongoRollupReader>();
            }

            // Elasticsearch (rollup indexing + rebuild)
            var elasticUrl =
                cfg["Elastic:Url"]
                ?? cfg.GetConnectionString("elasticsearch")
                ?? cfg.GetConnectionString("elastic");

            // Allow disabling explicitly
            var elasticEnabled = cfg.GetValue("Elastic:Enabled", true);

            if (elasticEnabled && !string.IsNullOrWhiteSpace(elasticUrl))
            {
                services.AddOptions<ElasticOptions>()
                    .Configure(o =>
                    {
                        o.Url = elasticUrl;

                        // Credentials (optional)
                        o.Username = cfg["Elastic:Username"];
                        o.Password = cfg["Elastic:Password"];

                        // Aliases (optional defaults)
                        o.DailyWriteAlias = cfg["Elastic:DailyWriteAlias"] ?? o.DailyWriteAlias;
                        o.PlayerDailyWriteAlias = cfg["Elastic:PlayerDailyWriteAlias"] ?? o.PlayerDailyWriteAlias;
                    })
                    .Validate(o => !string.IsNullOrWhiteSpace(o.Url), "Elastic:Url is required when Elastic is enabled.");

                services.AddSingleton(sp => sp.GetRequiredService<IOptions<ElasticOptions>>().Value);

                services.AddSingleton(sp =>
                {
                    var opt = sp.GetRequiredService<IOptions<ElasticOptions>>().Value;
                    var uri = new Uri(opt.Url);

                    var settings = new ElasticsearchClientSettings(uri);

                    if (!string.IsNullOrWhiteSpace(opt.Username) && !string.IsNullOrWhiteSpace(opt.Password))
                    {
                        settings = settings.Authentication(new BasicAuthentication(opt.Username, opt.Password));
                    }

                    // DEV convenience: allow self-signed certs when https
                    if (uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                    {
                        settings = settings.ServerCertificateValidationCallback((_, _, _, _) => true);
                    }

                    return new ElasticsearchClient(settings);
                });

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


        // ✅ Robust connection string resolution (Aspire + non-Aspire + legacy keys)
        // IMPORTANT:
        // - In Docker/Compose you often have ConnectionStrings:db
        // - In Aspire you may have ConnectionStrings:tycoon-db (or a named resource)
        // - Your current code only checks tycoon_db, which is why Docker shows empty connection string.
        private static string? ResolvePostgresConnectionString(IConfiguration cfg)
        {
            // Supports docker-compose, Aspire, and local conventions.
            var candidates = new (string Kind, string Key, string? Value)[]
            {
                ("ConnStr", "tycoon-db", cfg.GetConnectionString("tycoon-db")),  // common Aspire style
                ("ConnStr", "tycoon_db", cfg.GetConnectionString("tycoon_db")),  // your current key
                ("ConnStr", "db",       cfg.GetConnectionString("db")),         // common compose naming
                ("ConnStr", "PostgreSQL", cfg.GetConnectionString("PostgreSQL")),  // sometimes used
                ("Value",   "Postgres:ConnectionString", cfg["Postgres:ConnectionString"]), // optional custom
                ("Value",   "ConnectionStrings:tycoon_db", cfg["ConnectionStrings:tycoon_db"]), // optional custom
                ("Value",   "ConnectionStrings:db", cfg["ConnectionStrings:db"]),   // optional custom
            };

            var found = candidates.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Value)).Value;
            if (!string.IsNullOrWhiteSpace(found))
                return found!;

            var lookedFor = string.Join(", ", candidates.Select(c =>
                c.Kind == "ConnStr" ? $"ConnectionStrings:{c.Key}" : c.Key));

            throw new InvalidOperationException(
                "Missing PostgreSQL connection string. " +
                $"Looked for: {lookedFor}. " +
                "Fix docker-compose by setting ConnectionStrings__db (or ConnectionStrings__tycoon_db).");
        }
    }
}