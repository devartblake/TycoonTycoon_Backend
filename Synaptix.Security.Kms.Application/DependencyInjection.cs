using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Synaptix.Security.Kms.Application.Abstractions;
using Synaptix.Security.Kms.Application.Keys;
using Synaptix.Security.Kms.Application.Options;
using Synaptix.Security.Kms.Application.Payload;
using Synaptix.Security.Kms.Application.Sessions;

namespace Synaptix.Security.Kms.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddKmsApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<KmsOptions>()
            .Bind(configuration.GetSection(KmsOptions.SectionName));

        services.AddSingleton<ISecureSessionKeyExchange, SecureSessionKeyExchange>();
        services.AddScoped<ISecureSessionService, SecureSessionService>();
        services.AddScoped<ISecurePayloadProtector, SecurePayloadService>();
        services.AddScoped<KeyRotationService>();
        return services;
    }
}
