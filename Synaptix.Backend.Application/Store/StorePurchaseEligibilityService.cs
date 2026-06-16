using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Compliance.Client.Abstractions;

namespace Synaptix.Backend.Application.Store;

public sealed class StorePurchaseEligibilityService(
    IAppDb db,
    IComplianceClient compliance,
    ILogger<StorePurchaseEligibilityService> logger) : IStorePurchaseEligibilityService
{
    public async Task<EligibilityResult> CheckAsync(Guid userId, string sku, CancellationToken ct = default)
    {
        var item = await db.StoreItems.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Sku == sku && i.IsActive, ct);

        if (item is null)
            return EligibilityResult.Deny("NOT_FOUND", "Store item not found or not available.");

        // No compliance restrictions for items with no age gate
        if (item.AgeMin <= 0 && !item.RequiresParentApproval)
            return EligibilityResult.Allow();

        Synaptix.Compliance.Client.Models.Responses.UserRestrictionsResponse restrictions;
        try
        {
            restrictions = await compliance.GetUserRestrictionsAsync(userId, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not fetch compliance restrictions for user {UserId}; allowing purchase", userId);
            return EligibilityResult.Allow();
        }

        if (restrictions.Restrictions.Contains("minor_purchase_restricted",
                StringComparer.OrdinalIgnoreCase))
        {
            return EligibilityResult.Deny("MINOR_PURCHASE_RESTRICTED",
                "Purchases are not available for this account.");
        }

        if (item.RequiresParentApproval)
        {
            var control = await db.ParentalPurchaseControls.AsNoTracking()
                .FirstOrDefaultAsync(p => p.ChildUserId == userId, ct);

            if (control is null || !control.PurchasesEnabled)
                return EligibilityResult.Deny("PARENTAL_APPROVAL_REQUIRED",
                    "This item requires parental approval before purchase.");
        }

        return EligibilityResult.Allow();
    }
}
