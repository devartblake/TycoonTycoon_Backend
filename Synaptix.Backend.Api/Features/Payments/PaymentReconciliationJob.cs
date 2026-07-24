using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Synaptix.Backend.Api.Payments.PayPal;
using Synaptix.Backend.Api.Payments.Stripe;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Api.Features.Payments;

/// <summary>
/// Daily reconciliation of PayPal/Stripe checkout attempts against provider-reported state.
/// Lives in the Api project (not Application, where other recurring jobs live) because it
/// depends on IPayPalPaymentGateway/IStripePaymentGateway, which are defined here rather
/// than in Infrastructure — Application does not reference Api, so the job can't either way
/// live there without relocating the gateways, which is out of scope for this change.
///
/// Scope: this pass detects checkouts the provider captured but that never produced a local
/// PlayerTransaction (silent fulfillment failure), plus amount/currency drift at first
/// confirmation. It does NOT re-verify already-fulfilled transactions against later provider-side
/// disputes/refunds/chargebacks — that requires ingesting PayPal/Stripe dispute webhook event
/// types, which is a separate follow-up, not covered here.
/// </summary>
public sealed class PaymentReconciliationJob(
    IAppDb db,
    IPayPalPaymentGateway payPalGateway,
    IStripePaymentGateway stripeGateway,
    ILogger<PaymentReconciliationJob> logger)
{
    private static readonly TimeSpan GracePeriod = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan ExpiryThreshold = TimeSpan.FromHours(24);

    public async Task RunAsync(CancellationToken ct = default)
    {
        var cutoff = DateTimeOffset.UtcNow - GracePeriod;
        var pending = await db.PaymentCheckoutAttempts
            .Where(a => a.Status == PaymentCheckoutStatus.Created && a.CreatedAtUtc < cutoff)
            .ToListAsync(ct);

        var issuesRaised = 0;
        foreach (var attempt in pending)
        {
            try
            {
                if (await ReconcileAttemptAsync(attempt, ct))
                    issuesRaised++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "PaymentReconciliationJob: failed to reconcile attempt {AttemptId} ({Provider}/{ProviderRef})",
                    attempt.Id, attempt.Provider, attempt.ProviderRef);
            }
        }

        await db.SaveChangesAsync(ct);

        if (pending.Count > 0)
            logger.LogInformation(
                "PaymentReconciliationJob: checked {Count} pending checkout attempts, raised {IssueCount} issues",
                pending.Count, issuesRaised);
    }

    /// <summary>
    /// Reconciles a single attempt against the provider. Returns true if a
    /// PaymentReconciliationIssue was raised. Shared by the recurring job and the
    /// admin "reconcile now" endpoint. Does not call SaveChangesAsync — the caller commits.
    /// </summary>
    public async Task<bool> ReconcileAttemptAsync(PaymentCheckoutAttempt attempt, CancellationToken ct)
    {
        string providerStatus;
        decimal? actualAmount;
        string? actualCurrency;
        string? captureRef;

        if (string.Equals(attempt.Provider, "paypal", StringComparison.OrdinalIgnoreCase))
        {
            var order = await payPalGateway.GetOrderAsync(attempt.ProviderRef, ct);
            providerStatus = order.Status;
            actualAmount = order.TotalAmount;
            actualCurrency = order.Currency;
            captureRef = order.CaptureId;
        }
        else if (string.Equals(attempt.Provider, "stripe", StringComparison.OrdinalIgnoreCase))
        {
            var session = await stripeGateway.GetCheckoutSessionAsync(attempt.ProviderRef, ct);
            providerStatus = session.PaymentStatus ?? "unknown";
            actualAmount = session.AmountTotal.HasValue ? session.AmountTotal.Value / 100m : null;
            actualCurrency = session.Currency;
            captureRef = session.PaymentIntentId;
        }
        else
        {
            return false;
        }

        var isCompleted =
            string.Equals(providerStatus, "COMPLETED", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(providerStatus, "paid", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(providerStatus, "complete", StringComparison.OrdinalIgnoreCase);

        if (!isCompleted)
        {
            if (DateTimeOffset.UtcNow - attempt.CreatedAtUtc > ExpiryThreshold)
                attempt.MarkExpired();
            return false;
        }

        var localTx = await db.PlayerTransactions
            .FirstOrDefaultAsync(t => t.Receipt == attempt.ProviderRef, ct);

        if (localTx is null)
        {
            db.PaymentReconciliationIssues.Add(new PaymentReconciliationIssue(
                PaymentReconciliationCategory.ProviderCapturedFulfillmentMissing,
                attempt.Provider,
                attempt.ProviderRef,
                attempt.Id,
                attempt.PlayerId,
                attempt.ExpectedAmount,
                actualAmount,
                $"Provider reports '{providerStatus}' for {attempt.Provider} reference '{attempt.ProviderRef}' " +
                "but no local PlayerTransaction grant exists."));
            return true;
        }

        // Fulfillment exists locally — self-heal the attempt's status without raising an issue.
        attempt.MarkCaptured(localTx.Id, captureRef);

        if (actualAmount.HasValue && Math.Abs(actualAmount.Value - attempt.ExpectedAmount) > 0.01m)
        {
            db.PaymentReconciliationIssues.Add(new PaymentReconciliationIssue(
                PaymentReconciliationCategory.AmountMismatch,
                attempt.Provider, attempt.ProviderRef, attempt.Id, attempt.PlayerId,
                attempt.ExpectedAmount, actualAmount,
                $"Expected {attempt.ExpectedAmount} {attempt.Currency}, provider captured {actualAmount} {actualCurrency}."));
            return true;
        }

        if (!string.IsNullOrWhiteSpace(actualCurrency) &&
            !string.Equals(actualCurrency, attempt.Currency, StringComparison.OrdinalIgnoreCase))
        {
            db.PaymentReconciliationIssues.Add(new PaymentReconciliationIssue(
                PaymentReconciliationCategory.CurrencyMismatch,
                attempt.Provider, attempt.ProviderRef, attempt.Id, attempt.PlayerId,
                attempt.ExpectedAmount, actualAmount,
                $"Expected currency {attempt.Currency}, provider captured {actualCurrency}."));
            return true;
        }

        return false;
    }
}
