using Microsoft.Extensions.DependencyInjection;
using Synaptix.Security.Kms.Application.Abstractions;
using Synaptix.Security.Kms.Application.Keys;
using Synaptix.Security.Kms.Application.Payload;
using Synaptix.Security.Kms.Application.Sessions;

namespace Synaptix.Security.Kms.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddKmsApplication(this IServiceCollection services)
    {
        services.AddScoped<ISecureSessionService, SecureSessionService>();
        services.AddScoped<ISecurePayloadProtector, SecurePayloadService>();
        services.AddScoped<KeyRotationService>();
        return services;
    }
}
