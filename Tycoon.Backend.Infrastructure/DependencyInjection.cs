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

            //var postgres =
            //    cfg.GetConnectionString("tycoon-db")
            //    ?? cfg.GetConnectionString("db");

            //if (string.IsNullOrWhiteSpace(postgres))
            //    throw new InvalidOperationException(
            //        "Missing PostgreSQL connection string. Provide ConnectionStrings:tycoon-db (Aspire) or ConnectionStrings:db.");

            services.AddDbContext<AppDb>((sp, opt) =>
            {
                var connectionString = cfg.GetConnectionString("tycoon_db");

                opt.UseNpgsql(connectionString, npgsql =>
                {
                    // ✅ SINGLE SOURCE OF TRUTH
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

                // Enable sensitive data logging in development
                if (cfg.GetValue<bool>("Logging:EnableSensitiveDataLogging"))
                {
                    opt.EnableSensitiveDataLogging();
                }

                // Enable detailed errors in development
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
                services.AddScoped<IAnalyticsEventWriter, PostgresAnalyticsEventWriter>();

                services.AddSingleton<MongoClientFactory>();

                services.Replace(ServiceDescriptor.Singleton<IRollupStore, MongoRollupStore>());
                services.Replace(ServiceDescriptor.Singleton<IAnalyticsEventWriter, MongoAnalyticsEventWriter>());

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
                // Bind + validate options once, then DI uses the typed value.
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

                // Register the resolved options value as a concrete singleton too (handy for non-IOptions consumers).
                services.AddSingleton(sp => sp.GetRequiredService<IOptions<ElasticOptions>>().Value);

                // ✅ The missing piece: typed ElasticsearchClient
                services.AddSingleton(sp =>
                {
                    var opt = sp.GetRequiredService<IOptions<ElasticOptions>>().Value;

                    var uri = new Uri(opt.Url);

                    var settings = new ElasticsearchClientSettings(uri);

                    // Basic auth if provided
                    if (!string.IsNullOrWhiteSpace(opt.Username) && !string.IsNullOrWhiteSpace(opt.Password))
                    {
                        settings = settings.Authentication(new BasicAuthentication(opt.Username, opt.Password));
                    }

                    // DEV convenience: allow self-signed certs when https
                    if (uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                    {
                        // If you want this ONLY in development, gate it via env checks in the calling project.
                        settings = settings.ServerCertificateValidationCallback((_, _, _, _) => true);
                    }

                    return new ElasticsearchClient(settings);
                });

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
