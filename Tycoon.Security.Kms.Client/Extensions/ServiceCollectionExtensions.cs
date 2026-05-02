using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tycoon.Security.Kms.Client.Abstractions;
using Tycoon.Security.Kms.Client.Http;
using Tycoon.Security.Kms.Client.Options;

namespace Tycoon.Security.Kms.Client.Extensions;

public static class ServiceCollectionExtensions
{
    /// Registers all KMS typed clients and binds KmsClientOptions from configuration.
    /// Callers add a single line: services.AddKmsClient(configuration)
    public static IServiceCollection AddKmsClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<KmsClientOptions>()
            .Bind(configuration.GetSection(KmsClientOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddHttpClient<IKmsSessionClient, KmsSessionClient>()
            .ConfigureKmsHttpClient(configuration)
            .AddStandardResilienceHandler();

        services
            .AddHttpClient<IKmsPayloadClient, KmsPayloadClient>()
            .ConfigureKmsHttpClient(configuration)
            .AddStandardResilienceHandler();

        services
            .AddHttpClient<IKmsKeyClient, KmsKeyClient>()
            .ConfigureKmsHttpClient(configuration)
            .AddStandardResilienceHandler();

        services
            .AddHttpClient<IKmsInternalClient, KmsInternalClient>()
            .ConfigureKmsHttpClient(configuration)
            .AddStandardResilienceHandler();

        return services;
    }

    private static IHttpClientBuilder ConfigureKmsHttpClient(
        this IHttpClientBuilder builder,
        IConfiguration configuration)
    {
        return builder.ConfigureHttpClient((sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<KmsClientOptions>>().Value;
            client.BaseAddress = new Uri(opts.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);

            if (!string.IsNullOrEmpty(opts.ServiceToken))
                client.DefaultRequestHeaders.Add("X-Service-Token", opts.ServiceToken);
        });
    }
}
