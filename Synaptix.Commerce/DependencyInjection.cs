using Microsoft.Extensions.DependencyInjection;
using Synaptix.Commerce.Services;

namespace Synaptix.Commerce;

public static class DependencyInjection
{
    public static IServiceCollection AddCommerce(this IServiceCollection services)
    {
        services.AddScoped<IStoreStockService, StoreStockService>();
        services.AddScoped<IStorePurchaseEligibilityService, StorePurchaseEligibilityService>();
        return services;
    }
}
