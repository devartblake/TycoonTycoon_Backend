using Microsoft.Extensions.DependencyInjection;
using Synaptix.Compliance.Application.Abstractions;
using Synaptix.Compliance.Application.AgeVerification;
using Synaptix.Compliance.Application.Audit;
using Synaptix.Compliance.Application.Consent;
using Synaptix.Compliance.Application.ParentalConsent;
using Synaptix.Compliance.Application.PrivacyRequests;

namespace Synaptix.Compliance.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddComplianceApplication(this IServiceCollection services)
    {
        services.AddScoped<IAgeVerificationService, AgeVerificationService>();
        services.AddScoped<IParentalConsentService, ParentalConsentService>();
        services.AddScoped<IPrivacyRequestService, PrivacyRequestService>();
        services.AddScoped<IConsentService, ConsentService>();
        services.AddScoped<IComplianceAuditService, ComplianceAuditService>();
        return services;
    }
}
