namespace Synaptix.Commerce.Services;

public sealed record EligibilityResult(bool IsEligible, string? ErrorCode, string? ErrorMessage)
{
    public static EligibilityResult Allow() => new(true, null, null);
    public static EligibilityResult Deny(string code, string message) => new(false, code, message);
}

public interface IStorePurchaseEligibilityService
{
    Task<EligibilityResult> CheckAsync(Guid userId, string sku, CancellationToken ct = default);
}
