using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Synaptix.Compliance.Application.Abstractions;
using Synaptix.Compliance.Infrastructure.Persistence;

namespace Synaptix.Compliance.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddComplianceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("compliance")
            ?? configuration.GetConnectionString("db")
            ?? throw new InvalidOperationException(
                "Compliance DB connection string not configured. " +
                "Set ConnectionStrings:compliance or ConnectionStrings:db.");

        services.AddDbContext<ComplianceDb>(opts =>
            opts.UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IComplianceDb>(sp => sp.GetRequiredService<ComplianceDb>());

        return services;
    }
}
