using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;
using Synaptix.Shared.Core.Extensions;

namespace Synaptix.Shared.Validation.Extensions;

public static class RegistrationExtensions
{
    public static IServiceCollection AddCustomValidators(this IServiceCollection services, Assembly assembly)
    {
        services.Scan(scan =>
            scan.FromAssemblies(assembly)
                .AddClasses(classes => classes.AssignableTo(typeof(IValidator<>)), publicOnly: false)
                .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                .AsImplementedInterfaces()
                .WithTransientLifetime()
        );

        return services;
    }
}
