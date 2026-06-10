using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Synaptix.Compliance.Client.Abstractions;
using Synaptix.Compliance.Client.Http;
using Synaptix.Compliance.Client.Options;

namespace Synaptix.Compliance.Client.Extensions;

public static class ServiceCollectionExtensions
{
    /// Registers the compliance typed client. Single line: services.AddComplianceClient(configuration)
    public static IServiceCollection AddComplianceClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<ComplianceClientOptions>()
            .Bind(configuration.GetSection(ComplianceClientOptions.SectionName))
            .ValidateOnStart();

        services
            .AddHttpClient<IComplianceClient, ComplianceClient>()
            .ConfigureHttpClient((sp, client) =>
            {
                var opts = sp.GetRequiredService<IOptions<ComplianceClientOptions>>().Value;
                client.BaseAddress = new Uri(opts.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);

                if (!string.IsNullOrEmpty(opts.ServiceToken))
                    client.DefaultRequestHeaders.Add("X-Service-Token", opts.ServiceToken);
            })
            .AddStandardResilienceHandler();

        return services;
    }
}
