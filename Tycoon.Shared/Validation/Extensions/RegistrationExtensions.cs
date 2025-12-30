using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;
using Tycoon.Shared.Core.Extensions;

namespace Tycoon.Shared.Validation.Extensions;

public static class RegistrationExtensions
{
    public static IServiceCollection AddCustomValidators(this IServiceCollection services, Assembly assembly)
    {
        // TODO: problem with registering internal validators
        services.Scan(scan =>
            scan.FromAssemblies(assembly)
                .AddClasses(classes => classes.AssignableTo(typeof(IValidator<>)))
                .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                .AsImplementedInterfaces()
                .WithTransientLifetime()
        );

        return services;
    }
}
