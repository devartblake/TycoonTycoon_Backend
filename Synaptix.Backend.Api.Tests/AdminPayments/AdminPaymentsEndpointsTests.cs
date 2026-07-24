using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Synaptix.Backend.Api.Features.AdminPayments;
using Synaptix.Backend.Api.Features.Payments;
using Synaptix.Backend.Api.Payments.PayPal;
using Synaptix.Backend.Api.Payments.Stripe;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;
using Synaptix.Entitlements.Services;
using Xunit;

namespace Synaptix.Backend.Api.Tests.AdminPayments;

public sealed class AdminPaymentsEndpointsTests : IClassFixture<SynaptixApiFactory>
{
    private readonly SynaptixApiFactory _factory;

    public AdminPaymentsEndpointsTests(SynaptixApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Reconcile_WhenProviderCapturedButLocalFulfillmentMissing_RaisesIssue()
    {
        var fakePayPal = new FakePayPalGateway();
        using var factory = CreateFactory(fakePayPal, new FakeStripeGateway());
        using var admin = factory.CreateClient().WithAdminOpsKey();

        var playerId = Guid.NewGuid();
        Guid attemptId;

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            var attempt = new PaymentCheckoutAttempt(playerId, "paypal", "powerup:skip", 2, 5.98m, "USD", "ORDER-MISSING-1");
            db.PaymentCheckoutAttempts.Add(attempt);
            await db.SaveChangesAsync();
            attemptId = attempt.Id;
        }

        // Provider confirms the capture completed, but no PlayerTransaction was ever created for it.
        fakePayPal.NextOrderStatusResult = new PayPalOrderStatusResult("ORDER-MISSING-1", "COMPLETED", "CAPTURE-MISSING-1", "USD", 5.98m);

        var response = await admin.PostAsync($"/admin/payments/{attemptId}/reconcile", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AdminPaymentsEndpoints.AdminPaymentReconcileResponse>();
        body!.IssueRaised.Should().BeTrue();

        await using var verifyScope = factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDb>();
        var issue = await verifyDb.PaymentReconciliationIssues
            .FirstOrDefaultAsync(i => i.PaymentCheckoutAttemptId == attemptId);

        issue.Should().NotBeNull();
        issue!.Category.Should().Be(PaymentReconciliationCategory.ProviderCapturedFulfillmentMissing);
        issue.ResolvedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task RetryFulfillment_GrantsEntitlementAndResolvesIssue()
    {
        var fakePayPal = new FakePayPalGateway();
        using var factory = CreateFactory(fakePayPal, new FakeStripeGateway());
        using var admin = factory.CreateClient().WithAdminOpsKey();

        var playerId = Guid.NewGuid();
        Guid attemptId;

        await SeedStoreItemAsync(factory, new StoreItem
        {
            Sku = "powerup:retry",
            Name = "Retry Powerup",
            Description = "test",
            ItemType = "powerup",
            GrantQuantity = 3,
            IsActive = true,
            SortOrder = 1
        });

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            var attempt = new PaymentCheckoutAttempt(playerId, "paypal", "powerup:retry", 2, 5.98m, "USD", "ORDER-RETRY-1");
            db.PaymentCheckoutAttempts.Add(attempt);
            await db.SaveChangesAsync();
            attemptId = attempt.Id;

            var issue = new PaymentReconciliationIssue(
                PaymentReconciliationCategory.ProviderCapturedFulfillmentMissing,
                "paypal", "ORDER-RETRY-1", attemptId, playerId, 5.98m, 5.98m,
                "Provider captured but local fulfillment missing.");
            db.PaymentReconciliationIssues.Add(issue);

            await db.SaveChangesAsync();
        }

        var response = await admin.PostAsync($"/admin/payments/{attemptId}/retry-fulfillment", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AdminPaymentsEndpoints.AdminPaymentRetryFulfillmentResponse>();
        body!.Status.Should().Be("Fulfilled");

        await using var verifyScope = factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDb>();

        var attemptAfter = await verifyDb.PaymentCheckoutAttempts.FirstAsync(a => a.Id == attemptId);
        attemptAfter.Status.Should().Be(PaymentCheckoutStatus.Captured);

        var issueAfter = await verifyDb.PaymentReconciliationIssues.FirstAsync(i => i.PaymentCheckoutAttemptId == attemptId);
        issueAfter.ResolvedAtUtc.Should().NotBeNull();

        var entitlement = await verifyDb.PlayerEntitlements
            .FirstOrDefaultAsync(e => e.PlayerId == playerId && e.Sku == "powerup:retry");
        entitlement.Should().NotBeNull();
        entitlement!.Quantity.Should().Be(6); // GrantQuantity(3) * attempt.Quantity(2)
    }

    [Fact]
    public async Task Refund_RevokesEntitlementAndMarksAttemptRefunded()
    {
        var fakePayPal = new FakePayPalGateway();
        using var factory = CreateFactory(fakePayPal, new FakeStripeGateway());
        using var admin = factory.CreateClient().WithAdminOpsKey();

        var playerId = Guid.NewGuid();
        Guid attemptId;

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();

            var tx = new PlayerTransaction(Guid.NewGuid(), "paypal-order-payment", receipt: "ORDER-REFUND-1");
            tx.AddActor(playerId, PlayerTransactionActorRole.Buyer);
            tx.AddItemChange("powerup:refund", 6, ItemOperation.Grant);
            tx.MarkApplied();
            db.PlayerTransactions.Add(tx);
            await db.SaveChangesAsync();

            var entitlementService = scope.ServiceProvider.GetRequiredService<IEntitlementService>();
            await entitlementService.GrantAsync(playerId, "powerup:refund", "powerup", 6, tx.Id);

            var attempt = new PaymentCheckoutAttempt(playerId, "paypal", "powerup:refund", 2, 5.98m, "USD", "ORDER-REFUND-1");
            db.PaymentCheckoutAttempts.Add(attempt);
            attempt.MarkCaptured(tx.Id, "CAPTURE-REFUND-1");

            await db.SaveChangesAsync();
            attemptId = attempt.Id;
        }

        fakePayPal.NextRefundResult = new PayPalRefundResult("REFUND-1", "COMPLETED");

        var response = await admin.PostAsJsonAsync(
            $"/admin/payments/{attemptId}/refund",
            new AdminPaymentsEndpoints.AdminPaymentRefundRequest("player requested", null));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AdminPaymentsEndpoints.AdminPaymentRefundResponse>();
        body!.IsFullRefund.Should().BeTrue();
        body.RefundId.Should().Be("REFUND-1");

        await using var verifyScope = factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDb>();

        var attemptAfter = await verifyDb.PaymentCheckoutAttempts.FirstAsync(a => a.Id == attemptId);
        attemptAfter.Status.Should().Be(PaymentCheckoutStatus.Refunded);

        var entitlement = await verifyDb.PlayerEntitlements
            .FirstOrDefaultAsync(e => e.PlayerId == playerId && e.Sku == "powerup:refund");
        entitlement.Should().NotBeNull();
        entitlement!.Quantity.Should().Be(0);

        var refundTx = await verifyDb.PlayerTransactions
            .Include(t => t.Actors)
            .FirstOrDefaultAsync(t => t.Kind == "paypal-refund" && t.Actors.Any(a => a.PlayerId == playerId));
        refundTx.Should().NotBeNull();
        refundTx!.CorrelatedEventId.Should().NotBeNull();
    }

    private WebApplicationFactory<Program> CreateFactory(IPayPalPaymentGateway payPalGateway, IStripePaymentGateway stripeGateway)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPayPalPaymentGateway>();
                services.AddSingleton(payPalGateway);
                services.RemoveAll<IStripePaymentGateway>();
                services.AddSingleton(stripeGateway);
            });
        });
    }

    private static async Task SeedStoreItemAsync(WebApplicationFactory<Program> factory, StoreItem item)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();
        var existing = await db.StoreItems.Where(i => i.Sku == item.Sku).ToListAsync();
        db.StoreItems.RemoveRange(existing);
        db.StoreItems.Add(item);
        await db.SaveChangesAsync();
    }

    private sealed class FakePayPalGateway : IPayPalPaymentGateway
    {
        public PayPalOrderStatusResult NextOrderStatusResult { get; set; } =
            new("ORDER-123", "COMPLETED", "CAPTURE-123", "USD", 2.99m);

        public PayPalRefundResult NextRefundResult { get; set; } = new("REFUND-123", "COMPLETED");

        public Task<bool> VerifyWebhookAsync(PayPalWebhookVerificationRequest request, CancellationToken cancellationToken)
            => Task.FromResult(true);

        public Task<PayPalCreateOrderResult> CreateOrderAsync(PayPalCreateOrderRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new PayPalCreateOrderResult("ORDER-123", "CREATED", "https://paypal.test/approve/ORDER-123"));

        public Task<PayPalCaptureOrderResult> CaptureOrderAsync(string orderId, CancellationToken cancellationToken)
            => Task.FromResult(new PayPalCaptureOrderResult(orderId, "COMPLETED", "CAPTURE-123", null, "USD", 2.99m));

        public Task<PayPalOrderStatusResult> GetOrderAsync(string orderId, CancellationToken cancellationToken)
            => Task.FromResult(NextOrderStatusResult);

        public Task<PayPalRefundResult> RefundCaptureAsync(string captureId, decimal? amount, string? currency, CancellationToken cancellationToken)
            => Task.FromResult(NextRefundResult);

        public Task<PayPalCreateSubscriptionResult> CreateSubscriptionAsync(PayPalCreateSubscriptionRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new PayPalCreateSubscriptionResult("I-SUB-123", "APPROVAL_PENDING", "https://paypal.test/approve/I-SUB-123"));

        public Task<PayPalSubscriptionDetails> GetSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken)
            => Task.FromResult(new PayPalSubscriptionDetails("I-SUB-123", "ACTIVE", "P-PLAN-123", null, null, null, null));

        public Task CancelSubscriptionAsync(string subscriptionId, string reason, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeStripeGateway : IStripePaymentGateway
    {
        public Task<StripeCheckoutSessionResult> CreateCheckoutSessionAsync(StripeCheckoutSessionCreateRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new StripeCheckoutSessionResult("cs_test", "https://checkout.stripe.test/cs_test"));

        public Task<StripeCheckoutSessionResult> CreateSubscriptionCheckoutSessionAsync(StripeSubscriptionCheckoutSessionCreateRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new StripeCheckoutSessionResult("cs_sub_test", "https://checkout.stripe.test/cs_sub_test"));

        public Task<StripeBillingPortalSessionResult> CreateBillingPortalSessionAsync(StripeBillingPortalSessionCreateRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new StripeBillingPortalSessionResult("bps_test", "https://billing.stripe.test/bps_test"));

        public StripeWebhookEvent ParseWebhook(string payload, string? signatureHeader)
            => new("evt_test", "checkout.session.completed", null, null);

        public Task<StripeSessionStatusResult> GetCheckoutSessionAsync(string sessionId, CancellationToken cancellationToken)
            => Task.FromResult(new StripeSessionStatusResult(sessionId, "paid", "pi_test", "usd", 299));

        public Task<StripeRefundResult> RefundPaymentIntentAsync(string paymentIntentId, long? amount, CancellationToken cancellationToken)
            => Task.FromResult(new StripeRefundResult("re_test", "succeeded"));
    }
}
