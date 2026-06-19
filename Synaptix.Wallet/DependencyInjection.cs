using Microsoft.Extensions.DependencyInjection;

namespace Synaptix.Wallet;

public static class DependencyInjection
{
    public static IServiceCollection AddWallet(this IServiceCollection services)
    {
        return services;
    }
}
