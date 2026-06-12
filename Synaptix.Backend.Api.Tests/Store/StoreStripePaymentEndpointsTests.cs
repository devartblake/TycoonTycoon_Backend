using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Synaptix.Backend.Api.Payments.Stripe;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Tests.Store;

public sealed class StoreStripePaymentEndpointsTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;

    public StoreStripePaymentEndpointsTests(TycoonApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateCheckoutSession_WithAuthenticatedOwner_ReturnsSessionUrl()
    {
        var fakeGateway = new FakeStripePaymentGateway();
        using var factory = CreateFactory(fakeGateway);
        using var client = factory.CreateClient();

        var signup = await SignupAsync(client, "stripe-session");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", signup.AccessToken);

        var playerId = Guid.Parse(signup.UserId);
        await SeedStoreItemAsync(factory, new StoreItem
        {
            Sku = "powerup:skip",
            Name = "Skip Powerup",
            Description = "Skip one question.",
            ItemType = "powerup",
            GrantQuantity = 1,
            MaxPerPlayer = 0,
            IsActive = true,
            SortOrder = 1
        });
        await StoreTestSupport.EnableStorePurchasesAsync(factory);

        var response = await client.PostAsJsonAsync(
            "/api/v1/store/payments/checkout/session",
            new CreateStripeCheckoutSessionRequest(playerId, "powerup:skip", 2));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<CreateStripeCheckoutSessionResponse>();
        body.Should().NotBeNull();
        body!.SessionId.Should().Be("cs_test_123");
        body.CheckoutUrl.Should().Be("https://checkout.stripe.test/session/cs_test_123");
        body.UnitAmount.Should().Be(299);
        body.TotalAmount.Should().Be(598);

        fakeGateway.LastCheckoutRequest.Should().NotBeNull();
        fakeGateway.LastCheckoutRequest!.PlayerId.Should().Be(playerId);
        fakeGateway.LastCheckoutRequest.Sku.Should().Be("powerup:skip");
        fakeGateway.LastCheckoutRequest.Quantity.Should().Be(2);
    }

    [Fact]
    public async Task CreateCheckoutSession_WithDifferentAuthenticatedPlayer_ReturnsForbidden()
    {
        var fakeGateway = new FakeStripePaymentGateway();
        using var factory = CreateFactory(fakeGateway);
        using var client = factory.CreateClient();

        var signup = await SignupAsync(client, "stripe-forbidden");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", signup.AccessToken);

        await SeedStoreItemAsync(factory, new StoreItem
        {
            Sku = "powerup:skip",
            Name = "Skip Powerup",
            Description = "Skip one question.",
            ItemType = "powerup",
            GrantQuantity = 1,
            MaxPerPlayer = 0,
            IsActive = true,
            SortOrder = 1
        });
        await StoreTestSupport.EnableStorePurchasesAsync(factory);

        var response = await client.PostAsJsonAsync(
            "/api/v1/store/payments/checkout/session",
            new CreateStripeCheckoutSessionRequest(Guid.NewGuid(), "powerup:skip", 1));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await response.HasErrorCodeAsync("FORBIDDEN");
    }

    [Fact]
    public async Task StripeWebhook_CompletedCheckout_AppliesTransactionOnce()
    {
        var fakeGateway = new FakeStripePaymentGateway
        {
            NextWebhookEvent = new StripeWebhookEvent(
                "evt_test_checkout",
                "checkout.session.completed",
                new StripeWebhookCheckoutCompletedData(
                    "cs_completed_123",
                    "payment",
                    "paid",
                    "usd",
                    299,
                    "player@example.com",
                    null,
                    null,
                    null,
                    new Dictionary<string, string>()))
        };

        using var factory = CreateFactory(fakeGateway);
        using var client = factory.CreateClient();

        var signup = await SignupAsync(client, "stripe-webhook");
        var playerId = Guid.Parse(signup.UserId);

        fakeGateway.NextWebhookEvent = fakeGateway.NextWebhookEvent with
        {
            CheckoutCompleted = fakeGateway.NextWebhookEvent.CheckoutCompleted! with
            {
                Metadata = new Dictionary<string, string>
                {
                    ["player_id"] = playerId.ToString(),
                    ["sku"] = "powerup:skip",
                    ["quantity"] = "2"
                }
            }
        };

        await SeedStoreItemAsync(factory, new StoreItem
        {
            Sku = "powerup:skip",
            Name = "Skip Powerup",
            Description = "Skip one question.",
            ItemType = "powerup",
            GrantQuantity = 3,
            MaxPerPlayer = 0,
            IsActive = true,
            SortOrder = 1
        });

        using var content = new StringContent(
            JsonSerializer.Serialize(new { id = "evt_test_checkout", type = "checkout.session.completed" }),
            Encoding.UTF8,
            "application/json");

        var first = await client.PostAsync("/api/v1/store/payments/webhook", content);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        using var duplicateContent = new StringContent(
            JsonSerializer.Serialize(new { id = "evt_test_checkout", type = "checkout.session.completed" }),
            Encoding.UTF8,
            "application/json");

        var second = await client.PostAsync("/api/v1/store/payments/webhook", duplicateContent);
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();
        var transactions = db.PlayerTransactions
            .Include(t => t.Actors)
            .Include(t => t.ItemChanges)
            .Where(t => t.Kind == "stripe-checkout-payment"
                        && t.Actors.Any(a => a.PlayerId == playerId))
            .ToList();

        transactions.Should().HaveCount(1);
        transactions[0].Actors.Should().ContainSingle(a => a.PlayerId == playerId);
        transactions[0].ItemChanges.Should().ContainSingle(i => i.ItemType == "powerup:skip" && i.Quantity == 6);
    }

    private WebApplicationFactory<Program> CreateFactory(FakeStripePaymentGateway fakeGateway)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Stripe:Enabled"] = "true",
                    ["Stripe:SecretKey"] = "sk_test_123",
                    ["Stripe:PublishableKey"] = "pk_test_123",
                    ["Stripe:WebhookSecret"] = "whsec_test_123",
                    ["Stripe:SuccessUrl"] = "https://localhost:3000/store/success?session_id={CHECKOUT_SESSION_ID}",
                    ["Stripe:CancelUrl"] = "https://localhost:3000/store/cancel",
                    ["Stripe:PortalReturnUrl"] = "https://localhost:3000/store/subscription",
                    ["Stripe:DefaultCurrency"] = "usd",
                    ["Stripe:Catalog:0:Sku"] = "powerup:skip",
                    ["Stripe:Catalog:0:UnitAmount"] = "299",
                    ["Stripe:Catalog:0:Currency"] = "usd",
                    ["Stripe:Catalog:0:ProductName"] = "Skip Powerup",
                    ["Stripe:SubscriptionPlans:0:Tier"] = "premium",
                    ["Stripe:SubscriptionPlans:0:BillingPeriod"] = "monthly",
                    ["Stripe:SubscriptionPlans:0:PriceId"] = "price_premium_monthly",
                    ["Stripe:SubscriptionPlans:1:Tier"] = "elite",
                    ["Stripe:SubscriptionPlans:1:BillingPeriod"] = "monthly",
                    ["Stripe:SubscriptionPlans:1:PriceId"] = "price_elite_monthly"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IStripePaymentGateway>();
                services.AddSingleton<IStripePaymentGateway>(fakeGateway);
            });
        });
    }

    private static async Task<SignupResponse> SignupAsync(HttpClient client, string prefix)
    {
        var signupResp = await client.PostAsJsonAsync(
            "/api/v1/auth/signup",
            new SignupRequest(
                Email: $"{prefix}-{Guid.NewGuid():N}@example.com",
                Password: "Passw0rd!",
                DeviceId: "ios-sim",
                Username: $"{prefix}_{Guid.NewGuid():N}"));

        signupResp.EnsureSuccessStatusCode();
        var signup = await signupResp.Content.ReadFromJsonAsync<SignupResponse>();
        signup.Should().NotBeNull();
        return signup!;
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

    private sealed class FakeStripePaymentGateway : IStripePaymentGateway
    {
        public StripeCheckoutSessionCreateRequest? LastCheckoutRequest { get; private set; }

        public StripeSubscriptionCheckoutSessionCreateRequest? LastSubscriptionCheckoutRequest { get; private set; }

        public StripeBillingPortalSessionCreateRequest? LastPortalRequest { get; private set; }

        public StripeWebhookEvent NextWebhookEvent { get; set; } =
            new("evt_default", "checkout.session.completed", null, null);

        public Task<StripeCheckoutSessionResult> CreateCheckoutSessionAsync(
            StripeCheckoutSessionCreateRequest request,
            CancellationToken cancellationToken)
        {
            LastCheckoutRequest = request;
            return Task.FromResult(new StripeCheckoutSessionResult(
                "cs_test_123",
                "https://checkout.stripe.test/session/cs_test_123"));
        }

        public Task<StripeCheckoutSessionResult> CreateSubscriptionCheckoutSessionAsync(
            StripeSubscriptionCheckoutSessionCreateRequest request,
            CancellationToken cancellationToken)
        {
            LastSubscriptionCheckoutRequest = request;
            return Task.FromResult(new StripeCheckoutSessionResult(
                "cs_sub_test_123",
                "https://checkout.stripe.test/session/cs_sub_test_123"));
        }

        public Task<StripeBillingPortalSessionResult> CreateBillingPortalSessionAsync(
            StripeBillingPortalSessionCreateRequest request,
            CancellationToken cancellationToken)
        {
            LastPortalRequest = request;
            return Task.FromResult(new StripeBillingPortalSessionResult(
                "bps_test_123",
                "https://billing.stripe.test/session/bps_test_123"));
        }

        public StripeWebhookEvent ParseWebhook(string payload, string? signatureHeader)
        {
            return NextWebhookEvent;
        }
    }
}
