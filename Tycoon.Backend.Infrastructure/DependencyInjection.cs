using Elastic.Clients.Elasticsearch;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System.Reflection;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Application.Analytics.Abstractions;
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
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration cfg,string? serviceName = null, params Assembly[] mediatRAssemblies)
        {
            var useInMemory = cfg.GetValue<bool>("Testing:UseInMemoryDb");
            if (useInMemory)
            {
                services.AddDbContext<AppDb>(opt => opt.UseInMemoryDatabase("tycoon-test-db"));
                services.AddScoped<IAppDb>(sp => sp.GetRequiredService<AppDb>());
                services.AddSingleton<IClock, SystemClock>();
                services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
                return services;
            }

            // PostgreSQL (EF Core)
            var postgres =
                cfg.GetConnectionString("tycoon-db")
                ?? cfg.GetConnectionString("db");

            if (string.IsNullOrWhiteSpace(postgres))
                throw new InvalidOperationException(
                    "Missing PostgreSQL connection string. Provide ConnectionStrings:tycoon-db (Aspire) or ConnectionStrings:db.");

            services.AddDbContext<AppDb>(opt =>
                opt.UseNpgsql(postgres, npg =>
                    npg.MigrationsAssembly(typeof(AppDb).Assembly.FullName)));

            // Expose AppDb behind IAppDb
            services.AddScoped<IAppDb>(sp => sp.GetRequiredService<AppDb>());

            // Redis cache
            var redis =
                cfg.GetConnectionString("cache")
                ?? cfg.GetConnectionString("redis");

            if (!string.IsNullOrWhiteSpace(redis))
            {
                services.AddStackExchangeRedisCache(opt => opt.Configuration = redis);
            }

            // Mongo (analytics)
            var analyticsEnabled = cfg.GetValue("Analytics:Enabled", true);

            var mongo = cfg.GetConnectionString("mongo") ?? cfg["Mongo:ConnectionString"];
            if (analyticsEnabled && !string.IsNullOrWhiteSpace(mongo))
            {
                services.Configure<MongoOptions>(o =>
                {
                    // IMPORTANT: If MongoOptions properties are init-only in your project,
                    // replace this with services.AddSingleton(new MongoOptions { ... });
                    o.ConnectionString = mongo;
                    o.Database = cfg["Mongo:Database"] ?? "tycoon_analytics";
                });

                services.AddSingleton<MongoClientFactory>();

                // Replace default no-op registrations with Mongo-backed implementations
                services.Replace(ServiceDescriptor.Singleton<IAnalyticsEventWriter, MongoAnalyticsEventWriter>());
                services.Replace(ServiceDescriptor.Singleton<IRollupStore, MongoRollupStore>());

                // Step 7: rollup replay source
                services.AddSingleton<MongoRollupReader>();
            }

            // Elasticsearch (rollup indexing + rebuild)
            var elastic =
                cfg["Elastic:Url"]
                ?? cfg.GetConnectionString("elasticsearch")
                ?? cfg.GetConnectionString("elastic");

            if (!string.IsNullOrWhiteSpace(elastic))
            {
                services.Configure<ElasticOptions>(o =>
                {
                    o.Url = elastic;

                    // Prefer write aliases if you plan rollover; otherwise these can be plain index names.
                    o.DailyWriteAlias = cfg["Elastic:DailyWriteAlias"] ?? "tycoon-qa-daily-rollups-write";
                    o.PlayerDailyWriteAlias = cfg["Elastic:PlayerDailyWriteAlias"] ?? "tycoon-qa-player-daily-rollups-write";
                });

                services.AddSingleton(sp => sp.GetRequiredService<IOptions<ElasticOptions>>().Value);

                services.AddSingleton(sp =>
                {
                    var opt = sp.GetRequiredService<IOptions<ElasticOptions>>().Value;
                    var settings = new ElasticsearchClientSettings(new Uri(opt.Url));
                    return new ElasticsearchClient(settings);
                });

                services.AddSingleton<ElasticAdmin>();
                services.AddSingleton<ElasticIndexBootstrapper>();
                services.AddSingleton<IRollupIndexer, ElasticRollupIndexer>();

                // Step 7: rebuild Elastic from Mongo rollups
                services.AddSingleton<IRollupRebuilder, ElasticRollupRebuilder>();
            }

            // Common
            services.AddSingleton<IClock, SystemClock>();
            services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

            // Always return services (prevents CS0161 even if merges get weird)
            return services;
        }
    }
}
