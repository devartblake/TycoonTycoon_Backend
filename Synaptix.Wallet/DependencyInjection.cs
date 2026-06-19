using Microsoft.Extensions.DependencyInjection;
using Synaptix.Shared.Contracts.Abstractions;
using Synaptix.Wallet.Services;

namespace Synaptix.Wallet;

public static class DependencyInjection
{
    public static IServiceCollection AddWallet(this IServiceCollection services)
    {
        services.AddScoped<IEconomyService, EconomyService>();
        services.AddScoped<IPlayerTransactionService, PlayerTransactionService>();
        return services;
    }
}
