using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Synaptix.Security.Kms.Application.Abstractions;
using Synaptix.Security.Kms.Infrastructure.Redis;
using Synaptix.Security.Kms.Infrastructure.Vault;

namespace Synaptix.Security.Kms.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddKmsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Redis — session store + replay protection
        var redis = configuration.GetConnectionString("cache")
                 ?? configuration.GetConnectionString("redis");

        if (!string.IsNullOrWhiteSpace(redis))
        {
            services.AddStackExchangeRedisCache(opt => opt.Configuration = redis);
        }
        else
        {
            // Fall back to in-memory cache for local dev without Redis
            services.AddDistributedMemoryCache();
        }

        services.AddScoped<ISessionStore, RedisSessionStore>();
        services.AddScoped<IReplayProtectionStore, RedisReplayProtectionStore>();

        // Vault Transit — key wrapping
        var vaultSection = configuration.GetSection(VaultOptions.SectionName);
        if (vaultSection.Exists())
        {
            services.AddOptions<VaultOptions>()
                .Bind(vaultSection)
                .ValidateOnStart();

            services.AddHttpClient<VaultTransitClient>((sp, client) =>
            {
                var opts = sp.GetRequiredService<IOptions<VaultOptions>>().Value;
                client.BaseAddress = new Uri(opts.Address);
                client.DefaultRequestHeaders.Add("X-Vault-Token", opts.Token);
            });

            services.AddScoped<IKeyWrappingService, VaultEnvelopeEncryptionService>();
        }
        else
        {
            services.AddScoped<IKeyWrappingService, NullKeyWrappingService>();
        }

        return services;
    }
}
