using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.Store;

internal static class StoreSystemStatusSupport
{
    public const string StoreEnabledFlag = "store_enabled";
    public const string PaymentsEnabledFlag = "payments_enabled";
    public const string StripeEnabledFlag = "stripe_payments_enabled";
    public const string PayPalEnabledFlag = "paypal_payments_enabled";

    public static async Task<StoreSystemStatusDto> GetStatusAsync(
        IAppDb db,
        IConfiguration configuration,
        CancellationToken ct)
    {
        var flags = await LoadFlagsAsync(db, ct);

        var storeEnabled = flags.GetValueOrDefault(StoreEnabledFlag, true);
        var paymentsEnabled = flags.GetValueOrDefault(PaymentsEnabledFlag, true);
        var stripeConfigured = configuration.GetValue("Stripe:Enabled", false);
        var payPalConfigured = configuration.GetValue("PayPal:Enabled", false);
        var stripeToggleEnabled = flags.GetValueOrDefault(StripeEnabledFlag, true);
        var payPalToggleEnabled = flags.GetValueOrDefault(PayPalEnabledFlag, true);

        var stripeEnabled = storeEnabled && paymentsEnabled && stripeConfigured && stripeToggleEnabled;
        var payPalEnabled = storeEnabled && paymentsEnabled && payPalConfigured && payPalToggleEnabled;

        return new StoreSystemStatusDto(
            StoreEnabled: storeEnabled,
            PaymentsEnabled: paymentsEnabled,
            StripeConfigured: stripeConfigured,
            StripeEnabled: stripeEnabled,
            PayPalConfigured: payPalConfigured,
            PayPalEnabled: payPalEnabled,
            Message: BuildMessage(storeEnabled, paymentsEnabled, stripeEnabled, payPalEnabled, stripeConfigured, payPalConfigured));
    }

    public static async Task<AdminAppConfig> GetOrCreateConfigAsync(IAppDb db, CancellationToken ct)
    {
        var existing = await db.AdminAppConfigs.FirstOrDefaultAsync(x => x.Id == "default", ct);
        if (existing is not null)
            return existing;

        var created = new AdminAppConfig("https://api.example.com", false, JsonSerializer.Serialize(new Dictionary<string, bool>
        {
            ["adminEventUpload"] = true,
            ["game_events_enabled"] = true,
            ["guardian_enabled"] = true,
            ["territory_enabled"] = true,
            [StoreEnabledFlag] = true,
            [PaymentsEnabledFlag] = true,
            [StripeEnabledFlag] = true,
            [PayPalEnabledFlag] = true
        }));

        db.AdminAppConfigs.Add(created);
        await db.SaveChangesAsync(ct);
        return created;
    }

    public static void ApplyUpdate(Dictionary<string, bool> flags, UpdateStoreSystemStatusRequest request)
    {
        if (request.StoreEnabled.HasValue)
            flags[StoreEnabledFlag] = request.StoreEnabled.Value;

        if (request.PaymentsEnabled.HasValue)
            flags[PaymentsEnabledFlag] = request.PaymentsEnabled.Value;

        if (request.StripeEnabled.HasValue)
            flags[StripeEnabledFlag] = request.StripeEnabled.Value;

        if (request.PayPalEnabled.HasValue)
            flags[PayPalEnabledFlag] = request.PayPalEnabled.Value;
    }

    public static async Task<Dictionary<string, bool>> LoadFlagsAsync(IAppDb db, CancellationToken ct)
    {
        var config = await db.AdminAppConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == "default", ct);

        if (config is null || string.IsNullOrWhiteSpace(config.FeatureFlagsJson))
            return [];

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, bool>>(config.FeatureFlagsJson) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static string BuildMessage(
        bool storeEnabled,
        bool paymentsEnabled,
        bool stripeEnabled,
        bool payPalEnabled,
        bool stripeConfigured,
        bool payPalConfigured)
    {
        if (!storeEnabled)
            return "Store transactions are currently disabled.";

        if (!paymentsEnabled)
            return "External payment flows are currently disabled.";

        if (!stripeConfigured && !payPalConfigured)
            return "No external payment provider is configured.";

        if (!stripeEnabled && !payPalEnabled)
            return "No external payment provider is currently available.";

        if (stripeEnabled && payPalEnabled)
            return "Stripe and PayPal payments are available.";

        if (stripeEnabled)
            return "Stripe payments are available. PayPal is unavailable.";

        return "PayPal payments are available. Stripe is unavailable.";
    }
}
