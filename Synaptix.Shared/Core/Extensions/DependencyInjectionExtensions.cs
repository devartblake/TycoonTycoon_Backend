using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Timeout;
using Polly.Wrap;
using Synaptix.Shared.Abstractions.Core.Domain.Events;
using Synaptix.Shared.Core.Domain.Events;
using Synaptix.Shared.Core.Extensions.ServiceCollectionsExtensions;
using Synaptix.Shared.Core.Paging;
using Synaptix.Shared.Core.Persistence.Extensions;
using Synaptix.Shared.Core.Reflection;
using Synaptix.Shared.Resiliency.Options;
using Sieve.Services;

namespace Synaptix.Shared.Core.Extensions
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddCore(this IServiceCollection services)
        {
            services.AddTransient<IAggregatesDomainEventsRequestStore, AggregatesDomainEventsStore>();
            services.AddTransient<IAggregatesDomainEventsRequestStore, AggregatesDomainEventsStore>();
            services.AddTransient<IDomainEventPublisher, DomainEventPublisher>();
            services.AddTransient<IDomainEventsAccessor, DomainEventAccessor>();

            services.AddScoped<ISieveProcessor, ApplicationSieveProcessor>();

            services.AddPersistenceCore();

            return services;
        }
    }
}
