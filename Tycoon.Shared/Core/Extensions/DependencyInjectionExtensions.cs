using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Timeout;
using Polly.Wrap;
using Tycoon.Shared.Abstractions.Core.Domain.Events;
using Tycoon.Shared.Core.Domain.Events;
using Tycoon.Shared.Core.Extensions.ServiceCollectionsExtensions;
using Tycoon.Shared.Core.Paging;
using Tycoon.Shared.Core.Persistence.Extensions;
using Tycoon.Shared.Core.Reflection;
using Tycoon.Shared.Resiliency.Options;
using Sieve.Services;

namespace Tycoon.Shared.Core.Extensions
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
