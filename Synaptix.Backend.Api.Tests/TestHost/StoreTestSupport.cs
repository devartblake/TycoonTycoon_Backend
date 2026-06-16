using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;

namespace Synaptix.Backend.Api.Tests.TestHost
{
    public static class StoreTestSupport
    {
        /// <summary>
        /// Enables the DB-backed store/payment feature flags so payment-flow tests
        /// can exercise the endpoints. The <c>store_purchases_enabled</c> flag
        /// defaults to false (Alpha gating) and is not settable through the admin
        /// API, so tests that verify the enabled path must seed it directly.
        /// </summary>
        public static async Task EnableStorePurchasesAsync(WebApplicationFactory<Program> factory)
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();

            var json = JsonSerializer.Serialize(new Dictionary<string, bool>
            {
                ["store_enabled"] = true,
                ["store_purchases_enabled"] = true,
                ["payments_enabled"] = true,
                ["stripe_payments_enabled"] = true,
                ["paypal_payments_enabled"] = true,
            });

            var existing = await db.AdminAppConfigs.FirstOrDefaultAsync(x => x.Id == "default");
            if (existing is null)
                db.AdminAppConfigs.Add(new AdminAppConfig("https://api.example.com", false, json));
            else
                existing.Update(enableLogging: null, featureFlagsJson: json);

            await db.SaveChangesAsync();
        }
    }
}
